using GameData;
using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestions.Suggestors.Attributes
{
    public sealed class FogSettingsDataBlockIDAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new GameDataBlockIDTag<FogSettingsDataBlock>() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
