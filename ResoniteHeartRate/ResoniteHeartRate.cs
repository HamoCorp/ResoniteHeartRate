using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ResoniteHeartRate {

    public class ResoniteHeartRate : ResoniteMod {

        public override string Name => "Resonite HeartRate";
        public override string Author => "HamoCorp";
        public override string Version => "1.0.4";

        public override string Link => "https://github.com/HamoCorp/ResoniteHeartRate";

        private static ModConfiguration Config;

        public const int UPDATE_RATE = 500;

        public override void OnEngineInit() {
            
            Config = GetConfiguration();
            Config.Save(true);
            Harmony harmony = new Harmony("com.HamoCorp.ResoniteHeartRate");
            harmony.PatchAll();

            _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
            _token = _HR.HeartRateInit(_pulsoidKeyPrev, Config.GetValue(_service));
            
        }

        public static string nameGenerator(int length) {

            string name = " ";
            for (int i = 0; i < length; i++) {
                name += " ";
            }
            return name;
        }

        private static void ResetButtonPressed() {
            // Immediately reset engine values on main thread
            updateValueStreamValues(0);
            _HR = null;
            _HypeRate = null;

            // Background reconnect logic
            Task.Run(() =>
            {
                Thread.Sleep(3000);

                if (Config.GetValue(_service) == HeartRateClient.HRService.HypeRate) {
                    _HypeRateKeyPrev = Config.GetValue(_HypeRateKey);
                    _HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
                }
                else if (Config.GetValue(_service) == HeartRateClient.HRService.Pulsoid) {
                    Thread.Sleep(10000);
                }

                _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
                _HR = new HeartRateClient();
                _token = _HR.HeartRateInit(_pulsoidKeyPrev, Config.GetValue(_service));

                // Reset flag on main thread
                _mainThreadActions.Enqueue(() => { _resetPending = false; });
            });
        }

        private static void HRUpdate() {
            while (!_stopHRThread) {
                Thread.Sleep(UPDATE_RATE);

                int hearRate = 0;

                if (Config.GetValue(_service) == HeartRateClient.HRService.HypeRate) {
                    // Ensure WebSocket exists
                    if (_HypeRate == null)
                        _HypeRate = new HypeRateWebSocket(Config.GetValue(_HypeRateKey));

                    // Poll heart rate
                    hearRate = _HypeRate.getHypeRateHeartRate();

                    // Send keep-alive directly (no need to enqueue)
                    _HypeRate.SendKeepAlive();

                    // Reconnect if dead
                    if (!_HypeRate.getHypeRateAlive()) {
                        _HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
                    }
                }
                else if (_HR != null) {
                    hearRate = _HR.ReadCurrentHR(_pulsoidKeyPrev, Config.GetValue(_service));
                }

                // Schedule engine updates on main thread
                int finalHR = hearRate;
                _mainThreadActions.Enqueue(() =>
                {
                    updateValueStreamValues(finalHR);
                    if (_HR != null) _HR.TimerCount++;
                });
            }
        }

        private static void updateValueStreamValues(int HeartRate) {

            foreach (ValueStream<int> vs in _valueStreamList.ToList()) {
                if (vs.World != null) {
                    vs.Value = HeartRate;
                }
                else {
                    _valueStreamList.Remove(vs);
                }
            }
            _valueStreamList.Last().Value = HeartRate;            
        }

        private static void setStreamPerams(ValueStream<int> stream) {

            stream.SetInterpolation();
            stream.SetUpdatePeriod(0, 0);
            stream.Encoding = ValueEncoding.Full;
            stream.FullFrameBits = 10;
            stream.FullFrameMin = 0;
            stream.FullFrameMax = 999;
        }
        private static Slot addHeartRateDataSlot(Slot userRoot, bool userSpace) {

            _valueStreamList.Add(userRoot.LocalUser.GetStreamOrAdd<ValueStream<int>>("HeartRateMod", setStreamPerams));
            _valueStreamList.Last().Value = 0;

            Slot HeartRateSlot = userRoot.AddSlot(Config.GetValue(_slotName), true);

            string DName;
            if (userSpace) {
                DName = "World/com.HamoCorp.ResoniteHeartRate";

                _userSpaceResetboolButton = HeartRateSlot.AttachComponent<DynamicValueVariable<bool>>(true, null);
                _userSpaceResetboolButton.VariableName.Value = "World/ResoniteHeartRate.Reset";
                _userSpaceResetboolButton.Value.Value = false;
            }
            else {
                DName = "User/" + Config.GetValue(_dynVarName);
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

                if (__instance.Slot.ActiveUser == null || !__instance.Slot.ActiveUser.IsLocalUser || !__instance.Slot.Name.StartsWith("User") || !Config.GetValue(_enabled)) {
                    return;
                }
                bool userSpace = __instance.Slot.World.IsUserspace();
                addHeartRateDataSlot(__instance.Slot, userSpace);

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


                if (Config.GetValue(_pulsoidKey) != _pulsoidKeyPrev) {
                    _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
                    _HR.HeartRateInit(_pulsoidKeyPrev, Config.GetValue(_service));

                }

                if (Config.GetValue(_HypeRateKey) != _HypeRateKeyPrev) {
                    _HypeRateKeyPrev = Config.GetValue(_HypeRateKey);
                    if (Config.GetValue(_internalTesting) == true) {
                        _HypeRateKeyPrev = "internal-testing";
                    }
                    _HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
                }

                if (Config.GetValue(_service) != HeartRateClient.HRService.HypeRate) {
                    _HypeRate = null;
                }

                _HRLoop.Start();


            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Userspace), "OnCommonUpdate")]
            public static void UspaceUpdate(Userspace __instance) {

                // Process queued actions
                while (_mainThreadActions.TryDequeue(out Action action)) {
                    try { action(); }
                    catch (Exception ex) { ResoniteMod.Error($"Queued main-thread action failed: {ex}"); }
                }

                // Trigger reset if button pressed
                if (!_resetPending && _userSpaceResetboolButton?.Value?.Value == true) {
                    _resetPending = true;

                    _mainThreadActions.Enqueue(() => ResetButtonPressed());
                }

            }
        }

        private static bool _resetPending = false;
        private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        private static string _token = "";
        private static string _pulsoidKeyPrev = "";
        private static string _HypeRateKeyPrev = "";

        private static HeartRateClient _HR = new HeartRateClient();
        private static HypeRateWebSocket _HypeRate;
        private static Thread _HRLoop;
        private static volatile bool _stopHRThread;
        private static int _threadEndDeltay = 200;

        private static List<ValueStream<int>> _valueStreamList = new List<ValueStream<int>>();

        private static DynamicValueVariable<bool> _userSpaceResetboolButton;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> _enabled = new ModConfigurationKey<bool>("enabled", "Enabled (Require Respawn for changing some settings)", () => true);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d29 = new ModConfigurationKey<dummy>(nameGenerator(29), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d28 = new ModConfigurationKey<dummy>(nameGenerator(28), "█▀█ █▀▀ █▀ █▀█ █▄░█ █ ▀█▀ █▀▀   █░█ █▀▀ ▄▀█ █▀█ ▀█▀ █▀█ ▄▀█ ▀█▀ █▀▀");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d27 = new ModConfigurationKey<dummy>(nameGenerator(27), "█▀▄ ██▄ ▄█ █▄█ █░▀█ █ ░█░ ██▄   █▀█ ██▄ █▀█ █▀▄ ░█░ █▀▄ █▀█ ░█░ ██▄");





        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d30 = new ModConfigurationKey<dummy>(nameGenerator(30), "");

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<HeartRateClient.HRService> _service = new ModConfigurationKey<HeartRateClient.HRService>("service", "Heart Rate Service, Pulsoid or HypeRate", () => HeartRateClient.HRService.Pulsoid);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d31 = new ModConfigurationKey<dummy>(nameGenerator(31), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> _pulsoidKey = new ModConfigurationKey<string>("Pulsoid Key", "Pulsoid Key", () => "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d0 = new ModConfigurationKey<dummy>(nameGenerator(0), "Get your pulsoid Key from https://pulsoid.net");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d1 = new ModConfigurationKey<dummy>(nameGenerator(1), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> _HypeRateKey = new ModConfigurationKey<string>("HypeRate Key", "HypeRate session id", () => "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d32 = new ModConfigurationKey<dummy>(nameGenerator(32), "Get your HypeRate session id from https://hyperate.io");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d26 = new ModConfigurationKey<dummy>(nameGenerator(26), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> _internalTesting = new ModConfigurationKey<bool>("HypeRate testing", "HypeRate Debug Internal Testing, For testing HypeRate without a monitor", () => false);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d33 = new ModConfigurationKey<dummy>(nameGenerator(33), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d2 = new ModConfigurationKey<dummy>(nameGenerator(2), "----------------------------------------------------------------------------------------------");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d3 = new ModConfigurationKey<dummy>(nameGenerator(3), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> _dynVarName = new ModConfigurationKey<string>("DynVarName", "Dynamic Variable Name   User/", () => "com.HamoCorp.ResoniteHeartRate");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d4 = new ModConfigurationKey<dummy>(nameGenerator(4), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> _slotName = new ModConfigurationKey<string>("SlotName", "Slot Name under User Root", () => "HeartRate Mod");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d9 = new ModConfigurationKey<dummy>(nameGenerator(9), "");

//        [AutoRegisterConfigKey]
//        public static readonly ModConfigurationKey<bool> _facetsEnable = new ModConfigurationKey<bool>("enable facets", "Enable variables for UserSpace HeartRate Facets", () => true);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d25 = new ModConfigurationKey<dummy>(nameGenerator(25), "----------------------------------------------------------------------------------------------");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d40 = new ModConfigurationKey<dummy>(nameGenerator(40), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d10 = new ModConfigurationKey<dummy>(nameGenerator(10), "_____$$$$_________$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d11 = new ModConfigurationKey<dummy>(nameGenerator(11), "___$$$$$$$$_____$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d12 = new ModConfigurationKey<dummy>(nameGenerator(12), "_$$$$$$$$$$$$_$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d13 = new ModConfigurationKey<dummy>(nameGenerator(13), "$$$$$$$$$$$$$$$$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d14 = new ModConfigurationKey<dummy>(nameGenerator(14), "$$$$$$$$$$$$$$$$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d15 = new ModConfigurationKey<dummy>(nameGenerator(15), "_$$$$$$$$$$$$$$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d16 = new ModConfigurationKey<dummy>(nameGenerator(16), "__$$$$$$$$$$$$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d17 = new ModConfigurationKey<dummy>(nameGenerator(17), "____$$$$$$$$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d18 = new ModConfigurationKey<dummy>(nameGenerator(18), "_______$$$$$$$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d19 = new ModConfigurationKey<dummy>(nameGenerator(19), "__________$$$$$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d20 = new ModConfigurationKey<dummy>(nameGenerator(20), "____________$$$");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d21 = new ModConfigurationKey<dummy>(nameGenerator(21), "_____________$");

    }


}
