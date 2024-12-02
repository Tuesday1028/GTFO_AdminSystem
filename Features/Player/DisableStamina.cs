using Hikaria.AdminSystem.Utilities;
using Hikaria.QC;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    internal class DisableStamina : Feature
    {
        public override string Name => "禁用心率系统";

        public override string Description => "启用以禁用心率系统";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        [FeatureConfig]
        public static DisableStaminaSettings Settings { get; private set; }

        public class DisableStaminaSettings
        {
            [FSDisplayName("禁用心率")]
            [Command("DisableStamina", MonoTargetType.Registry)]
            public bool DisableStamina { get; set; }

            [FSDisplayName("禁用附近敌人对自身移动速度的影响")]
            [Command("NearbyEnemyMoveSpeedMultiplier", MonoTargetType.Registry)]
            public bool DisableNearByEnemyMoveSpeedMultiplier { get; set; }
        }

        public override void Init()
        {
            QuantumRegistry.RegisterObject(Settings);
        }

        [ArchivePatch(typeof(PlayerStamina), nameof(PlayerStamina.LateUpdate))]
        private class PlayerStamina__LateUpdate__Patch
        {
            private static void Postfix(PlayerStamina __instance)
            {
                if (__instance.m_owner.Owner.IsLocal && Settings.DisableStamina)
                {
                    __instance.ResetStamina();
                }
            }
        }

        [ArchivePatch(typeof(PlayerEnemyCollision), nameof(PlayerEnemyCollision.FindNearbyEnemiesMovementReduction))]
        private class PlayerEnemyCollision_FindNearbyEnemiesMovementReduction_Patch
        {
            private static void Postfix(PlayerEnemyCollision __instance, ref float __result)
            {
                if (!__instance.m_owner.Owner.IsLocal)
                {
                    return;
                }
                if (Settings.DisableNearByEnemyMoveSpeedMultiplier)
                {
                    __result = 1f;
                }
            }
        }
    }
}
