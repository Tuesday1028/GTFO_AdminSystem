using Hikaria.DevConsoleLite;
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

        public override bool InlineSettingsIntoParentMenu => true;

        [FeatureConfig]
        public static DisableStaminaSettings Settings { get; private set; }

        public class DisableStaminaSettings
        {
            [FSDisplayName("禁用心率")]
            public bool DisableStamina { get; set; }

            [FSDisplayName("禁用附近敌人对自身移动速度的影响")]
            public bool DisableNearByEnemyMoveSpeedMultiplier { get; set; }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("DisableStamina", "禁用心率", "禁用心率", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.DisableStamina;
                }
                Settings.DisableStamina = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 禁用心率");
            }, () =>
            {
                DevConsole.LogVariable("禁用心率", Settings.DisableStamina);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("DisableNearByEnemyMoveSpeedMultiplier", "禁用附近敌人对自身移动速度的影响", "禁用附近敌人对自身移动速度的影响", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.DisableNearByEnemyMoveSpeedMultiplier;
                }
                Settings.DisableNearByEnemyMoveSpeedMultiplier = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 禁用附近敌人对自身移动速度的影响");
            }, () =>
            {
                DevConsole.LogVariable("禁用附近敌人对自身移动速度的影响", Settings.DisableNearByEnemyMoveSpeedMultiplier);
            }));
        }

        [ArchivePatch(typeof(PlayerStamina), nameof(PlayerStamina.LateUpdate))]
        private class PlayerStamina_LateUpdate_Patch
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
