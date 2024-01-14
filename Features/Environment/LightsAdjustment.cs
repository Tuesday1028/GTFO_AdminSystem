using CullingSystem;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Environment
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class LightsAdjustment : Feature
    {
        public override string Name => "灯光调节";

        public override FeatureGroup Group => EntryPoint.Groups.Environment;

        [FeatureConfig]
        public static LightSettings Settings { get; set; }

        //public static Dictionary<LG_Light, Tuple<float, float>> Lights { get; set; } = new();

        public class LightSettings
        {
            [FSDisplayName("禁用灯光损坏")]
            public bool DisableLightsBreak { get; set; }
            /*

            [FSDisplayName("设置灯光强度倍率")]
            public float LightsIntensityMulti
            {
                get
                {
                    return _lightIntensityMulti;
                }
                set
                {
                    SetLightIntensityMulti(value);
                    _lightIntensityMulti = value;
                }
            }

            private float _lightIntensityMulti = 1f;

            [FSDisplayName("设置灯光范围倍率")]
            public float LightsRangeMulti
            {
                get
                {
                    return _lightRangeMulti;
                }
                set
                {
                    SetLightRangeMulti(value);
                    _lightRangeMulti = value;
                }
            }

            private float _lightRangeMulti = 1f;


            [FSDisplayName("设置所有本地灯光")]
            public bool SetAllLights
            {
                get
                {
                    return _allLights;
                }
                set
                {
                    SetAllLightsEnabledForce(value);
                    _allLights = value;
                }
            }

            private bool _allLights = true;
            */

            [FSDisplayName("设置所有同步灯光状态")]
            public bool AllSyncLightsEnabled { get; set; } = true;

            [FSDisplayName("操作灯光")]
            public FButton LightInteraction { get; set; } = new FButton("操作", "操作灯光");
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("LightsSynced", "设置同步灯光", "设置同步灯光", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.AllSyncLightsEnabled;
                }
                Settings.AllSyncLightsEnabled = enable.Value;
                SetLightsEnabledSync(Settings.AllSyncLightsEnabled);
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 同步灯光");
            }, () =>
            {
                DevConsole.LogVariable("同步灯光状态", Settings.AllSyncLightsEnabled);
            }));
        }
        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.ExpeditionSuccess || current == eGameStateName.ExpeditionAbort || current == eGameStateName.AfterLevel)
            {
                //Lights.Clear();
                Settings.DisableLightsBreak = false;
                Settings.AllSyncLightsEnabled = true;
            }
        }

        public override void OnButtonPressed(ButtonSetting setting)
        {
            if (setting.ButtonID == "操作灯光")
            {
                SetLightsEnabledSync(Settings.AllSyncLightsEnabled);
            }
        }

        /*
        [ArchivePatch(typeof(LG_Light), nameof(LG_Light.Start))]
        private class LG_Light__Start__Patch
        {
            private static void Postfix(LG_Light __instance)
            {
                C_Light c_Light = __instance.GetC_Light();
                if (c_Light == null)
                {
                    Lights.TryAdd(__instance, Tuple.Create(__instance.m_intensity, -1f));
                }
                Lights.TryAdd(__instance, Tuple.Create(__instance.m_intensity, c_Light.m_unityLight.range));
            }
        }

        [ArchivePatch(typeof(LG_Light), nameof(LG_Light.SetEnabled), new Type[] { typeof(bool) })]
        private class LG_Light__SetEnabled__Patch
        {
            private static void Prefix(ref bool enabled)
            {
                if (Settings.DisableLightsBreak)
                {
                    enabled = true;
                }
            }
        }

        [ArchivePatch(typeof(LG_SpotLight), nameof(LG_SpotLight.SetEnabled), new Type[] { typeof(bool) })]
        private class LG_SpotLight__SetEnabled__Patch
        {
            private static void Prefix(ref bool enabled)
            {
                if (Settings.DisableLightsBreak)
                {
                    enabled = true;
                }
            }
        }

        [ArchivePatch(typeof(LG_PointLight), nameof(LG_PointLight.SetEnabled), new Type[] { typeof(bool) })]
        private class LG_PointLight__SetEnabled__Patch
        {
            private static void Prefix(ref bool enabled)
            {
                if (Settings.DisableLightsBreak)
                {
                    enabled = true;
                }
            }
        }

        [ArchivePatch(typeof(LG_SpotLightAmbient), nameof(LG_SpotLightAmbient.SetEnabled), new Type[] { typeof(bool) })]
        private class LG_SpotLightAmbient__SetEnabled__Patch
        {
            private static void Prefix(ref bool enabled)
            {
                if (Settings.DisableLightsBreak)
                {
                    enabled = true;
                }
            }
        }

        [ArchivePatch(typeof(LG_LightEmitterMesh), nameof(LG_LightEmitterMesh.SetMeshColor))]
        private class LG_LightEmitterMesh__SetMeshColor__Patch
        {
            private static void Prefix(LG_LightEmitterMesh __instance, ref Color color)
            {
                if (Settings.DisableLightsBreak)
                {
                    color = __instance.m_colorCurrent;
                }
            }
        }

        [ArchivePatch(typeof(BlinkingLight), nameof(BlinkingLight.SetColor))]
        private class BlinkingLight__SetColor__Patch
        {
            private static void Prefix(BlinkingLight __instance, ref Color col)
            {
                if (Settings.DisableLightsBreak)
                {
                    col = __instance.m_emissionOn;
                }
            }
        }

        private static void SetLightIntensityMulti(float multi)
        {
            foreach (var item in Lights)
            {
                try
                {
                    item.Key.ChangeIntensity(item.EffectValue.Item1 * multi);
                }
                catch
                {

                }
            }
        }

        private static void SetLightRangeMulti(float multi)
        {
            foreach (var item in Lights)
            {
                try
                {
                    C_Light c_Light = item.Key.GetC_Light();
                    if (c_Light != null)
                    {
                        if (item.EffectValue.Item2 == -1f)
                        {
                            Lights[item.Key] = new(item.EffectValue.Item1, c_Light.m_unityLight.range);
                        }
                        c_Light.m_unityLight.range = item.EffectValue.Item2 * multi;
                    }
                }
                catch
                {

                }
            }
        }

        private static void SetAllLightsEnabledForce(bool enable)
        {
            foreach (var item in Lights.Keys)
            {
                try
                {
                    item.SetEnabled(enable);
                }
                catch
                {

                }
            }
        }
        */

        private static void SetLightsEnabledSync(bool enable)
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
            {
                return;
            }
            EnvironmentStateManager.AttemptSetExpeditionLightMode(enable);
        }
    }
}