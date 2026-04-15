using System.Collections.Concurrent;

using Elements.Core;

using FrooxEngine;

using HarmonyLib;

using ResoniteModLoader;

namespace ResoniteHeartRate;

public class ResoniteHeartRate : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.1.0"; //Changing the version here updates it in all locations needed
	public override string Name => "Resonite HeartRate";
	public override string Author => "HamoCorp";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/HamoCorp/ResoniteHeartRate";

	private static ModConfiguration Config;

	public const int UPDATE_RATE = 500;

	public override void OnEngineInit() {
		Config = GetConfiguration();
		Config.Save(true);
		Harmony harmony = new("com.HamoCorp.ResoniteHeartRate");
		harmony.PatchAll();

		_pulsoidKeyPrev = _pulsoidKey.Value;
		_token = _HR.HeartRateInit(_pulsoidKeyPrev, _service.Value);
	}

	public static string NameGenerator(int length) {
		string name = " ";
		for (int i = 0; i < length; i++) {
			name += " ";
		}
		return name;
	}

	private static void ResetButtonPressed() {
		// Immediately reset engine values on main thread
		UpdateValueStreamValues(0);
		_HR = null;
		_HypeRate = null;

		// Background reconnect logic
		Task.Run(() => {
			Thread.Sleep(3000);

			if (_service.Value == HeartRateClient.HRService.HypeRate) {
				_HypeRateKeyPrev = _HypeRateKey.Value;
				_HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
			} else if (_service.Value == HeartRateClient.HRService.Pulsoid) {
				Thread.Sleep(10000);
			}

			_pulsoidKeyPrev = _pulsoidKey.Value;
			_HR = new HeartRateClient();
			_token = _HR.HeartRateInit(_pulsoidKeyPrev, _service.Value);

			// Reset flag on main thread
			_mainThreadActions.Enqueue(() => { _resetPending = false; });
		});
	}

	private static void HRUpdate() {
		while (!_stopHRThread) {
			Thread.Sleep(UPDATE_RATE);

			int hearRate = 0;

			if (_service.Value == HeartRateClient.HRService.HypeRate) {
				// Ensure WebSocket exists
				_HypeRate ??= new HypeRateWebSocket(_HypeRateKey.Value);

				// Poll heart rate
				hearRate = _HypeRate.GetHypeRateHeartRate();

				// Send keep-alive directly (no need to enqueue)
				_HypeRate.SendKeepAlive();

				// Reconnect if dead
				if (!_HypeRate.GetHypeRateAlive()) {
					_HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
				}
			} else if (_HR != null) {
				hearRate = _HR.ReadCurrentHR(_pulsoidKeyPrev, _service.Value);
			}

			// Schedule engine updates on main thread
			int finalHR = hearRate;
			_mainThreadActions.Enqueue(() => {
				UpdateValueStreamValues(finalHR);
				if (_HR != null) _HR.TimerCount++;
			});
		}
	}

	private static void UpdateValueStreamValues(int HeartRate) {
		foreach (ValueStream<int> vs in _valueStreamList.ToList()) {
			if (vs.World != null) {
				vs.Value = HeartRate;
			} else {
				_valueStreamList.Remove(vs);
			}
		}
		_valueStreamList.Last().Value = HeartRate;
	}

	private static void SetStreamPerams(ValueStream<int> stream) {
		stream.SetInterpolation();
		stream.SetUpdatePeriod(_valueStreamUpdatePeriod.Value, 0);
		stream.Encoding = ValueEncoding.Full;
		stream.FullFrameBits = 10;
		stream.FullFrameMin = 0;
		stream.FullFrameMax = 999;
	}
	private static Slot AddHeartRateDataSlot(Slot userRoot, bool userSpace) {
		_valueStreamList.Add(userRoot.LocalUser.GetStreamOrAdd<ValueStream<int>>("HeartRateMod", SetStreamPerams));
		_valueStreamList.Last().Value = 0;

		Slot HeartRateSlot = userRoot.AddSlot(_slotName.Value, true);

		string DName;
		if (userSpace) {
			DName = "World/com.HamoCorp.ResoniteHeartRate";

			_userSpaceResetboolButton = HeartRateSlot.AttachComponent<DynamicValueVariable<bool>>(true, null);
			_userSpaceResetboolButton.VariableName.Value = "World/ResoniteHeartRate.Reset";
			_userSpaceResetboolButton.Value.Value = false;
		} else {
			DName = "User/" + _dynVarName.Value;
		}

		DynamicValueVariable<int> dynamicValueHR = HeartRateSlot.AttachComponent<DynamicValueVariable<int>>(true, null);
		dynamicValueHR.VariableName.Value = DName;
		ValueDriver<int> valueDriver = HeartRateSlot.AttachComponent<ValueDriver<int>>(true, null);

		valueDriver.ValueSource.Target = _valueStreamList.Last();
		valueDriver.DriveTarget.Target = dynamicValueHR.Value;

		return HeartRateSlot;
	}

	[HarmonyPatch]
	class HeartRatePatch {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UserRoot), "OnStart")]
		public static void EditLocalUserRoot(UserRoot __instance) {
			if (__instance.Slot.ActiveUser == null || !__instance.Slot.ActiveUser.IsLocalUser || !__instance.Slot.Name.StartsWith("User") || !_enabled.Value) {
				return;
			}
			bool userSpace = __instance.Slot.World.IsUserspace();
			AddHeartRateDataSlot(__instance.Slot, userSpace);

			if (!userSpace) {
				// In your UserRoot OnStart patch, start HR thread safely
				if (_HRLoop != null && _HRLoop.IsAlive) {
					_stopHRThread = true;
					_HRLoop.Join(_threadEndDeltay);
				}

				_stopHRThread = false;
				_HRLoop = new Thread(HRUpdate) { IsBackground = true };
				_HRLoop.Start();
			}

			if (_pulsoidKey.Value != _pulsoidKeyPrev) {
				_pulsoidKeyPrev = _pulsoidKey.Value;
				_HR.HeartRateInit(_pulsoidKeyPrev, _service.Value);
			}

			if (_HypeRateKey.Value != _HypeRateKeyPrev) {
				_HypeRateKeyPrev = _HypeRateKey.Value;
				if (_internalTesting.Value == true) {
					_HypeRateKeyPrev = "internal-testing";
				}
				_HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
			}

			if (_service.Value != HeartRateClient.HRService.HypeRate) {
				_HypeRate = null;
			}

			_HRLoop.Start();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Userspace), "OnCommonUpdate")]
		public static void UserspaceUpdate() {
			// Process queued actions
			while (_mainThreadActions.TryDequeue(out Action action)) {
				try { action(); } catch (Exception ex) { ResoniteMod.Error($"Queued main-thread action failed: {ex}"); }
			}

			// Trigger reset if button pressed
			if (!_resetPending && _userSpaceResetboolButton?.Value?.Value == true) {
				_resetPending = true;

				_mainThreadActions.Enqueue(() => ResetButtonPressed());
			}
		}
	}

	private static bool _resetPending = false;
	private static readonly ConcurrentQueue<Action> _mainThreadActions = new();

	private static string _token = "";
	private static string _pulsoidKeyPrev = "";
	private static string _HypeRateKeyPrev = "";

	private static HeartRateClient _HR = new();
	private static HypeRateWebSocket _HypeRate;
	private static Thread _HRLoop;
	private static volatile bool _stopHRThread;
	private static int _threadEndDeltay = 200;

	private static List<ValueStream<int>> _valueStreamList = [];

	private static DynamicValueVariable<bool> _userSpaceResetboolButton;

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> _enabled = new("enabled", "Enabled (Require Respawn for changing some settings)", () => true);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d29 = new(NameGenerator(29), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d28 = new(NameGenerator(28), "█▀█ █▀▀ █▀ █▀█ █▄░█ █ ▀█▀ █▀▀   █░█ █▀▀ ▄▀█ █▀█ ▀█▀ █▀█ ▄▀█ ▀█▀ █▀▀");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d27 = new(NameGenerator(27), "█▀▄ ██▄ ▄█ █▄█ █░▀█ █ ░█░ ██▄   █▀█ ██▄ █▀█ █▀▄ ░█░ █▀▄ █▀█ ░█░ ██▄");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d30 = new(NameGenerator(30), "");

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<HeartRateClient.HRService> _service = new("service", "Heart Rate Service, Pulsoid or HypeRate", () => HeartRateClient.HRService.Pulsoid);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d31 = new(NameGenerator(31), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> _pulsoidKey = new("Pulsoid Key", "Pulsoid Key", () => "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d0 = new(NameGenerator(0), "Get your pulsoid Key from https://pulsoid.net");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d1 = new(NameGenerator(1), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> _HypeRateKey = new("HypeRate Key", "HypeRate session id", () => "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d32 = new(NameGenerator(32), "Get your HypeRate session id from https://hyperate.io");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d26 = new(NameGenerator(26), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<bool> _internalTesting = new("HypeRate testing", "HypeRate Debug Internal Testing, For testing HypeRate without a monitor", () => false);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d33 = new(NameGenerator(33), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d2 = new(NameGenerator(2), "----------------------------------------------------------------------------------------------");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d3 = new(NameGenerator(3), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> _dynVarName = new("DynVarName", "Dynamic Variable Name   User/", () => "com.HamoCorp.ResoniteHeartRate");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d4 = new(NameGenerator(4), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<string> _slotName = new("SlotName", "Slot Name under User Root", () => "HeartRate Mod");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<uint> _valueStreamUpdatePeriod = new("ValueStream Update Period", "How frequently the ValueStreams sends updates, smaller values = more frequent", () => 30);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d9 = new(NameGenerator(9), "");

	//        [AutoRegisterConfigKey]
	//        public static readonly ModConfigurationKey<bool> _facetsEnable = new ModConfigurationKey<bool>("enable facets", "Enable variables for UserSpace HeartRate Facets", () => true);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d25 = new(NameGenerator(25), "----------------------------------------------------------------------------------------------");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d40 = new(NameGenerator(40), "");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d10 = new(NameGenerator(10), "_____$$$$_________$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d11 = new(NameGenerator(11), "___$$$$$$$$_____$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d12 = new(NameGenerator(12), "_$$$$$$$$$$$$_$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d13 = new(NameGenerator(13), "$$$$$$$$$$$$$$$$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d14 = new(NameGenerator(14), "$$$$$$$$$$$$$$$$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d15 = new(NameGenerator(15), "_$$$$$$$$$$$$$$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d16 = new(NameGenerator(16), "__$$$$$$$$$$$$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d17 = new(NameGenerator(17), "____$$$$$$$$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d18 = new(NameGenerator(18), "_______$$$$$$$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d19 = new(NameGenerator(19), "__________$$$$$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d20 = new(NameGenerator(20), "____________$$$");

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<dummy> _d21 = new(NameGenerator(21), "_____________$");

}
