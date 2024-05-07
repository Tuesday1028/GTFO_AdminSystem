using BepInEx;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using UnityEngine;
using static Hikaria.AdminSystem.Features.Player.WarpPlayer.WarpPlayerSettings;
using static Player.PlayerAgent;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class WarpPlayer : Feature
    {
        public override string Name => "传送玩家";

        public override string Description => "传送玩家";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        public static Dictionary<string, WarpStorePosEntry> WarpStoresLookup { get; set; } = new();

        [FeatureConfig]
        public static WarpPlayerSettings Settings { get; set; }

        public class WarpPlayerSettings
        {
            [FSDisplayName("当前位置名称")]
            [FSIdentifier("保存位置")]
            public string CurrentPostionNameForStore { get; set; }
            [FSDisplayName("保存当前位置")]
            [FSIdentifier("保存位置")]
            public FButton StoreCurrentPostion { get; set; } = new FButton("保存", "保存当前位置");

            [FSIdentifier("传送设置")]
            [FSDisplayName("传送操作类型")]
            public WarpInteraction Interaction { get; set; } = WarpInteraction.WarpPlayerToPlayer;
            [FSIdentifier("传送设置")]
            [FSDisplayName("目标象限")]
            public eDimensionIndex TargetDimensionIndex { get; set; } = eDimensionIndex.Reality;
            [FSIdentifier("传送设置")]
            [FSDisplayName("被传送玩家")]
            public SlotIndex Slot1 { get; set; } = SlotIndex.Slot1;
            [FSIdentifier("传送设置")]
            [FSDisplayName("传送目标玩家")]
            public SlotIndex Slot2 { get; set; } = SlotIndex.Slot1;
            [FSDisplayName("传送目标位置名称")]
            [FSIdentifier("传送设置")]
            public string TargetPositionName { get; set; }
            [FSIdentifier("传送设置")]
            [FSDisplayName("传送")]
            public FButton StartWarp { get; set; } = new FButton("传送", "开始传送");

            [FSIdentifier("保存位置")]
            [FSDisplayName("已保存的位置")]
            [FSDescription("仅当局游戏有效")]
            public List<WarpStorePosEntry> WarpStores
            {
                get
                {
                    return WarpStoresLookup.Values.ToList();
                }
                set
                {
                }
            }

            public static WarpOptions WarpOption => WarpOptions.ShowScreenEffectForLocal | WarpOptions.PlaySounds | WarpOptions.WithoutBots;

            public class WarpStorePosEntry
            {
                public WarpStorePosEntry()
                {
                    Remove = new FButton("删除", Name, RemoveFromStorePos, true);
                }

                [FSReadOnly]
                [FSDisplayName("名称")]
                public string Name { get; set; }

                [FSDisplayName("位置")]
                [FSReadOnly]
                public Vector3 Position { get; set; }


                [FSDisplayName("象限")]
                [FSReadOnly]
                public eDimensionIndex DimensionIndex { get; set; }

                [FSReadOnly]
                [FSDisplayName("朝向")]
                public Vector3 Forward { get; set; }

                public override string ToString()
                {
                    return $"Name: {Name}, Dimension: {DimensionIndex}, Position: {Position.ToDetailedString()}, Forward: {Forward.ToDetailedString()}";
                }

                [FSDisplayName("删除")]
                public FButton Remove { get; set; }

                public void RemoveFromStorePos()
                {
                    WarpStoresLookup.Remove(Name);
                }
            }

            public enum WarpInteraction
            {
                WarpPlayerToPlayer,
                WarpAllPlayersToPlayer,
                WarpPlayerToDimension,
                WarpAllPlayerToDimension,
                WarpPlayerToStoredPosition,
                WarpAllPlayersToStoredPosition
            }

            public enum SlotIndex
            {
                Slot1 = 1,
                Slot2 = 2,
                Slot3 = 3,
                Slot4 = 4
            }
        }

        public override void Init()
        {
            DevConsole.AddParameterType((str) =>
            {
                int slot = Convert.ToInt32(str);
                return (SlotIndex)slot;
            }); 
            DevConsole.AddParameterType((str) =>
            {
                foreach (eDimensionIndex item in Enum.GetValues(typeof(eDimensionIndex)))
                {
                    if ((int)item == Convert.ToInt32(str))
                    {
                        return item;
                    }
                }
                return eDimensionIndex.Reality;
            });
            DevConsole.AddCommand(Command.Create<SlotIndex, SlotIndex>("WarpPlayerToPlayer", "传送玩家到玩家", "传送玩家到玩家", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("Slot", "槽位, 1-4"), WarpPlayerToPlayer));
            DevConsole.AddCommand(Command.Create<SlotIndex>("WarpAllPlayerToPlayer", "传送所有玩家到玩家", "传送所有玩家到玩家", Parameter.Create("Slot", "槽位, 1-4"), WarpAllPlayerToPlayer));
            DevConsole.AddCommand(Command.Create<eDimensionIndex>("WarpAllPlayerToDimension", "传送所有玩家到象限", "传送所有玩家到象限", Parameter.Create("DimensionIndex", "象限, 范围0-21"), WarpAllPlayerToDimension));
            DevConsole.AddCommand(Command.Create<SlotIndex, string>("WarpPlayerToStoredPos", "传送到存储地点", "传送到存储地点", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("StoredName", "传送地点名称"), WarpPlayerToStoredPos));
            DevConsole.AddCommand(Command.Create<SlotIndex>("WarpPlayerToEye", "传送玩家到目标位置", "传送玩家到目标位置", Parameter.Create("Slot", "槽位, 1-4"), WarpPlayerToEyePos));
            DevConsole.AddCommand(Command.Create<SlotIndex, eDimensionIndex>("WarpPlayerToDimension", "传送玩家到象限", "传送玩家到象限", Parameter.Create("Slot", "槽位, 1-4"), Parameter.Create("DimensionIndex", "象限, 范围0-21"), WarpPlayerToDimension));
            DevConsole.AddCommand(Command.Create<SlotIndex>("TeleportToPlayer", "瞬移到玩家", "瞬移到玩家", Parameter.Create("Slot", "槽位, 1-4"), TeleportToPlayer));
            DevConsole.AddCommand(Command.Create("TeleportToEye", "瞬移到目标位置", "瞬移到目标位置", TeleportToEyePos));
        }

        public override void OnButtonPressed(ButtonSetting setting)
        {
            if (setting.ButtonID == "保存当前位置")
            {
                StoreWarpPos(Settings.CurrentPostionNameForStore);
            }
            if (setting.ButtonID == "开始传送")
            {
                switch(Settings.Interaction)
                {
                    case WarpInteraction.WarpPlayerToPlayer:
                        WarpPlayerToPlayer(Settings.Slot1, Settings.Slot2);
                        break;
                    case WarpInteraction.WarpAllPlayersToPlayer:
                        WarpAllPlayerToPlayer(Settings.Slot2);
                        break;
                    case WarpInteraction.WarpPlayerToDimension:
                        WarpPlayerToDimension(Settings.Slot1, Settings.TargetDimensionIndex);
                        break;
                    case WarpInteraction.WarpAllPlayerToDimension:
                        WarpAllPlayerToDimension(Settings.TargetDimensionIndex);
                        break;
                    case WarpInteraction.WarpPlayerToStoredPosition:
                        WarpPlayerToStoredPos(Settings.Slot1, Settings.TargetPositionName);
                        break;
                    case WarpInteraction.WarpAllPlayersToStoredPosition:
                        WarpAllBackToStoredPos(Settings.TargetPositionName);
                        break;
                }
            }
        }

        private static void WarpPlayerToPlayer(SlotIndex slot1, SlotIndex slot2)
        {
            bool flag = AdminUtils.TryGetPlayerAgentFromSlotIndex((int)slot1, out PlayerAgent playerAgent);
            bool flag2 = AdminUtils.TryGetPlayerAgentFromSlotIndex((int)slot2, out PlayerAgent playerAgent2);
            if (!flag || !flag2)
            {
                return;
            }
            else
            {
                playerAgent.RequestWarpToSync(playerAgent2.DimensionIndex, playerAgent2.Position, playerAgent2.Forward, WarpOption);
            }
        }

        private static void WarpAllPlayerToPlayer(SlotIndex slot)
        {
            foreach (SlotIndex value in Enum.GetValues(typeof(SlotIndex)))
            {
                if (value == slot)
                {
                    continue;
                }
                WarpPlayerToPlayer(value, slot);
            }
        }

        private static void StoreWarpPos(string key)
        {
            if (key.IsNullOrWhiteSpace())
            {
                return;
            }
            WarpStoresLookup[key] = new WarpStorePosEntry
            {
                DimensionIndex = AdminUtils.LocalPlayerAgent.DimensionIndex,
                Position = AdminUtils.LocalPlayerAgent.Position,
                Forward = AdminUtils.LocalPlayerAgent.Forward,
                Name = key
            };
        }

        private static void WarpPlayerToStoredPos(SlotIndex slot, string key)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex((int)slot, out PlayerAgent playerAgent))
            {
                return;
            }
            if (WarpStoresLookup.TryGetValue(key, out WarpStorePosEntry value))
            {
                playerAgent.RequestWarpToSync(value.DimensionIndex, value.Position, value.Forward, WarpOption);
            }
        }

        private static void WarpAllBackToStoredPos(string key)
        {
            foreach (SlotIndex value in Enum.GetValues(typeof(SlotIndex)))
            {
                WarpPlayerToStoredPos(value, key);
            }
        }

        private static void TeleportToEyePos()
        {
            AdminUtils.LocalPlayerAgent.TeleportTo(AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos);
        }

        private static void TeleportToPlayer(SlotIndex slot)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex((int)slot, out PlayerAgent playerAgent))
            {
                return;
            }
            AdminUtils.LocalPlayerAgent.TeleportTo(playerAgent.Position);
        }

        private static void WarpPlayerToEyePos(SlotIndex slot)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex((int)slot, out PlayerAgent playerAgent))
            {
                return;
            }
            playerAgent.RequestWarpToSync(AdminUtils.LocalPlayerAgent.DimensionIndex, AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayPos, playerAgent.Forward, WarpOption);
        }

        private static void WarpPlayerToDimension(SlotIndex slot, eDimensionIndex dimensionIndex)
        {
            if (!AdminUtils.TryGetPlayerAgentFromSlotIndex((int)slot, out PlayerAgent playerAgent))
            {
                return;
            }
            Dimension.GetDimension(dimensionIndex, out Dimension dimension);
            playerAgent.RequestWarpToSync(dimensionIndex, dimension.GetStartCourseNode().Position, playerAgent.Forward, WarpOption);
        }

        private static void WarpAllPlayerToDimension(eDimensionIndex dimensionIndex)
        {
            foreach (SlotIndex value in Enum.GetValues(typeof(SlotIndex)))
            {
                WarpPlayerToDimension(value, dimensionIndex);
            }
        }

        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.AfterLevel)
            {
                Settings.WarpStores.Clear();
            }
        }
    }
}
