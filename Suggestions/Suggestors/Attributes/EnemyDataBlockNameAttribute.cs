using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion.Suggestors.Attributes
{
    public sealed class EnemyDataBlockNameAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new GameDataBlockNameTag<EnemyDataBlock>() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
