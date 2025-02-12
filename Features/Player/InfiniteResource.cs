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
    public class InfiniteResource : Feature, IOnSessionMemberChanged
    {
        public override string Name => "无限资源";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        private static Dictionary<ulong, InfiniteResourceSettings> InfResourceLookup = new();

        public class InfiniteResourceSettings
        {
            public bool InfResource { get; set; }
            public bool NoResource { get; set; }
            public bool InfSentry { get; set; }
            public bool ForceDeploy { get; set; }
        }

        public override void Init()
        {
            GameEventAPI.RegisterListener(this);
        }

        [Command("InfResource", "无限资源")]
        private static void ToggleInfResource([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !InfResourceLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.InfResource = !entry.InfResource;
            ConsoleLogs.LogToConsole($"已{(entry.InfResource ? "启用" : "禁用")} {player.Owner.NickName} 无限资源");
        }


        [Command("NoResource", "禁用资源")]
        private static void ToggleNoResource([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !InfResourceLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.NoResource = !entry.NoResource;
            ConsoleLogs.LogToConsole($"已{(entry.NoResource ? "启用" : "禁用")} {player.Owner.NickName} 禁用资源");
        }

        [Command("InfSentry", "无限哨戒炮")]
        private static void ToggleInfSentry([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !InfResourceLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.InfSentry = !entry.InfSentry;
            ConsoleLogs.LogToConsole($"已{(entry.InfSentry ? "启用" : "禁用")} {player.Owner.NickName} 无限哨戒炮");
        }

        [Command("ForceDeploy", "强制部署")]
        private static void ToggleForceDeploy([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var player) || !InfResourceLookup.TryGetValue(player.Owner.Lookup, out var entry))
            {
                ConsoleLogs.LogToConsole("输入有误", LogLevel.Error);
                return;
            }
            entry.ForceDeploy = !entry.ForceDeploy;
            ConsoleLogs.LogToConsole($"已{(entry.ForceDeploy ? "启用" : "禁用")} {player.Owner.NickName} 强制部署");
        }

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (playerEvent == SessionMemberEvent.JoinSessionHub)
            {
                InfiniteResourceSettings entry = new()
                {
                    InfResource = false,
                    InfSentry = false,
                    NoResource = false,
                    ForceDeploy = false
                };
                InfResourceLookup.TryAdd(player.Lookup, entry);
            }
            else if (playerEvent == SessionMemberEvent.LeftSessionHub)
            {
                if (player.IsLocal)
                {
                    InfResourceLookup.Clear();
                }
                else
                {
                    InfResourceLookup.Remove(player.Lookup);
                }
            }
        }


        [ArchivePatch(typeof(PlayerBackpack), nameof(PlayerBackpack.SetDeployed))]
        private class PlayerBackpack__SetDeployed__Patch
        {
            private static void Prefix(PlayerBackpack __instance, InventorySlot slot, ref bool mode)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (slot != InventorySlot.GearClass || !mode)
                {
                    return;
                }
                SNet_Player player = __instance.Owner;
                if (InfResourceLookup[player.Lookup].InfSentry)
                {
                    mode = false;
                    pInventoryItemStatus data = new();
                    data.sourcePlayer.SetPlayer(player);
                    data.slot = slot;
                    data.status = eInventoryItemStatus.InBackpack;
                    PlayerBackpackManager.Current.m_itemStatusSync.Send(data, SNet_ChannelType.GameNonCritical);
                    player.PlayerAgent.Cast<PlayerAgent>().GiveAmmoRel(AdminUtils.LocalPlayerAgent, 0f, 0f, 1f);
                }
            }
        }

        //炮台开火不减子弹, 主机使用的Hook函数, 此函数可能有同步效果, 未经测试
        [ArchivePatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateAmmo))]
        private class SentryGunInstance_Firing_Bullets__UpdateAmmo__Patch
        {
            private static void Prefix(SentryGunInstance_Firing_Bullets __instance, ref int bullets)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                SentryGunInstance core = __instance.m_core.Cast<SentryGunInstance>();

                if (core.Owner == null)
                {
                    return;
                }
                SNet_Player player = __instance.m_core.Owner.Owner;
                ulong lookup = player.Lookup;
                if (!InfResourceLookup[player.Lookup].InfResource)
                {
                    return;
                }
                //bullets是偏移量, 计算出差值后设定为满弹药量
                int max = (int)(core.AmmoMaxCap / core.CostOfBullet);
                int current = (int)(core.Ammo / core.CostOfBullet);
                bullets = max - current;
            }
        }


        //此函数影响 ResourcePack, Consumable, WeaponClass, 仅本地有效
        [ArchivePatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.UpdateBulletsInPack))]
        private class PlayerAmmoStorage__UpdateBulletsInPack__Patch
        {
            private static void Prefix(PlayerAmmoStorage __instance, AmmoType ammoType, ref int bulletCount)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                SNet_Player player = __instance.m_playerBackpack.Owner;
                if (!InfResourceLookup[player.Lookup].InfResource)
                {
                    return;
                }
                //bulletCount是实际弹药量, 将其改为满弹药量
                int max = __instance.GetBulletMaxCap(ammoType);
                bulletCount = max;

                if (ammoType == AmmoType.ResourcePackRel)
                {
                    __instance.SetAmmo(ammoType, max * __instance.ResourcePackAmmo.CostOfBullet);
                }
                else if (ammoType == AmmoType.Class)
                {
                    __instance.SetAmmo(ammoType, max * __instance.ClassAmmo.CostOfBullet);
                }
                else if (ammoType == AmmoType.CurrentConsumable)
                {
                    __instance.SetAmmo(ammoType, max * __instance.ConsumableAmmo.CostOfBullet);
                }
            }

            private static void Postfix(PlayerAmmoStorage __instance)
            {
                SNet_Player player = __instance.m_playerBackpack.Owner;
                if (!InfResourceLookup[player.Lookup].InfResource)
                {
                    return;
                }
                //获取当前的AmmoStorageData, 再进行通告
                pAmmoStorageData storageData = __instance.GetStorageData();
                __instance.m_playerBackpack.OnStorageUpdatedCallback?.Invoke(__instance.m_playerBackpack);
                PlayerBackpackManager.Current.m_ammoStoragePacket.Send(storageData, SNet_ChannelType.GameOrderCritical);
            }
        }

        //客机使用的Hook函数
        [ArchivePatch(typeof(SentryGunInstance_Sync), nameof(SentryGunInstance_Sync.OnTargetingData))]
        private class SentryGunInstance_Sync__OnTargetingData__Patch
        {
            private static void Postfix(SentryGunInstance_Sync __instance)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (SNet.IsMaster)
                {
                    return;
                }

                SentryGunInstance core = __instance.m_core.Cast<SentryGunInstance>();
                SNet_Player player = core.Owner.Owner;
                if (!InfResourceLookup.TryGetValue(player.Lookup, out var entry) || !entry.InfResource)
                {
                    return;
                }

                //炮台的最大弹药量
                int max = (int)(core.AmmoMaxCap / core.CostOfBullet);

                //更新并通告弹药量
                __instance.ForceReliableAmmoUpdate(max);
            }
        }


        [ArchivePatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.ReceiveAmmoSync))]
        private class PlayerBackpackManager__ReceiveAmmoSync__Patch
        {
            private static void Prefix(ref pAmmoStorageData data)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (!SNet.Core.TryGetPlayer(data.PlayerLookup, out var player) || !PlayerBackpackManager.TryGetBackpack(player, out var playerBackpack) || !InfResourceLookup.TryGetValue(player.Lookup, out var entry))
                {
                    return;
                }
                if (entry.InfResource)
                {
                    data.standardAmmo.Set(playerBackpack.AmmoStorage.StandardAmmo.AmmoMaxCap + playerBackpack.AmmoStorage.StandardAmmo.BulletClipSize * playerBackpack.AmmoStorage.StandardAmmo.CostOfBullet, 500f);
                    data.specialAmmo.Set(playerBackpack.AmmoStorage.SpecialAmmo.AmmoMaxCap + playerBackpack.AmmoStorage.SpecialAmmo.BulletClipSize * playerBackpack.AmmoStorage.SpecialAmmo.CostOfBullet, 500f);
                    data.classAmmo.Set(playerBackpack.AmmoStorage.ClassAmmo.AmmoMaxCap, 500f);
                    data.resourcePackAmmo.Set(playerBackpack.AmmoStorage.ResourcePackAmmo.AmmoMaxCap, 500f);
                    data.consumableAmmo.Set(playerBackpack.AmmoStorage.ConsumableAmmo.AmmoMaxCap, 500f);
                }
                else if (entry.NoResource)
                {
                    data.standardAmmo.Set(-500f, 500f);
                    data.specialAmmo.Set(-500f, 500f);
                    data.classAmmo.Set(-500f, 500f);
                    data.resourcePackAmmo.Set(-500f, 500f);
                    data.consumableAmmo.Set(-500f, 500f);
                }
            }

            private static void Postfix(ref pAmmoStorageData data)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (!SNet.Core.TryGetPlayer(data.PlayerLookup, out SNet_Player player) || !PlayerBackpackManager.TryGetBackpack(player, out _) || !InfResourceLookup.TryGetValue(player.Lookup, out var entry))
                {
                    return;
                }
                if (entry.InfResource || entry.NoResource)
                {
                    PlayerBackpackManager.Current.m_ammoStoragePacket.Send(data, SNet_ChannelType.GameOrderCritical);
                }
                if (entry.ForceDeploy)
                {
                    pInventoryItemStatus status = new();
                    status.sourcePlayer.SetPlayer(player);
                    status.status = eInventoryItemStatus.Deployed;
                    status.slot = InventorySlot.GearMelee;
                    PlayerBackpackManager.Current.m_itemStatusSync.Send(status, SNet_ChannelType.GameNonCritical, player);
                    status.slot = InventorySlot.GearStandard;
                    PlayerBackpackManager.Current.m_itemStatusSync.Send(status, SNet_ChannelType.GameNonCritical, player);
                    status.slot = InventorySlot.GearSpecial;
                    PlayerBackpackManager.Current.m_itemStatusSync.Send(status, SNet_ChannelType.GameNonCritical, player);
                    status.slot = InventorySlot.GearClass;
                    PlayerBackpackManager.Current.m_itemStatusSync.Send(status, SNet_ChannelType.GameNonCritical, player);
                    if (AdminUtils.TryGetPlayerAgentBySlotIndex(player.PlayerSlotIndex(), out var playerAgent))
                    {
                        playerAgent.Sync.WantsToWieldSlot(InventorySlot.None);
                    }
                }
            }
        }

        [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.SyncInventoryStatus))]
        private class PlayerSync__SyncInventoryStatus__Patch
        {
            private static void Postfix(PlayerSync __instance, pInventoryStatus data)
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (__instance.m_agent == null || __instance.m_agent.Owner == null)
                {
                    return;
                }
                if (InfResourceLookup.TryGetValue(__instance.m_agent.Owner.Lookup, out var entry) && entry.ForceDeploy && data.wieldedSlot != InventorySlot.None)
                {
                    __instance.WantsToWieldSlot(InventorySlot.None);
                }
            }
        }

        //通告本地AmmoStorage
        [ArchivePatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.SendLocalAmmoData))]
        private class PlayerBackpackManager__SendLocalAmmoData__Patch
        {
            private static void Prefix()
            {
                if (CurrentGameState != (int)eGameStateName.InLevel || !InfResourceLookup[SNet.LocalPlayer.Lookup].InfResource)
                {
                    return;
                }
                pAmmoStorageData data = PlayerBackpackManager.LocalBackpack.AmmoStorage.GetStorageData();
                if (PlayerBackpackManager.LocalBackpack.AmmoStorage.GetBulletsInPack(AmmoType.Standard) < PlayerBackpackManager.LocalBackpack.AmmoStorage.GetBulletMaxCap(AmmoType.Standard))
                {
                    data.standardAmmo.Set(PlayerBackpackManager.LocalBackpack.AmmoStorage.StandardAmmo.AmmoMaxCap, 500f);
                }
                if (PlayerBackpackManager.LocalBackpack.AmmoStorage.GetBulletsInPack(AmmoType.Special) < PlayerBackpackManager.LocalBackpack.AmmoStorage.GetBulletMaxCap(AmmoType.Special))
                {
                    data.specialAmmo.Set(PlayerBackpackManager.LocalBackpack.AmmoStorage.SpecialAmmo.AmmoMaxCap, 500f);
                }
                PlayerBackpackManager.LocalBackpack.AmmoStorage.SetStorageData(ref data);
            }
        }

        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.InLevel)
            {
                foreach (var item in InfResourceLookup.Values)
                {
                    item.NoResource = false;
                    item.InfResource = false;
                    item.InfSentry = false;
                    item.ForceDeploy = false;
                }
            }
        }
    }
}
