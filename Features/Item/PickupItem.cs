using AIGraph;
using GameData;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Item
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class PickupItem : Feature
    {
        public override string Name => "物品";

        public override FeatureGroup Group => EntryPoint.Groups.Item;

        [FeatureConfig]
        public static ItemSettings Settings { get; set; }

        public class ItemSettings
        {
            [FSHeader("物品数据查询")]
            [FSDisplayName("物品信息表")]
            [FSReadOnly]
            public List<ItemDataEntry> ItemDataLookup
            {
                get
                {
                    List<ItemDataEntry> list = new();
                    foreach (var item in NameIDLookup)
                    {
                        list.Add(new(item.Value, item.Key));
                    }
                    return list;
                }
                set
                {
                }
            }
        }

        public class ItemDataEntry
        {
            public ItemDataEntry(uint id, string name)
            {
                ID = id;
                Name = name;
            }

            [FSSeparator]
            [FSDisplayName("物品ID")]
            [FSReadOnly]
            public uint ID { get; set; }

            [FSDisplayName("物品名称")]
            [FSReadOnly]
            public string Name { get; set; }
        }


        public static Dictionary<string, uint> NameIDLookup { get; set; } = new();

        public static Dictionary<uint, ItemDataBlock> IDBlockLookup { get; set; } = new();

        public static Dictionary<string, global::Item> ItemsInLevel { get; set; } = new();

        private static bool IsValidItemSlot(pItemData data)
        {
            return data.slot == InventorySlot.ResourcePack || data.slot == InventorySlot.Consumable || data.slot == InventorySlot.ConsumableHeavy || data.slot == InventorySlot.InLevelCarry || data.slot == InventorySlot.InPocket;
        }

        [ArchivePatch(typeof(LG_PickupItem_Sync), nameof(LG_PickupItem_Sync.OnStateChange))]
        private class LG_PickupItem_Sync__OnStateChange__Patch
        {
            private static void Prefix(LG_PickupItem_Sync __instance, pPickupItemState newState, bool isRecall)
            {
                if (IsValidItemSlot(__instance.item.pItemData))
                {
                    if (newState.status == ePickupItemStatus.PickedUp)
                    {
                        string[] array = __instance.item.ToString().Split(' ');
                        ItemsInLevel.Remove(array[1]);
                    }
                    if (newState.status == ePickupItemStatus.PlacedInLevel)
                    {
                        string[] array2 = __instance.item.ToString().Split(' ');
                        if (!ItemsInLevel.ContainsKey(array2[1]))
                        {
                            ItemsInLevel.Add(array2[1], __instance.item);
                        }
                        else
                        {
                            ItemsInLevel[array2[1]] = __instance.item;
                        }
                    }
                }
            }
        }

        public override void OnGameDataInitialized()
        {
            foreach (ItemDataBlock block in GameDataBlockBase<ItemDataBlock>.GetAllBlocksForEditor())
            {
                NameIDLookup[block.publicName.Replace(" ", "").ToUpperInvariant()] = block.persistentID;
                IDBlockLookup.Add(block.persistentID, block);
            }
        }


        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current >= eGameStateName.ExpeditionSuccess && current <= eGameStateName.ExpeditionAbort || current == eGameStateName.AfterLevel)
            {
                ItemsInLevel.Clear();
            }
            if (current == eGameStateName.InLevel)
            {
                ItemsInLevel.Clear();
                foreach (var item in Object.FindObjectsOfType<global::Item>())
                {
                    if (IsValidItemSlot(item.pItemData))
                    {
                        string[] array = item.ToString().Split(' ');
                        if (!ItemsInLevel.TryAdd(array[1], item))
                        {
                            ItemsInLevel[array[1]] = item;
                        }
                    }
                }
            }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<int, string>("Pickup", "捡起物品", "捡起指定物品", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("itemName", "物品名称"), PlayerPickupItem));
            DevConsole.AddCommand(Command.Create<int>("PickupEye", "捡起目标物品", "捡起目标物品", Parameter.Create("Slot", "槽位, 1-4"), PickupItemInEyePos));
            DevConsole.AddCommand(Command.Create<string, string>("SpawnItemName", "生成物品(Name)", "在目标处生成指定物品", Parameter.Create("name", "物品名字"), Parameter.Create("mode", "生成类型, Pickup 或 Instance"), SpawnItem));
            DevConsole.AddCommand(Command.Create<uint, string>("SpawnItemID", "生成物品(ID)", "在目标处生成指定物品", Parameter.Create("ID", "物品ID"), Parameter.Create("mode", "生成类型, Pickup 或 Instance"), SpawnItem));
            DevConsole.AddCommand(Command.Create("ListItemData", "列出物品名字ID", "列出所有可生成物品的名字和ID", ListItemData));
            DevConsole.AddCommand(Command.Create<string>("SpawnMine", "生成拌雷", "生成拌雷", Parameter.Create("Type", "类型, Explosive 或 Glue"), SpawnMine));
            DevConsole.AddCommand(Command.Create<int>("ListItemsInZone", "统计地区中资源数量", "统计地区中资源数量", Parameter.Create("ZoneID", "地区ID"), ListItemsInZone));
        }

        private static void PlayerPickupItem(int slot, string itemName)
        {
            itemName = itemName.ToUpperInvariant();
            SNet_Player playerInSlot = SNet.Slots.GetPlayerInSlot(slot - 1);
            if (playerInSlot == null)
            {
                DevConsole.LogError($"不存在slot为 {slot} 的玩家");
                return;
            }
            if (!ItemsInLevel.TryGetValue(itemName, out var value))
            {
                DevConsole.LogError($"不存在名为 {itemName} 的物品");
                return;
            }

            value.Cast<ItemInLevel>().GetSyncComponent().AttemptPickupInteraction(ePickupItemInteractionType.Pickup, playerInSlot, default(pItemData_Custom), default(Vector3), default(Quaternion), null, false, true);
            DevConsole.LogSuccess($"{playerInSlot.NickName} 已捡起 {itemName}");
        }

        private static void PickupItemInEyePos(int slot)
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
                        DevConsole.LogSuccess($"{playerInSlot.NickName} 捡起了 {componentInParent.PublicName}");
                        return;
                    }
                    DevConsole.LogError($"不存在slot为 {slot} 的玩家");
                }
            }
            DevConsole.LogError("目标物品为空");
        }

        private static void SpawnItem(string itemName, string spawnmode)
        {
            itemName = itemName.ToUpperInvariant();
            if (NameIDLookup.TryGetValue(itemName, out uint value))
            {
                SpawnItem(value, spawnmode);
            }
            else
            {
                DevConsole.LogError($"不存在名为{itemName}的物品");
            }
        }

        private static void SpawnItem(uint id, string spawnmode)
        {
            spawnmode = spawnmode.ToLowerInvariant();
            if (!IDBlockLookup.TryGetValue(id, out ItemDataBlock value))
            {
                DevConsole.LogError($"不存在ID为{id}的物品");
                return;
            }
            uint persistID = id;
            InventorySlot slot = value.inventorySlot;
            float maxAmmo = value.ConsumableAmmoMax;
            if (slot == InventorySlot.ResourcePack)
            {
                maxAmmo = 100f;
            }
            ItemMode mode = spawnmode == "instance" ? ItemMode.Instance : ItemMode.Pickup;
            pItemData_Custom custom = new()
            {
                ammo = maxAmmo,
                byteId = 0,
                byteState = 0
            };
            pItemData data = new()
            {
                custom = custom,
                itemID_gearCRC = persistID,
                originCourseNode = new pCourseNode(),
                originLayer = LG_LayerType.MainLayer,
                replicatorRef = new SNetStructs.pReplicator(),
                slot = slot
            };
            ItemReplicationManager.SpawnItem(data, null, mode, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos, default, AdminUtils.LocalPlayerAgent.CourseNode, AdminUtils.LocalPlayerAgent);
        }


        private static void ListItemData()
        {
            DevConsole.Log("----------------------------------------------------------------");
            foreach (string key in NameIDLookup.Keys)
            {
                DevConsole.Log($"[{NameIDLookup[key]}] {key}");
            }
            DevConsole.Log("----------------------------------------------------------------");
        }

        private static void SpawnMine(string choice)
        {
            uint MineID = 125U;
            choice = choice.ToLowerInvariant();
            switch (choice)
            {
                case "glue":
                    MineID = 126U;
                    break;
                case "explosive":
                    MineID = 125U;
                    break;
                default:
                    MineID = 125U;
                    break;
            }
            pItemData data = new()
            {
                itemID_gearCRC = MineID
            };
            ItemReplicationManager.SpawnItem(data, null, ItemMode.Instance, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos, Quaternion.LookRotation(AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayNormal * -1f, AdminUtils.LocalPlayerAgent.Forward), AdminUtils.LocalPlayerAgent.CourseNode, AdminUtils.LocalPlayerAgent);
        }

        private static void ListItemsInZone(int zoneID)
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
            Dictionary<LG_Area, Dictionary<string, int>> resourcesInZone = new();
            Dictionary<LG_Area, Dictionary<string, int>> consumableInZone = new();
            Dictionary<string, int> value = new();
            foreach (LG_Area area in zone.m_areas)
            {
                if (!resourcesInZone.TryGetValue(area, out value))
                {
                    resourcesInZone.Add(area, value);
                }
                if (!consumableInZone.TryGetValue(area, out value))
                {
                    consumableInZone.Add(area, value);
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

                        if (!value.TryAdd(itemName, count))
                        {
                            value[itemName] += count;
                        }
                    }
                    else
                    {
                        if (!value.TryAdd(itemName, count))
                        {
                            value[itemName] += count;
                        }
                    }
                }
            }

            if (resourcesInZone.Count == 0 && consumableInZone.Count == 0)
            {
                DevConsole.LogError($"ZONE_{zoneID}中没有资源");
            }
            resourcesInZone = resourcesInZone.OrderBy(x => x.Key.m_navInfo.UID).ToDictionary(x => x.Key, x => x.Value.OrderBy(y => y.Key).ToDictionary(y => y.Key, y => y.Value));
            consumableInZone = consumableInZone.OrderBy(x => x.Key.m_navInfo.UID).ToDictionary(x => x.Key, x => x.Value.OrderBy(y => y.Key).ToDictionary(y => y.Key, y => y.Value));
            Dictionary<string, int> totalResource = new();
            Dictionary<string, int> totalConsumable = new();

            DevConsole.Log("-------------------------------------------------------------------------");
            DevConsole.Log($"                           ZONE_{zoneID} 资源统计");
            foreach (LG_Area area in resourcesInZone.Keys)
            {
                if (resourcesInZone[area].Count == 0 && consumableInZone[area].Count == 0)
                {
                    continue;
                }
                DevConsole.Log("-------------------------------------------------------------------------");
                DevConsole.Log($"{area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)}:");
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
                    DevConsole.Log($"           资源包:{itemName.FormatInLength(35)}数量:{resourcesInZone[area][itemName]}次");
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
                    DevConsole.Log($"           可消耗品:{itemName.FormatInLength(35)}数量:{consumableInZone[area][itemName]}次");
                }
            }

            DevConsole.Log("-------------------------------------------------------------------------");
            DevConsole.Log("总计:");
            if (totalResource.Count == 0 && totalConsumable.Count == 0)
            {
                DevConsole.Log("           没有资源");
            }
            else
            {
                totalResource = totalResource.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                totalConsumable = totalConsumable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                foreach (string itemName in totalResource.Keys)
                {
                    DevConsole.Log($"           资源包:{itemName.FormatInLength(35)}数量:{totalResource[itemName]}次");
                }
                foreach (string itemName in totalConsumable.Keys)
                {
                    DevConsole.Log($"           可消耗品:{itemName.FormatInLength(35)}数量:{totalConsumable[itemName]}次");
                }
            }
            DevConsole.Log("-------------------------------------------------------------------------");
        }
    }
}
