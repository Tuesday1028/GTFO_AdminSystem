using Hikaria.AdminSystem.Suggestions.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestions.Suggestors.Attributes
{
    public sealed class ZoneAliasAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new ZoneAliasTag() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
