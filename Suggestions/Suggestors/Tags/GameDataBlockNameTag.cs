using GameData;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion.Suggestors.Tags
{
    public struct GameDataBlockNameTag<T> : IQcSuggestorTag where T : GameDataBlockBase<T>
    {
    }
}
