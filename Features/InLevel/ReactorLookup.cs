using Hikaria.AdminSystem.Suggestion;
using Hikaria.AdminSystem.Utilities;
using Hikaria.QC;
using LevelGeneration;
using System.Collections.Generic;
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.InLevel
{
    [DisallowInGameToggle]
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
    [HideInModSettings]
    [CommandPrefix("Reactor")]
    public class ReactorLookup : Feature
    {
        public override string Name => "反应堆";

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        public static Dictionary<int, LG_WardenObjective_Reactor> ReactorsInLevel { get; set; } = new();


        [ArchivePatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private class LG_WardenObjective_Reactor_OnBuildDone_Patch
        {
            private static void Postfix(LG_WardenObjective_Reactor __instance)
            {
                if (!ReactorsInLevel.TryAdd(__instance.m_serialNumber, __instance))
                {
                    ReactorsInLevel[__instance.m_serialNumber] = __instance;
                }
            }
        }

        [ArchivePatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnDestroy))]
        private class LG_WardenObjective_Reactor_OnDestroy_Patch
        {
            private static void Prefix(LG_WardenObjective_Reactor __instance)
            {
                ReactorsInLevel.Remove(__instance.m_serialNumber);
            }
        }

        [Command("Codes")]
        private static void ShowReactorCodes([ReactorInLevel] int id, bool all = false)
        {
            if (!ReactorsInLevel.TryGetValue(id, out var reactor))
            {
                ConsoleLogs.LogToConsole($"不存在 REACTOR_{id}", LogLevel.Error);
                return;
            }
            var codes = reactor.GetOverrideCodes();
            StringBuilder sb = new(200);
            sb.AppendLine($"<color=orange>REACTOR_{id} 验证秘钥：</color>");
            for (int i = 0; i < (all ? codes.Count : reactor.m_waveCountMax); i++)
            {
                sb.AppendLine($"{i + 1}. {codes[i]}");
            }
            ConsoleLogs.LogToConsole(sb.ToString());
        }

        [Command("Interaction")]
        private static void ReactorInteraction([ReactorInLevel] int id, eReactorInteraction interaction, float progression = 0f)
        {
            if (!ReactorsInLevel.TryGetValue(id, out var reactor))
            {
                ConsoleLogs.LogToConsole($"不存在 REACTOR_{id}", LogLevel.Error);
                return;
            }

            reactor.AttemptInteract(interaction, progression);
            ConsoleLogs.LogToConsole($"REACTOR_{id}: {interaction}");
        }

        public struct ReactorInLevelTag : IQcSuggestorTag
        {

        }

        public sealed class ReactorInLevelAttribute : SuggestorTagAttribute
        {
            private readonly IQcSuggestorTag[] _tags = { new ReactorInLevelTag() };

            public override IQcSuggestorTag[] GetSuggestorTags()
            {
                return _tags;
            }
        }

        public class ReactorInLevelSuggestor : BasicQcSuggestor<int>
        {
            protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
            {
                return context.HasTag<ReactorInLevelTag>();
            }

            protected override IQcSuggestion ItemToSuggestion(int item)
            {
                return new ReactorInLevelSuggestion(item);
            }

            protected override IEnumerable<int> GetItems(SuggestionContext context, SuggestorOptions options)
            {
                return ReactorsInLevel.Keys;
            }
        }

        public class ReactorInLevelSuggestion : IQcSuggestion
        {
            private readonly int _id;
            private readonly string _completion;
            private readonly string _secondarySignature;

            public string FullSignature => _id.ToString();
            public string PrimarySignature => _id.ToString();
            public string SecondarySignature => _secondarySignature;

            public ReactorInLevelSuggestion(int id)
            {
                _id = id;
                _secondarySignature = string.Empty;

                if (ReactorsInLevel.TryGetValue(id, out var reactor))
                {
                    _secondarySignature = $" ZONE_{reactor.SpawnNode.m_zone.Alias}, {reactor.m_currentState.status}";
                }
                _completion = _id.ToString();
            }

            public bool MatchesPrompt(string prompt)
            {
                return prompt == _id.ToString();
            }

            public string GetCompletion(string prompt)
            {
                return _completion;
            }

            public string GetCompletionTail(string prompt)
            {
                return string.Empty;
            }

            public SuggestionContext? GetInnerSuggestionContext(SuggestionContext context)
            {
                return null;
            }
        }

    }
}
