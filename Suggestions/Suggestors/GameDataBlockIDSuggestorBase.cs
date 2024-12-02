using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;
using System.Collections.Generic;

namespace Hikaria.AdminSystem.Suggestion.Suggestors
{
    public abstract class GameDataBlockIDSuggestorBase<TBlock> : BasicCachedQcSuggestor<uint> where TBlock : GameDataBlockBase<TBlock>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<GameDataBlockIDTag<TBlock>>();
        }

        protected override IQcSuggestion ItemToSuggestion(uint item)
        {
            return new GameDataBlockIDSuggestion<TBlock>(item, GameDataBlockBase<TBlock>.GetBlock(item));
        }

        protected override IEnumerable<uint> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return GameDataBlockBase<TBlock>.GetAllPersistentIDs();
        }
    }
}
