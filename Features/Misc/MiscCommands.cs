using Agents;
using AIGraph;
using CellMenu;
using Enemies;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc
{
    [EnableFeatureByDefault]
    [HideInModSettings]
    [DoNotSaveToConfig]
    [DisallowInGameToggle]
    public class MiscCommands : Feature
    {
        public override string Name => "杂项指令";

        public override string Description => "杂项指令";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;

        public override void Init()
        {
            //资源相关
            DevConsole.AddCommand(Command.Create<int, float>("GiveHealth", "加血", "给玩家加血", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Percentage", "百分比, 范围-100-100"), GiveHealth));
            DevConsole.AddCommand(Command.Create<int, float>("GiveDisinfection", "解毒", "给玩家解毒", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Percentage", "百分比, 范围-100-100"), GiveDisinfection));
            DevConsole.AddCommand(Command.Create<int, float>("GiveAmmo", "加弹药", "给玩家加弹药", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Percentage", "百分比, 范围-100-100"), GiveAmmo));
            DevConsole.AddCommand(Command.Create<int, float>("GiveTool", "加工具", "给玩家加工具", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Percentage", "百分比, 范围-100-100"), GiveTool));
            DevConsole.AddCommand(Command.Create("Drop", "丢弃物品", "丢弃手中物品", DropItem));

            //玩家相关
            DevConsole.AddCommand(Command.Create<int>("KickPlayer", "踢出玩家", "踢出玩家", Parameter.Create("Slot", "槽位, 1-4"), KickPlayer));
            DevConsole.AddCommand(Command.Create<int>("BanPlayer", "封禁玩家", "封禁玩家", Parameter.Create("Slot", "槽位, 1-4"), BanPlayer));
            DevConsole.AddCommand(Command.Create<int, bool>("CtrlPlayer", "玩家控制", "开启关闭玩家活动", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Enable", "开启或关闭, True或False"), SetPlayerControl));
            DevConsole.AddCommand(Command.Create("FuckMaster", "踢出房主", "踢出房主", FuckMaster));
            DevConsole.AddCommand(Command.Create("ForceMigration", "更换房主", "更换房主", ForceMigration));
            DevConsole.AddCommand(Command.Create<int>("RevivePlayer", "扶起倒地玩家", "扶起倒地玩家", Parameter.Create("Slot", "槽位, 1-4"), RevivePlayer));
            DevConsole.AddCommand(Command.Create("ReviveAllPlayer", "扶起所有倒地玩家", "扶起所有倒地玩家", ReviveAllPlayer));
            DevConsole.AddCommand(Command.Create<int>("KillPlayer", "杀死玩家", "杀死玩家", Parameter.Create("Slot", "槽位, 1-4"), KillPlayer));

            //作弊相关
            DevConsole.AddCommand(Command.Create("Operate", "操作或解锁", "操作或解锁", OperateOrUnlock));
            DevConsole.AddCommand(Command.Create<int>("SpawnGlue", "生成胶粒", "生成胶粒", Parameter.Create("GlueExpand", "分量"), FireGlue));
            DevConsole.AddCommand(Command.Create("PortableFogRepeller", "便携驱雾器", "便携驱雾器", TogglePortableFogRepeller));

            //地图相关
            DevConsole.AddCommand(Command.Create("StopAlarms", "关闭所有警报", "关闭所有警报, 仅房主有效", StopAllAlarms));
            DevConsole.AddCommand(Command.Create("StopEnemyWave", "停止刷怪", "停止进行中的刷怪波次, 仅房主有效", StopAllEnemyWave));
            DevConsole.AddCommand(Command.Create<int, int, int>("LightningStrike", "释放闪电", "释放闪电", Parameter.Create("R", "red, 0-255"), Parameter.Create("G", "green, 0-255"), Parameter.Create("B", "blue, 0-255"), LightningStrike));
            DevConsole.AddCommand(Command.Create<uint, int>("SetFog", "切换雾气", "切换雾气", Parameter.Create("ID", "FogDataID"), Parameter.Create("Dimension", "象限Index"), StartFogTransition));

            DevConsole.AddCommand(Command.Create<int, int, int>("FireTargeting", "发射炮弹", "发射炮弹", Parameter.Create("Type", "种类"), Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Count", "个数"), FireTargeting));
            DevConsole.AddCommand(Command.Create("FullBright", "点亮全图", "点亮全图", ToggleFullBright));

            DevConsole.AddCommand(Command.Create<int>("FinishWardenObjectiveChain", "强行完成目标任务链", "强行完成目标任务链", Parameter.Create("LayerType", "0: 主要,1: 次要, 2: 附加"), ForceFinishWardenObjectiveChain));
            DevConsole.AddCommand(Command.Create("FinishAllWardenObjectiveChain", "强行完成所有目标任务链", "强行完成所有目标任务链", ForceFinishAllWardenObjectiveChain));

            DevConsole.AddCommand(Command.Create("StoreCheckPoint", "保存重生点", "保存重生点", StoreCheckPoint));

            DevConsole.AddCommand(Command.Create("MiniMapAllVisible", "小地图全显", "小地图全显", MapForceAllVisible));

            //敌人相关
            DevConsole.AddCommand(Command.Create<int>("TagEnemy", "标记敌人", "标记敌人", Parameter.Create("Choice", "0: 惊醒的, 1: 可到达的, 2: 所有的"), TagEnemies));
            DevConsole.AddCommand(Command.Create<int>("KillEnemy", "杀死敌人", "杀死敌人", Parameter.Create("Choice", "0: 惊醒的, 1: 可到达的, 2: 所有的"), KillEnemies));
            DevConsole.AddCommand(Command.Create<int, bool>("SetEnemyTarget", "设定敌人攻击目标", "设定敌人攻击目标", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("All", "True为所有敌人, False为惊醒的敌人"), SetEnemyTarget));
            DevConsole.AddCommand(Command.Create<int, bool>("SetEnemyState", "设定敌人状态", "设定敌人状态", Parameter.Create("State", "状态, 0: 沉睡, 1: 战斗, 2: 死亡"), Parameter.Create("All", "True为所有敌人, False为惊醒的敌人"), SetEnemyState));
            DevConsole.AddCommand(Command.Create<int>("ListEnemyInZone", "统计地区中敌人数量", "统计地区中敌人数量", Parameter.Create("ZoneID", "地区ID"), ListEnemiesInZone));
            DevConsole.AddCommand(Command.Create("ListEnemyData", "列出敌人数据", "列出敌人数据", ListEnemyData));

            //好像没什么实际用处
            DevConsole.AddCommand(Command.Create<int>("ChangeLookup", "修改唯一识别码", "修改唯一识别码", Parameter.Create("Slot", "槽位, 1-4"), ChangeLookup));
            DevConsole.AddCommand(Command.Create("RestoreLookup", "恢复唯一识别码", "恢复唯一识别码", RestoreLookup));
        }

        private static void KillEnemies(int choice)
        {
            List<EnemyAgent> enemies;
            string msg = string.Empty;
            switch (choice)
            {
                default:
                case 0:
                    enemies = GameObject.FindObjectsOfType<EnemyAgent>().ToList();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if ((!enemies[i].IsScout && enemies[i].Locomotion.CurrentStateEnum != ES_StateEnum.Hibernate) || (enemies[i].IsScout && enemies[i].HasValidTarget()))
                        {
                            enemies[i].Damage.ExplosionDamage(1000000000f, enemies[i].Position, enemies[i].Forward, 0);
                        }
                    }
                    msg = "惊醒";
                    break;
                case 1:
                    enemies = AIG_CourseGraph.GetReachableEnemiesInNodes(AdminUtils.LocalPlayerAgent.CourseNode, 100).ToArray().ToList();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        enemies[i].Damage.ExplosionDamage(1000000000f, enemies[i].Position, enemies[i].Forward, 0);
                    }
                    msg = "可到达";
                    break;
                case 2:
                    enemies = GameObject.FindObjectsOfType<EnemyAgent>().ToArray().ToList();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        enemies[i].Damage.ExplosionDamage(1000000000f, enemies[i].Position, enemies[i].Forward, 0);
                    }
                    msg = "所有";
                    break;
            }
            DevConsole.Log($"<color=orange>已处死{msg}的怪物</color>");
        }

        private static void TagEnemies(int choice)
        {
            List<EnemyAgent> enemies;
            string msg = string.Empty;
            switch (choice)
            {
                case 0:
                    enemies = GameObject.FindObjectsOfType<EnemyAgent>().ToArray().ToList();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if ((!enemies[i].IsScout && enemies[i].Locomotion.CurrentStateEnum != ES_StateEnum.Hibernate) || (enemies[i].IsScout && enemies[i].HasValidTarget()))
                        {
                            ToolSyncManager.WantToTagEnemy(enemies[i]);
                        }
                    }
                    msg = "惊醒";
                    break;
                case 1:
                    enemies = AIG_CourseGraph.GetReachableEnemiesInNodes(AdminUtils.LocalPlayerAgent.CourseNode, 100).ToArray().ToList();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        ToolSyncManager.WantToTagEnemy(enemies[i]);
                    }
                    msg = "可到达";
                    break;
                case 2:
                    enemies = GameObject.FindObjectsOfType<EnemyAgent>().ToArray().ToList();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        ToolSyncManager.WantToTagEnemy(enemies[i]);
                    }
                    msg = "所有";
                    break;
            }
            DevConsole.Log($"<color=orange>已标记{msg}的怪物</color>");
        }

        private static void ListEnemyData()
        {
            DevConsole.Log("----------------------------------------------------------------");
            foreach (uint id in EnemyDataManager.EnemyDataBlockLookup.Keys)
            {
                DevConsole.Log($"[{id}] {EnemyDataManager.EnemyDataBlockLookup[id].name}");
            }
            DevConsole.Log("----------------------------------------------------------------");
        }

        private static void ListEnemiesInZone(int zoneID)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                DevConsole.LogError("不在游戏中");
                return;
            }
            if (!Dimension.GetDimension(AdminUtils.LocalPlayerAgent.DimensionIndex, out Dimension dimension))
            {
                DevConsole.LogError($"无法获取当前所在象限: {AdminUtils.LocalPlayerAgent.DimensionIndex}");
                return;
            }
            if (!Builder.CurrentFloor.TryGetZoneByAlias(AdminUtils.LocalPlayerAgent.DimensionIndex, dimension.DimensionData.LinkedToLayer, zoneID, out LG_Zone zone))
            {
                DevConsole.LogError($"无法获取ZONE_{zoneID}");
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
                DevConsole.LogError($"ZONE_{zoneID}中没有敌人");
            }
            enemiesInZone = enemiesInZone.OrderBy(x => x.Key.m_navInfo.UID).ToDictionary(x => x.Key, x => x.Value.OrderBy(y => y.Key).ToDictionary(y => y.Key, y => y.Value));
            Dictionary<string, int> total = new();
            DevConsole.Log("-------------------------------------------------------------------------");
            DevConsole.Log($"                           ZONE_{zoneID} 敌人统计");
            foreach (LG_Area area in enemiesInZone.Keys)
            {
                if (enemiesInZone[area].Count == 0)
                {
                    continue;
                }
                DevConsole.Log("-------------------------------------------------------------------------");
                DevConsole.Log($"{area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)}:");
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
                    DevConsole.Log($"           敌人:{enemyName.FormatInLength(35)}数量:{enemiesInZone[area][enemyName]}");
                }
            }

            DevConsole.Log("-------------------------------------------------------------------------");
            DevConsole.Log("总计:");
            if (total.Count == 0)
            {
                DevConsole.Log("           没有敌人");
            }
            else
            {
                foreach (string enemyName in total.Keys)
                {
                    DevConsole.Log($"           敌人:{enemyName.FormatInLength(35)}数量:{total[enemyName]}");
                }
            }
            DevConsole.Log("-------------------------------------------------------------------------");
        }


        private static void SetEnemyTarget(int slot, bool all)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent player))
            {
                DevConsole.LogError($"不存在slot为 {slot} 的玩家");
                return;
            }
            EnemySync.pEnemyStateData data = new();
            EnemyAgent[] enemies = GameObject.FindObjectsOfType<EnemyAgent>();
            foreach (EnemyAgent enemy in enemies)
            {
                if (all || enemy.HasValidTarget())
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
        }

        private static void SetEnemyState(int state, bool all)
        {
            EB_States eB_States;
            AgentMode agentMode;
            switch (state)
            {
                default:
                case 0:
                    eB_States = EB_States.Hibernating;
                    agentMode = AgentMode.Hibernate;
                    break;
                case 1:
                    eB_States = EB_States.InCombat;
                    agentMode = AgentMode.Agressive;
                    break;
                case 2:
                    eB_States = EB_States.Dead;
                    agentMode = AgentMode.Off;
                    break;
            }
            EnemySync.pEnemyStateData data;
            EnemyAgent[] enemies = GameObject.FindObjectsOfType<EnemyAgent>();
            foreach (EnemyAgent enemy in enemies)
            {
                if (all || enemy.HasValidTarget())
                {
                    data = enemy.Sync.m_enemyStateData;
                    data.behaviourState = eB_States;
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
        }

        private static void RevivePlayer(int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent targetPlayer))
            {
                DevConsole.LogError($"不存在slot为 {slot} 的玩家");
                return;
            }
            AgentReplicatedActions.PlayerReviveAction(targetPlayer, AdminUtils.LocalPlayerAgent, targetPlayer.Position);
            DevConsole.LogSuccess($"已复活玩家 {targetPlayer.PlayerName}");
        }

        private static void ReviveAllPlayer()
        {
            for (int i = 0; i < 4; i++)
            {
                if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(i + 1, out PlayerAgent targetPlayer))
                {
                    continue;
                }
                AgentReplicatedActions.PlayerReviveAction(targetPlayer, AdminUtils.LocalPlayerAgent, targetPlayer.Position);
                DevConsole.LogSuccess($"已复活玩家 {targetPlayer.PlayerName}");
            }
        }

        private static void KillPlayer(int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                DevConsole.LogError($"不存在Slot为 {slot} 的玩家");
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
            DevConsole.LogSuccess($"已处死玩家 {playerAgent.Owner.NickName}");
        }


        private static void MapForceAllVisible()
        {
            CM_PageMap.Current.ForceAllVisible();

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

            DevConsole.LogSuccess("已设置小地图全显");
        }

        private static void KickPlayer(int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var agent))
            {
                DevConsole.LogError("输入有误");
                return;
            }
            if (!SNet.IsMaster && !agent.Owner.IsLocal)
            {
                DevConsole.LogError("只有房主可以踢人");
                return;
            }
            if (agent.Owner.IsLocal)
            {
                SNet.SessionHub.KickPlayer(SNet.LocalPlayer, SNet_PlayerEventReason.Kick_ByVote);
            }
            else
            {
                TheArchive.Features.Security.PlayerLobbyManagement.KickPlayer(agent.Owner);
            }
            DevConsole.LogSuccess($"已踢出玩家 {agent.Owner.NickName}");
        }

        private static void BanPlayer(int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var agent))
            {
                DevConsole.LogError("输入有误");
                return;
            }
            TheArchive.Features.Security.PlayerLobbyManagement.BanPlayer(agent.Owner);
            DevConsole.LogSuccess($"已封禁玩家 {agent.Owner.NickName}");
        }

        private static void GiveHealth(int slot, float value)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                DevConsole.LogError($"不存在slot为 {slot} 的玩家");
                return;
            }
            value = Math.Max(-100f, Math.Min(value, 100f));
            playerAgent.GiveHealth(AdminUtils.LocalPlayerAgent, value / 100f);
            DevConsole.LogSuccess($"{playerAgent.PlayerName} 生命值 {(value >= 0f ? "增加" : "减少")} {Math.Abs(value)}%");
        }

        private static void GiveAmmo(int slot, float value)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                DevConsole.LogError("输入有误");
                return;
            }
            value = Math.Max(-100f, Math.Min(value, 100f));
            playerAgent.GiveAmmoRel(AdminUtils.LocalPlayerAgent, value / 100f, value / 100f, 0f);
            DevConsole.LogSuccess($"{playerAgent.PlayerName} 武器弹药 {(value >= 0f ? "增加" : "减少")} {Math.Abs(value)}%");
        }

        private static void GiveDisinfection(int slot, float value)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                DevConsole.LogError("输入有误");
                return;
            }
            value = Math.Max(-100f, Math.Min(value, 100f));
            playerAgent.GiveDisinfection(AdminUtils.LocalPlayerAgent, value / 100f);
            DevConsole.LogSuccess($"{playerAgent.PlayerName} 感染值 {(value < 0f ? "增加" : "减少")} {Math.Abs(value)}%");
        }

        private static void GiveTool(int slot, float value)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                DevConsole.LogError("输入有误");
                return;
            }
            value = Math.Max(-100f, Math.Min(value, 100f));
            playerAgent.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 0f, 0f, value / 100f);
            DevConsole.LogSuccess($"{playerAgent.PlayerName} 工具弹药 {(value >= 0f ? "增加" : "减少")} {Math.Abs(value)}%");
        }

        private static void SetPlayerControl(int slot, bool enable)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                DevConsole.LogError("输入有误");
                return;
            }
            playerAgent.RequestToggleControlsEnabled(enable);
            DevConsole.LogSuccess($"已{(enable ? "允许" : "禁止")} {playerAgent.PlayerName} 活动");
        }

        private static void DropItem()
        {
            try
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
                        DevConsole.LogSuccess($"已将 {item.ArchetypeName} 丢弃");
                        return;
                    }
                }
                DevConsole.LogError("丢弃失败");
            }
            catch
            {
            }
        }


        private static void StopAllAlarms()
        {
            if (!SNet.IsMaster)
            {
                DevConsole.LogError("只有房主才可以关闭所有警报");
                return;
            }
            WardenObjectiveManager.StopAlarms();
            DevConsole.LogSuccess("已关闭所有警报");
        }

        private static void StopAllEnemyWave()
        {
            if (!SNet.IsMaster)
            {
                DevConsole.LogError("只有房主才可以强制更换主机");
                return;
            }
            WardenObjectiveManager.StopAllWardenObjectiveEnemyWaves();
            DevConsole.LogSuccess("已停止所有进行中的刷怪进程");
        }

        //差不多能用了
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
        }


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
            DevConsole.LogError("目标物体为非法物体或空");
        }

        // 极大概率失败, 慎用
        private static void ForceMigration()
        {
            if (SNet.IsMaster)
            {
                SNet.MasterManagement.ForceMigration();
                return;
            }
            DevConsole.LogError("只有房主才可以强制更换主机");
        }

        private static void StartFogTransition(uint fogDataID, int dimensionIndex)
        {
            if (dimensionIndex < 0 || dimensionIndex > 21)
            {
                DevConsole.LogError("非法象限");
                return;
            }
            EnvironmentStateManager.AttemptStartFogTransition(fogDataID, 1f, (eDimensionIndex)dimensionIndex);
        }

        private static void LightningStrike(int r, int g, int b)
        {
            Color color = new(r, g, b);
            Vector3 dir = AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir;
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
        }

        private static void FireGlue(int glueExpand)
        {
            ProjectileManager.WantToFireGlue(AdminUtils.LocalPlayerAgent, AdminUtils.LocalPlayerAgent.FPSCamera.Position + AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir * 0.2f, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir * 35f, glueExpand, true);
        }

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
            DevConsole.LogSuccess($"已{(false ? "启用" : "禁用")}便携驱雾器");

        }

        private static void FireTargeting(int type, int slot, int count = 1)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent player))
            {
                DevConsole.LogError($"不存在slot为 {slot} 的玩家");
                return;
            }
            while (count > 0)
            {
                ProjectileManager.WantToFireTargeting((ProjectileType)type, player, AdminUtils.LocalPlayerAgent.EyePosition + AdminUtils.LocalPlayerAgent.Forward * 0.25f, AdminUtils.LocalPlayerAgent.Forward, count, 100);
                count--;
            }
        }

        private static void ToggleFullBright()
        {
            bool enabled = false;
            EffectLight light = AdminUtils.LocalPlayerAgent.gameObject.GetComponent<EffectLight>();
            if (light == null)
            {
                light = AdminUtils.LocalPlayerAgent.gameObject.AddComponent<EffectLight>();
                light.Intensity = 0.2f;
                light.Range = 200.0f;
                light.Color = new Color(1f, 1f, 0.78431f) * 0.65f;
            }
            else
            {
                enabled = light.enabled;
            }

            light.enabled = !enabled;

            DevConsole.LogSuccess($"已{(light.enabled ? "启用" : "禁用")}点亮全图");
        }

        private static void ForceFinishWardenObjectiveChain(int layer)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                DevConsole.LogError("不在游戏中");
                return;
            }

            LG_LayerType type = (LG_LayerType)layer;

            string name = string.Empty;
            if (type == LG_LayerType.MainLayer)
            {
                name = "主要";
            }
            else if (type == LG_LayerType.SecondaryLayer)
            {
                name = "次要";
            }
            else if (type == LG_LayerType.ThirdLayer)
            {
                name = "附加";
            }
            if (!WardenObjectiveManager.TryGetLastWardenObjectiveDataForLayer(type, out _))
            {
                DevConsole.LogError($"不存在{name}任务目标");
                return;
            }
            WardenObjectiveManager.ForceCompleteObjectiveAll(type);
            DevConsole.LogSuccess($"已完成{name}任务目标"); ;
        }

        private static void ForceFinishAllWardenObjectiveChain()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            {
                DevConsole.LogError("不在游戏中");
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
                    DevConsole.LogSuccess($"已完成{name}任务目标"); ;
                    WardenObjectiveManager.ForceCompleteObjectiveAll(layer);
                }
            }
        }

        private static void StoreCheckPoint()
        {
            if (!SNet.IsMaster)
            {
                DevConsole.LogError("你不是房主无法保存重生点");
                return;
            }
            CheckpointManager.StoreCheckpoint(AdminUtils.LocalPlayerAgent.EyePosition);
            SNet.Capture.CaptureGameState(eBufferType.Checkpoint);
            DevConsole.LogSuccess("重生点已保存");
        }

        private static void ChangeLookup(int slot)
        {
            if (AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var player))
            {
                SNet.LocalPlayer.Lookup = player.Owner.Lookup;
            }
        }

        private static void RestoreLookup()
        {
            SNet.LocalPlayer.Lookup = Steamworks.SteamUser.GetSteamID().m_SteamID;
        }
    }
}