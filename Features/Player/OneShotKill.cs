using Agents;
using Gear;
using Hikaria.AdminSystem.Suggestion.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.QC;
using Player;
using SNetwork;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [HideInModSettings]
    public class OneShotKill : Feature, IOnSessionMemberChanged
    {
        public override string Name => "秒杀";

        public override string Description => "秒杀敌人";

        public override FeatureGroup Group => EntryPoint.Groups.Player;


        public static Dictionary<ulong, bool> OneShotKillLookup = new();

        public override void Init()
        {
            GameEventAPI.RegisterListener(this);
        }

        [Command("OneShotKill", "一击必杀")]
        private static void ToggleOneShotKill([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !OneShotKillLookup.TryGetValue(player.Owner.Lookup, out var enable))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            OneShotKillLookup[player.Owner.Lookup] = !enable;
            ConsoleLogs.LogToConsole($"已{(OneShotKillLookup[player.Owner.Lookup] ? "启用" : "禁用")} {player.Owner.NickName} 秒杀敌人");
        }

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (playerEvent == SessionMemberEvent.JoinSessionHub)
            {
                OneShotKillLookup.TryAdd(player.Lookup, false);
            }
            else if (playerEvent == SessionMemberEvent.LeftSessionHub)
            {
                if (player.IsLocal)
                {
                    OneShotKillLookup.Clear();
                }
                else
                {
                    OneShotKillLookup.Remove(player.Lookup);
                }
            }

        }

        // 客机时使用
        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        private class BulletWeapon__BulletHit__Patch
        {
            private static void Prefix(global::Weapon.WeaponHitData weaponRayData, ref bool doDamage)
            {
                if (SNet.IsMaster)
                {
                    return;
                }
                if (OneShotKillLookup.TryGetValue(weaponRayData.owner.Owner.Lookup, out var enable))
                {
                    if (enable)
                    {
                        doDamage = true;
                    }
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        private class Dam_EnemyDamageLimb__MeleeDamage__Patch
        {
            private static void Prefix(Agent sourceAgent, ref float dam)
            {
                PlayerAgent player = sourceAgent.TryCast<PlayerAgent>();
                if (player == null)
                {
                    return;
                }
                ulong lookup = player.Owner.Lookup;
                if (OneShotKillLookup.TryGetValue(lookup, out var enable) && enable)
                {
                    dam = float.MaxValue;
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        private class Dam_EnemyDamageLimb__BulletDamage__Patch
        {
            private static void Prefix(Agent sourceAgent, ref float dam)
            {
                PlayerAgent player = sourceAgent.TryCast<PlayerAgent>();
                if (player == null)
                {
                    return;
                }
                ulong lookup = player.Owner.Lookup;
                if (OneShotKillLookup.TryGetValue(lookup, out var enable) && enable)
                {
                    dam = float.MaxValue;
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.ExplosionDamage))]
        private class Dam_EnemyDamageLimb__ExplosionDamage__Patch
        {
            private static void Prefix(ref float dam)
            {
                if (SNet.IsMaster && OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var enable) && enable)
                {
                    dam = float.MaxValue;
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage))]
        private class Dam_EnemyDamageBase__ReceiveMeleeDamage__Patch
        {
            private static void Prefix(Dam_EnemyDamageBase __instance, ref pFullDamageData data)
            {
                if (!data.source.TryGet(out var agent))
                {
                    return;
                }
                PlayerAgent player = agent.TryCast<PlayerAgent>();
                if (player == null)
                {
                    return;
                }
                if (OneShotKillLookup.TryGetValue(player.Owner.Lookup, out var enable) && enable)
                {
                    data.damage.Set(float.MaxValue, __instance.HealthMax);
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage))]
        private class Dam_EnemyDamageBase__ReceiveBulletDamage__Patch
        {
            private static void Prefix(Dam_EnemyDamageBase __instance, ref pBulletDamageData data)
            {
                if (!data.source.TryGet(out var agent))
                {
                    return;
                }
                PlayerAgent player = agent.TryCast<PlayerAgent>();
                if (player == null)
                {
                    return;
                }
                if (OneShotKillLookup.TryGetValue(player.Owner.Lookup, out var enable) && enable)
                {
                    data.damage.Set(float.MaxValue, __instance.HealthMax);
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage))]
        private class Dam_EnemyDamageBase__ReceiveExplosionDamage__Patch
        {
            private static void Prefix(Dam_EnemyDamageBase __instance, ref pExplosionDamageData data)
            {
                if (SNet.IsMaster && OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var enable) && enable)
                {
                    data.damage.Set(float.MaxValue, __instance.HealthMax);
                }
            }
        }
    }
}
