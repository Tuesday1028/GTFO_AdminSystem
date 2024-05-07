using Hikaria.DevConsoleLite;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class ShotInAir : Feature
    {
        public override string Name => "空中开枪";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        public override bool InlineSettingsIntoParentMenu => true;

        [FeatureConfig]
        public static ShotInAirSettings Settings { get; set; }

        public class ShotInAirSettings
        {
            [FSDisplayName("空中开枪")]
            public bool EnableShotInAir { get; set; }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("ShotInAir", "空中开枪", "空中开枪", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableShotInAir;
                }

                Settings.EnableShotInAir = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 空中开枪");
            }, () =>
            {
                DevConsole.LogVariable("空中开枪", Settings.EnableShotInAir);
            }));
        }

        [ArchivePatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.IsInAir), null, ArchivePatch.PatchMethodType.Getter)]
        private class PlayerLocomotion__IsInAir__Patch
        {
            private static bool Prefix(PlayerLocomotion __instance, ref bool __result)
            {
                if (!__instance.m_owner.Owner.IsLocal || !Settings.EnableShotInAir)
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }
    }
}
