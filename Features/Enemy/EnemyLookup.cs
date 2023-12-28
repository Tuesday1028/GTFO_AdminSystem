using Enemies;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Enemy
{
    [HideInModSettings]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class EnemyLookup : Feature
    {
        public override string Name => "敌人查询";

        public override string Group => EntryPoint.Groups.Enemy;


        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
        private class EnemyAgent_Setup_Patch
        {
            private static void Prefix(EnemyAgent __instance)
            {
                if (__instance.IsSetup)
                {
                    return;
                }
                if (EnemiesInLevel.Add(__instance))
                {
                    __instance.add_OnDeadCallback((Action)(() => EnemiesInLevel.Remove(__instance)));
                }
            }
        }

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.OnDestroy))]
        private class EnemyAgent_OnDestroy_Patch
        {
            static void Prefix(EnemyAgent __instance)
            {
                EnemiesInLevel.Remove(__instance);
            }
        }

        public static HashSet<EnemyAgent> EnemiesInLevel { get; private set; } = new();
    }
}
