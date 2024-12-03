using Agents;
using AIGraph;
using Enemies;
using Globals;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Suggestion.Suggestors.Attributes;
using Hikaria.AdminSystem.Suggestions.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.Core;
using Hikaria.QC;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc
{
    [HideInModSettings]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class MiscCommandsHolder : Feature
    {
        public override string Name => "杂项指令";

        public override string Description => "杂项指令";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;


        [Command("WantToSay")]
        private static void WantToSay(int playerID, uint eventID, uint inDialogID, uint startDialogID, uint subtitleId)
        {
            PlayerVoiceManager.WantToSayInternal(playerID - 1, eventID, inDialogID, startDialogID, subtitleId);
        }

        [Command("EnemyDetection")]
        private static bool DisableEnemyPlayerDetection
        {
            get
            {
                return !Global.EnemyPlayerDetectionEnabled;
            }
            set
            {
                Global.EnemyPlayerDetectionEnabled = !value;
                ConsoleLogs.LogToConsole($"已{(Global.EnemyPlayerDetectionEnabled ? "启用" : "禁用")} 禁用敌人检测");
            }
        }

        [Command("LightningStrike", "释放闪电")]
        private static void LightningStrike(int r, int g, int b)
        {
            Color color = new(r, g, b);
            Vector3 dir = AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir;
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
        }

        [Command("FireGlue", "喷射结沫")]
        private static void FireGlue(int glueExpand)
        {
            ProjectileManager.WantToFireGlue(AdminUtils.LocalPlayerAgent, AdminUtils.LocalPlayerAgent.FPSCamera.Position + AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir * 0.2f, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir * 35f, glueExpand, true);
        }

        [Command("PortableFogTurbine", "切换便携驱雾器")]
        private static void TogglePortableFogRepeller()
        {
            FogRepeller_Sphere fogRepeller_Sphere = AdminUtils.LocalPlayerAgent.gameObject.GetComponent<FogRepeller_Sphere>();
            if (fogRepeller_Sphere == null)
            {
                fogRepeller_Sphere = AdminUtils.LocalPlayerAgent.gameObject.AddComponent<FogRepeller_Sphere>();
                fogRepeller_Sphere.InfiniteDuration = true;
                fogRepeller_Sphere.Range = 100f;
            }
            if (!fogRepeller_Sphere.m_repellerEnabled)
            {
                fogRepeller_Sphere.StartRepelling();
            }
            else
            {
                fogRepeller_Sphere.StopRepelling();
            }
            ConsoleLogs.LogToConsole($"已{(false ? "启用" : "禁用")}便携驱雾器");
        }

        [Command("FireTargeting", "发射追踪粒子")]
        private static void FireTargeting(int type, [PlayerSlotIndex] int slot, int count = 1)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out PlayerAgent player))
            {
                ConsoleLogs.LogToConsole($"不存在slot为 {slot} 的玩家", LogLevel.Error);
                return;
            }
            while (count > 0)
            {
                ProjectileManager.WantToFireTargeting((ProjectileType)type, player, AdminUtils.LocalPlayerAgent.EyePosition + AdminUtils.LocalPlayerAgent.Forward * 0.25f, AdminUtils.LocalPlayerAgent.Forward, count, 100);
                count--;
            }
        }

        [Command("FuckMaster", "强制夺取房主")]
        private static void FuckMaster()
        {
            SNet_Player master = SNet.Master;
            SNet.MasterManagement.TryStartOnBadConnectionWithMaster();
            pMigrationReport pMigrationReport = new()
            {
                type = MigrationReportType.MigartionIsDone,
                hasNewMaster = true
            };
            pMigrationReport.NewMaster.SetPlayer(SNet.LocalPlayer);
            SNet.MasterManagement.m_migrationReportPacket.Send(pMigrationReport, SNet_ChannelType.SessionOrderCritical);
            SNet.MasterManagement.OnMigrationReport(pMigrationReport);
            SNet.SessionHub.KickPlayer(master, SNet_PlayerEventReason.Kick_ByVote);
            SNet.MasterManagement.EndOnBadConnectionWithMaster();
            if (CurrentGameState == (int)eGameStateName.ExpeditionFail || CurrentGameState == (int)eGameStateName.ExpeditionSuccess)
                return;
            if (SNet.MasterManagement.TryFindBestCaptureBuffer(out var bestBufferSummary))
                SNet.Sync.StartRecallWithAllSyncedPlayers(bestBufferSummary.bufferType, false);
            else
                SNet.Sync.StartRecallWithAllSyncedPlayers(eBufferType.RestartLevel, false);

            ConsoleLogs.LogToConsole("已强行夺取房主权限");
        }

        [Command("RevealMap", "地图全显")]
        private static void RevealMap()
        {
            GameObject coneObj = new();
            coneObj.transform.localScale = new Vector3(128f, 32f, 128f);
            foreach (AIG_CourseNode node in AIG_CourseNode.s_allNodes)
            {
                try
                {
                    LG_AreaAIGraphSource source = node.m_area.GraphSource;
                    coneObj.transform.position = source.transform.position;
                    MapDetails.AddVisiblityCone(coneObj.transform, MapDetails.VisibilityLayer.LocalPlayer);
                }
                catch
                {
                }
            }

            foreach (var zone in Builder.CurrentFloor.allZones)
            {
                foreach (var area in zone.m_areas)
                {
                    var comps = area.GetComponentsInChildren<LG_MapLookatRevealerBase>();
                    foreach (var comp in comps)
                    {
                        MapDataManager.WantToSetGUIObjVisible(comp.MapGUIObjID, comp.CurrentStatus);
                    }
                }
            }

            ConsoleLogs.LogToConsole("已设置地图全显");
        }

        [Command("OperateOrUnlock", "操作或解锁")]
        private static void OperateOrUnlock()
        {
            PlayerAgent player = AdminUtils.LocalPlayerAgent;
            if (player != null && player.FPSCamera.CameraRayObject != null)
            {
                iLG_Door_Core iLG_Door_Core = player.FPSCamera.CameraRayObject.GetComponentInParent<iLG_Door_Core>();
                if (iLG_Door_Core == null)
                {
                    iLG_Door_Core = player.FPSCamera.CameraRayObject.GetComponentInChildren<iLG_Door_Core>();
                }
                if (iLG_Door_Core != null)
                {
                    LG_WeakDoor door = iLG_Door_Core.TryCast<LG_WeakDoor>();
                    if (door != null)
                    {
                        if (door.WeakLocks != null && door.WeakLocks.Count != 0)
                        {
                            pWeakLockInteraction unlock = new()
                            {
                                open = true,
                                type = eWeakLockInteractionType.Melt //使用DoDamage会惊怪，直接全部用熔锁器解锁
                            };
                            foreach (var weaklock in door.WeakLocks)
                            {
                                weaklock.AttemptInteract(unlock);
                            }
                        }

                    }
                    iLG_Door_Core.AttemptOpenCloseInteraction(true);
                }
                else
                {
                    iLG_ResourceContainer_Core iLG_ResourceContainer_Core = player.FPSCamera.CameraRayObject.GetComponentInParent<iLG_ResourceContainer_Core>();
                    if (iLG_ResourceContainer_Core == null)
                    {
                        iLG_ResourceContainer_Core = player.FPSCamera.CameraRayObject.GetComponentInChildren<iLG_ResourceContainer_Core>();
                    }
                    if (iLG_ResourceContainer_Core != null)
                    {
                        LG_WeakResourceContainer container = iLG_ResourceContainer_Core.Cast<LG_WeakResourceContainer>();
                        if (container != null && !container.ISOpen)
                        {
                            if (container.IsLocked())
                            {
                                pWeakLockInteraction unlock = new()
                                {
                                    open = true,
                                    type = eWeakLockInteractionType.Melt //使用DoDamage会惊怪，直接全部用熔锁器解锁
                                };
                                container.WeakLockComponent.AttemptInteract(unlock);
                            }
                            container.TriggerOpen(true);
                        }
                    }
                }
                return;
            }
            ConsoleLogs.LogToConsole("目标物体为非法物体或空", LogLevel.Error);
        }

        // 极大概率失败, 慎用
        [Command("ForceMigration", "强制迁移主机")]
        private static void ForceMigration()
        {
            if (SNet.IsMaster)
            {
                SNet.MasterManagement.ForceMigration();
                return;
            }
            ConsoleLogs.LogToConsole("只有房主才可以强制更换主机", LogLevel.Error);
        }


        [Command("StoreCheckPoint", "保存重生点")]
        private static void StoreCheckPoint()
        {
            if (!SNet.IsMaster)
            {
                ConsoleLogs.LogToConsole("只有房主可以保存重生点", LogLevel.Error);
                return;
            }
            CheckpointManager.StoreCheckpoint(AdminUtils.LocalPlayerAgent.EyePosition);
            SNet.Capture.CaptureGameState(eBufferType.Checkpoint);
            ConsoleLogs.LogToConsole("重生点已保存");
        }

        //[Command("ChangeLookup", "修改识别码")]
        //private static void ChangeLookup(int slot)
        //{
        //    if (AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player))
        //    {
        //        SNet.LocalPlayer.Lookup = player.Owner.Lookup;
        //    }
        //}

        //[Command("RestoreLookup", "恢复识别码")]
        //private static void RestoreLookup()
        //{
        //    SNet.LocalPlayer.Lookup = Steamworks.SteamUser.GetSteamID().m_SteamID;
        //}

        private enum EnemyChoiceType
        {
            Awake = 0,
            Reachable = 1,
            All = 2,
        }

        [Command("KillEnemies", "杀死敌人")]
        private static void KillEnemies(EnemyChoiceType choice = EnemyChoiceType.Awake)
        {
            string msg = string.Empty;
            switch (choice)
            {
                default:
                case EnemyChoiceType.Awake:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        if (enemy.AI.Mode == AgentMode.Agressive)
                        {
                            if (SNet.IsMaster && enemy.Damage.IsImortal)
                                enemy.Damage.IsImortal = false;
                            enemy.Damage.MeleeDamage(float.MaxValue, null, enemy.transform.position, Vector3.up);
                        }
                    }
                    msg = "惊醒";
                    break;
                case EnemyChoiceType.Reachable:
                    foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(AdminUtils.LocalPlayerAgent.CourseNode, 100))
                    {
                        if (SNet.IsMaster && enemy.Damage.IsImortal)
                            enemy.Damage.IsImortal = false;
                        enemy.Damage.MeleeDamage(float.MaxValue, null, enemy.transform.position, Vector3.up);
                    }
                    msg = "可到达";
                    break;
                case EnemyChoiceType.All:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        if (SNet.IsMaster && enemy.Damage.IsImortal)
                            enemy.Damage.IsImortal = false;
                        enemy.Damage.MeleeDamage(float.MaxValue, null, enemy.transform.position, Vector3.up);
                    }
                    msg = "所有";
                    break;
            }
            ConsoleLogs.LogToConsole($"<color=orange>已处死{msg}的敌人</color>");
        }

        [Command("KillPlayer", "处死玩家")]
        private static void KillPlayer([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole($"不存在Slot为 {slot} 的玩家", LogLevel.Error);
                return;
            }
            pSetDeadData data = new()
            {
                allowRevive = true
            };
            if (playerAgent.Owner.IsLocal)
            {
                playerAgent.Locomotion.ChangeState(PlayerLocomotion.PLOC_State.Downed);
            }
            playerAgent.Damage.m_setDeadPacket.Send(data, SNet_ChannelType.GameNonCritical);
            ConsoleLogs.LogToConsole($"已处死玩家 {playerAgent.Owner.NickName}");
        }

        [Command("TagEnemy", "标记敌人")]
        private static void TagEnemies(EnemyChoiceType choice = EnemyChoiceType.Awake)
        {
            List<EnemyAgent> enemies;
            string msg = string.Empty;
            switch (choice)
            {
                case EnemyChoiceType.Awake:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        if (enemy.AI.Mode == AgentMode.Agressive)
                        {
                            ToolSyncManager.WantToTagEnemy(enemy);
                        }
                    }
                    msg = "惊醒";
                    break;
                case EnemyChoiceType.Reachable:
                    foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(AdminUtils.LocalPlayerAgent.CourseNode, 100))
                    {
                        ToolSyncManager.WantToTagEnemy(enemy);
                    }
                    msg = "可到达";
                    break;
                case EnemyChoiceType.All:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        ToolSyncManager.WantToTagEnemy(enemy);
                    }
                    msg = "所有";
                    break;
            }
            ConsoleLogs.LogToConsole($"<color=orange>已标记{msg}的怪物</color>");
        }

        [Command("ListEnemyData")]
        private static void ListEnemyData()
        {
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
            foreach (uint id in EnemyDataHelper.EnemyDataBlockLookup.Keys)
            {
                ConsoleLogs.LogToConsole($"[{id}] {EnemyDataHelper.EnemyDataBlockLookup[id].name}");
            }
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
        }

        [Command("ListEnemyInZone", "列出敌人")]
        private static void ListEnemiesInZone([ZoneAlias] int alias)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                ConsoleLogs.LogToConsole("不在游戏中", LogLevel.Error);
                return;
            }
            if (!Dimension.GetDimension(AdminUtils.LocalPlayerAgent.DimensionIndex, out Dimension dimension))
            {
                ConsoleLogs.LogToConsole($"无法获取当前所在象限: {AdminUtils.LocalPlayerAgent.DimensionIndex}", LogLevel.Error);
                return;
            }
            if (!Builder.CurrentFloor.TryGetZoneByAlias(AdminUtils.LocalPlayerAgent.DimensionIndex, dimension.DimensionData.LinkedToLayer, alias, out LG_Zone zone))
            {
                ConsoleLogs.LogToConsole($"无法获取ZONE_{alias}", LogLevel.Error);
                return;
            }
            Dictionary<LG_Area, Dictionary<string, int>> enemiesInZone = new();
            foreach (LG_Area area in zone.m_areas)
            {
                if (!enemiesInZone.TryGetValue(area, out Dictionary<string, int> value))
                {
                    value = new Dictionary<string, int>();
                    enemiesInZone.Add(area, value);
                }
                foreach (EnemyAgent enemy in area.m_courseNode.m_enemiesInNode)
                {
                    string EnemyName = enemy.EnemyData.name;
                    if (!value.TryGetValue(EnemyName, out int count))
                    {
                        value.Add(EnemyName, 1);
                    }
                    else
                    {
                        value[EnemyName] = ++count;
                    }
                }
            }

            if (enemiesInZone.Count == 0)
            {
                ConsoleLogs.LogToConsole($"ZONE_{alias}中没有敌人");
                return;
            }
            enemiesInZone = enemiesInZone.OrderBy(x => x.Key.m_navInfo.UID).ToDictionary(x => x.Key, x => x.Value.OrderBy(y => y.Key).ToDictionary(y => y.Key, y => y.Value));
            Dictionary<string, int> total = new();
            ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
            ConsoleLogs.LogToConsole($"                           ZONE_{alias} 敌人统计");
            foreach (LG_Area area in enemiesInZone.Keys)
            {
                if (enemiesInZone[area].Count == 0)
                {
                    continue;
                }
                ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
                ConsoleLogs.LogToConsole($"{area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)}:");
                foreach (string enemyName in enemiesInZone[area].Keys)
                {
                    if (!total.ContainsKey(enemyName))
                    {
                        total.Add(enemyName, enemiesInZone[area][enemyName]);
                    }
                    else
                    {
                        total[enemyName] += enemiesInZone[area][enemyName];
                    }
                    ConsoleLogs.LogToConsole($"           敌人:{enemyName.FormatInLength(35)}数量:{enemiesInZone[area][enemyName]}");
                }
            }

            ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
            ConsoleLogs.LogToConsole("总计:");
            if (total.Count == 0)
            {
                ConsoleLogs.LogToConsole("           没有敌人");
            }
            else
            {
                foreach (string enemyName in total.Keys)
                {
                    ConsoleLogs.LogToConsole($"           敌人:{enemyName.FormatInLength(35)}数量:{total[enemyName]}");
                }
            }
            ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
        }

        [Command("SetEnemyTarget", "设置敌人目标")]
        private static void SetEnemyTarget([PlayerSlotIndex] int slot, EnemyChoiceType choice)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player))
            {
                ConsoleLogs.LogToConsole($"不存在slot为 {slot} 的玩家", LogLevel.Error);
                return;
            }
            EnemySync.pEnemyStateData data = new();

            switch (choice)
            {
                case EnemyChoiceType.Reachable:
                    foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(AdminUtils.LocalPlayerAgent.CourseNode, 100))
                    {
                        enemy.Damage.BulletDamage(0f, player, enemy.Position, enemy.Forward, enemy.Forward);
                        data = enemy.Sync.m_enemyStateData;
                        data.target.Set(player);
                        data.agentMode = AgentMode.Agressive;
                        if (SNet.IsMaster)
                        {
                            enemy.Sync.IncomingState(data);
                        }
                        else
                        {
                            enemy.Sync.m_aiStatePacket.Send(data, SNet_ChannelType.GameNonCritical);
                        }
                    }
                    break;
                case EnemyChoiceType.Awake:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        if (enemy.AI.Mode == AgentMode.Agressive)
                        {
                            enemy.Damage.BulletDamage(0f, player, enemy.Position, enemy.Forward, enemy.Forward);
                            data = enemy.Sync.m_enemyStateData;
                            data.target.Set(player);
                            data.agentMode = AgentMode.Agressive;
                            if (SNet.IsMaster)
                            {
                                enemy.Sync.IncomingState(data);
                            }
                            else
                            {
                                enemy.Sync.m_aiStatePacket.Send(data, SNet_ChannelType.GameNonCritical);
                            }
                        }
                    }
                    break;
                case EnemyChoiceType.All:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        enemy.Damage.BulletDamage(0f, player, enemy.Position, enemy.Forward, enemy.Forward);
                        data = enemy.Sync.m_enemyStateData;
                        data.target.Set(player);
                        data.agentMode = AgentMode.Agressive;
                        if (SNet.IsMaster)
                        {
                            enemy.Sync.IncomingState(data);
                        }
                        else
                        {
                            enemy.Sync.m_aiStatePacket.Send(data, SNet_ChannelType.GameNonCritical);
                        }
                    }
                    break;
            }

        }

        [Command("EnemySetState", "设置敌人状态")]
        private static void SetEnemyState(EB_States state, EnemyChoiceType choice = EnemyChoiceType.Awake)
        {
            AgentMode agentMode;
            switch (state)
            {
                default:
                case EB_States.Hibernating:
                    agentMode = AgentMode.Hibernate;
                    break;
                case EB_States.InCombat:
                    agentMode = AgentMode.Agressive;
                    break;
                case EB_States.Dead:
                    agentMode = AgentMode.Off;
                    break;
            }
            EnemySync.pEnemyStateData data;
            switch (choice)
            {
                case EnemyChoiceType.Reachable:
                    foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(AdminUtils.LocalPlayerAgent.CourseNode, 100))
                    {
                        data = enemy.Sync.m_enemyStateData;
                        data.behaviourState = state;
                        data.agentMode = agentMode;
                        if (enemy.IsScout)
                        {
                            data.agentMode = AgentMode.Scout;
                        }
                        if (SNet.IsMaster)
                        {
                            enemy.Sync.IncomingState(data);
                        }
                        else
                        {
                            enemy.Sync.m_aiStatePacket.Send(data, SNet_ChannelType.GameNonCritical);
                        }
                    }
                    break;
                case EnemyChoiceType.Awake:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        if (enemy.AI.Mode == AgentMode.Agressive)
                        {
                            data = enemy.Sync.m_enemyStateData;
                            data.behaviourState = state;
                            data.agentMode = agentMode;
                            if (enemy.IsScout)
                            {
                                data.agentMode = AgentMode.Scout;
                            }
                            if (SNet.IsMaster)
                            {
                                enemy.Sync.IncomingState(data);
                            }
                            else
                            {
                                enemy.Sync.m_aiStatePacket.Send(data, SNet_ChannelType.GameNonCritical);
                            }
                        }
                    }
                    break;
                case EnemyChoiceType.All:
                    foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                    {
                        data = enemy.Sync.m_enemyStateData;
                        data.behaviourState = state;
                        data.agentMode = agentMode;
                        if (enemy.IsScout)
                        {
                            data.agentMode = AgentMode.Scout;
                        }
                        if (SNet.IsMaster)
                        {
                            enemy.Sync.IncomingState(data);
                        }
                        else
                        {
                            enemy.Sync.m_aiStatePacket.Send(data, SNet_ChannelType.GameNonCritical);
                        }
                    }
                    break;
            }
        }

        [Command("Revive", "复活玩家")]
        private static void RevivePlayer([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var targetPlayer))
            {
                ConsoleLogs.LogToConsole($"不存在slot为 {slot} 的玩家", LogLevel.Error);
                return;
            }
            AgentReplicatedActions.PlayerReviveAction(targetPlayer, AdminUtils.LocalPlayerAgent, targetPlayer.Position);
            ConsoleLogs.LogToConsole($"已复活玩家 {targetPlayer.PlayerName}");
        }

        [Command("ReviveAll", "复活所有玩家")]
        private static void ReviveAllPlayer()
        {
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                AgentReplicatedActions.PlayerReviveAction(player, AdminUtils.LocalPlayerAgent, player.Position);
                ConsoleLogs.LogToConsole($"已复活玩家 {player.PlayerName}");
            }
        }

        [Command("DropItem", "丢弃物品")]
        private static void DropItem()
        {
            InventorySlot wieldedSlot = AdminUtils.LocalPlayerAgent.Inventory.WieldedSlot;
            if ((wieldedSlot == InventorySlot.ResourcePack || wieldedSlot == InventorySlot.Consumable || wieldedSlot == InventorySlot.InLevelCarry) && PlayerBackpackManager.TryGetItemInLevelFromItemData(AdminUtils.LocalPlayerAgent.Inventory.WieldedItem.Get_pItemData(), out var item))
            {
                ItemInLevel itemInLevel = item.Cast<ItemInLevel>();
                if (AIG_CourseNode.TryGetCourseNode(AdminUtils.LocalPlayerAgent.DimensionIndex, AdminUtils.LocalPlayerAgent.Position, 0f, out AIG_CourseNode aig_CourseNode))
                {
                    AmmoType ammoType = wieldedSlot == InventorySlot.ResourcePack ? AmmoType.ResourcePackRel : AmmoType.CurrentConsumable;
                    float ammoInPack = PlayerBackpackManager.GetBackpack(SNet.LocalPlayer).AmmoStorage.GetAmmoInPack(ammoType);
                    pItemData_Custom custom = itemInLevel.pItemData.custom;
                    custom.ammo = ammoInPack;
                    itemInLevel.GetSyncComponent().AttemptPickupInteraction(ePickupItemInteractionType.Place, SNet.LocalPlayer, custom, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos, AdminUtils.LocalPlayerAgent.Rotation, aig_CourseNode, true, true);
                    ConsoleLogs.LogToConsole($"已将 {item.ArchetypeName} 丢弃");
                    return;
                }
            }
            ConsoleLogs.LogToConsole("丢弃失败", LogLevel.Error);
        }

        [Command("PlayerGiveBirth", "玩家下崽子")]
        private static void PlayerGiveBirth([PlayerSlotIndex] int slot, [EnemyDataBlockID] uint id, int count = 1)
        {
            UnityMainThreadDispatcher.Enqueue(PlayerGiveBirthCoroutine(slot, id, count));
        }

        private static IEnumerator PlayerGiveBirthCoroutine(int slot, uint id, int count)
        {
            WaitForSecondsRealtime yielder = new(0.2f);
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || player.Owner.IsBot)
            {
                yield break;
            }
            uint dropSound = count > 15 ? 2806903738U : 3495940948U;
            uint startSound = count > 15 ? 3461742098 : 1544195408U;
            ConsoleLogs.LogToConsole($"玩家 {player.Owner.NickName} 开始生 {TranslateHelper.EnemyName(id)}, 数量: {count} 只");
            PlayerVoiceManager.WantToSayInternal(slot - 1, startSound, 0U, 0U, 0U);
            yield return new WaitForSecondsRealtime(2.5f);
            while (count > 0)
            {
                if (!player.Owner.IsInLobby || CurrentGameState != (int)eGameStateName.InLevel)
                {
                    yield break;
                }
                if (AIG_CourseNode.TryGetCourseNode(player.DimensionIndex, player.Position, 1f, out AIG_CourseNode node))
                {
                    PlayerVoiceManager.WantToSayInternal(slot - 1, dropSound, 0U, 0U, 0U);
                    EnemyAgent.SpawnEnemy(id, player.Position, node, AgentMode.Agressive);
                    count--;
                }
                yield return yielder;
            }
        }

        [Command("PlayerControl", "设置玩家控制")]
        private static void SetPlayerControl([PlayerSlotIndex] int slot, bool enable)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out PlayerAgent playerAgent))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            playerAgent.RequestToggleControlsEnabled(enable);
            ConsoleLogs.LogToConsole($"已{(enable ? "允许" : "禁止")} {playerAgent.PlayerName} 活动");
        }


        [Command("KickPlayer", "踢出玩家")]
        private static void KickPlayer([PlayerSlotIndex] int slot)
        {
            var player = SNet.Slots.GetPlayerInSlot(slot - 1);
            if (player == null || player.IsBot)
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            KickPlayer(player);
            ConsoleLogs.LogToConsole($"已踢出玩家 {player.NickName}");
        }

        [Command("BanPlayer", "封禁玩家")]
        private static void BanPlayer([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var agent))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            TheArchive.Features.Security.PlayerLobbyManagement.BanPlayer(agent.Owner);
            ConsoleLogs.LogToConsole($"已封禁玩家 {agent.Owner.NickName}");
        }

        private static void KickPlayer(SNet_Player player)
        {
            if (player.IsLocal)
            {
                SNet.SessionHub.LeaveHub();
            }
            else
            {
                if (SNet.IsMaster)
                {
                    TheArchive.Features.Security.PlayerLobbyManagement.KickPlayer(player);
                }
                else
                {
                    if (player.IsMaster)
                    {
                        pMigrationReport migrationReportData = new();
                        migrationReportData.hasNewMaster = true;
                        migrationReportData.NewMaster.SetPlayer(SNet.LocalPlayer);
                        migrationReportData.type = MigrationReportType.NoAction;
                        SNet.MasterManagement.m_migrationReportPacket.Send(migrationReportData, SNet_ChannelType.SessionOrderCritical, player);

                        SNet.SessionHub.m_masterSessionAnswerPacket.Send(new()
                        {
                            answer = pMasterSessionAnswerType.LeaveLobby
                        }, SNet_ChannelType.SessionOrderCritical, player);
                    }
                    else
                    {
                        pMigrationReport migrationReportData = new();
                        migrationReportData.hasNewMaster = true;
                        migrationReportData.NewMaster.SetPlayer(SNet.LocalPlayer);
                        migrationReportData.type = MigrationReportType.NoAction;
                        SNet.MasterManagement.m_migrationReportPacket.Send(migrationReportData, SNet_ChannelType.SessionOrderCritical, player);

                        SNet.SessionHub.m_masterSessionAnswerPacket.Send(new pMasterAnswer()
                        {
                            answer = pMasterSessionAnswerType.LeaveLobby
                        }, SNet_ChannelType.SessionOrderCritical, player);

                        /*
                        pPlayerData_Session forceJoinLobbyData = player.Session;
                        forceJoinLobbyData.playerSlotIndex = 100;
                        SNet.Sync.m_playerSessionPacket.Send(forceJoinLobbyData, SNet_ChannelType.GameOrderCritical, SNet.Master);
                        */

                        /*
                        pMigrationReport migrationReportData = new();
                        migrationReportData.hasNewMaster = true;
                        migrationReportData.NewMaster.SetPlayer(SNet.LocalPlayer);
                        migrationReportData.type = MigrationReportType.NoAction;
                        SNet.MasterManagement.m_migrationReportPacket.Send(migrationReportData, SNet_ChannelType.SessionOrderCritical, player);

                        pSessionMemberStateChange forceJoinLobbyData = new();
                        forceJoinLobbyData.player.SetPlayer(SNet.Master);
                        forceJoinLobbyData.reason = SNet_PlayerEventReason.None;
                        forceJoinLobbyData.type = SessionMemberChangeType.Kicked;
                        SNet.SessionHub.m_masterSessionMemberChangePacket.Send(forceJoinLobbyData, SNet_ChannelType.SessionOrderCritical, player);

                        SNet.SessionHub.m_masterSessionAnswerPacket.Send(new pMasterAnswer()
                        {
                            answer = pMasterSessionAnswerType.LeaveLobby
                        }, SNet_ChannelType.SessionOrderCritical, player);
                        */
                    }
                }
            }
        }

        [Command("GiveHealth", "给予玩家生命值")]
        private static void GiveHealth([PlayerSlotIndex] int slot, [CommandParameterDescription("数量")] float amount)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole($"不存在slot为 {slot} 的玩家", LogLevel.Error);
                return;
            }
            amount = Math.Max(-100f, Math.Min(amount, 100f));
            playerAgent.GiveHealth(AdminUtils.LocalPlayerAgent, amount / 100f);
            ConsoleLogs.LogToConsole($"{playerAgent.PlayerName} 生命值 {(amount >= 0f ? "增加" : "减少")} {Math.Abs(amount)}%");
        }

        [Command("GiveAmmo", "给予玩家武器弹药")]
        private static void GiveAmmo([PlayerSlotIndex] int slot, [CommandParameterDescription("数量")] float amount)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            amount = Math.Max(-100f, Math.Min(amount, 100f));
            playerAgent.GiveAmmoRel(AdminUtils.LocalPlayerAgent, amount / 100f, amount / 100f, 0f);
            ConsoleLogs.LogToConsole($"{playerAgent.PlayerName} 武器弹药 {(amount >= 0f ? "增加" : "减少")} {Math.Abs(amount)}%");
        }

        [Command("GiveDisinfection", "给予玩家消毒")]
        private static void GiveDisinfection([PlayerSlotIndex] int slot, [CommandParameterDescription("数量")] float amount)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            amount = Math.Max(-100f, Math.Min(amount, 100f));
            playerAgent.GiveDisinfection(AdminUtils.LocalPlayerAgent, amount / 100f);
            ConsoleLogs.LogToConsole($"{playerAgent.PlayerName} 感染值 {(amount < 0f ? "增加" : "减少")} {Math.Abs(amount)}%");
        }

        [Command("GiveTool", "给予玩家工具弹药")]
        private static void GiveTool([PlayerSlotIndex] int slot, [CommandParameterDescription("数量")] float amount)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            amount = Math.Max(-100f, Math.Min(amount, 100f));
            playerAgent.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 0f, 0f, amount / 100f);

            var sentryGuns = GameObject.FindObjectsOfType<SentryGunInstance>();
            foreach (var sg in sentryGuns)
            {
                if (sg.Owner?.GlobalID == playerAgent.GlobalID)
                {
                    sg.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 0f, 0f, amount / 100f);
                }
            }
            ConsoleLogs.LogToConsole($"{playerAgent.PlayerName} 工具弹药 {(amount >= 0f ? "增加" : "减少")} {Math.Abs(amount)}%");
        }

        [Command("GiveResources", "给予玩家资源")]
        private static void GiveResources([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            playerAgent.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 1f, 1f, 1f);
            playerAgent.GiveHealth(AdminUtils.LocalPlayerAgent, 1f);
            if (playerAgent.Damage.Infection > 0f)
                playerAgent.GiveDisinfection(AdminUtils.LocalPlayerAgent, 1f);

            var sentryGuns = GameObject.FindObjectsOfType<SentryGunInstance>();
            foreach (var sg in sentryGuns)
            {
                if (sg.Owner?.GlobalID == playerAgent.GlobalID)
                {
                    sg.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 1f, 1f, 1f);
                }
            }
            ConsoleLogs.LogToConsole($"{playerAgent.PlayerName} 已补充资源");
        }

        [Command("GiveAllResources", "给予所有玩家资源")]
        private static void GiveAllResources()
        {
            foreach (var playerAgent in PlayerManager.PlayerAgentsInLevel)
            {
                playerAgent.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 1f, 1f, 1f);
                playerAgent.GiveHealth(AdminUtils.LocalPlayerAgent, 1f);
                if (playerAgent.Damage.Infection > 0f)
                    playerAgent.GiveDisinfection(AdminUtils.LocalPlayerAgent, 1f);

                var sentryGuns = GameObject.FindObjectsOfType<SentryGunInstance>();
                foreach (var sg in sentryGuns)
                {
                    if (sg.Owner?.GlobalID == playerAgent.GlobalID)
                    {
                        sg.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 1f, 1f, 1f);
                    }
                }
                ConsoleLogs.LogToConsole($"{playerAgent.PlayerName} 已补充资源");
            }
        }

        //没写好，可能无法实现
        //private static void ForceInvitePlayer(ulong lookup)
        //{
        //    if (!SNet.IsInLobby)
        //    {
        //        ConsoleLogs.LogToConsole("不在大厅内", LogLevel.Error);
        //        return;
        //    }
        //    if (!SNet.Core.TryGetPlayer(lookup, out var player, true))
        //    {
        //        ConsoleLogs.LogToConsole($"不存在玩家 {lookup}", LogLevel.Error);
        //        return;
        //    }
        //    pForceJoinLobby forceJoinLobbyData = new() { lobbyID = SNet.Lobby.Identifier.ID };
        //    SNet.SessionHub.m_forceJoinLobby.Send(forceJoinLobbyData, SNet_ChannelType.SessionOrderCritical, player);
        //    pWhoIsMasterAnswer whoIsMasterAnswerData = new pWhoIsMasterAnswer
        //    {
        //        lobbyId = SNet.Lobby.Identifier.ID,
        //        sessionKey = SNet.SessionHub.SessionID
        //    };
        //    SNet.MasterManagement.m_whoIsMasterAnswerPacket.Send(whoIsMasterAnswerData, SNet_ChannelType.SessionOrderCritical, player);
        //    SNet.Lobby.TryCast<SNet_Lobby_STEAM>().PlayerJoined(player, new() { m_SteamID = player.Lookup });
        //    ConsoleLogs.LogToConsole($"已强制邀请玩家: {player.NickName}");
        //}


        [Command("LightsSynced", "设置同步灯光")]
        private static void SetLightsEnabledSync(bool enable)
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
            {
                return;
            }
            EnvironmentStateManager.AttemptSetExpeditionLightMode(enable);
            ConsoleLogs.LogToConsole($"已{(enable ? "启用" : "禁用")} 同步灯光");
        }

        [Command("StopAlarms", "停止所有警报")]
        private static void StopAllAlarms()
        {
            if (!SNet.IsMaster)
            {
                ConsoleLogs.LogToConsole("只有房主才可以关闭所有警报", LogLevel.Error);
                return;
            }
            WardenObjectiveManager.StopAlarms();
            ConsoleLogs.LogToConsole("已关闭所有警报");
        }

        [Command("StopEnemyWaves", "停止当前所有刷怪")]
        private static void StopAllEnemyWaves()
        {
            if (!SNet.IsMaster)
            {
                ConsoleLogs.LogToConsole("只有房主可以停止所有刷怪进程", LogLevel.Error);
                return;
            }
            WardenObjectiveManager.StopAllWardenObjectiveEnemyWaves();
            ConsoleLogs.LogToConsole("已停止所有进行中的刷怪进程");
        }


        [Command("FogTransition", "改变雾气")]
        private static void StartFogTransition(uint fogDataID, int dimensionIndex, int duration = 1)
        {
            if (dimensionIndex < 0 || dimensionIndex > 21)
            {
                ConsoleLogs.LogToConsole("非法象限");
                return;
            }
            EnvironmentStateManager.AttemptStartFogTransition(fogDataID, duration, (eDimensionIndex)dimensionIndex);
            ConsoleLogs.LogToConsole($"开始变更雾气, 象限: {(eDimensionIndex)dimensionIndex}, ID: {fogDataID}, 过渡时长 {duration} 秒");
        }

        [Command("FinishWardenObjectiveChain", "完成任务")]
        private static void ForceFinishWardenObjectiveChain(LG_LayerType layer)
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
            {
                ConsoleLogs.LogToConsole("不在游戏中", LogLevel.Error);
                return;
            }

            string name = string.Empty;
            if (layer == LG_LayerType.MainLayer)
            {
                name = "主要";
            }
            else if (layer == LG_LayerType.SecondaryLayer)
            {
                name = "次要";
            }
            else if (layer == LG_LayerType.ThirdLayer)
            {
                name = "附加";
            }
            if (!WardenObjectiveManager.TryGetLastWardenObjectiveDataForLayer(layer, out _))
            {
                ConsoleLogs.LogToConsole($"不存在{name}任务目标", LogLevel.Error);
                return;
            }
            WardenObjectiveManager.ForceCompleteObjectiveAll(layer);
            ConsoleLogs.LogToConsole($"已完成{name}任务目标"); ;
        }

        [Command("FinishAllWardenObjectiveChain")]
        private static void ForceFinishAllWardenObjectiveChain()
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
            {
                ConsoleLogs.LogToConsole("不在游戏中", LogLevel.Error);
                return;
            }

            foreach (LG_LayerType layer in Enum.GetValues(typeof(LG_LayerType)))
            {
                string name = string.Empty;
                if (layer == LG_LayerType.MainLayer)
                {
                    name = "主要";
                }
                else if (layer == LG_LayerType.SecondaryLayer)
                {
                    name = "次要";
                }
                else if (layer == LG_LayerType.ThirdLayer)
                {
                    name = "附加";
                }
                if (WardenObjectiveManager.TryGetLastWardenObjectiveDataForLayer(layer, out _))
                {
                    WardenObjectiveManager.ForceCompleteObjectiveAll(layer);
                    ConsoleLogs.LogToConsole($"已完成{name}任务目标");
                }
            }
        }

        [Command("PauseGame", "切换游戏暂停")]
        private static bool PuaseGame
        {
            get
            {
                return GameEventAPI.IsGamePaused;
            }
            set
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    ConsoleLogs.LogToConsole("不在游戏中", LogLevel.Error);
                    return;
                }
                if (!SNet.IsMaster)
                {
                    ConsoleLogs.LogToConsole("主机才能暂停游戏", LogLevel.Error);
                    return;
                }
                GameEventAPI.IsGamePaused = !GameEventAPI.IsGamePaused;
                ConsoleLogs.LogToConsole($"已{(GameEventAPI.IsGamePaused ? "暂停" : "继续")}游戏");
            }
        }
    }
}