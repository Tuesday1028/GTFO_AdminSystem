using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion.Suggestors.Attributes
{
    public sealed class EnemyDataBlockIDAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new GameDataBlockIDTag<EnemyDataBlock>() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
