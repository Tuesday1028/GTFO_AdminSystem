using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ChainedPuzzles;
using GameData;
using Gear;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Utilities;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Item
{
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    public class ItemMarkerManager : Feature, IOnRecallComplete
    {
        public override string Name => "物品标记";

        public override string Description => "对游戏内主要物品进行标记";

        public override FeatureGroup Group => EntryPoint.Groups.Item;

        [FeatureConfig]
        public static ItemMarkerSettings Settings { get; set; }

        public override void OnGameStateChanged(int state)
        {
            ItemMarker.DoLoad((eGameStateName)state);
            ItemMarker.DoClear((eGameStateName)state);
        }

        public class ItemMarkerSettings
        {
            public ItemMarkerSettings()
            {
                ReloadItemMarker = new FButton("重载", "重载物品标记", () => { CoroutineManager.StartCoroutine(ItemMarker.LoadingItem(true, false).WrapToIl2Cpp()); });
            }

            [FSDisplayName("启用物品标记")]
            public bool EnableItemMarker
            {
                get
                {
                    return ItemMarker.MarkItems;
                }
                set
                {
                    ItemMarker.SetVisible(value);
                }
            }

            [FSDisplayName("重载物品标记")]
            [FSDescription("点击按钮来修正物品标记错误")]
            public FButton ReloadItemMarker { get; set; } 
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("ItemMarker", "物品标记", "物品标记", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableItemMarker;
                }

                Settings.EnableItemMarker = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 物品标记");
            }, () =>
            {
                DevConsole.LogVariable("物品标记", Settings.EnableItemMarker);
            }));

            GameEventAPI.RegisterSelf(this);
        }

        public void OnRecallComplete(eBufferType bufferType)
        {
            if (CurrentGameState == (int)eGameStateName.InLevel)
            {
                FeatureLogger.Notice("OnRecallComplete");
                CoroutineManager.StartCoroutine(ItemMarker.LoadingItem(false, false).WrapToIl2Cpp());
            }
        }

        [ArchivePatch(typeof(ItemSpawnManager), nameof(ItemSpawnManager.SpawnItem))]
        private class ItemSpawnManager__SpawnItem__Patch
        {
            private static void Postfix(global::Item __result)
            {
                var itemInLevel = __result.TryCast<ItemInLevel>();
                if (itemInLevel != null)
                    ItemMarker._AllItemInLevels.Add(itemInLevel);
            }
        }

        [ArchivePatch(typeof(LG_FunctionMarkerBuilder), nameof(LG_FunctionMarkerBuilder.SetupFunctionGO))]
        private class LG_FunctionMarkerBuilder__SetupFunctionGO__Patch
        {
            private static void Postfix(GameObject GO)
            {
                ItemMarker._AllGameObjectsToInspect.Add(GO);
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.Setup))]
        private class LG_SecurityDoor__Setup__Patch
        {
            private static void Postfix(LG_SecurityDoor __instance)
            {
                ItemMarker._AllGameObjectsToInspect.Add(__instance.gameObject);
            }
        }

        [ArchivePatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private class LG_WardenObjective_Reactor__OnBuildDone__Patch
        {
            private static void Postfix(LG_WardenObjective_Reactor __instance)
            {
                if (__instance.SpawnNode != null && __instance.m_terminal != null)
                {
                    if (!__instance.SpawnNode.m_zone.TerminalsSpawnedInZone.Contains(__instance.m_terminal))
                        __instance.SpawnNode.m_zone.TerminalsSpawnedInZone.Add(__instance.m_terminal);
                }
            }
        }

        [ArchivePatch(typeof(PlayerAgent), nameof(PlayerAgent.CourseNode), null, ArchivePatch.PatchMethodType.Setter)]
        private class PlayerAgent__CourseNode__Patch
        {
            private static void Prefix(PlayerAgent __instance)
            {
                if (__instance.Owner.IsLocal)
                {
                    lastEnteredZone = __instance.m_lastEnteredZone;
                }
            }

            private static void Postfix(PlayerAgent __instance, AIG_CourseNode value)
            {
                if (__instance.Owner.IsLocal)
                {
                    currentZone = value.m_zone;
                    if (currentZone != lastEnteredZone && lastEnteredZone != null)
                    {
                        foreach (var nodes in lastEnteredZone.m_courseNodes)
                        {
                            foreach (var item in nodes.m_itemsInNode)
                            {
                                int key = item.GetInstanceID();
                                if (ItemMarker._DynamicItemMarkers.TryGetValue(key, out ItemMarker itemMarker))
                                {
                                    itemMarker.Marker.SetVisible(false);
                                }
                            }
                        }

                        foreach (var terminal in lastEnteredZone.TerminalsSpawnedInZone)
                        {
                            int key = terminal.gameObject.GetInstanceID();
                            if (ItemMarker._DynamicItemMarkers.TryGetValue(key, out ItemMarker itemMarker))
                            {
                                itemMarker.Marker.SetVisible(false);
                            }
                        }

                        foreach (var nodes in currentZone.m_courseNodes)
                        {
                            foreach (var item in nodes.m_itemsInNode)
                            {
                                int key = item.GetInstanceID();
                                if (ItemMarker._DynamicItemMarkers.TryGetValue(key, out ItemMarker itemMarker))
                                {
                                    itemMarker.Marker.SetVisible(ItemMarker.MarkItems);
                                }
                            }
                        }

                        foreach (var terminal in currentZone.TerminalsSpawnedInZone)
                        {
                            int key = terminal.gameObject.GetInstanceID();
                            if (ItemMarker._DynamicItemMarkers.TryGetValue(key, out ItemMarker itemMarker))
                            {
                                itemMarker.Marker.SetVisible(ItemMarker.MarkItems);
                            }
                        }
                    }
                }
            }

            private static LG_Zone lastEnteredZone;

            private static LG_Zone currentZone;
        }

        private class ItemMarker
        {
            public NavMarker Marker { get; private set; }
            public float Alpha { get; private set; }

            public ItemMarker(NavMarker marker)
            {
                Marker = marker;
            }

            public void SetTitle(string text)
            {
                Marker.SetTitle(text);
            }

            public void SetTitle(iTerminalItem terminalItem, string name = "")
            {
                if (terminalItem == null || string.IsNullOrEmpty(terminalItem.TerminalItemKey))
                {
                    if (!name.IsNullOrEmptyOrWhiteSpace())
                    {
                        Marker.SetTitle(name);
                    }
                    else
                    {
                        Marker.SetTitle("<i>UNKNOWN</i>");
                    }
                }
                else
                {
                    Marker.SetTitle(terminalItem.TerminalItemKey);
                }
            }

            public void SetAlpha(float alpha)
            {
                Alpha = alpha;
                Marker.SetAlpha(Alpha);
            }

            public void SetColor(Color color)
            {
                Marker.SetColor(color);
                Marker.SetAlpha(Alpha);
            }

            public void SetColor(ColorType color)
            {
                SetColor(GetUnityColor(color));
            }


            public readonly static Dictionary<int, ItemMarker> _FixedItemMarkers = new();

            public readonly static Dictionary<int, ItemMarker> _DynamicItemMarkers = new();

            public readonly static Dictionary<int, ItemMarker> _OtherItemMarkers = new();
            public readonly static HashSet<ItemInLevel> _AllItemInLevels = new();
            public readonly static HashSet<GameObject> _AllGameObjectsToInspect = new();

            public static bool MarkItems;

            private static readonly Color _GenericColor = Color.blue; //Dark Blue
            private static readonly Color _TerminalColor = ColorExt.Hex("#42CE1F"); //Lime Green
            private static readonly Color _PickupItemColor = ColorExt.Hex("#FF2400"); //Red
            private static readonly Color _KeycardColor = ColorExt.Hex("#FF9DE7"); //Light Pink
            private static readonly Color _GeneratorPowerCellColor = ColorExt.Hex("#FFA500"); //Gold Orange
            private static readonly Color _FogTurbineColor = Color.cyan; //Cyan
            private static readonly Color _BulkheadColor = ColorExt.Hex("#92A9B7"); //Slate Blue
            private static readonly Color _ObjectiveColor = ColorExt.Hex("#7719FF"); //Purple
            private static readonly Color _EnemyColor = new(0.8235f, 0.1843f, 0.1176f); //Red-Orange

            public static ItemMarker Place(ItemInLevel item, ItemType type)
            {
                if (type == ItemType.Resource || type == ItemType.Consumable || type == ItemType.SmallPickupItems)
                {
                    NavMarker navMarker;
                    if (_DynamicItemMarkers.ContainsKey(item.GetInstanceID()))
                    {
                        navMarker = _DynamicItemMarkers[item.GetInstanceID()].Marker;
                    }
                    else
                    {
                        navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.WaypointTitle, item.gameObject, item.gameObject.name, 0, false);
                    }

                    navMarker.SetVisible(MarkItems);
                    if (AIG_CourseNode.TryGetCourseNode(Dimension.GetDimensionFromPos(item.transform.position).DimensionIndex, item.transform.position, 1f, out AIG_CourseNode node) && node.m_zone.ID != AdminUtils.LocalPlayerAgent.CourseNode.m_zone.ID || (item.CourseNode != null && item.CourseNode.m_zone.ID != AdminUtils.LocalPlayerAgent.CourseNode.m_zone.ID))
                    {
                        navMarker.SetVisible(false);
                    }
                    navMarker.SetIconScale(0.275f);
                    navMarker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;
                    navMarker.gameObject.name += "__AdminSystem";

                    var itemMarker = new ItemMarker(navMarker);
                    itemMarker.SetColor(ColorType.PickupItems);
                    itemMarker.SetAlpha(0.9f);
                    Register(item.GetInstanceID(), itemMarker, type);

                    return itemMarker;
                }
                else if (type == ItemType.InLevelCarry || type == ItemType.PickupItems)
                {
                    NavMarker navMarker;
                    if (_OtherItemMarkers.ContainsKey(item.GetInstanceID()))
                    {
                        navMarker = _OtherItemMarkers[item.GetInstanceID()].Marker;
                    }
                    else
                    {
                        navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.WaypointTitle, item.gameObject, item.gameObject.name, 0, false);
                    }

                    navMarker.SetVisible(MarkItems);
                    navMarker.SetIconScale(0.275f);
                    navMarker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;
                    navMarker.gameObject.name += "__AdminSystem";

                    var itemMarker = new ItemMarker(navMarker);
                    itemMarker.SetColor(ColorType.Generic);
                    itemMarker.SetAlpha(0.9f);
                    Register(item.GetInstanceID(), itemMarker, type);
                    return itemMarker;
                }
                else if (type == ItemType.FixedItems || type == ItemType.DoorLock)
                {
                    NavMarker navMarker;
                    if (_FixedItemMarkers.ContainsKey(item.GetInstanceID()))
                    {
                        navMarker = _FixedItemMarkers[item.GetInstanceID()].Marker;
                    }
                    else
                    {
                        navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.WaypointTitle, item.gameObject, item.gameObject.name, 0, false);
                    }

                    navMarker.SetVisible(MarkItems);
                    navMarker.SetIconScale(0.275f);
                    navMarker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;
                    navMarker.gameObject.name += "__AdminSystem";

                    var itemMarker = new ItemMarker(navMarker);
                    itemMarker.SetColor(ColorType.Generic);
                    itemMarker.SetAlpha(0.9f);
                    Register(item.GetInstanceID(), itemMarker, type);

                    return itemMarker;
                }

                return null;
            }

            public static ItemMarker Place(GameObject gameObject, ItemType type)
            {
                if (type == ItemType.FixedItems || type == ItemType.DoorLock)
                {
                    NavMarker navMarker;
                    if (_FixedItemMarkers.ContainsKey(gameObject.GetInstanceID()))
                    {
                        navMarker = _FixedItemMarkers[gameObject.GetInstanceID()].Marker;
                    }
                    else
                    {
                        navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.WaypointTitle, gameObject, gameObject.name, 0, false);
                    }

                    navMarker.SetVisible(MarkItems);
                    navMarker.SetIconScale(0.275f);
                    navMarker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;
                    navMarker.gameObject.name += "__AdminSystem";

                    var itemMarker = new ItemMarker(navMarker);
                    itemMarker.SetColor(ColorType.Generic);
                    itemMarker.SetAlpha(0.9f);
                    Register(gameObject.GetInstanceID(), itemMarker, type);

                    return itemMarker;
                }
                else if (type == ItemType.Terminal)
                {
                    NavMarker navMarker;
                    if (_DynamicItemMarkers.ContainsKey(gameObject.GetInstanceID()))
                    {
                        navMarker = _DynamicItemMarkers[gameObject.GetInstanceID()].Marker;
                    }
                    else
                    {
                        navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.WaypointTitle, gameObject, gameObject.name, 0, false);
                    }

                    navMarker.SetVisible(MarkItems);
                    navMarker.SetIconScale(0.275f);
                    navMarker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;
                    navMarker.gameObject.name += "__AdminSystem";

                    var itemMarker = new ItemMarker(navMarker);
                    itemMarker.SetColor(ColorType.Terminal);
                    itemMarker.SetAlpha(0.9f);
                    Register(gameObject.GetInstanceID(), itemMarker, type);

                    return itemMarker;
                }

                return null;
            }

            public static void Register(int id, ItemMarker marker, ItemType type)
            {
                switch (type)
                {
                    case ItemType.Resource:
                    case ItemType.Consumable:
                    case ItemType.Terminal:
                    case ItemType.SmallPickupItems:
                        _DynamicItemMarkers[id] = marker;
                        break;
                    case ItemType.FixedItems:
                    case ItemType.DoorLock:
                        _FixedItemMarkers[id] = marker;
                        break;
                    case ItemType.InLevelCarry:
                    case ItemType.PickupItems:
                        _OtherItemMarkers[id] = marker;
                        break;
                }
            }

            public static void Remove(int id, ItemType type)
            {
                if (type == ItemType.Resource || type == ItemType.Consumable || type == ItemType.Terminal || type == ItemType.SmallPickupItems)
                {
                    if (_DynamicItemMarkers.TryGetValue(id, out ItemMarker itemMarker))
                    {
                        itemMarker.Marker.SetVisible(false);
                        GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                        _DynamicItemMarkers.Remove(id);
                    }
                }
                else if (type == ItemType.FixedItems || type == ItemType.DoorLock)
                {
                    if (_FixedItemMarkers.TryGetValue(id, out ItemMarker itemMarker))
                    {
                        itemMarker.Marker.SetVisible(false);
                        GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                        _FixedItemMarkers.Remove(id);
                    }
                }
                else if (type == ItemType.InLevelCarry || type == ItemType.PickupItems)
                {
                    if (_OtherItemMarkers.TryGetValue(id, out ItemMarker itemMarker))
                    {
                        itemMarker.Marker.SetVisible(false);
                        GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                        _OtherItemMarkers.Remove(id);
                    }
                }
            }

            public static void DoLoad(eGameStateName current)
            {
                if (current == eGameStateName.InLevel)
                {
                    CoroutineManager.StartCoroutine(LoadingItem(false, true).WrapToIl2Cpp());
                }
            }

            public static void DoClear(eGameStateName current)
            {
                if (current >= eGameStateName.ExpeditionSuccess && current <= eGameStateName.ExpeditionAbort || current == eGameStateName.AfterLevel)
                {
                    foreach (var itemMarker in _FixedItemMarkers.Values)
                    {
                        itemMarker.Marker.SetVisible(false);
                        GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                        UnityEngine.Object.Destroy(itemMarker.Marker.gameObject);
                    }
                    _FixedItemMarkers.Clear();
                    foreach (var itemMarker in _DynamicItemMarkers.Values)
                    {
                        itemMarker.Marker.SetVisible(false);
                        GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                        UnityEngine.Object.Destroy(itemMarker.Marker.gameObject);
                    }
                    _DynamicItemMarkers.Clear();
                    foreach (var itemMarker in _OtherItemMarkers.Values)
                    {
                        itemMarker.Marker.SetVisible(false);
                        GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                        UnityEngine.Object.Destroy(itemMarker.Marker.gameObject);
                    }
                    _OtherItemMarkers.Clear();

                    _AllItemInLevels.Clear();
                    _AllGameObjectsToInspect.Clear();
                }
            }

            private static void RegisterItemInLevelInCourseNodes()
            {
                foreach (var zone in Builder.CurrentFloor.allZones)
                {
                    foreach (var node in zone.m_courseNodes)
                    {
                        node.m_itemsInNode.Clear();
                    }
                }

                foreach (var item in _AllItemInLevels)
                {
                    Vector3 pos = item.gameObject.transform.position;
                    bool getNode = AIG_CourseNode.TryGetCourseNode(Dimension.GetDimensionFromPos(pos).DimensionIndex, pos, 1.25f, out AIG_CourseNode node);
                    LG_GenericTerminalItem terminalItem = item.GetComponent<LG_GenericTerminalItem>();
                    if (getNode)
                    {
                        if (!node.m_itemsInNode.Contains(item))
                        {
                            node.m_itemsInNode.Add(item);
                        }
                    }
                    else if (terminalItem != null && terminalItem.SpawnNode != null)
                    {
                        terminalItem.SpawnNode.m_itemsInNode.Add(item);
                    }
                    else if (item.CourseNode != null)
                    {
                        if (!item.CourseNode.m_itemsInNode.Contains(item))
                        {
                            item.CourseNode.m_itemsInNode.Add(item);
                        }
                    }
                }
            }

            public static void InspectGameObject(GameObject go)
            {
                if (go == null) return;

                do
                {
                    var door = go.GetComponentInChildren<LG_SecurityDoor>();
                    if (door == null)
                        break;
                    if (door.m_locks == null)
                        return;
                    var doorLock = door.m_locks.TryCast<LG_SecurityDoor_Locks>();
                    if (doorLock == null)
                        return;
                    if (doorLock.m_lastStatus == eDoorStatus.Closed_LockedWithBulkheadDC)
                    {
                        var marker = Place(doorLock.m_intOpenDoor.gameObject, ItemType.DoorLock);
                        marker.SetColor(ColorType.BulkheadItems);
                        marker.SetTitle($"<color=orange>::REQ::</color> {doorLock.m_bulkheadDCNeeded.m_terminalItem.TerminalItemKey}");

                    }
                    else if (doorLock.m_lastStatus == eDoorStatus.Closed_LockedWithKeyItem)
                    {
                        var marker = Place(doorLock.m_intOpenDoor.gameObject, ItemType.DoorLock);
                        marker.SetColor(ColorType.KeycardItems);
                        marker.SetTitle($"<color=orange>::REQ::</color> {doorLock.m_gateKeyItemNeeded.keyPickupCore.m_terminalItem.TerminalItemKey}");
                    }
                    else if (doorLock.m_lastStatus == eDoorStatus.Closed_LockedWithPowerGenerator)
                    {
                        var marker = Place(doorLock.m_intOpenDoor.gameObject, ItemType.DoorLock);
                        marker.SetColor(ColorType.GeneratorItems);
                        marker.SetTitle($"<color=orange>::REQ::</color> {doorLock.m_powerGeneratorNeeded.m_terminalItem.TerminalItemKey}");
                    }
                    Action OnChainPuzzleActivate = new(delegate ()
                    {
                        Remove(doorLock.m_intOpenDoor.gameObject.GetInstanceID(), ItemType.DoorLock);
                    });
                    Action<SNet_Player> OnKeyItemSolved = new(delegate (SNet_Player player)
                    {
                        Remove(doorLock.m_intOpenDoor.gameObject.GetInstanceID(), ItemType.DoorLock);
                    });
                    if (doorLock.OnKeyItemSolved != null)
                    {
                        doorLock.OnKeyItemSolved += OnKeyItemSolved;
                    }
                    if (door.OnChainPuzzleActivate != null)
                    {
                        door.OnChainPuzzleActivate += OnChainPuzzleActivate;
                    }
                    door.m_sync.Cast<LG_Door_Sync>().OnDoorStateChange += new Action<pDoorState, bool>(delegate (pDoorState state, bool isDropinState)
                    {
                        if (state.status == eDoorStatus.ChainedPuzzleActivated || state.status == eDoorStatus.Unlocked || state.status == eDoorStatus.Opening || state.status == eDoorStatus.Open)
                        {
                            Remove(doorLock.m_intOpenDoor.gameObject.GetInstanceID(), ItemType.DoorLock);
                        }
                    });
                    return;
                }
                while (false);

                do
                {
                    var gene = go.GetComponentInChildren<LG_PowerGenerator_Core>();
                    if (gene == null)
                        break;
                    Remove(gene.gameObject.GetInstanceID(), ItemType.FixedItems);
                    if (!gene.m_powerCellInteraction.TryCast<LG_GenericCarryItemInteractionTarget>()?.isActiveAndEnabled ?? true)
                    {
                        return;
                    }
                    if (gene.m_graphics.m_gfxSlot.active || (gene.m_isWardenObjective && !gene.ObjectiveItemSolved))
                    {

                        var marker = Place(gene.gameObject, ItemType.FixedItems);
                        marker.SetColor(ColorType.GeneratorItems);
                        marker.SetTitle(gene.m_terminalItem);

                        gene.OnSyncStatusChanged += new Action<ePowerGeneratorStatus>(delegate (ePowerGeneratorStatus status)
                        {
                            if (status == ePowerGeneratorStatus.Powered)
                            {
                                Remove(gene.gameObject.GetInstanceID(), ItemType.FixedItems);
                                if (gene.LinkedSecurityDoor != null)
                                {
                                    var doorLock = gene.LinkedSecurityDoor.m_locks.TryCast<LG_SecurityDoor_Locks>();
                                    Remove(doorLock.m_intOpenDoor.gameObject.GetInstanceID(), ItemType.DoorLock);
                                }
                            }
                            else if (status == ePowerGeneratorStatus.UnPowered)
                            {
                                marker = Place(gene.gameObject, ItemType.FixedItems);
                                marker.SetColor(ColorType.GeneratorItems);
                                marker.SetTitle(gene.m_terminalItem);

                                if (gene.LinkedSecurityDoor != null)
                                {
                                    var doorLock = gene.LinkedSecurityDoor.m_locks.TryCast<LG_SecurityDoor_Locks>();
                                    marker = Place(doorLock.m_intOpenDoor.gameObject, ItemType.DoorLock);
                                    marker.SetColor(ColorType.GeneratorItems);
                                    marker.SetTitle($"<color=orange>::REQ::</color> {doorLock.m_powerGeneratorNeeded.m_terminalItem.TerminalItemKey}");
                                }
                            }
                        });
                    }
                    return;
                }
                while (false);


                do
                {
                    var hsu = go.GetComponentInChildren<LG_HSU>();
                    if (hsu == null)
                        break;
                    if (hsu.m_isWardenObjective && !hsu.ObjectiveItemSolved)
                    {
                        var marker = Place(hsu.gameObject, ItemType.FixedItems);
                        marker.SetColor(ColorType.Objective);
                        marker.SetTitle(hsu.m_terminalItem);
                        hsu.m_pickupSampleInteraction.OnInteractionTriggered += new Action<PlayerAgent>(delegate (PlayerAgent player)
                        {
                            Remove(hsu.gameObject.GetInstanceID(), ItemType.FixedItems);
                        });
                    }
                    return;
                }
                while (false);


                do
                {
                    var hsuActivator = go.GetComponentInChildren<LG_HSUActivator_Core>();
                    if (hsuActivator == null)
                        break;
                    if (hsuActivator.m_isWardenObjective && !hsuActivator.ObjectiveItemSolved)
                    {
                        var marker = Place(hsuActivator.gameObject, ItemType.FixedItems);
                        marker.SetColor(ColorType.Objective);
                        marker.SetTitle(hsuActivator.m_terminalItem);
                        hsuActivator.OnHSUInsertSequenceDone += new Action<LG_HSUActivator_Core>(delegate (LG_HSUActivator_Core hsuActivator_Core)
                        {
                            Remove(hsuActivator.gameObject.GetInstanceID(), ItemType.FixedItems);
                        });
                        hsuActivator.OnHSUInsertSequenceDone += new Action<LG_HSUActivator_Core>(delegate (LG_HSUActivator_Core hsuActivator_Core)
                        {
                            Remove(hsuActivator.gameObject.GetInstanceID(), ItemType.FixedItems);
                        });
                    }
                    return;
                }
                while (false);

                do
                {
                    var bulkDC = go.GetComponentInChildren<LG_BulkheadDoorController_Core>();
                    if (bulkDC == null)
                        break;
                    bool isAllSolved = true;
                    foreach (ChainedPuzzleInstance chainedPuzzle in bulkDC.m_bulkheadScans.Values)
                    {
                        if (!chainedPuzzle.IsSolved)
                        {
                            isAllSolved = false;
                            return;
                        }
                    }
                    if (isAllSolved)
                    {
                        Remove(bulkDC.gameObject.GetInstanceID(), ItemType.FixedItems);
                        return;
                    }

                    var marker = Place(bulkDC.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.BulkheadItems);
                    marker.SetTitle(bulkDC.m_terminalItem);
                    bulkDC.OnScanDoneCallback += new Action<LG_LayerType>(delegate (LG_LayerType layer)
                    {
                        Remove(bulkDC.m_connectedBulkheadDoors[layer].m_locks.Cast<LG_SecurityDoor_Locks>().m_intOpenDoor.gameObject.GetInstanceID(), ItemType.DoorLock);
                        foreach (ChainedPuzzleInstance chainedPuzzle in bulkDC.m_bulkheadScans.Values)
                        {
                            if (!chainedPuzzle.IsSolved)
                            {
                                return;
                            }
                        }
                        Remove(bulkDC.gameObject.GetInstanceID(), ItemType.FixedItems);
                    });
                    return;
                }
                while (false);

                do
                {
                    var terminal = go.GetComponentInChildren<LG_ComputerTerminal>();
                    if (terminal == null)
                        break;
                    var marker = Place(terminal.gameObject, ItemType.Terminal);
                    marker.SetColor(ColorType.Terminal);
                    if (terminal.m_terminalItem == null)
                    {
                        marker.SetTitle($"TERMINAL_{terminal.m_serialNumber}");
                    }
                    else
                    {
                        marker.SetTitle(terminal.m_terminalItem);
                    }

                    if (terminal.IsPasswordProtected)
                    {
                        if (terminal.m_passwordLinkerJob == null)
                        {
                            return;
                        }
                        var linkedTerminals = terminal.m_passwordLinkerJob.m_terminalsWithPasswordParts;
                        if (linkedTerminals == null)
                        {
                            return;
                        }
                        string req = string.Empty;
                        foreach (var linkedTerminal in linkedTerminals)
                        {
                            req += linkedTerminal.m_terminalItem.TerminalItemKey + '\n';
                        }
                        marker = Place(terminal.gameObject, ItemType.Terminal);
                        marker.SetColor(ColorType.Terminal);
                        marker.SetTitle($"{terminal.m_terminalItem.TerminalItemKey}\n<color=orange>::REQ::</color>\n{req}");
                    }
                    return;
                }
                while (false);

                do
                {
                    var disinfectionStation = go.GetComponentInChildren<LG_DisinfectionStation>();
                    if (disinfectionStation == null)
                        break;
                    var marker = Place(disinfectionStation.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.Generic);
                    marker.SetTitle(disinfectionStation.m_terminalItem.TerminalItemKey);
                    return;
                }
                while (false);
            }

            public static void RegisterItemInLevel(ItemInLevel itemInLevel)
            {
                do
                {
                    var resourcePackItem = itemInLevel.TryCast<ResourcePackPickup>();
                    if (resourcePackItem == null)
                        break;
                    if (resourcePackItem.ItemDataBlock == null)
                        return;
                    var marker = Place(resourcePackItem, ItemType.Resource);
                    marker.SetColor(ColorType.PickupItems);
                    marker.SetTitle(resourcePackItem.m_terminalItem);
                    resourcePackItem.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                    {
                        if (status == ePickupItemStatus.PickedUp)
                        {
                            Remove(resourcePackItem.GetInstanceID(), ItemType.Resource);
                        _retry:
                            try
                            {
                                if (resourcePackItem.CourseNode != null)
                                {
                                    foreach (var i in resourcePackItem.CourseNode.m_itemsInNode)
                                    {
                                        if (i.GetInstanceID() == resourcePackItem.GetInstanceID())
                                        {
                                            resourcePackItem.CourseNode.m_itemsInNode.Remove(i);
                                        }
                                    }
                                }
                                else
                                {
                                    if (AIG_CourseNode.TryGetCourseNode(Dimension.GetDimensionFromPos(resourcePackItem.transform.position).DimensionIndex, resourcePackItem.transform.position, 1f, out AIG_CourseNode node))
                                    {
                                        foreach (var i in node.m_itemsInNode)
                                        {
                                            if (i.GetInstanceID() == resourcePackItem.GetInstanceID())
                                            {
                                                node.m_itemsInNode.Remove(i);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                goto _retry;
                            }
                        }
                        else if (status == ePickupItemStatus.PlacedInLevel)
                        {
                            marker = Place(resourcePackItem, ItemType.Resource);
                            marker.SetColor(ColorType.PickupItems);
                            marker.SetTitle(resourcePackItem.m_terminalItem);
                        }
                    });
                    return;
                }
                while (false);

                do
                {
                    var consumable = itemInLevel.TryCast<ConsumablePickup_Core>();
                    if (consumable == null)
                        break;
                    if (consumable.ItemDataBlock == null)
                        return;
                    var marker = Place(consumable, ItemType.Consumable);
                    marker.SetColor(ColorType.PickupItems);
                    marker.SetTitle(consumable.PublicName);
                    consumable.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                    {
                        if (status == ePickupItemStatus.PickedUp)
                        {
                            Remove(consumable.GetInstanceID(), ItemType.Consumable);
                        _retry:
                            try
                            {
                                if (consumable.CourseNode != null)
                                {
                                    foreach (var i in consumable.CourseNode.m_itemsInNode)
                                    {
                                        if (i.GetInstanceID() == consumable.GetInstanceID())
                                        {
                                            consumable.CourseNode.m_itemsInNode.Remove(i);
                                        }
                                    }
                                }
                                else
                                {
                                    if (AIG_CourseNode.TryGetCourseNode(Dimension.GetDimensionFromPos(consumable.transform.position).DimensionIndex, consumable.transform.position, 1f, out AIG_CourseNode node))
                                    {
                                        foreach (var i in node.m_itemsInNode)
                                        {
                                            if (i.GetInstanceID() == consumable.GetInstanceID())
                                            {
                                                node.m_itemsInNode.Remove(i);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                goto _retry;
                            }
                        }
                        else if (status == ePickupItemStatus.PlacedInLevel)
                        {
                            marker = Place(consumable, ItemType.Consumable);
                            marker.SetColor(ColorType.PickupItems);
                            marker.SetTitle(consumable.PublicName);
                        }
                    });
                    return;
                }
                while (false);

                do
                {
                    var smallPickupItem = itemInLevel.TryCast<GenericSmallPickupItem_Core>();
                    if (smallPickupItem == null)
                        break;
                    var terminalItemName = smallPickupItem.m_terminalItem?.TerminalItemKey ?? smallPickupItem.PublicName;
                    var name = string.IsNullOrEmpty(terminalItemName) ? smallPickupItem.ArchetypeName : terminalItemName;
                    var marker = Place(smallPickupItem, ItemType.SmallPickupItems);
                    marker.SetColor(ColorType.Objective);
                    marker.SetTitle(name);
                    smallPickupItem.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                    {
                        if (status == ePickupItemStatus.PickedUp)
                        {
                            Remove(smallPickupItem.GetInstanceID(), ItemType.Consumable);
                        }
                        else if (status == ePickupItemStatus.PlacedInLevel)
                        {
                            marker = Place(smallPickupItem, ItemType.SmallPickupItems);
                            marker.SetColor(ColorType.Objective);
                            marker.SetTitle(smallPickupItem.m_terminalItem, name);
                        }
                    });
                    return;
                }
                while (false);

                do
                {
                    var carry = itemInLevel.TryCast<CarryItemPickup_Core>();
                    if (carry == null)
                        break;
                    if (carry.ItemDataBlock == null)
                        return;
                    if (carry.ItemDataBlock.persistentID == GD.Item.Carry_Generator_PowerCell)
                    {
                        if (carry.GetCustomData().byteState == 1)
                        {
                            return;
                        }
                        var marker = Place(carry, ItemType.InLevelCarry);
                        marker.SetColor(ColorType.GeneratorItems);
                        marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                        carry.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                if (carry.GetCustomData().byteState == 1 || !carry.gameObject.active)
                                {
                                    Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                                }
                                else
                                {
                                    marker = Place(carry, ItemType.InLevelCarry);
                                    marker.SetColor(ColorType.GeneratorItems);
                                    marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                                }
                            }
                        });
                    }
                    else if (carry.ItemDataBlock.persistentID == GD.Item.Carry_HeavyFogRepeller)
                    {
                        var marker = Place(carry, ItemType.InLevelCarry);
                        marker.SetColor(ColorType.FogItems);
                        marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                        carry.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                marker = Place(carry, ItemType.InLevelCarry);
                                marker.SetColor(ColorType.FogItems);
                                marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                            }
                        });
                    }
                    else
                    {
                        var marker = Place(carry, ItemType.InLevelCarry);
                        marker.SetColor(ColorType.Objective);
                        marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                        carry.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                if (carry.IsLinkedToMachine || carry.GetCustomData().byteState == 1 || !carry.m_interact.IsActive)
                                {
                                    Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                                    return;
                                }
                                marker = Place(carry, ItemType.InLevelCarry);
                                marker.SetColor(ColorType.Objective);
                                marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                            }
                        });
                    }
                    return;
                }
                while (false);

                do
                {
                    var key = itemInLevel.TryCast<KeyItemPickup_Core>();
                    if (key == null)
                        break;
                    if (key.ItemDataBlock.persistentID == GD.Item.Pickup_bulkheadKey)
                    {
                        var marker = Place(key, ItemType.PickupItems);
                        marker.SetColor(ColorType.BulkheadItems);
                        marker.SetTitle(key.m_terminalItem, key.ArchetypeName);
                        key.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(key.GetInstanceID(), ItemType.PickupItems);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                marker = Place(key, ItemType.PickupItems);
                                marker.SetColor(ColorType.BulkheadItems);
                                marker.SetTitle(key.m_terminalItem, key.ArchetypeName);
                            }
                        });
                    }
                    else
                    {
                        var marker = Place(key, ItemType.PickupItems);
                        marker.SetColor(ColorType.KeycardItems);
                        marker.SetTitle(key.m_terminalItem, key.ArchetypeName);
                        key.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>(delegate (ePickupItemStatus status, pPickupPlacement placement, PlayerAgent player, bool isRecall)
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(key.GetInstanceID(), ItemType.PickupItems);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                marker = Place(key, ItemType.PickupItems);
                                marker.SetColor(ColorType.KeycardItems);
                                marker.SetTitle(key.m_terminalItem, key.ArchetypeName);
                            }
                        });
                    }
                    return;
                }
                while (false);
            }

            public static void UpdateVisiableForItemMarkers()
            {
                foreach (var marker in _DynamicItemMarkers.Values)
                {
                    marker.Marker.SetVisible(false);
                }
                foreach (var nodes in AdminUtils.LocalPlayerAgent.CourseNode.m_zone.m_courseNodes)
                {
                    foreach (var item in nodes.m_itemsInNode)
                    {
                        int key = item.GetInstanceID();
                        if (_DynamicItemMarkers.TryGetValue(key, out ItemMarker value))
                        {
                            value.Marker.SetVisible(MarkItems);
                        }
                    }
                }
                foreach (var terminal in AdminUtils.LocalPlayerAgent.CourseNode.m_zone.TerminalsSpawnedInZone)
                {
                    int key = terminal.gameObject.GetInstanceID();
                    if (_DynamicItemMarkers.TryGetValue(key, out ItemMarker value))
                    {
                        value.Marker.SetVisible(MarkItems);
                    }
                }
            }

            public static IEnumerator LoadingItem(bool forceLoad = false, bool registerInNodes = false)
            {
                var yielder = new WaitForFixedUpdate();//WaitForSecondsRealtime(3f);
                if (!SNet.IsMaster && !forceLoad)
                {
                    yield return yielder;
                }

                if (registerInNodes)
                {
                    RegisterItemInLevelInCourseNodes();
                }

                foreach (var item in _FixedItemMarkers)
                {
                    Remove(item.Key, ItemType.FixedItems);
                }
                foreach (var item in _DynamicItemMarkers)
                {
                    Remove(item.Key, ItemType.Resource);
                }
                foreach (var item in _OtherItemMarkers)
                {
                    Remove(item.Key, ItemType.InLevelCarry);
                }

                /*
                var navMarkers = UnityEngine.Object.FindObjectsOfType<NavMarker>();
                foreach (var navMarker in navMarkers)
                {
                    if (navMarker.name.Contains("AdminSystem"))
                    {
                        UnityEngine.Object.Destroy(navMarker.gameObject);
                    }
                }
                */

                foreach (var item in _AllItemInLevels)
                {
                    RegisterItemInLevel(item);
                }
                foreach (var go in _AllGameObjectsToInspect)
                {
                    InspectGameObject(go);
                }


                UpdateVisiableForItemMarkers();
            }

            public static void SetVisible(bool active)
            {
                MarkItems = active;
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                foreach (var marker in _FixedItemMarkers.Values)
                {
                    marker.Marker.SetVisible(active);
                }
                foreach (var marker in _OtherItemMarkers.Values)
                {
                    marker.Marker.SetVisible(active);
                }
                foreach (var marker in _DynamicItemMarkers.Values)
                {
                    marker.Marker.SetVisible(false);
                }
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                foreach (var nodes in AdminUtils.LocalPlayerAgent.CourseNode.m_zone.m_courseNodes)
                {
                    foreach (var item in nodes.m_itemsInNode)
                    {
                        int id = item.GetInstanceID();
                        if (_DynamicItemMarkers.TryGetValue(id, out ItemMarker itemMarker))
                        {
                            itemMarker.Marker.SetVisible(active);
                        }
                    }
                }
                foreach (var terminal in AdminUtils.LocalPlayerAgent.CourseNode.m_zone.TerminalsSpawnedInZone)
                {
                    int key = terminal.gameObject.GetInstanceID();
                    if (_DynamicItemMarkers.TryGetValue(key, out ItemMarker itemMarker))
                    {
                        itemMarker.Marker.SetVisible(active);
                    }
                }
            }

            public static Color GetUnityColor(ColorType markerColor)
            {
                switch (markerColor)
                {
                    case ColorType.Generic:
                        return _GenericColor;
                    case ColorType.Terminal:
                        return _TerminalColor;
                    case ColorType.PickupItems:
                        return _PickupItemColor;
                    case ColorType.GeneratorItems:
                        return _GeneratorPowerCellColor;
                    case ColorType.FogItems:
                        return _FogTurbineColor;
                    case ColorType.BulkheadItems:
                        return _BulkheadColor;
                    case ColorType.KeycardItems:
                        return _KeycardColor;
                    case ColorType.Objective:
                        return _ObjectiveColor;
                    case ColorType.Enemy:
                        return _EnemyColor;
                    default:
                        return _GenericColor;
                }
            }

            public enum ColorType
            {
                Generic,
                Terminal,
                PickupItems,
                GeneratorItems,
                FogItems,
                BulkheadItems,
                KeycardItems,
                Objective,
                Enemy
            }

            public enum ItemType
            {
                FixedItems,
                Terminal,
                PickupItems,
                SmallPickupItems,
                Resource,
                Consumable,
                InLevelCarry,
                DoorLock
            }
        }
    }
}
