using Clonesoft.Json;
using Hikaria.AdminSystem.Utility;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
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
            [JsonIgnore]
            [FSDisplayName("门类型")]
            public eLG_DoorType DoorType { get; set; } = eLG_DoorType.Security;
            [JsonIgnore]
            [FSIdentifier("安全门")]
            [FSDisplayName("使用ZoneID")]
            [FSDescription("操作安全门时使用ZoneID, 否则使用门编号")]
            public bool UseZoneID { get; set; } = false;
            [JsonIgnore]
            [FSIdentifier("安全门")]
            [FSDisplayName("安全门通往区域编号")]
            public int ZoneID { get; set; }
            [JsonIgnore]
            [FSDisplayName("门编号")]
            [FSDescription("在地图上查看, 操作WeakDoor时必须填写该项")]
            public int DoorID { get; set; }
            [JsonIgnore]
            private LevelGeneration.eDoorInteractionType _interactionType = LevelGeneration.eDoorInteractionType.Open;
            [JsonIgnore]
            [FSDisplayName("操作类型")]
            public eDoorInteractionType InteractionType
            {
                get
                {
                    return _interactionType switch
                    {
                        LevelGeneration.eDoorInteractionType.Unlock => eDoorInteractionType.Unlock,
                        LevelGeneration.eDoorInteractionType.Close => eDoorInteractionType.Close,
                        LevelGeneration.eDoorInteractionType.Open => eDoorInteractionType.Open,
                        LevelGeneration.eDoorInteractionType.DoDamage => eDoorInteractionType.DoDamage
                    };
                }
                set
                {
                    _interactionType = value switch
                    {
                        eDoorInteractionType.Unlock => LevelGeneration.eDoorInteractionType.Unlock,
                        eDoorInteractionType.Close => LevelGeneration.eDoorInteractionType.Close,
                        eDoorInteractionType.Open => LevelGeneration.eDoorInteractionType.Open,
                        eDoorInteractionType.DoDamage => LevelGeneration.eDoorInteractionType.DoDamage
                    };
                    if (DoorType == eLG_DoorType.Security)
                    {
                        LG_SecurityDoor secDoor;
                        if (UseZoneID)
                        {
                            secDoor = SecurityDoors.FirstOrDefault(p => p.Value.LinkedToZoneData != null && p.Value.LinkedToZoneData.Alias == Settings.ZoneID).Value;
                            if (secDoor == null)
                            {
                                return;
                            }
                        }
                        else if (!SecurityDoors.TryGetValue(DoorID, out secDoor))
                        {
                            return;
                        }
                        if (secDoor.LastStatus != eDoorStatus.Open && value == eDoorInteractionType.Open)
                        {
                            secDoor.ForceOpenSecurityDoor();
                        }
                        else
                        {
                            secDoor.m_sync.AttemptDoorInteraction(_interactionType, 0f, 0f, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                        }
                    }
                    else if (DoorType == eLG_DoorType.Weak)
                    {
                        if (!WeakDoors.TryGetValue(DoorID, out var weakDoor))
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
                        weakDoor.m_sync.AttemptDoorInteraction(_interactionType, float.MaxValue, 0f, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                    }
                }
            }

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
                //Apex = 3
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
