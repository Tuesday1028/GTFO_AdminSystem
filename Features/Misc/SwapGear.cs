using CellMenu;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class SwapGear : Feature
    {
        public override string Name => "游戏内变更装备";

        public override string Description => "允许玩家在游戏内更换装备";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;

        [FeatureConfig]
        public static SwapGearSettings Settings { get; set; }

        public class SwapGearSettings
        {
            [FSDisplayName("游戏内更换装备")]
            public bool EnableSwapGearInLevel { get; set; } = true;
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("SwapGearInLevel", "游戏内更换装备", "游戏内更换装备", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableSwapGearInLevel;
                }

                Settings.EnableSwapGearInLevel = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 游戏内更换装备");
            }, () =>
            {
                DevConsole.LogVariable("游戏内更换装备", Settings.EnableSwapGearInLevel);
            }));
        }


        [ArchivePatch(typeof(CM_PlayerLobbyBar), nameof(CM_PlayerLobbyBar.HideLoadoutUI))]
        private class CM_PlayerLobbyBar__HideLoadoutUI__Patch
        {
            private static void Prefix(ref bool hide)
            {
                if (!Settings.EnableSwapGearInLevel)
                {
                    return;
                }
                if (hide)
                {
                    hide = false;
                }
            }
        }

        [ArchivePatch(typeof(CM_InventorySlotItem), nameof(CM_InventorySlotItem.LoadData))]
        private class CM_InventorySlotItem__LoadData__Patch
        {
            private static void Prefix(CM_InventorySlotItem __instance, ref bool clickable)
            {
                if (!Settings.EnableSwapGearInLevel)
                {
                    return;
                }
                if (!clickable)
                {
                    var player = __instance.m_parentBar.m_player;
                    clickable = player.IsLocal || SNet.IsMaster && player.IsBot;
                }
            }
        }

        [ArchivePatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.ResetLocalAmmoStorage))]
        private class PlayerBackpackManager__ResetLocalAmmoStorage__Patch
        {
            private static bool Prefix()
            {
                if (!Settings.EnableSwapGearInLevel)
                {
                    return true;
                }
                return CurrentGameState != (int)eGameStateName.InLevel;
            }
        }

        [ArchivePatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.EquipLocalGear))]
        private class PlayerBackpackManager__EquipLocalGear__Patch
        {
            private static void Prefix()
            {
                if (!Settings.EnableSwapGearInLevel || CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                SavePrevAmmoPercent();
                WaitingForGearEquiped = true;
            }
        }

        [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.WantsToWieldSlot))]
        private class PlayerSync__WantsToWieldSlot__Postfix
        {
            private static void Postfix(PlayerSync __instance)
            {
                if (!__instance.m_agent.Owner.IsLocal || !Settings.EnableSwapGearInLevel || CurrentGameState != (int)eGameStateName.InLevel || !WaitingForGearEquiped)
                {
                    return;
                }
                RestoreAmmoPercent();
                WaitingForGearEquiped = false;
            }
        }

        private static void SavePrevAmmoPercent()
        {
            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            foreach (InventorySlot slot in ValidSlots)
            {
                InventorySlotAmmo inventorySlotAmmo = ammoStorage.GetInventorySlotAmmo(slot);
                float clipAmmoFromSlot = ammoStorage.GetClipAmmoFromSlot(slot) * (slot == InventorySlot.GearClass ? 0f : inventorySlotAmmo.CostOfBullet);
                PrevAmmoInPack[slot] = clipAmmoFromSlot + inventorySlotAmmo.AmmoInPack;
            }
        }

        private static void RestoreAmmoPercent()
        {
            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            foreach (InventorySlot slot in ValidSlots)
            {
                InventorySlotAmmo inventorySlotAmmo = ammoStorage.GetInventorySlotAmmo(slot);
                inventorySlotAmmo.AmmoInPack = PrevAmmoInPack[slot];
                ammoStorage.SetClipAmmoInSlot(slot);
                ammoStorage.UpdateSlotAmmoUI(slot);
                ammoStorage.NeedsSync = true;
            }
        }

        private static bool WaitingForGearEquiped;

        private static readonly Dictionary<InventorySlot, float> PrevAmmoInPack = new();

        private static readonly List<InventorySlot> ValidSlots = new()
        {
            InventorySlot.GearStandard,
            InventorySlot.GearSpecial,
            InventorySlot.GearClass
        };
    }
}
