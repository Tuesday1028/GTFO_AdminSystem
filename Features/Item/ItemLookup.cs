using AIGraph;
using GameData;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Suggestion.Suggestors.Attributes;
using Hikaria.AdminSystem.Suggestions.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Item
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [HideInModSettings]
    public class ItemLookup : Feature
    {
        public override string Name => "物品";

        public override FeatureGroup Group => EntryPoint.Groups.Item;

        public static Dictionary<string, ItemInLevel> ItemsInLevel { get; set; } = new();

        [ArchivePatch(typeof(ItemSpawnManager), nameof(ItemSpawnManager.SpawnItem))]
        private class ItemSpawnManager__SpawnItem__Patch
        {
            private static void Postfix(global::Item __result)
            {
                var itemInLevel = __result.TryCast<ItemInLevel>();
                if (itemInLevel == null) return;
                string[] array = itemInLevel.ToString().Split(' ');
                var key = array[1].ToUpperInvariant();
                ItemsInLevel[key] = itemInLevel;

                var sync = itemInLevel.GetSyncComponent()?.TryCast<LG_PickupItem_Sync>();
                if (sync == null)
                    return;
                sync.OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
                {
                    if (status == ePickupItemStatus.PickedUp)
                    {
                        ItemsInLevel.Remove(key);
                    }
                    else if (status == ePickupItemStatus.PlacedInLevel)
                    {
                        ItemsInLevel[key] = itemInLevel;
                    }
                });
            }
        }

        public override void OnGameStateChanged(int state)
        {
            if (state == (int)eGameStateName.AfterLevel)
            {
                ItemsInLevel.Clear();
            }
        }

        [Command("PickupItem")]
        private static void PlayerPickupItem([PlayerSlotIndex] int slot, [ItemInLevel] string itemName)
        {
            itemName = itemName.ToUpperInvariant();
            SNet_Player playerInSlot = SNet.Slots.GetPlayerInSlot(slot - 1);
            if (playerInSlot == null)
            {
                ConsoleLogs.LogToConsole($"不存在slot为 {slot} 的玩家", LogLevel.Error);
                return;
            }
            if (!ItemsInLevel.TryGetValue(itemName, out var value))
            {
                ConsoleLogs.LogToConsole($"不存在名为 {itemName} 的物品", LogLevel.Error);
                return;
            }

            value.Cast<ItemInLevel>().GetSyncComponent().AttemptPickupInteraction(ePickupItemInteractionType.Pickup, playerInSlot, default(pItemData_Custom), default(Vector3), default(Quaternion), null, false, true);
            ConsoleLogs.LogToConsole($"{playerInSlot.NickName} 已捡起 {itemName}");
        }

        [Command("PickupItemEye")]
        private static void PickupItemInEyePos([PlayerSlotIndex] int slot)
        {
            if (Physics.Raycast(AdminUtils.LocalPlayerAgent.FPSCamera.Position, AdminUtils.LocalPlayerAgent.FPSCamera.Forward, out RaycastHit raycastHit, 10f, LayerManager.MASK_APPLY_CARRY_ITEM))
            {
                var componentInParent = raycastHit.collider.GetComponentInParent<global::Item>();
                if (componentInParent != null)
                {
                    SNet_Player playerInSlot = SNet.Slots.GetPlayerInSlot(slot - 1);
                    if (playerInSlot != null)
                    {
                        componentInParent.Cast<ItemInLevel>().GetSyncComponent().AttemptPickupInteraction(0, playerInSlot, default(pItemData_Custom), default(Vector3), default(Quaternion), null, false, false);
                        ConsoleLogs.LogToConsole($"{playerInSlot.NickName} 捡起了 {componentInParent.PublicName}");
                        return;
                    }
                    ConsoleLogs.LogToConsole($"不存在slot为 {slot} 的玩家", LogLevel.Error);
                    return;
                }
            }
            ConsoleLogs.LogToConsole("目标物品为空", LogLevel.Error);
        }

        private enum SpawnItemMode
        {
            Pickup,
            Instance
        }

        [Command("SpawnItem")]
        private static void SpawnItem([ItemDataBlockID] uint id, SpawnItemMode mode = SpawnItemMode.Pickup)
        {
            var block = ItemDataBlock.GetBlock(id);
            if (block == null)
            {
                ConsoleLogs.LogToConsole($"不存在 ID 为 {id} 的物品", LogLevel.Error);
                return;
            }
            InventorySlot slot = block.inventorySlot;
            float maxAmmo = block.ConsumableAmmoMax;
            if (slot == InventorySlot.ResourcePack)
                maxAmmo = 100f;
            var itemMode = mode switch
            {
                SpawnItemMode.Pickup => ItemMode.Pickup,
                SpawnItemMode.Instance => ItemMode.Instance,
                _ => ItemMode.Pickup
            };
            var localPlayer = AdminUtils.LocalPlayerAgent;
            pItemData data = new()
            {
                custom = new pItemData_Custom
                {
                    ammo = maxAmmo,
                    byteId = 0,
                    byteState = 0
                },
                itemID_gearCRC = block.persistentID,
                slot = slot,
                originLayer = localPlayer.CourseNode.LayerType
            };
            data.originCourseNode.Set(localPlayer.CourseNode);
            ItemReplicationManager.SpawnItem(data, DelegateSupport.ConvertDelegate<ItemReplicationManager.delItemCallback>(new Action<ISyncedItem, PlayerAgent>((item, player) => {
                var itemInLevel = item.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    return;
                itemInLevel.CourseNode ??= localPlayer.CourseNode;
                itemInLevel.internalSync.AttemptPickupInteraction(ePickupItemInteractionType.UpdateCustomData, SNet.LocalPlayer, new()
                {
                    ammo = maxAmmo,
                });
            })), itemMode, localPlayer.FPSCamera.CameraRayPos, localPlayer.Rotation, localPlayer.CourseNode, localPlayer);
        }

        [Command("SpawnItemByName")]
        private static void SpawnItemByName([ItemDataBlockName] string name, SpawnItemMode mode = SpawnItemMode.Pickup)
        {
            var block = ItemDataBlock.GetBlock(name);
            if (block == null)
            {
                ConsoleLogs.LogToConsole($"不存在名称为 {name} 的物品", LogLevel.Error);
                return;
            }
            InventorySlot slot = block.inventorySlot;
            float maxAmmo = block.ConsumableAmmoMax;
            if (slot == InventorySlot.ResourcePack)
                maxAmmo = 100f;

            var itemMode = mode switch
            {
                SpawnItemMode.Pickup => ItemMode.Pickup,
                SpawnItemMode.Instance => ItemMode.Instance,
                _ => ItemMode.Pickup
            };
            var localPlayer = AdminUtils.LocalPlayerAgent;
            pItemData data = new()
            {
                custom = new pItemData_Custom
                {
                    ammo = maxAmmo,
                    byteId = 0,
                    byteState = 0
                },
                itemID_gearCRC = block.persistentID,
                slot = slot,
                originLayer = localPlayer.CourseNode.LayerType
            };
            data.originCourseNode.Set(localPlayer.CourseNode);
            ItemReplicationManager.SpawnItem(data, DelegateSupport.ConvertDelegate<ItemReplicationManager.delItemCallback>(new Action<ISyncedItem, PlayerAgent>((item, player) => {
                var itemInLevel = item.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    return;
                itemInLevel.CourseNode ??= localPlayer.CourseNode;
                itemInLevel.internalSync.AttemptPickupInteraction(ePickupItemInteractionType.UpdateCustomData, SNet.LocalPlayer, new()
                {
                    ammo = maxAmmo,
                });
            })), itemMode, localPlayer.FPSCamera.CameraRayPos, localPlayer.Rotation, localPlayer.CourseNode, localPlayer);
        }

        [Command("GiveItem")]
        private static void GiveItem([PlayerSlotIndex] int slot, [ItemDataBlockID] uint id)
        {
            var block = ItemDataBlock.GetBlock(id);
            if (block == null)
            {
                ConsoleLogs.LogToConsole($"不存在 ID 为 {id} 的物品", LogLevel.Error);
                return;
            }
            if (block.PickupPrefabs.Count == 0)
            {
                ConsoleLogs.LogToConsole($"非法物品 {id}", LogLevel.Error);
                return;
            }
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole($"不存在 slot 为 {slot}的玩家", LogLevel.Error);
                return;
            }
            InventorySlot itemSlot = block.inventorySlot;
            float maxAmmo = block.ConsumableAmmoMax;
            if (itemSlot == InventorySlot.ResourcePack)
                maxAmmo = 100f;
            var localPlayer = AdminUtils.LocalPlayerAgent;
            pItemData data = new()
            {
                custom = new pItemData_Custom
                {
                    ammo = maxAmmo,
                    byteId = 0,
                    byteState = 0
                },
                itemID_gearCRC = block.persistentID,
                slot = itemSlot,
                originLayer = localPlayer.CourseNode.LayerType
            };
            data.originCourseNode.Set(playerAgent.CourseNode);
            ItemReplicationManager.SpawnItem(data, DelegateSupport.ConvertDelegate<ItemReplicationManager.delItemCallback>(new Action<ISyncedItem, PlayerAgent>((item, player) =>
            {
                var itemInLevel = item.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    return;
                itemInLevel.CourseNode ??= playerAgent.CourseNode;
                itemInLevel.internalSync.AttemptPickupInteraction(ePickupItemInteractionType.UpdateCustomData, playerAgent.Owner, new()
                {
                    ammo = maxAmmo,
                });
                itemInLevel.internalSync.AttemptPickupInteraction(ePickupItemInteractionType.Pickup, playerAgent.Owner);
            })), ItemMode.Pickup, playerAgent.Position, playerAgent.Rotation, playerAgent.CourseNode, localPlayer);
        }

        [Command("GiveItemByName")]
        private static void GiveItemByName([PlayerSlotIndex] int slot, [ItemDataBlockName] string name)
        {
            var block = ItemDataBlock.GetBlock(name);
            if (block == null)
            {
                ConsoleLogs.LogToConsole($"不存在名称为 {name} 的物品", LogLevel.Error);
                return;
            }
            if (block.PickupPrefabs.Count == 0)
            {
                ConsoleLogs.LogToConsole($"非法物品 {name}", LogLevel.Error);
                return;
            }
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                ConsoleLogs.LogToConsole($"不存在 slot 为 {slot}的玩家", LogLevel.Error);
                return;
            }
            InventorySlot itemSlot = block.inventorySlot;
            float maxAmmo = block.ConsumableAmmoMax;
            if (itemSlot == InventorySlot.ResourcePack)
                maxAmmo = 100f;
            var localPlayer = AdminUtils.LocalPlayerAgent;
            pItemData data = new()
            {
                custom = new pItemData_Custom
                {
                    ammo = maxAmmo,
                    byteId = 0,
                    byteState = 0
                },
                itemID_gearCRC = block.persistentID,
                slot = itemSlot,
                originLayer = localPlayer.CourseNode.LayerType
            };
            data.originCourseNode.Set(playerAgent.CourseNode);
            ItemReplicationManager.SpawnItem(data, DelegateSupport.ConvertDelegate<ItemReplicationManager.delItemCallback>(new Action<ISyncedItem, PlayerAgent>((item, player) =>
            {
                var itemInLevel = item.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    return;
                itemInLevel.CourseNode ??= playerAgent.CourseNode;
                itemInLevel.internalSync.AttemptPickupInteraction(ePickupItemInteractionType.UpdateCustomData, playerAgent.Owner, new()
                {
                    ammo = maxAmmo,
                });
                itemInLevel.internalSync.AttemptPickupInteraction(ePickupItemInteractionType.Pickup, playerAgent.Owner);
            })), ItemMode.Pickup, playerAgent.Position, playerAgent.Rotation, playerAgent.CourseNode, localPlayer);
        }

        [Command("ListItemData")]
        private static void ListItemData()
        {
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
            foreach (var block in ItemDataBlock.GetAllBlocksForEditor())
            {
                ConsoleLogs.LogToConsole($"[{block.persistentID}] {block.name}");
            }
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
        }

        private enum TripMineType
        {
            Explosive,
            Glue
        }

        [Command("SpawnMine")]
        private static void SpawnMine(TripMineType type)
        {
            pItemData data = new()
            {
                itemID_gearCRC = type switch
                {
                    TripMineType.Explosive => 125U,
                    TripMineType.Glue => 126U,
                    _ => 125U
                }
            };
            ItemReplicationManager.SpawnItem(data, null, ItemMode.Instance, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos, Quaternion.LookRotation(AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayNormal * -1f, AdminUtils.LocalPlayerAgent.Forward), AdminUtils.LocalPlayerAgent.CourseNode, AdminUtils.LocalPlayerAgent);
        }

        [Command("ListItemsInZone")]
        private static void ListItemsInZone([ZoneAlias] int alias = -1)
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
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
            Dictionary<LG_Area, Dictionary<string, int>> resourcesInZone = new();
            Dictionary<LG_Area, Dictionary<string, int>> consumableInZone = new();
            foreach (LG_Area area in zone.m_areas)
            {
                if (!resourcesInZone.TryGetValue(area, out var resources))
                {
                    resources = new();
                    resourcesInZone.Add(area, resources);
                }
                if (!consumableInZone.TryGetValue(area, out var consumables))
                {
                    consumables = new();
                    consumableInZone.Add(area, consumables);
                }
                foreach (ItemInLevel item in area.m_courseNode.m_itemsInNode)
                {
                    InventorySlot slot = item.pItemData.slot;
                    if (slot < InventorySlot.ResourcePack || slot > InventorySlot.ConsumableHeavy)
                    {
                        continue;
                    }

                    string itemName = item.ItemDataBlock.publicName;
                    int count = (int)item.pItemData.custom.ammo;
                    if (slot == InventorySlot.ResourcePack)
                    {
                        count /= 20;

                        if (!resources.TryAdd(itemName, count))
                        {
                            resources[itemName] += count;
                        }
                    }
                    else
                    {
                        if (!consumables.TryAdd(itemName, count))
                        {
                            consumables[itemName] += count;
                        }
                    }
                }
            }

            if (resourcesInZone.Count == 0 && consumableInZone.Count == 0)
            {
                ConsoleLogs.LogToConsole($"ZONE_{alias}中没有资源", LogLevel.Error);
                return;
            }
            resourcesInZone = resourcesInZone.OrderBy(x => x.Key.m_navInfo.UID).ToDictionary(x => x.Key, x => x.Value.OrderBy(y => y.Key).ToDictionary(y => y.Key, y => y.Value));
            consumableInZone = consumableInZone.OrderBy(x => x.Key.m_navInfo.UID).ToDictionary(x => x.Key, x => x.Value.OrderBy(y => y.Key).ToDictionary(y => y.Key, y => y.Value));
            Dictionary<string, int> totalResource = new();
            Dictionary<string, int> totalConsumable = new();

            ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
            ConsoleLogs.LogToConsole($"                           ZONE_{alias} 资源统计");
            foreach (LG_Area area in resourcesInZone.Keys)
            {
                if (resourcesInZone[area].Count == 0 && consumableInZone[area].Count == 0)
                {
                    continue;
                }
                ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
                ConsoleLogs.LogToConsole($"{area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)}:");
                foreach (string itemName in resourcesInZone[area].Keys)
                {
                    if (!totalResource.ContainsKey(itemName))
                    {
                        totalResource.Add(itemName, resourcesInZone[area][itemName]);
                    }
                    else
                    {
                        totalResource[itemName] += resourcesInZone[area][itemName];
                    }
                    ConsoleLogs.LogToConsole($"           资源包: {itemName.PadRight(36)}数量: {resourcesInZone[area][itemName]}次");
                }
                foreach (string itemName in consumableInZone[area].Keys)
                {
                    if (!totalConsumable.ContainsKey(itemName))
                    {
                        totalConsumable.Add(itemName, consumableInZone[area][itemName]);
                    }
                    else
                    {
                        totalConsumable[itemName] += consumableInZone[area][itemName];
                    }
                    ConsoleLogs.LogToConsole($"           可消耗品: {itemName.PadRight(35)}数量: {consumableInZone[area][itemName]}次");
                }
            }

            ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
            ConsoleLogs.LogToConsole("总计:");
            if (totalResource.Count == 0 && totalConsumable.Count == 0)
            {
                ConsoleLogs.LogToConsole("           没有资源");
            }
            else
            {
                totalResource = totalResource.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                totalConsumable = totalConsumable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                foreach (string itemName in totalResource.Keys)
                {
                    ConsoleLogs.LogToConsole($"           资源包:{itemName.PadRight(36)}数量:{totalResource[itemName]}次");
                }
                foreach (string itemName in totalConsumable.Keys)
                {
                    ConsoleLogs.LogToConsole($"           可消耗品:{itemName.PadRight(35)}数量:{totalConsumable[itemName]}次");
                }
            }
            ConsoleLogs.LogToConsole("-------------------------------------------------------------------------");
        }

        [Command("ItemPing")]
        private static void PingItem([ItemInLevel] string itemName)
        {
            itemName = itemName.ToUpperInvariant();
            if (!LG_LevelInteractionManager.TryGetTerminalInterface(itemName, AdminUtils.LocalPlayerAgent.DimensionIndex, out iTerminalItem iTerminalItem))
            {
                ConsoleLogs.LogToConsole($"不存在名为 {itemName} 的物品", LogLevel.Error);
                return;
            }
            iTerminalItem.PlayPing();
        }

        [Command("ItemQuery")]
        private static void QueryItem([ItemInLevel] string itemName)
        {
            itemName = itemName.ToUpperInvariant();
            eDimensionIndex dimensionIndex = AdminUtils.LocalPlayerAgent.DimensionIndex;
            if (!LG_LevelInteractionManager.TryGetTerminalInterface(itemName, dimensionIndex, out iTerminalItem iTerminalItem))
            {
                ConsoleLogs.LogToConsole($"不存在名为 {itemName} 的物品", LogLevel.Error);
                return;
            }
            Il2CppSystem.Collections.Generic.List<string> itemDetails = new();
            itemDetails.Add("ID: " + iTerminalItem.TerminalItemKey);
            itemDetails.Add("物品状态: " + iTerminalItem.FloorItemStatus);
            string locationText = iTerminalItem.FloorItemLocation;
            if (AIG_CourseNode.TryGetCourseNode(dimensionIndex, iTerminalItem.LocatorBeaconPosition, 1f, out var node))
            {
                locationText += $" Area_{node.m_area.m_navInfo.Suffix}";
            }
            itemDetails.Add("位置: " + locationText);
            itemDetails.Add("----------------------------------------------------------------");
            foreach (string detailInfo in iTerminalItem.GetDetailedInfo(itemDetails))
            {
                ConsoleLogs.LogToConsole(detailInfo);
            }
        }

        [Command("ItemList")]
        private static void ListItem(string param1, string param2 = "")
        {
            StringBuilder sb = new(500);
            bool flag2 = param1 == string.Empty;
            bool flag3 = param1 != string.Empty;
            bool flag4 = param2 != string.Empty;
            if (flag2)
            {
                ConsoleLogs.LogToConsole("参数1不可为空", LogLevel.Error);
                return;
            }
            sb.AppendLine("-----------------------------------------------------------------------------------");
            sb.AppendLine("ID                       物品类型             物品状态              位置");
            foreach (var keyValuePair in LG_LevelInteractionManager.Current.m_terminalItemsByKeyString)
            {
                if (keyValuePair.Value.ShowInFloorInventory)
                {
                    var terminalItem = keyValuePair.Value;
                    string locationInfo = terminalItem.FloorItemLocation;
                    if (AIG_CourseNode.TryGetCourseNode(terminalItem.LocatorBeaconPosition.GetDimension().DimensionIndex, terminalItem.LocatorBeaconPosition, 1f, out var node))
                    {
                        locationInfo += $" Area_{node.m_area.m_navInfo.Suffix}";
                    }
                    string text2 = string.Concat(new object[]
                    {
                        terminalItem.TerminalItemKey,
                        " ",
                        terminalItem.FloorItemType,
                        " ",
                        terminalItem.FloorItemStatus,
                        " ",
                        terminalItem.FloorItemLocation,
                        " ",
                        eFloorInventoryObjectBeaconStatus.NoBeacon.ToString()
                    });
                    bool flag5 = flag3 && text2.Contains(param1, StringComparison.InvariantCultureIgnoreCase);
                    bool flag6 = flag4 && text2.Contains(param2, StringComparison.InvariantCultureIgnoreCase);
                    bool flag7 = !flag3 && !flag4;
                    bool flag8 = (!flag3 || flag5) && (!flag4 || flag6);
                    if (flag7 || flag8)
                    {
                        sb.AppendLine(terminalItem.TerminalItemKey.PadRight(25) + terminalItem.FloorItemType.ToString().PadRight(20) + terminalItem.FloorItemStatus.ToString().PadRight(20) + locationInfo);
                    }
                }
            }
            sb.AppendLine("-----------------------------------------------------------------------------------");
            ConsoleLogs.LogToConsole(sb.ToString());
        }


        public struct ItemInLevelTag : IQcSuggestorTag
        {

        }

        public sealed class ItemInLevelAttribute : SuggestorTagAttribute
        {
            private readonly IQcSuggestorTag[] _tags = { new ItemInLevelTag() };

            public override IQcSuggestorTag[] GetSuggestorTags()
            {
                return _tags;
            }
        }

        public class ItemInLevelSuggestor : BasicQcSuggestor<string>
        {
            protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
            {
                return context.HasTag<ItemInLevelTag>();
            }

            protected override IQcSuggestion ItemToSuggestion(string item)
            {
                return new RawSuggestion(item);
            }

            protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
            {
                return ItemsInLevel.Keys;
            }
        }
    }
}
