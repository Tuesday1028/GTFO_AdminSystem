using Agents;
using AIGraph;
using BepInEx.Unity.IL2CPP.Utils;
using Enemies;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System.Collections;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Enemy
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class EnemySpawner : Feature
    {
        public override string Name => "刷怪";

        public override FeatureGroup Group => EntryPoint.Groups.Enemy;

        [FeatureConfig]
        public static EnemySpawnerSettings Settings { get; set; }

        public class EnemySpawnerSettings
        {
            [FSDisplayName("刷怪")]
            [FSDescription("刷怪位置在视线处")]
            public FButton SpawnEnemy { get; set; } = new FButton("刷怪", "刷怪");

            [FSDisplayName("敌人ID")]
            public uint SpawnEnemyID { get; set; } = 0;

            [FSDisplayName("敌人模式")]
            public AgentMode SpawnMode { get; set; } = AgentMode.Hibernate;

            [FSDisplayName("敌人数量")]
            public int Count { get; set; } = 1;

            [FSHeader("敌人数据查询")]
            [FSDisplayName("敌人信息表")]
            [FSReadOnly]
            public List<EnemyDataEntry> EnemyDataLookup { get; set; } = new();

            public enum AgentMode
            {
                Off,
                Agressive,
                Patrolling,
                Scout,
                Hibernate
            }
        }

        public class EnemyDataEntry
        {
            [FSSeparator]
            [FSReadOnly]
            [FSDisplayName("名称")]
            public string Name { get; set; }

            [FSReadOnly]
            public uint ID { get; set; }

            [FSDisplayName("原始名称")]
            [FSReadOnly]
            public string FullName { get; set; }
        }

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<EnemySpawnHandler>();
            DevConsole.AddCommand(Command.Create<uint, int, int>("SpawnEnemy", "生成敌人", "生成敌人", Parameter.Create("ID", "敌人的ID"), Parameter.Create("Count", "敌人的数量"), Parameter.Create("Mode", "AI的模式, 0: Off, 1: Agressive, 2: Patrolling, 3: Scout, 4: Hibernation"), SpawnEnemy));
        }

        public override void OnGameDataInitialized()
        {
            foreach (var item in EnemyDataManager.EnemyDataBlockLookup)
            {
                EnemyDataEntry entry = new()
                {
                    ID = item.Key,
                    Name = TranslateManager.EnemyName(item.Key),
                    FullName = item.Value.name
                };
                Settings.EnemyDataLookup.Add(entry);
                //Settings.EnemyDataLookup = Settings.EnemyDataLookup.OrderBy(i => i.ID).ToList();
            }
        }

        public override void OnButtonPressed(ButtonSetting setting)
        {
            if (setting.ButtonID == "刷怪")
            {
                AgentMode mode = (AgentMode)(int)Settings.SpawnMode;
                SpawnEnemy(Settings.SpawnEnemyID, Settings.Count, mode);
            }
        }

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent__Setup__Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                if (__instance.gameObject.GetComponent<EnemySpawnHandler>() == null)
                {
                    __instance.gameObject.AddComponent<EnemySpawnHandler>();
                }
            }
        }

        [ArchivePatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
        private class EnemySync_OnSpawn_Patch
        {
            private static void Postfix(EnemySync __instance, pEnemySpawnData spawnData)
            {
                if (spawnData.mode != AgentMode.Scout)
                {
                    return;
                }
                __instance.m_agent.IsScout = true;
                SetupScoutPath(__instance.m_agent, spawnData);
            }
        }

        private static void SpawnEnemy(uint id, int count, int mode)
        {
            EnemySpawnHandler.Instance.StartCoroutine(SpawnEnemyCoroutine(id, count, (AgentMode)mode));
        }

        private static void SpawnEnemy(uint id, int count, AgentMode mode)
        {
            EnemySpawnHandler.Instance.StartCoroutine(SpawnEnemyCoroutine(id, count, mode));
        }


        private static IEnumerator SpawnEnemyCoroutine(uint id, int count, AgentMode mode)
        {
            WaitForSecondsRealtime yielder = new(0.05f);
            int num = count;
            Vector3 pos = AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos;
            if (AIG_CourseNode.TryGetCourseNode(AdminUtils.LocalPlayerAgent.m_dimensionIndex, pos, 1f, out AIG_CourseNode node))
            {
                do
                {
                    EnemyAgent.SpawnEnemy(id, pos, node, mode);
                    count--;
                    yield return yielder;
                }
                while (count > 0);
                DevConsole.Log($"<color=orange>在视线处生成 {num} 只 {TranslateManager.EnemyName(id)}, 模式: {mode}</color>");
            }
        }

        private static void SetupScoutPath(EnemyAgent enemy, pEnemySpawnData spawnData)
        {
            List<Vector3> wayPoints;
            pEnemyPathData data = new();
            data.currentIndex = 0;
            data.pathSteps = 4;
            spawnData.courseNode.TryGet(out var node);
            wayPoints = node.GetRandomPoints(4);
            data.p0 = wayPoints[0];
            data.p1 = wayPoints[1];
            data.p2 = wayPoints[2];
            data.p3 = wayPoints[3];
            if (SNet.IsMaster)
            {
                enemy.Sync.IncomingPath(data);
            }
            else
            {
                enemy.Sync.m_aiPathPacket.Send(data, SNet_ChannelType.GameNonCritical, SNet.Master);
            }
        }

        private class EnemySpawnHandler : MonoBehaviour
        {
            public static EnemySpawnHandler Instance { get; private set; }

            private void Awake()
            {
                Instance = this;
            }
        }
    }
}
