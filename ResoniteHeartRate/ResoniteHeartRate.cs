using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.IO;
using System.Threading;

namespace ResoniteHeartRate {

    public class ResoniteHeartRate : ResoniteMod {
        public override string Name => "Resonite HeartRate";
        public override string Author => "HamoCorp";
        public override string Version => "1.0.0";

        private static ModConfiguration Config;

        public override void OnEngineInit() {
            
            Config = GetConfiguration();
            Config.Save(true);
            Harmony harmony = new Harmony("com.HamoCorp.ResoniteHeartRate");
            harmony.PatchAll();

            Msg("ResoniteHeartRate Mod is running");

            _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
            _token = _HR.GenerateToken(_pulsoidKeyPrev);
            
        }

        public static string nameGenerator(int length) {

            string name = " ";
            for (int i = 0; i < length; i++) {
                name += " ";
            }
            return name;
        }

        private static void HRUpdate() {

            while (true) {
                Thread.Sleep(Config.GetValue(_updateRate));

                _valueStream.Value = _HR.ReadCurrentHR(_pulsoidKeyPrev);
            }
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

            _HeartRate = DateTime.Now.Second;

            _valueStream = _LocalUserRootSlot.LocalUser.GetStreamOrAdd<ValueStream<int>>("HeartRateMod", setStreamPerams);
            
            _valueStream.Value = DateTime.Now.Second;
            
            _HeartRateSlot = _LocalUserRootSlot.AddSlot(Config.GetValue(_slotName), true);
            

            _dynamicValueHR = _HeartRateSlot.AttachComponent<DynamicValueVariable<int>>(true, null);
            _dynamicValueHR.VariableName.Value = "User/" + Config.GetValue(_dynVarName);
            _valueDriver = _HeartRateSlot.AttachComponent<ValueDriver<int>>(true, null);

            _valueDriver.ValueSource.Target = _valueStream;
            _valueDriver.DriveTarget.Target = _dynamicValueHR.Value;

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

                                if (Config.GetValue(_pulsoidKey) != _pulsoidKeyPrev) {
                                    _pulsoidKeyPrev = Config.GetValue(_pulsoidKey);
                                    _HR.GenerateToken(_pulsoidKeyPrev);
                                }
                                _HRLoop.Start();
                            }
                        }
                    }

                    
                } 
            }
        }

        private static int _HeartRate = 0;
        private static string _token = "";
        private static string _pulsoidKeyPrev = "";

        private static HeartRateClient _HR = new HeartRateClient();
        private static Thread _HRLoop = new Thread(HRUpdate);

        private static Slot _LocalUserRootSlot;
        private static Slot _HeartRateSlot;

        private static DynamicValueVariable<int> _dynamicValueHR;
        private static ValueDriver<int> _valueDriver;
        private static ValueStream<int> _valueStream;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> _enabled = new ModConfigurationKey<bool>("enabled", "Enabled (Require Respawn)", () => true);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d30 = new ModConfigurationKey<dummy>(nameGenerator(30), "");

 //       [AutoRegisterConfigKey]
 //       private static readonly ModConfigurationKey<int> _service = new ModConfigurationKey<int>("service", "Heart Rate Service", () => 0);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> _pulsoidKey = new ModConfigurationKey<string>("Pulsoid Key", "Pulsoid Key", () => "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d0 = new ModConfigurationKey<dummy>(nameGenerator(0), "Get your pulsoid Key from https://pulsoid.net");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d1 = new ModConfigurationKey<dummy>(nameGenerator(1), "");

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<int> _updateRate = new ModConfigurationKey<int>("UpdateRate", "Update Rate (Milliseconds)", () => 500);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d2 = new ModConfigurationKey<dummy>(nameGenerator(2), "500ms for HR monitors of 1/s updates");

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

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<dummy> _d31 = new ModConfigurationKey<dummy>(nameGenerator(31), "");

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
