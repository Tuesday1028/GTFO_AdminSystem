using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion.Suggestors.Attributes
{
    public sealed class ItemDataBlockIDAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new GameDataBlockIDTag<ItemDataBlock>() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
