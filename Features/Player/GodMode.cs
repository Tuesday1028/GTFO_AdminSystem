using Agents;
using Hikaria.AdminSystem.Suggestion.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.QC;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [HideInModSettings]
    public class GodMode : Feature, IOnSessionMemberChanged
    {
        public override string Name => "无敌模式";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        private static Dictionary<ulong, GodModeSettings> GodModeLookup = new();

        public class GodModeSettings
        {
            public bool IgnoreAllDamage { get; set; }

            public bool IgnoreInfection { get; set; }

            public bool CannotDie { get; set; }
        }

        public override void Init()
        {
            GameEventAPI.RegisterListener(this);
        }

        [Command("IgnoreAllDamage")]
        private static void ToggleIgnoreAllDamage([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !GodModeLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.IgnoreAllDamage = !entry.IgnoreAllDamage;
            ConsoleLogs.LogToConsole($"已{(entry.IgnoreAllDamage ? "启用" : "禁用")} {player.Owner.NickName} 免疫伤害");
        }

        [Command("IgnoreInfection")]
        private static void ToggleIgnoreInfection([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !GodModeLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.IgnoreInfection = !entry.IgnoreInfection;
            ConsoleLogs.LogToConsole($"已{(entry.IgnoreInfection ? "启用" : "禁用")} {player.Owner.NickName} 免疫感染");
        }

        [Command("CannotDie")]
        private static void ToggleCannotDie([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !GodModeLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.CannotDie = !entry.CannotDie;
            ConsoleLogs.LogToConsole($"已{(entry.CannotDie ? "启用" : "禁用")} {player.Owner.NickName} 免疫倒地");
        }

        public override void OnGameStateChanged([PlayerSlotIndex] int state)
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
            if (playerEvent == SessionMemberEvent.JoinSessionHub)
            {
                GodModeSettings entry = new()
                {
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
                ConsoleLogs.LogToConsole($"<color=green>已复活玩家 {player.PlayerName}</color>");
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
