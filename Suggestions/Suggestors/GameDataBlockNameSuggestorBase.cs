using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;
using System.Collections.Generic;

namespace Hikaria.AdminSystem.Suggestion.Suggestors
{
    public abstract class GameDataBlockNameSuggestorBase<TBlock> : BasicCachedQcSuggestor<string> where TBlock : GameDataBlockBase<TBlock>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<GameDataBlockNameTag<TBlock>>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item)
        {
            var block = GameDataBlockBase<TBlock>.GetBlock(item);
            return new GameDataBlockNameSuggestion<TBlock>(item, block);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return GameDataBlockBase<TBlock>.GetAllNames();
        }
    }
}
