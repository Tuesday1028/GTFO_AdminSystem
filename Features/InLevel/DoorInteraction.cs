using Hikaria.AdminSystem.Suggestions.Suggestors.Attributes;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using LevelGeneration;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.InLevel
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class DoorInteraction : Feature
    {
        public override string Name => "操作门";

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        private static Dictionary<int, LG_SecurityDoor> SecurityDoorsInLevel = new();

        private static Dictionary<int, LG_WeakDoor> WeakDoorsInLevel = new();

        [ArchivePatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.Setup))]
        private class LG_WeakDoor__Setup__Patch
        {
            private static void Postfix(LG_WeakDoor __instance)
            {
                if (!WeakDoorsInLevel.TryAdd(__instance.m_serialNumber, __instance))
                {
                    WeakDoorsInLevel[__instance.m_serialNumber] = __instance;
                }
            }
        }

        [ArchivePatch(typeof(LG_WeakDoor), nameof(LG_WeakDoor.OnDestroy))]
        private class LG_WeakDoor__OnDestroy__Patch
        {
            private static void Prefix(LG_WeakDoor __instance)
            {
                WeakDoorsInLevel.Remove(__instance.m_serialNumber);
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.Setup))]
        private class LG_SecurityDoor__Setup__Patch
        {
            private static void Postfix(LG_SecurityDoor __instance)
            {
                if (!SecurityDoorsInLevel.TryAdd(__instance.m_serialNumber, __instance))
                {
                    SecurityDoorsInLevel[__instance.m_serialNumber] = __instance;
                }
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.OnDestroy))]
        private class LG_SecurityDoor__OnDestroy__Patch
        {
            private static void Prefix(LG_SecurityDoor __instance)
            {
                SecurityDoorsInLevel.Remove(__instance.m_serialNumber);
            }
        }

        [Command("InteractWeakDoor")]
        private static void WeakDoorInteraction([WeakDoorInLevel] int id, eDoorInteractionType interactionType = eDoorInteractionType.Open)
        {
            if (WeakDoorsInLevel.TryGetValue(id, out var door))
            {
                door.m_sync.AttemptDoorInteraction(interactionType, float.MaxValue, float.MaxValue, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                ConsoleLogs.LogToConsole($"WeakDoor_{id} {interactionType}");
            }
        }

        [Command("InteractSecurityDoor")]
        private static void SecurityDoorInteraction([SecurityDoorInLevel] int id, eDoorInteractionType interactionType = eDoorInteractionType.Open)
        {
            if (SecurityDoorsInLevel.TryGetValue(id, out var door))
            {
                door.m_sync.AttemptDoorInteraction(interactionType, float.MaxValue, float.MaxValue, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                ConsoleLogs.LogToConsole($"SecurityDoor_{id} {interactionType}");
            }
        }

        [Command("OperateSecurityDoor")]
        private static void SecurityDoorInteraction([ZoneAlias] int alias)
        {
            var pair = SecurityDoorsInLevel.FirstOrDefault(p => p.Value.LinkedToZoneData.Alias == alias);
            LG_SecurityDoor door = pair.Value;
            if (door == null)
            {
                ConsoleLogs.LogToConsole($"<color=red>不存在通往</color><color=orange>ZONE_{alias}</color><color=red>的安全门</color>");
                return;
            }
            if (door.LastStatus != eDoorStatus.Open)
            {
                door.ForceOpenSecurityDoor();
                ConsoleLogs.LogToConsole($"<color=green>通往</color><color=orange>ZONE_{alias}</color><color=green>的安全门已开启</color>");
            }
            else
            {
                door.m_sync.AttemptDoorInteraction(eDoorInteractionType.Close, 0f, 0f, AdminUtils.LocalPlayerAgent.Position, AdminUtils.LocalPlayerAgent);
                ConsoleLogs.LogToConsole($"<color=red>通往</color><color=orange>ZONE_{alias}</color><color=red>的安全门已关闭</color>");
            }
        }

        public struct WeakDoorInLevelTag : IQcSuggestorTag
        {

        }

        public sealed class WeakDoorInLevelAttribute : SuggestorTagAttribute
        {
            private readonly IQcSuggestorTag[] _tags = { new WeakDoorInLevelTag() };

            public override IQcSuggestorTag[] GetSuggestorTags()
            {
                return _tags;
            }
        }

        public class WeakDoorInLevelSuggestor : BasicCachedQcSuggestor<int>
        {
            protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
            {
                return context.HasTag<WeakDoorInLevelTag>();
            }

            protected override IQcSuggestion ItemToSuggestion(int item)
            {
                return new RawSuggestion(item.ToString());
            }

            protected override IEnumerable<int> GetItems(SuggestionContext context, SuggestorOptions options)
            {
                return WeakDoorsInLevel.Keys;
            }
        }


        public struct SecurityDoorInLevelTag : IQcSuggestorTag
        {

        }

        public sealed class SecurityDoorInLevelAttribute : SuggestorTagAttribute
        {
            private readonly IQcSuggestorTag[] _tags = { new SecurityDoorInLevelTag() };

            public override IQcSuggestorTag[] GetSuggestorTags()
            {
                return _tags;
            }
        }

        public class SecurityDoorInLevelSuggestor : BasicCachedQcSuggestor<int>
        {
            protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
            {
                return context.HasTag<SecurityDoorInLevelTag>();
            }

            protected override IQcSuggestion ItemToSuggestion(int item)
            {
                return new RawSuggestion(item.ToString());
            }

            protected override IEnumerable<int> GetItems(SuggestionContext context, SuggestorOptions options)
            {
                return SecurityDoorsInLevel.Keys;
            }
        }
    }
}
