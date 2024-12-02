using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestions.Suggestors.Attributes
{
    public sealed class ItemDataBlockNameAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new GameDataBlockNameTag<ItemDataBlock>() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
