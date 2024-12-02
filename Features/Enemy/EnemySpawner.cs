using Agents;
using AIGraph;
using Enemies;
using GameData;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Suggestion.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using SNetwork;
using System.Collections;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
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

        [ArchivePatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
        private class EnemySync__OnSpawn__Patch
        {
            private static void Postfix(EnemySync __instance, pEnemySpawnData spawnData)
            {
                if (spawnData.mode != AgentMode.Scout)
                    return;

                __instance.m_agent.IsScout = true;
                SetupScoutPath(__instance.m_agent, spawnData);
            }
        }

        [Command("SpawnEnemy", "生成敌人")]
        private static void SpawnEnemy([EnemyDataBlockID] uint id, int count = 1, AgentMode mode = AgentMode.Hibernate)
        {
            var block = EnemyDataBlock.GetBlock(id);
            if (block == null)
            {
                ConsoleLogs.LogToConsole($"未找到ID为 {id} 的 EnemyDataBlock");
                return;
            }
            UnityMainThreadDispatcher.Enqueue(SpawnEnemyCoroutine(id, count, mode));
        }

        [Command("SpawnEnemyByName", "生成敌人")]
        private static void SpawnEnemy([EnemyDataBlockName] string name, int count = 1, AgentMode mode = AgentMode.Hibernate)
        {
            var block = EnemyDataBlock.GetBlock(name);
            if (block == null)
            {
                ConsoleLogs.LogToConsole($"未找到名称为 {name} 的 EnemyDataBlock");
                return;
            } 
            UnityMainThreadDispatcher.Enqueue(SpawnEnemyCoroutine(block.persistentID, count, mode));
        }

        private static IEnumerator SpawnEnemyCoroutine(uint id, int count, AgentMode mode)
        {
            WaitForSecondsRealtime yielder = new(0.05f);
            int num = count;
            Vector3 pos = AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos;
            if (AIG_CourseNode.TryGetCourseNode(AdminUtils.LocalPlayerAgent.m_dimensionIndex, pos, 1f, out var node))
            {
                do
                {
                    EnemyAgent.SpawnEnemy(id, pos, node, mode);
                    count--;
                    yield return yielder;
                }
                while (count > 0);
                ConsoleLogs.LogToConsole($"<color=orange>在视线处生成 {num} 只 {TranslateHelper.EnemyName(id)}, 模式: {mode}</color>");
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
    }
}
