using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc;

#if false
[EnableFeatureByDefault]
public class UnpowerGenerator : Feature
{
    public override string Name => "可插拔发电机";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    private static Dictionary<>

    [ArchivePatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.Setup))]
    private class LG_PowerGenerator_Core__Setup__Patch
    {
        private static void Postfix(LG_PowerGenerator_Core __instance)
        {
            GameObject obj = new GameObject($"{__instance.name}__Interact_Remove_Cell");
            obj.layer = LayerManager.LAYER_INTERACTION;
            obj.transform.SetParent(__instance.m_powerCellInteractionTargetComp.transform.parent);

            var interact = obj.AddComponent<Interact_Timed>();
            interact.InteractionMessage = "取出{0}";
            interact.OnInteractionTriggered += new Action<PlayerAgent>((player) =>
            {
                var itemData = __instance.m_stateReplicator.State.itemDataThatChangedTheState;
                if (!PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out var item))
                {
                    return;
                }
                var itemInLevel = item.TryCast<ItemInLevel>();
                __instance.AttemptPowerCellRemove(player.Owner, itemInLevel);
            });
            interact.ExternalPlayerCanInteract += new Func<PlayerAgent, bool>((player) =>
            {
                if (!PlayerBackpackManager.TryGetBackpack(player.Owner, out var backpack) || backpack.HasBackpackItem(InventorySlot.InLevelCarry))
                {
                    return false;
                }
                return true;
            });
            interact.SetActive(false);
            __instance.OnSyncStatusChanged += new Action<ePowerGeneratorStatus>((status) =>
            {
                var itemData = __instance.m_stateReplicator.State.itemDataThatChangedTheState;
                if (!PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out var item))
                {
                    return;
                }
                var itemInLevel = item.TryCast<ItemInLevel>();

                if (status == ePowerGeneratorStatus.UnPowered)
                {
                    interact.SetActive(false);
                    itemInLevel.PickupInteraction.GetComponent<>();
                }
                else if (status == ePowerGeneratorStatus.Powered)
                {
                    interact.SetActive(true);
                }
            });
        }
    }
}
#endif