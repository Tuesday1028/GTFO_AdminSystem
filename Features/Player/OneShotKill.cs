using Agents;
using Gear;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Utilities;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Player
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class OneShotKill : Feature, IOnSessionMemberChanged
    {
        public override string Name => "秒杀";

        public override string Description => "秒杀敌人";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        [FeatureConfig]
        public static OneShotKillSettings Settings { get; set; }

        public class OneShotKillSettings
        {
            [FSDisplayName("玩家设置")]
            [FSReadOnly]
            [FSInline]
            public List<OneShotKillEntry> PlayerSettings
            {
                get
                {
                    return OneShotKillLookup.Values.ToList();
                }
                set
                {
                }
            }
        }

        public class OneShotKillEntry
        {
            [FSSeparator]
            [FSReadOnly]
            [FSDisplayName("昵称")]
            public string Name
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

            [FSDisplayName("秒杀敌人")]
            public bool EnableOneShotKill { get; set; }
        }

        public static Dictionary<ulong, OneShotKillEntry> OneShotKillLookup { get; set; } = new();

        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
            DevConsole.AddCommand(Command.Create<int, bool?>("OneShotKill", "秒杀敌人", "秒杀敌人", Parameter.Create("Slot", "玩家所在槽位"), Parameter.Create("Enable", "True: 启用, False: 禁用"), (slot, enable) =>
            {
                if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out var player) || !OneShotKillLookup.TryGetValue(player.Owner.Lookup, out var entry))
                {
                    DevConsole.LogError("输入有误");
                    return;
                }
                if (!enable.HasValue)
                {
                    enable = entry.EnableOneShotKill;
                }
                entry.EnableOneShotKill = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} {player.Owner.NickName} 秒杀敌人");
            }));
        }

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (playerEvent == SessionMemberEvent.JoinSessionHub)
            {
                OneShotKillEntry entry = new()
                {
                    Lookup = player.Lookup,
                    EnableOneShotKill = false
                };
                OneShotKillLookup.TryAdd(player.Lookup, entry);
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
                if (OneShotKillLookup.TryGetValue(weaponRayData.owner.Owner.Lookup, out var entry))
                {
                    if (entry.EnableOneShotKill)
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
                if (OneShotKillLookup.TryGetValue(lookup, out var entry) && entry.EnableOneShotKill)
                {
                    dam = 10000000000f;
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
                if (OneShotKillLookup.TryGetValue(lookup, out var entry) && entry.EnableOneShotKill)
                {
                    dam = 10000000000f;
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.ExplosionDamage))]
        private class Dam_EnemyDamageLimb__ExplosionDamage__Patch
        {
            private static void Prefix(ref float dam)
            {
                if (SNet.IsMaster && OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var entry) && entry.EnableOneShotKill)
                {
                    dam = 10000000000f;
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
                if (OneShotKillLookup.TryGetValue(player.Owner.Lookup, out var entry) && entry.EnableOneShotKill)
                {
                    data.damage.Set(10000000000f, __instance.HealthMax);
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
                if (OneShotKillLookup.TryGetValue(player.Owner.Lookup, out var entry) && entry.EnableOneShotKill)
                {
                    data.damage.Set(10000000000f, __instance.HealthMax);
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage))]
        private class Dam_EnemyDamageBase__ReceiveExplosionDamage__Patch
        {
            private static void Prefix(Dam_EnemyDamageBase __instance, ref pExplosionDamageData data)
            {
                if (SNet.IsMaster && OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var entry) && entry.EnableOneShotKill)
                {
                    data.damage.Set(10000000000f, __instance.HealthMax);
                }
            }
        }
    }
}
