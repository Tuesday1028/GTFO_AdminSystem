using Hikaria.DevConsoleLite;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class NoCameraShake : Feature
    {
        public override string Name => "无视角抖动";

        public override string Description => "禁用受到伤害时视角抖动";

        public override string Group => EntryPoint.Groups.Player;

        [FeatureConfig]
        public static NoCameraShakeSettings Settings { get; set; }

        public class NoCameraShakeSettings
        {
            [FSDisplayName("无视角抖动")]
            public bool EnableNoCamerShake { get; set; }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("NoCamerShake", "无视角抖动", "无视角抖动", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableNoCamerShake;
                }
                Settings.EnableNoCamerShake = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 无视角抖动");
            }, () =>
            {
                DevConsole.LogVariable("无视角抖动", Settings.EnableNoCamerShake);
            }));
        }

        [ArchivePatch(typeof(FPSCamera), nameof(FPSCamera.AddHitReact))]
        private class FPSCamera_AddHitReact_Patch
        {
            static bool Prefix()
            {
                return !Settings.EnableNoCamerShake;
            }
        }

    }
}
