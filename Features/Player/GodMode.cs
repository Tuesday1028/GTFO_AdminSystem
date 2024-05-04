using Agents;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Utilities;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [DoNotSaveToConfig]
    public class GodMode : Feature, IOnSessionMemberChanged
    {
        public override string Name => "无敌模式";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        public static Dictionary<ulong, GodModeEntry> GodModeLookup { get; set; } = new();
        [FeatureConfig]
        public static GodModeSettings Settings { get; set; }

        public class GodModeSettings
        {
            [FSDisplayName("玩家设置")]
            [FSReadOnly]
            [FSInline]
            public List<GodModeEntry> PlayerSettings
            {
                get
                {
                    return GodModeLookup.Values.ToList();
                }
                set
                {
                }
            }
        }

        public class GodModeEntry
        {
            [FSDisplayName("昵称")]
            [FSSeparator]
            [FSReadOnly]
            public string NickName
            {
                get
                {
                    if (SNet.TryGetPlayer(Lookup, out var player))
                    {
                        return player.NickName;
                    }
                    return Lookup.ToString();
                }
                set
                {

                }
            }

            [FSIgnore]
            public ulong Lookup { get; set; }

            [FSDisplayName("忽略所有伤害")]
            public bool IgnoreAllDamage { get; set; }

            [FSDisplayName("忽略感染")]
            public bool IgnoreInfection { get; set; }

            [FSDisplayName("无法倒地")]
            public bool CannotDie { get; set; }
        }


        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
            DevConsole.AddCommand(Command.Create<int, bool?>("IgnoreAllDamage", "无敌", "无敌", Parameter.Create("Slot", "玩家所在槽位"), Parameter.Create("Enable", "True: 启用, False: 禁用"), (slot, enable) =>
            {
                if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var player) || !GodModeLookup.TryGetValue(player.Owner.Lookup, out var entry))
                {
                    DevConsole.LogError("输入有误");
                    return;
                }
                if (!enable.HasValue)
                {
                    enable = entry.IgnoreAllDamage;
                }
                entry.IgnoreAllDamage = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} {player.Owner.NickName} 无敌模式");
            }));
            DevConsole.AddCommand(Command.Create<int, bool?>("IgnoreInfection", "免毒", "免毒", Parameter.Create("Slot", "玩家所在槽位"), Parameter.Create("Enable", "True: 启用, False: 禁用"), (slot, enable) =>
            {
                if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var player) || !GodModeLookup.TryGetValue(player.Owner.Lookup, out var entry))
                {
                    DevConsole.LogError("输入有误");
                    return;
                }
                if (!enable.HasValue)
                {
                    enable = entry.IgnoreInfection;
                }
                entry.IgnoreInfection = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} {player.Owner.NickName} 免毒");
            }));
            DevConsole.AddCommand(Command.Create<int, bool?>("CannotDie", "无法倒地", "无法倒地", Parameter.Create("Slot", "玩家所在槽位"), Parameter.Create("Enable", "True: 启用, False: 禁用"), (slot, enable) =>
            {
                if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var player) || !GodModeLookup.TryGetValue(player.Owner.Lookup, out var entry))
                {
                    DevConsole.LogError("输入有误");
                    return;
                }
                if (!enable.HasValue)
                {
                    enable = entry.CannotDie;
                }
                entry.CannotDie = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} {player.Owner.NickName} 无法倒地");
            }));
        }


        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.AfterLevel)
            {
                foreach (var item in GodModeLookup.Values)
                {
                    item.IgnoreAllDamage = false;
                    item.IgnoreInfection = false;
                    item.CannotDie = false;
                }
            }
        }

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (!player.IsValid())
            {
                return;
            }
            if (playerEvent == SessionMemberEvent.JoinSessionHub)
            {
                GodModeEntry entry = new()
                {
                    Lookup = player.Lookup,
                    IgnoreAllDamage = false,
                    IgnoreInfection = false,
                    CannotDie = false
                };
                GodModeLookup.TryAdd(player.Lookup, entry);
            }
            else if (playerEvent == SessionMemberEvent.LeftSessionHub)
            {
                if (player.IsLocal)
                {
                    GodModeLookup.Clear();
                }
                else
                {
                    GodModeLookup.Remove(player.Lookup);
                }
            }

        }

        [ArchivePatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.ChangeState), new Type[]
        {
            typeof(PlayerLocomotion.PLOC_State),
            typeof(bool)
        })]
        private class PlayerLocomotion__ChangeState__Patch
        {
            private static void Postfix(PlayerLocomotion __instance, PlayerLocomotion.PLOC_State state)
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                PlayerAgent player = __instance.m_owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.CannotDie || state != PlayerLocomotion.PLOC_State.Downed)
                {
                    return;
                }
                AgentReplicatedActions.PlayerReviveAction(player, AdminUtils.LocalPlayerAgent, player.Position);
                DevConsole.Log($"<color=green>已复活玩家 {player.PlayerName}</color>");
            }
        }


        [ArchivePatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveSetHealth))]
        private class Dam_PlayerDamageBase__ReceiveSetHealth__Patch
        {
            private static void Prefix(Dam_PlayerDamageBase __instance, ref pSetHealthData data)
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                PlayerAgent player = __instance.Owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.IgnoreAllDamage)
                {
                    return;
                }
                data.health.Set(25f, 25f);
                __instance.m_setHealthPacket.Send(data, SNet_ChannelType.GameNonCritical);
            }
        }

        [ArchivePatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveSetDead))]
        private class Dam_PlayerDamageBase__ReceiveSetDead__Patch
        {
            private static bool Prefix(Dam_PlayerDamageBase __instance, ref pSetDeadData data)
            {
                PlayerAgent player = __instance.Owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.CannotDie)
                {
                    return true;
                }
                if (player.Owner.IsLocal || SNet.IsMaster)
                {
                    return false;
                }
                return true;
            }
        }

        [ArchivePatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
        private class Dam_PlayerDamageLocal__ReceiveSetHealth__Patch
        {
            private static void Prefix(Dam_PlayerDamageLocal __instance, ref pSetHealthData data)
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                PlayerAgent player = __instance.Owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.IgnoreAllDamage)
                {
                    return;
                }
                data.health.Set(25f, 25f);
                __instance.m_setHealthPacket.Send(data, SNet_ChannelType.GameNonCritical);
            }
        }

        [ArchivePatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetDead))]
        private class Dam_PlayerDamageLocal__ReceiveSetDead__Patch
        {
            private static bool Prefix(Dam_PlayerDamageLocal __instance)
            {
                PlayerAgent player = __instance.Owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.CannotDie)
                {
                    return true;
                }
                AgentReplicatedActions.PlayerReviveAction(AdminUtils.LocalPlayerAgent, AdminUtils.LocalPlayerAgent, AdminUtils.LocalPlayerAgent.Position);
                return false;
            }
        }

        [ArchivePatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ModifyInfection))]
        private class Dam_PlayerDamageLocal__ModifyInfection__Patch
        {
            private static void Prefix(Dam_PlayerDamageLocal __instance, ref pInfection data)
            {
                PlayerAgent player = __instance.Owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.IgnoreInfection)
                {
                    return;
                }
                data.amount = 0;
                data.mode = pInfectionMode.Set;
                data.effect = pInfectionEffect.None;
                __instance.m_receiveModifyInfectionPacket.Send(data, SNet_ChannelType.GameNonCritical);
            }
        }

        [ArchivePatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveModifyInfection))]
        private class Dam_PlayerDamageBase__ReceiveModifyInfection__Patch
        {
            private static void Prefix(Dam_PlayerDamageBase __instance, ref pInfection data)
            {
                PlayerAgent player = __instance.Owner;
                ulong lookup = player.Owner.Lookup;
                if (!GodModeLookup.TryGetValue(lookup, out var entry) || !entry.IgnoreInfection)
                {
                    return;
                }
                data.amount = 0;
                data.mode = pInfectionMode.Set;
                data.effect = pInfectionEffect.None;
                __instance.m_receiveModifyInfectionPacket.Send(data, SNet_ChannelType.GameNonCritical);
            }
        }
    }
}
