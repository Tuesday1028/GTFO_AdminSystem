using Hikaria.AdminSystem.Suggestion.Suggestors.Tags;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion.Suggestors.Attributes
{
    public sealed class PlayerSlotIndexAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new PlayerSlotIndexTag() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }
}
