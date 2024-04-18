using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ResoniteHeartRate {

    public class ResoniteHeartRate : ResoniteMod {

        public override string Name => "Resonite HeartRate";
        public override string Author => "HamoCorp";
        public override string Version => "1.0.1";

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

        private static void HRUpdate() {
            int hearRate = 0;
            while (true) {
                Thread.Sleep(UPDATE_RATE);
                if (_HR.TimerCount >= 18) {
                    // Reset
                    if (_userSpaceResetboolButton.Value.Value == true) {

                        updateValueStreamValues(0);

                        _HR = null;
                        _HypeRate = null;

                        Thread.Sleep(3000);
                        if (Config.GetValue(_service) == HeartRateClient.HRService.HypeRate) {

                            _HypeRateKeyPrev = Config.GetValue(_HypeRateKey);
                            _HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
                        }
                        else if(Config.GetValue(_service) == HeartRateClient.HRService.Pulsoid) {
                            Thread.Sleep(10000);
                        }

                        _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
                        _HR = new HeartRateClient();
                        _token = _HR.HeartRateInit(_pulsoidKeyPrev, Config.GetValue(_service));

                        _HR.TimerCount = 0;
                        continue;
                    }
                    else {

                        if (Config.GetValue(_service) == HeartRateClient.HRService.HypeRate) {
                            _HypeRate.SendKeepAlive();
                            if (_HypeRate.getHypeRateAlive() == false) {
                                _HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
                            }
                        }
                        _HR.TimerCount = 0;
                    }                    
                }

                if (Config.GetValue(_service) == HeartRateClient.HRService.HypeRate) {
                    if (_HypeRate == null) {
                        _HypeRate = new HypeRateWebSocket(Config.GetValue(_HypeRateKey));
                    }

                    hearRate = _HypeRate.getHypeRateHeartRate();
                }
                else {
                    hearRate = _HR.ReadCurrentHR(_pulsoidKeyPrev, Config.GetValue(_service));
                }

                updateValueStreamValues(hearRate);

                _HR.TimerCount++;
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

            //if (_usingFacets) {
            _UserSpaceHRValueStream.Value = HeartRate;
            
        }

        private static void setStreamPerams(ValueStream<int> stream) {

            stream.SetInterpolation();
            stream.SetUpdatePeriod(0, 0);
            stream.Encoding = ValueEncoding.Full;
            stream.FullFrameBits = 10;
            stream.FullFrameMin = 0;
            stream.FullFrameMax = 999;
        }
        private static void addHeartRateDataSlot() {

            _valueStreamList.Add(_LocalUserRootSlot.LocalUser.GetStreamOrAdd<ValueStream<int>>("HeartRateMod", setStreamPerams));
            _valueStreamList.Last().Value = 0;

            _HeartRateSlot = _LocalUserRootSlot.AddSlot(Config.GetValue(_slotName), true);

            DynamicValueVariable<int> dynamicValueHR = _HeartRateSlot.AttachComponent<DynamicValueVariable<int>>(true, null);
            dynamicValueHR.VariableName.Value = "User/" + Config.GetValue(_dynVarName);
            ValueDriver<int> valueDriver = _HeartRateSlot.AttachComponent<ValueDriver<int>>(true, null);
            
            valueDriver.ValueSource.Target = _valueStreamList.Last();
            valueDriver.DriveTarget.Target = dynamicValueHR.Value;

        }
        private static void addUserSpaceHeartRateDataSlot() {

            _UserSpaceHRValueStream = _LocalUserRootSlot.LocalUser.GetStreamOrAdd<ValueStream<int>>("HeartRateModLocal", setStreamPerams);
            _UserSpaceHRValueStream.Value = 0;
            
            _UserSpaceHeartRateSlot = _LocalUserRootSlot.LocalUserSpace.AddSlot(Config.GetValue(_slotName), false);

            DynamicValueVariable<int> UserSpacedynValueHR = _UserSpaceHeartRateSlot.AttachComponent<DynamicValueVariable<int>>(true, null);
            UserSpacedynValueHR.VariableName.Value = "World/com.HamoCorp.ResoniteHeartRate";
            ValueDriver<int> UserSpaceHRValueDriver = _UserSpaceHeartRateSlot.AttachComponent<ValueDriver<int>>(true, null);
            
            UserSpaceHRValueDriver.ValueSource.Target = _UserSpaceHRValueStream;
            UserSpaceHRValueDriver.DriveTarget.Target = UserSpacedynValueHR.Value;

            _userSpaceResetboolButton = _UserSpaceHeartRateSlot.AttachComponent<DynamicValueVariable<bool>>(true, null);
            _userSpaceResetboolButton.VariableName.Value = "World/ResoniteHeartRate.Reset";
            _userSpaceResetboolButton.Value.Value = false;

        }
       
        [HarmonyPatch]
        class HeartRatePatch {

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UserRoot), "OnStart")]
            public static void EditLocalUserRoot(UserRoot __instance) {

                if (__instance.Slot != null) {
                    
                    if (__instance.Slot.ActiveUser != null && __instance.Slot.ActiveUser.IsLocalUser) {

                        if (__instance.Slot.Name.StartsWith("User")) {
                            _LocalUserRootSlot = __instance.Slot;
                            if (Config.GetValue(_enabled)) {

                                addHeartRateDataSlot();
                                //_usingFacets = Config.GetValue(_facetsEnable);
                                if (_UserSpaceHeartRateSlot == null ) {//&& _usingFacets
                                addUserSpaceHeartRateDataSlot();
                                }
                                if (_HRLoop.IsAlive) {
                                    _HRLoop.Abort();
                                    _HRLoop = new Thread(HRUpdate);
                                }
                                if (Config.GetValue(_pulsoidKey) != _pulsoidKeyPrev) {
                                    _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
                                    _HR.HeartRateInit(_pulsoidKeyPrev, Config.GetValue(_service));
                                    
                                }

                                if (Config.GetValue(_HypeRateKey) != _HypeRateKeyPrev) {
                                    _HypeRateKeyPrev = Config.GetValue(_HypeRateKey);
                                    if(Config.GetValue(_internalTesting) == true) {
                                        _HypeRateKeyPrev = "internal-testing";
                                    }
                                    _HypeRate = new HypeRateWebSocket(_HypeRateKeyPrev);
                                }

                                if(Config.GetValue(_service) != HeartRateClient.HRService.HypeRate) {
                                    _HypeRate = null;
                                }
                                _HRLoop.Start();
                            }
                        }
                    }

                    
                }
            }
        }

        //private static bool _usingFacets = true;
        private static string _token = "";
        private static string _pulsoidKeyPrev = "";
        private static string _HypeRateKeyPrev = "";

        private static HeartRateClient _HR = new HeartRateClient();
        private static HypeRateWebSocket _HypeRate;
        private static Thread _HRLoop = new Thread(HRUpdate);

        private static Slot _LocalUserRootSlot;
        private static Slot _HeartRateSlot;
        private static List<ValueStream<int>> _valueStreamList = new List<ValueStream<int>>();

        private static Slot _UserSpaceHeartRateSlot;
        private static ValueStream<int> _UserSpaceHRValueStream;
        private static DynamicValueVariable<bool> _userSpaceResetboolButton;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> _enabled = new ModConfigurationKey<bool>("enabled", "Enabled (Require Respawn for changing any settings)", () => true);

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
        public static readonly ModConfigurationKey<string> _HypeRateKey = new ModConfigurationKey<string>("HypeRate Key", "HypeRate Key", () => "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d32 = new ModConfigurationKey<dummy>(nameGenerator(32), "Get your HypeRate Key from https://hyperate.io");

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
