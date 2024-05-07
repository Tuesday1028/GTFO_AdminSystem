#if false
using Enemies;
using SNetwork;
using System;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Features.Security;
using TheArchive.Interfaces;
using TheArchive.Utilities;

namespace Hikaria.AdminSystem.Features.Security
{
    [DisallowInGameToggle]
    [EnableFeatureByDefault]
    public class AntiEnemySpawn : Feature
    {
        public override string Name => "反刷怪";

        public override string Description => "阻止玩家刷怪并给予惩罚";

        public override FeatureGroup Group => EntryPoint.Groups.Security;

        public static new IArchiveLogger FeatureLogger { get; set; }

        public static AntiEnemySpawn Instance { get; private set; }

        [FeatureConfig]
        public static AntiSpawnSettings Settings { get; set; }

        public class AntiSpawnSettings
        {
            [FSDisplayName("反刷怪")]
            public bool EnableAntiSpawn { get; set; } = true;
            [FSDisplayName("惩罚好友")]
            [FSDescription("是否惩罚好友")]
            public bool PunishFriends { get; set; } = false;

            [FSDisplayName("惩罚方式")]
            [FSDescription("当玩家尝试刷怪时如何进行惩罚")]
            public PunishmentMode Punishment { get; set; } = PunishmentMode.Kick;

            [Localized]
            public enum PunishmentMode
            {
                NoneAndLog,
                Kick,
                KickAndBan
            }
        }

        [ArchivePatch(typeof(SNet_Replication), nameof(SNet_Replication.RegisterReplicationManager))]
        private class SNet_Replication__RegisterReplicationManager__Patch
        {
            static void Postfix(ref SNet_ReplicationManager manager)
            {
                var manager1 = manager.TryCast<SNet_ReplicationManager<pEnemyGroupSpawnData, SNet_DynamicReplicator<pEnemyGroupSpawnData>>>();
                if (manager1 != null)
                {
                    FeatureLogger.Info($"Find SNet_ReplicationManager of {nameof(pEnemyGroupSpawnData)}");
                    Il2CppSystem.Action<pEnemyGroupSpawnData> _original1 = manager1.m_spawnRequestPacket.ReceiveAction;
                    Action<pEnemyGroupSpawnData> detour1 = delegate (pEnemyGroupSpawnData data)
                    {
                        if (Settings.EnableAntiSpawn && SNet.IsMaster &&!SNet.Capture.IsCheckpointRecall)
                        {
                            bool cancelSpawn = true;

                            if (SNet.Replication.TryGetLastSender(out var sender))
                            {
                                cancelSpawn = PunishPlayer(sender);
                            }

                            if (cancelSpawn)
                            {
                                FeatureLogger.Notice("刷怪行为被拦截!");
                                return;
                            }
                        }
                        _original1?.Invoke(data);
                    };
                    manager1.m_spawnRequestPacket.ReceiveAction = detour1;
                    return;
                }

                var manager2 = manager.TryCast<EnemyAllocator.EnemyReplicationManager>();
                if (manager2 != null)
                {
                    FeatureLogger.Info($"Find SNet_ReplicationManager of {nameof(pEnemySpawnData)}");
                    Il2CppSystem.Action<pEnemySpawnData> _original2 = manager2.m_spawnRequestPacket.ReceiveAction;
                    Action<pEnemySpawnData> detour2 = delegate (pEnemySpawnData data)
                    {
                        if (Settings.EnableAntiSpawn && SNet.IsMaster && !SNet.Capture.IsCheckpointRecall)
                        {
                            bool cancelSpawn = true;

                            if (SNet.Replication.TryGetLastSender(out var sender))
                            {
                                cancelSpawn = PunishPlayer(sender);
                            }

                            if (cancelSpawn)
                            {
                                FeatureLogger.Notice("刷怪行为被拦截!");
                                return;
                            }
                        }
                        _original2?.Invoke(data);
                    };
                    manager2.m_spawnRequestPacket.ReceiveAction = detour2;
                    return;
                }
            }
        }

        public static bool PunishPlayer(SNet_Player player)
        {
            if (player == null)
                return true;

            if (player.IsFriend() && !Settings.PunishFriends)
            {
                FeatureLogger.Notice($"好友玩家 {player.NickName} 正在尝试刷怪！");
                return false;
            }

            switch (Settings.Punishment)
            {
                case AntiSpawnSettings.PunishmentMode.KickAndBan:
                    PlayerLobbyManagement.BanPlayer(player);
                    goto default;
                case AntiSpawnSettings.PunishmentMode.Kick:
                    PlayerLobbyManagement.KickPlayer(player);
                    goto default;
                default:
                case AntiSpawnSettings.PunishmentMode.NoneAndLog:
                    FeatureLogger.Notice($"玩家 {player.NickName} 正在尝试刷怪！({Settings.Punishment})");
                    return true;
            }
        }
    }
}
#endif