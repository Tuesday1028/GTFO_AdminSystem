using Hikaria.AdminSystem.Suggestions.Suggestors.Tags;
using Hikaria.QC;
using System;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Utilities;

namespace Hikaria.AdminSystem.Suggestion.Suggestors
{
    public sealed class ZoneAliasSuggestor : BasicCachedQcSuggestor<int>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<ZoneAliasTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(int item)
        {

            return new RawSuggestion(item.ToString());
        }

        protected override IEnumerable<int> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return LevelGeneration.Builder.CurrentFloor?.allZones.ToSystemList().Select(zone => zone.Alias) ?? Array.Empty<int>();
        }
    }
}
