using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ChainedPuzzles;
using GameData;
using Gear;
using Hikaria.AdminSystem.Utility;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Interfaces;
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

        public static new IArchiveLogger FeatureLogger { get; set; }

        [FeatureConfig]
        public static ItemMarkerSettings Settings { get; set; }

        public class ItemMarkerSettings
        {
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

            LG_Factory.OnFactoryBuildDone += new Action(OnFactoryBuildDone);
        }

        private void OnFactoryBuildDone()
        {
            ItemMarker.SearchGameObjects();
        }

        public override void OnGameStateChanged(int state)
        {
            var currentState = (eGameStateName)state;
            if (currentState == eGameStateName.InLevel)
            {
                if (!SNet.LocalPlayer.IsOutOfSync)
                {
                    ItemMarker.DoEnterLevelOnceLoad();
                }
            }
            if (currentState == eGameStateName.AfterLevel || currentState == eGameStateName.Lobby)
            {
                ItemMarker.DoAfterLevelClear();
            }
        }

        public void OnRecallComplete(eBufferType bufferType)
        {
            if (CurrentGameState < (int)eGameStateName.InLevel)
                return;
            CoroutineManager.StartCoroutine(ItemMarker.LoadingItem(SNet.IsMaster ? 0f : 3f, !SNet.IsMaster).WrapToIl2Cpp());
            //if (bufferType == eBufferType.DropIn || bufferType == eBufferType.Checkpoint || bufferType == eBufferType.Migration_B)
            //{
            //    ItemMarker.SearchGameObjects();
            //    CoroutineManager.StartCoroutine(ItemMarker.LoadingItem().WrapToIl2Cpp());
            //}
        }

        [ArchivePatch(typeof(ItemInLevel), nameof(ItemInLevel.OnDespawn))]
        private class ItemInLevel__OnDespawn__Patch
        {
            private static void Prefix(ItemInLevel __instance)
            {
                ItemMarker.Remove(__instance.GetInstanceID(), ItemMarker.ItemType.Resource);
                ItemMarker.Remove(__instance.GetInstanceID(), ItemMarker.ItemType.FixedItems);
                ItemMarker.Remove(__instance.GetInstanceID(), ItemMarker.ItemType.InLevelCarry);
                ItemMarker._AllItemInLevels.Remove(__instance);
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
        private class PlayerAgent__set_CourseNode__Patch
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

            public void SetTitle(iTerminalItem terminalItem, string fallbackTitile = "")
            {
                if (terminalItem == null || string.IsNullOrEmpty(terminalItem.TerminalItemKey))
                {
                    if (!string.IsNullOrEmpty(fallbackTitile))
                    {
                        Marker.SetTitle(fallbackTitile);
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

            public static void SearchGameObjects()
            {
                _AllItemInLevels.Clear();
                _AllGameObjectsToInspect.Clear();

                foreach (var itemInLevel in UnityEngine.Object.FindObjectsOfType<ItemInLevel>())
                {
                    _AllItemInLevels.Add(itemInLevel);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<LG_ComputerTerminal>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<LG_HSU>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<LG_HSUActivator_Core>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<LG_SecurityDoor>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<LG_BulkheadDoorController_Core>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<LG_DisinfectionStation>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
                foreach (var obj in UnityEngine.Object.FindObjectsOfType <LG_PowerGenerator_Core>())
                {
                    _AllGameObjectsToInspect.Add(obj.gameObject);
                }
            }


            public readonly static Dictionary<int, ItemMarker> _FixedItemMarkers = new();
            public readonly static Dictionary<int, ItemMarker> _DynamicItemMarkers = new();
            public readonly static Dictionary<int, ItemMarker> _OtherItemMarkers = new();

            public static HashSet<ItemInLevel> _AllItemInLevels = new();
            public static HashSet<GameObject> _AllGameObjectsToInspect = new();

            public static bool MarkItems;
            private static bool IsFirstLoadPerLevel = true;

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
                    if (!item.internalSync.GetCurrentState().placement.node.TryGet(out var node) || node.m_zone.ID != AdminUtils.LocalPlayerAgent.CourseNode.m_zone.ID)
                    {
                        navMarker.SetVisible(false);
                    }
                    navMarker.SetIconScale(0.275f);
                    navMarker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;

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
                    default:
                        _OtherItemMarkers[id] = marker;
                        break;
                }
            }

            public static void Remove(int id, ItemType type)
            {
                ItemMarker itemMarker;
                switch (type)
                {
                    case ItemType.Resource:
                    case ItemType.Consumable:
                    case ItemType.Terminal:
                    case ItemType.SmallPickupItems:
                        if (_DynamicItemMarkers.TryGetValue(id, out itemMarker))
                        {
                            itemMarker.Marker.SetVisible(false);
                            GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                            _DynamicItemMarkers.Remove(id);
                        }
                        break;
                    case ItemType.FixedItems:
                    case ItemType.DoorLock:
                        if (_FixedItemMarkers.TryGetValue(id, out itemMarker))
                        {
                            itemMarker.Marker.SetVisible(false);
                            GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                            _FixedItemMarkers.Remove(id);
                        }
                        break;
                    case ItemType.InLevelCarry:
                    case ItemType.PickupItems:
                    default:
                        if (_OtherItemMarkers.TryGetValue(id, out itemMarker))
                        {
                            itemMarker.Marker.SetVisible(false);
                            GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                            _OtherItemMarkers.Remove(id);
                        }
                        break;
                }
            }

            public static void DoEnterLevelOnceLoad()
            {
                if (IsFirstLoadPerLevel)
                {
                    CoroutineManager.StartCoroutine(LoadingItem().WrapToIl2Cpp());
                }
            }

            private static void CleanupItemMarkers()
            {
                foreach (var itemMarker in _FixedItemMarkers.Values)
                {
                    if (itemMarker?.Marker == null) continue;
                    itemMarker.Marker.SetVisible(false);
                    GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                }
                _FixedItemMarkers.Clear();
                foreach (var itemMarker in _DynamicItemMarkers.Values)
                {
                    if (itemMarker?.Marker == null) continue;
                    itemMarker.Marker.SetVisible(false);
                    GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                }
                _DynamicItemMarkers.Clear();
                foreach (var itemMarker in _OtherItemMarkers.Values)
                {
                    if (itemMarker?.Marker == null) continue;
                    itemMarker.Marker.SetVisible(false);
                    GuiManager.NavMarkerLayer.RemoveMarker(itemMarker.Marker);
                }
                _OtherItemMarkers.Clear();
            }

            public static void DoAfterLevelClear()
            {
                CleanupItemMarkers();

                _AllItemInLevels.Clear();
                _AllGameObjectsToInspect.Clear();

                IsFirstLoadPerLevel = true;
            }

            private static void RegisterItemInLevelInCourseNodesOncePerLevel()
            {
                if (!IsFirstLoadPerLevel)
                {
                    return;
                }
                foreach (var item in _AllItemInLevels.ToList())
                {
                    SetCourseNodeForItemInLevel(item);
                    if (item.CourseNode == null)
                    {
                        FeatureLogger.Error($"'{item.name}' CourseNode is null, remove!");
                        _AllItemInLevels.Remove(item);
                        continue;
                    }
                    if (!item.CourseNode.m_itemsInNode.Contains(item))
                    {
                        item.CourseNode.m_itemsInNode.Add(item);
                    }
                }
            }

            private static void SetCourseNodeForItemInLevel(ItemInLevel item)
            {
                if (item.CourseNode != null)
                {
                    return;
                }
                if (item.internalSync != null)
                {
                    var state = item.internalSync.GetCurrentState();
                    if (state.placement.node.TryGet(out var node) && state.status != ePickupItemStatus.PickedUp)
                    {
                        item.CourseNode = node;
                        return;
                    }
                }
                if (item.container != null)
                {
                    item.CourseNode = item.container.m_core.SpawnNode;
                    return;
                }
                var pickupItem = item.transform.parent.parent.GetComponentInChildren<LG_PickupItem>();
                if (pickupItem != null)
                {
                    item.CourseNode = pickupItem.SpawnNode;
                    return;
                }
            }

            public static void InspectGameObject(GameObject go)
            {
                if (go == null) return;
                /*
                var portals = go.GetComponentsInChildren<LG_DimensionPortal>();
                foreach (var portal in portals)
                {
                    var marker = Place(portal.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.Objective);
                    marker.SetTitle(portal.m_terminalItem);
                    portal.OnPortalKeyInsertSequenceDone += new Action<LG_DimensionPortal>((portal) =>
                    {
                        Remove(portal.gameObject.GetInstanceID(), ItemType.FixedItems);
                    });
                    if (portal.ObjectiveItemSolved || portal.m_stateReplicator.State.status == eDimensionPortalStatus.InsertDone)
                    {
                        Remove(portal.gameObject.GetInstanceID(), ItemType.FixedItems);
                    }
                }
                */

                var genes = go.GetComponentsInChildren<LG_PowerGenerator_Core>();
                foreach (var gene in genes)
                {
                    var marker = Place(gene.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.GeneratorItems);
                    marker.SetTitle(gene.m_terminalItem);
                    if (IsFirstLoadPerLevel)
                    {
                        gene.OnSyncStatusChanged += new Action<ePowerGeneratorStatus>((status) =>
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
                    if ((gene.ObjectiveItemSolved || !gene.m_isWardenObjective) && !gene.m_graphics.m_gfxSlot.active)
                    {
                        Remove(gene.gameObject.GetInstanceID(), ItemType.FixedItems);
                    }
                }


                var hsus = go.GetComponentsInChildren<LG_HSU>();
                foreach (var hsu in hsus)
                {
                    var marker = Place(hsu.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.Objective);
                    marker.SetTitle(hsu.m_terminalItem);
                    if (IsFirstLoadPerLevel)
                    {
                        hsu.m_pickupSampleInteraction.OnInteractionTriggered += new Action<PlayerAgent>((player) =>
                        {
                            Remove(hsu.gameObject.GetInstanceID(), ItemType.FixedItems);
                        });
                    }
                    if (!hsu.m_isWardenObjective || hsu.ObjectiveItemSolved)
                    {
                        Remove(hsu.gameObject.GetInstanceID(), ItemType.FixedItems);
                    }
                }

                var hsuActivators = go.GetComponentsInChildren<LG_HSUActivator_Core>();
                foreach (var hsuActivator in hsuActivators)
                {
                    var marker = Place(hsuActivator.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.Objective);
                    marker.SetTitle(hsuActivator.m_terminalItem);
                    if (IsFirstLoadPerLevel)
                    {
                        hsuActivator.OnHSUInsertSequenceDone += new Action<LG_HSUActivator_Core>((hsuActivator_Core) =>
                        {
                            var hsuIn = hsuActivator.LinkedItemGoingIn;
                            if (hsuIn != null)
                            {
                                Remove(hsuIn.GetInstanceID(), ItemType.InLevelCarry);
                            }
                            Remove(hsuActivator.gameObject.GetInstanceID(), ItemType.FixedItems);
                        });
                        hsuActivator.OnHSUExitSequence += new Action<LG_HSUActivator_Core>((hsuActivator_Core) =>
                        {
                            var hsuOut = hsuActivator.LinkedItemComingOut;
                            if (hsuOut != null)
                            {
                                var marker = Place(hsuOut, ItemType.InLevelCarry);
                                marker.SetColor(ColorType.Objective);
                                marker.SetTitle(hsuOut.m_terminalItem, hsuOut.ArchetypeName);
                            }
                        });
                    }
                    if ((!hsuActivator.m_isWardenObjective || hsuActivator.ObjectiveItemSolved) &&
                        (hsuActivator.m_stateReplicator == null || hsuActivator.m_stateReplicator.State.status == eHSUActivatorStatus.ExtractionDone))
                    {
                        Remove(hsuActivator.gameObject.GetInstanceID(), ItemType.FixedItems);
                    }
                }


                var doors = go.GetComponentsInChildren<LG_SecurityDoor>();
                foreach (var door in doors)
                {
                    var doorLock = door.m_locks?.TryCast<LG_SecurityDoor_Locks>();
                    if (doorLock == null)
                        continue;
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
                    if (IsFirstLoadPerLevel)
                    {
                        door.m_sync.Cast<LG_Door_Sync>().OnDoorStateChange += new Action<pDoorState, bool>((state, isDropinState) =>
                        {
                            switch (state.status)
                            {
                                case eDoorStatus.ChainedPuzzleActivated:
                                case eDoorStatus.Unlocked:
                                case eDoorStatus.Opening:
                                case eDoorStatus.Open:
                                case eDoorStatus.Closed:
                                case eDoorStatus.Closed_LockedWithChainedPuzzle:
                                case eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm:
                                    Remove(doorLock.m_intOpenDoor.gameObject.GetInstanceID(), ItemType.DoorLock);
                                    break;
                            }
                        });
                    }
                }

                var bulkDCs = go.GetComponentsInChildren<LG_BulkheadDoorController_Core>();
                foreach (var bulkDC in bulkDCs)
                {
                    var marker = Place(bulkDC.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.BulkheadItems);
                    marker.SetTitle(bulkDC.m_terminalItem);
                    if (IsFirstLoadPerLevel)
                    {
                        bulkDC.OnScanDoneCallback += new Action<LG_LayerType>((LG_LayerType layer) =>
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
                    }
                    bool isAllSolved = true;
                    foreach (ChainedPuzzleInstance chainedPuzzle in bulkDC.m_bulkheadScans.Values)
                    {
                        if (!chainedPuzzle.IsSolved)
                        {
                            isAllSolved = false;
                            break;
                        }
                    }
                    if (isAllSolved)
                    {
                        Remove(bulkDC.gameObject.GetInstanceID(), ItemType.FixedItems);
                    }
                }

                var terminals = go.GetComponentsInChildren<LG_ComputerTerminal>();
                foreach (var terminal in terminals)
                {
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
                        var linkedTerminals = terminal.m_passwordLinkerJob?.m_terminalsWithPasswordParts;
                        if (linkedTerminals == null)
                        {
                            continue;
                        }
                        string req = string.Empty;
                        foreach (var linkedTerminal in linkedTerminals)
                        {
                            req += linkedTerminal.m_terminalItem.TerminalItemKey + '\n';
                        }
                        marker.SetTitle($"{terminal.m_terminalItem.TerminalItemKey}\n<color=orange>::REQ::</color>\n{req}");
                    }
                }

                var disinfectionStations = go.GetComponentsInChildren<LG_DisinfectionStation>();
                foreach (var disinfectionStation in disinfectionStations)
                {
                    var marker = Place(disinfectionStation.gameObject, ItemType.FixedItems);
                    marker.SetColor(ColorType.Generic);
                    marker.SetTitle(disinfectionStation.m_terminalItem.TerminalItemKey);
                }
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
                    if (IsFirstLoadPerLevel)
                    {
                        resourcePackItem.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(resourcePackItem.GetInstanceID(), ItemType.Resource);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                marker = Place(resourcePackItem, ItemType.Resource);
                                marker.SetColor(ColorType.PickupItems);
                                marker.SetTitle(resourcePackItem.m_terminalItem);
                            }
                        });
                    }
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
                    if (IsFirstLoadPerLevel)
                    {
                        consumable.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
                        {
                            if (status == ePickupItemStatus.PickedUp)
                            {
                                Remove(consumable.GetInstanceID(), ItemType.Consumable);
                            }
                            else if (status == ePickupItemStatus.PlacedInLevel)
                            {
                                marker = Place(consumable, ItemType.Consumable);
                                marker.SetColor(ColorType.PickupItems);
                                marker.SetTitle(consumable.PublicName);
                            }
                        });
                    }
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
                    if (IsFirstLoadPerLevel)
                    {
                        smallPickupItem.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
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
                    }
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
                        var marker = Place(carry, ItemType.InLevelCarry);
                        marker.SetColor(ColorType.GeneratorItems);
                        marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                        if (IsFirstLoadPerLevel)
                        {
                            carry.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
                            {
                                if (status == ePickupItemStatus.PickedUp)
                                {
                                    Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                                }
                                else if (status == ePickupItemStatus.PlacedInLevel)
                                {
                                    if (carry.GetCustomData().byteState != 0 || !carry.gameObject.active)
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
                        if (carry.IsLinkedToMachine || carry.GetCustomData().byteState != 0 || !carry.m_interact.IsActive)
                        {
                            Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                        }
                    }
                    else if (carry.ItemDataBlock.persistentID == GD.Item.Carry_HeavyFogRepeller)
                    {
                        var marker = Place(carry, ItemType.InLevelCarry);
                        marker.SetColor(ColorType.FogItems);
                        marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                        if (IsFirstLoadPerLevel)
                        {
                            carry.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
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
                        if (carry.IsLinkedToMachine || carry.GetCustomData().byteState != 0 || !carry.m_interact.IsActive)
                        {
                            Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                        }
                    }
                    else
                    {
                        var marker = Place(carry, ItemType.InLevelCarry);
                        marker.SetColor(ColorType.Objective);
                        marker.SetTitle(carry.m_terminalItem, carry.ArchetypeName);
                        if (IsFirstLoadPerLevel)
                        {
                            carry.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
                            {
                                if (status == ePickupItemStatus.PickedUp)
                                {
                                    Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                                }
                                else if (status == ePickupItemStatus.PlacedInLevel)
                                {
                                    if (carry.IsLinkedToMachine || carry.GetCustomData().byteState != 0 || !carry.m_interact.IsActive)
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
                        if (carry.IsLinkedToMachine || carry.GetCustomData().byteState != 0 || !carry.m_interact.IsActive)
                        {
                            Remove(carry.GetInstanceID(), ItemType.InLevelCarry);
                        }
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
                        if (IsFirstLoadPerLevel)
                        {
                            key.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
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
                    }
                    else
                    {
                        var marker = Place(key, ItemType.PickupItems);
                        marker.SetColor(ColorType.KeycardItems);
                        marker.SetTitle(key.m_terminalItem, key.ArchetypeName);
                        if (IsFirstLoadPerLevel)
                        {
                            key.GetSyncComponent().Cast<LG_PickupItem_Sync>().OnSyncStateChange += new Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, player, isRecall) =>
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
                    }
                    return;
                }
                while (false);
            }

            public static void UpdateDynamicItemMarkersVisiable()
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

            public static IEnumerator LoadingItem(float delay = 0f, bool isRecall = false)
            {
                /*
                foreach (var key in _FixedItemMarkers.Keys.ToList())
                {
                    Remove(key, ItemType.FixedItems);
                }
                foreach (var key in _DynamicItemMarkers.Keys.ToList())
                {
                    Remove(key, ItemType.Resource);
                }
                foreach (var key in _OtherItemMarkers.Keys.ToList())
                {
                    Remove(key, ItemType.InLevelCarry);
                }
                */

                if (delay > 0f)
                    yield return new WaitForSecondsRealtime(delay);

                if (isRecall)
                    SearchGameObjects();

                if (SNet.LocalPlayer.IsOutOfSync)
                    yield break;

                var yielder = new WaitForFixedUpdate();

                yield return yielder;

                while (!SNet.LocalPlayer.HasPlayerAgent)
                {
                    yield return yielder;
                }

                CleanupItemMarkers();
                
                RegisterItemInLevelInCourseNodesOncePerLevel();

                foreach (var go in _AllGameObjectsToInspect)
                {
                    InspectGameObject(go);
                }
                foreach (var itemInLevel in _AllItemInLevels)
                {
                    RegisterItemInLevel(itemInLevel);
                }

                UpdateDynamicItemMarkersVisiable();
                IsFirstLoadPerLevel = false;
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
