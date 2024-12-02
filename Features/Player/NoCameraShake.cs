using Hikaria.AdminSystem.Utilities;
using Hikaria.QC;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
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
            [Command("NoCameraShake", MonoTargetType.Registry)]
            public bool EnableNoCamerShake { get; set; }
        }

        public override void Init()
        {
            QuantumRegistry.RegisterObject(Settings);
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
