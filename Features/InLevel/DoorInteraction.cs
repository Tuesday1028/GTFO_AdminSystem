using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Core.Localization;

namespace Hikaria.AdminSystem.Features.InLevel
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class DoorInteraction : Feature
    {
        public override string Name => "操作门";

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        [FeatureConfig]
        public static DoorInteractionSettings Settings { get; set; }

        private static Dictionary<int, LG_SecurityDoor> SecurityDoors = new();

        private static Dictionary<int, LG_WeakDoor> WeakDoors = new();

        public class DoorInteractionSettings
        {
            [FSDisplayName("门类型")]
            public eLG_DoorType DoorType { get; set; } = eLG_DoorType.Security;

            [FSIdentifier("安全门")]
            [FSDisplayName("使用ZoneID")]
            [FSDescription("操作安全门时使用ZoneID, 否则使用门编号")]
            public bool UseZoneID { get; set; } = false;
            [FSIdentifier("安全门")]
            [FSDisplayName("安全门通往区域编号")]
            public int ZoneID { get; set; }
            [FSDisplayName("门编号")]
            [FSDescription("在地图上查看, 操作WeakDoor时必须填写该项")]
            public int DoorID { get; set; }
            [FSDisplayName("操作类型")]
            public eDoorInteractionType InteractionType { get; set; } = eDoorInteractionType.Open;

            [FSDisplayName("操作")]
            public FButton AttemptInteraction { get; set; } = new FButton("操作", "操作门");

            [Localized]
            public enum eDoorInteractionType
            {
                Open = 0,
                //SetLockedWithChainedPuzzle_Alarm = 1,
                //SetLockedWithChainedPuzzle = 2,
                //SetLockedNoKey = 3,
                //ActivateChainedPuzzle = 4,
                Unlock = 5,
                Close = 6,
                DoDamage = 7,
                //SetGluedMaxEnabled = 8,
                //SetGluedMaxDisabled = 9,
                //SetGlueLevel = 10,
                //Approach = 11
            }

            [Localized]
            public enum eLG_DoorType
            {
                Weak = 1,
                Security = 2,
                Apex = 3
            }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<int>("ToggleSecurityDoor", "开关安全门", "开启关闭安全门", Parameter.Create("ZoneID", "通往区域的ID"), ToggleSecurityDoor));
        }

        [ArchivePatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.Setup))]
        private class LG_WeakDoor__Setup__Patch
        {
            private static void Postfix(LG_WeakDoor __instance)
            {
                if (!WeakDoors.TryAdd(__instance.m_serialNumber, __instance))
                {
                    WeakDoors[__instance.m_serialNumber] = __instance;
                }
            }
        }

        [ArchivePatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.OnDestroy))]
        private class LG_WeakDoor__OnDestroy__Patch
        {
            private static void Prefix(LG_WeakDoor __instance)
            {
                WeakDoors.Remove(__instance.m_serialNumber);
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.Setup))]
        private class LG_SecurityDoor__Setup__Patch
        {
            private static void Postfix(LG_SecurityDoor __instance)
            {
                if (!SecurityDoors.TryAdd(__instance.m_serialNumber, __instance))
                {
                    SecurityDoors[__instance.m_serialNumber] = __instance;
                }
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.OnDestroy))]
        private class LG_SecurityDoor__OnDestroy__Patch
        {
            private static void Prefix(LG_SecurityDoor __instance)
            {
                SecurityDoors.Remove(__instance.m_serialNumber);
            }
        }

        public override void OnButtonPressed(ButtonSetting setting)
        {
            if (setting.ButtonID == "操作门")
            {
                if (Settings.DoorType == DoorInteractionSettings.eLG_DoorType.Security)
                {
                    LG_SecurityDoor secDoor;
                    if (Settings.UseZoneID)
                    {
                        secDoor = SecurityDoors.FirstOrDefault(p => p.Value.LinkedToZoneData != null && p.Value.LinkedToZoneData.Alias == Settings.ZoneID).Value;
                        if (secDoor == null)
                        {
                            return;
                        }
                    }
                    else if (!SecurityDoors.TryGetValue(Settings.DoorID, out secDoor))
                    {
                        return;
                    }
                    if (secDoor.LastStatus != eDoorStatus.Open && Settings.InteractionType == DoorInteractionSettings.eDoorInteractionType.Open)
                    {
                        secDoor.ForceOpenSecurityDoor();
                    }
                    else
                    {
                        secDoor.m_sync.AttemptDoorInteraction((eDoorInteractionType)Settings.InteractionType, 0f, 0f, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                    }
                }
                else if (Settings.DoorType == DoorInteractionSettings.eLG_DoorType.Weak)
                {
                    if (!WeakDoors.TryGetValue(Settings.DoorID, out var weakDoor))
                    {
                        return;
                    }
                    if (weakDoor.WeakLocks != null && weakDoor.WeakLocks.Count > 0)
                    {
                        pWeakLockInteraction unlock = new()
                        {
                            open = true,
                            type = eWeakLockInteractionType.Melt
                        };
                        foreach (var weaklock in weakDoor.WeakLocks)
                        {
                            weaklock.AttemptInteract(unlock);
                        }
                    }
                    weakDoor.m_sync.AttemptDoorInteraction((eDoorInteractionType)Settings.InteractionType, 0f, 0f, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                }
            }
        }

        private static void ToggleSecurityDoor(int toZoneID)
        {
            var pair = SecurityDoors.FirstOrDefault(p => p.Value.LinkedToZoneData.Alias == toZoneID);
            LG_SecurityDoor door = pair.Value;
            if (door == null)
            {
                DevConsole.Log($"<color=red>不存在通往</color><color=orange>ZONE_{toZoneID}</color><color=red>的安全门</color>");
                return;
            }
            if (door.LastStatus != eDoorStatus.Open)
            {
                door.ForceOpenSecurityDoor();
                DevConsole.Log($"<color=green>通往</color><color=orange>ZONE_{toZoneID}</color><color=green>的安全门已开启</color>");
            }
            else
            {
                door.m_sync.AttemptDoorInteraction(eDoorInteractionType.Close, 0f, 0f, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                DevConsole.Log($"<color=red>通往</color><color=orange>ZONE_{toZoneID}</color><color=red>的安全门已关闭</color>");
            }
        }
    }
}
