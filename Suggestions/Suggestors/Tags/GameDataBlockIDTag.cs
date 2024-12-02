using GameData;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion.Suggestors.Tags
{
    public struct GameDataBlockIDTag<T> : IQcSuggestorTag where T : GameDataBlockBase<T>
    {
    }
}
