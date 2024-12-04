using Hikaria.QC;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

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

            [FSDisplayName("无击退效果")]
            public bool EnableNoKnockback { get => _enableNoKnockback; set => _enableNoKnockback = value; }
        }

        [Command("NoCameraShake")]
        public static bool _enableNoCamerShake;
        [Command("NoKnockback")]
        public static bool _enableNoKnockback;

        [ArchivePatch(typeof(FPSCamera), nameof(FPSCamera.AddHitReact))]
        private class FPSCamera_AddHitReact_Patch
        {
            static bool Prefix()
            {
                return !_enableNoCamerShake;
            }
        }


        [ArchivePatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.AddExternalPushForce))]
        internal static class PlayerLocomotion__AddExternalPushForce__Patch
        {
            private static void Postfix(PlayerLocomotion __instance)
            {
                if (!_enableNoKnockback || !__instance.LocallyOwned)
                    return;

                __instance.m_externalPushForce = Vector3.zero;
                __instance.m_hasExternalPushForce = true;
            }
        }
    }
}
