using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using Player;
using System.Collections.Generic;

namespace Hikaria.AdminSystem.Suggestion.Suggestors
{
    public sealed class PlayerSlotIndexSuggestor : BasicQcSuggestor<int>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<PlayerSlotIndexTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(int item)
        {
            AdminUtils.TryGetPlayerAgentBySlotIndex(item + 1, out var player);
            return new PlayerSlotIndexSuggestion(item + 1, player);
        }

        protected override IEnumerable<int> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            var result = new List<int>();
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                result.Add(player.PlayerSlotIndex);
            }
            return result;
        }
    }
}
