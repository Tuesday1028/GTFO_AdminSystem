using Enemies;
using Globals;
using Hikaria.DevConsoleLite;
using SNetwork;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Enemy
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    internal class EnemyBehavior : Feature
    {
        public override string Name => "敌人行为";

        public override string Group => EntryPoint.Groups.Enemy;

        [FeatureConfig]
        public static EnemyBehaviorSettings Settings { get; set; }

        public class EnemyBehaviorSettings
        {
            [FSDisplayName("禁用敌人检测")]
            [FSDescription("启用后敌人不会攻击和惊醒(仅房主时有效)")]
            public bool DisableEnemyPlayerDetection
            {
                get
                {
                    return !Global.EnemyPlayerDetectionEnabled;
                }
                set
                {
                    Global.EnemyPlayerDetectionEnabled = !value;
                    foreach (var enemy in EnemyLookup.EnemiesInLevel)
                    {
                        SetEnemyHibernate(enemy, Global.EnemyPlayerDetectionEnabled);
                    }
                }
            }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("DisableEnemyPlayerDetection", "禁用敌人检测", "启用后敌人不会攻击和惊醒(仅房主时有效)", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.DisableEnemyPlayerDetection;
                }

                Settings.DisableEnemyPlayerDetection = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 禁用敌人检测");
            }, () =>
            {
                DevConsole.LogVariable("禁用敌人检测", Settings.DisableEnemyPlayerDetection);
            }));
        }

        [ArchivePatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
        private class EnemySync_OnSpawn_Patch
        {
            private static void Postfix(EnemySync __instance, pEnemySpawnData spawnData)
            {
                SetEnemyHibernate(__instance.m_agent, Global.EnemyPlayerDetectionEnabled);
            }
        }


        private static void SetEnemyHibernate(EnemyAgent enemy, bool enable)
        {
            if (SNet.IsMaster)
            {
                if (enable)
                {
                    enemy.Locomotion.Hibernate.MasterStartDetection();
                }
                else
                {
                    enemy.Locomotion.Hibernate.MasterEndDetection();
                }
            }
            else
            {
                detectData.detectOn = enable;
                enemy.Locomotion.Hibernate.m_detectPacket.Send(detectData, SNet_ChannelType.GameNonCritical);
            }
        }

        private static pES_DetectData detectData;
    }
}
