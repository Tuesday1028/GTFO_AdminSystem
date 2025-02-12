using Hikaria.QC;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class ShotInAir : Feature
    {
        public override string Name => "空中开枪";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;

        [FeatureConfig]
        public static ShotInAirSettings Settings { get; set; }

        public class ShotInAirSettings
        {
            [FSDisplayName("空中开枪")]
            public bool EnableShotInAir { get => _enableShotInAir; set => _enableShotInAir = value; }
        }

        [Command("ShotInAir")]
        private static bool _enableShotInAir;

        [ArchivePatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.IsInAir), null, ArchivePatch.PatchMethodType.Getter)]
        private class PlayerLocomotion__IsInAir__Patch
        {
            private static bool Prefix(PlayerLocomotion __instance, ref bool __result)
            {
                if (!__instance.m_owner.Owner.IsLocal || !_enableShotInAir)
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }
    }
}
