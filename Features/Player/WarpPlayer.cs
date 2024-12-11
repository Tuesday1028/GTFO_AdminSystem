using Hikaria.AdminSystem.Features.Item;
using Hikaria.AdminSystem.Suggestion;
using Hikaria.AdminSystem.Suggestion.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;
using static Hikaria.AdminSystem.Features.Item.ItemLookup;

namespace Hikaria.AdminSystem.Features.Player
{
    [HideInModSettings]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class WarpPlayer : Feature
    {
        public override string Name => "传送玩家";

        public override string Description => "传送玩家";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        private static Dictionary<string, Tuple<eDimensionIndex, Vector3, Vector3>> WarpStoresLookup = new();

        [Command("WarpToPlayer")]
        private static void WarpPlayerToPlayer([PlayerSlotIndex] int slot1, [PlayerSlotIndex] int slot2)
        {
            bool flag = AdminUtils.TryGetPlayerAgentBySlotIndex(slot1, out var playerAgent);
            bool flag2 = AdminUtils.TryGetPlayerAgentBySlotIndex(slot2, out var playerAgent2);
            if (!flag || !flag2)
                return;
            playerAgent.RequestWarpToSync(playerAgent2.DimensionIndex, playerAgent2.Position, playerAgent2.Forward, PlayerAgent.WarpOptions.All);
        }

        [Command("WarpAllToPlayer")]
        private static void WarpAllPlayersToPlayer([PlayerSlotIndex] int slot)
        {
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (player.PlayerSlotIndex != slot)
                    WarpPlayerToPlayer(player.PlayerSlotIndex, slot);
            }
        }

        [Command("WarpStorePos")]
        private static void StoreWarpPos(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            key = key.ToUpperInvariant();
            var localPlayer = AdminUtils.LocalPlayerAgent;
            WarpStoresLookup[key] = new Tuple<eDimensionIndex, Vector3, Vector3>(localPlayer.DimensionIndex, localPlayer.Position, localPlayer.Forward);
        }

        [Command("WarpToStore")]
        private static void WarpPlayerToStoredPos([PlayerSlotIndex] int slot, [WarpStoredPosition] string key)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                return;
            }
            if (WarpStoresLookup.TryGetValue(key.ToUpperInvariant(), out var pair))
            {
                playerAgent.RequestWarpToSync(pair.Item1, pair.Item2, pair.Item3, PlayerAgent.WarpOptions.All);
            }
        }

        [Command("WarpAllToStore")]
        private static void WarpAllBackToStoredPos([WarpStoredPosition] string key)
        {
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                WarpPlayerToStoredPos(player.PlayerSlotIndex, key);
            }

        }

        [Command("TeleportToEye")]
        private static void TeleportToEyePos()
        {
            AdminUtils.LocalPlayerAgent.TeleportTo(AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos);
        }

        [Command("TeleportToPlayer")]
        private static void TeleportToPlayer([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                return;
            }
            AdminUtils.LocalPlayerAgent.TeleportTo(playerAgent.Position);
        }

        [Command("WarpToEye")]
        private static void WarpPlayerToEyePos([PlayerSlotIndex] int slot)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                return;
            }
            playerAgent.RequestWarpToSync(AdminUtils.LocalPlayerAgent.DimensionIndex, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos, playerAgent.Forward, PlayerAgent.WarpOptions.All);
        }

        [Command("WarpToDimension")]
        private static void WarpPlayerToDimension([PlayerSlotIndex] int slot, eDimensionIndex dimensionIndex)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent))
            {
                return;
            }
            if (!Dimension.GetDimension(dimensionIndex, out var dimension))
            {
                return;
            }
            playerAgent.RequestWarpToSync(dimensionIndex, dimension.GetStartCourseNode().Position, playerAgent.Forward, PlayerAgent.WarpOptions.All);
        }

        [Command("WarpAllToDimension")]
        private static void WarpAllPlayerToDimension(eDimensionIndex dimensionIndex)
        {
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                WarpPlayerToDimension(player.PlayerSlotIndex, dimensionIndex);
            }
        }

        [Command("WarpToItem")]
        private static void WarpPlayerToItem([PlayerSlotIndex] int slot, [TerminalItemKey] string itemName)
        {
            if (!AdminUtils.TryGetPlayerAgentBySlotIndex(slot, out var playerAgent) || ItemLookup.ItemsInLevel.TryGetValue(itemName.ToUpperInvariant(), out var item))
            {
                ConsoleLogs.LogToConsole($"输入有误");
                return;
            }

            if (item.CourseNode == null)
            {
                ConsoleLogs.LogToConsole($"物品处于未知位置, 传送失败");
                return;
            }

            playerAgent.RequestWarpToSync(item.CourseNode.m_dimension.DimensionIndex, item.transform.position, Vector3.down, PlayerAgent.WarpOptions.All);
        }

        [Command("WarpAllToItem")]
        private static void WarpAllPlayersToItem([TerminalItemKey] string itemName)
        {
            if (ItemLookup.ItemsInLevel.TryGetValue(itemName.ToUpperInvariant(), out var item))
            {
                ConsoleLogs.LogToConsole($"不存在物品 {itemName}");
                return;
            }

            if (item.CourseNode == null)
            {
                ConsoleLogs.LogToConsole($"物品处于未知位置, 传送失败");
                return;
            }
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                player.RequestWarpToSync(item.CourseNode.m_dimension.DimensionIndex, item.transform.position, Vector3.down, PlayerAgent.WarpOptions.All);
            }
        }

        public override void OnGameStateChanged([PlayerSlotIndex] int state)
        {
            if (state == (int)eGameStateName.AfterLevel)
            {
                WarpStoresLookup.Clear();
            }
        }


        public struct WarpStoredPositionTag : IQcSuggestorTag
        {

        }

        public sealed class WarpStoredPositionAttribute : SuggestorTagAttribute
        {
            private readonly IQcSuggestorTag[] _tags = { new WarpStoredPositionTag() };

            public override IQcSuggestorTag[] GetSuggestorTags()
            {
                return _tags;
            }
        }

        public class WarpStoredPositionSuggestor : BasicQcSuggestor<string>
        {
            protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
            {
                return context.HasTag<WarpStoredPositionTag>();
            }

            protected override IQcSuggestion ItemToSuggestion(string item)
            {
                return new RawSuggestion(item.ToUpperInvariant());
            }

            protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
            {
                return WarpStoresLookup.Keys;
            }
        }
    }
}
