using Hikaria.QC;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    public class NoCameraShake : Feature
    {
        public override string Name => "无视角抖动";

        public override string Description => "禁用受到伤害时视角抖动";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        [FeatureConfig]
        public static NoCameraShakeSettings Settings { get; set; }

        public class NoCameraShakeSettings
        {
            [FSDisplayName("无视角抖动")]
            public bool EnableNoCamerShake { get => _enableNoCamerShake; set => _enableNoCamerShake = value; }
        }

        [Command("NoCameraShake")]
        public static bool _enableNoCamerShake;

        [ArchivePatch(typeof(FPSCamera), nameof(FPSCamera.AddHitReact))]
        private class FPSCamera_AddHitReact_Patch
        {
            static bool Prefix()
            {
                return !_enableNoCamerShake;
            }
        }
    }
}
