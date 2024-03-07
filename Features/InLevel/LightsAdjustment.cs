using Hikaria.DevConsoleLite;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;

namespace Hikaria.AdminSystem.Features.InLevel
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class LightsAdjustment : Feature
    {
        public override string Name => "灯光调节";

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        [FeatureConfig]
        public static LightSettings Settings { get; set; }

        public class LightSettings
        {
            [FSDisplayName("禁用灯光损坏")]
            public bool DisableLightsBreak { get; set; }

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