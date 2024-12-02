using GameData;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion
{
    public sealed class GameDataBlockIDSuggestion<T> : IQcSuggestion where T : GameDataBlockBase<T>
    {
        private readonly uint _id;
        private readonly string _completion;
        private readonly string _secondarySignature;

        public string FullSignature => _id.ToString();
        public string PrimarySignature => _id.ToString();
        public string SecondarySignature => _secondarySignature;

        public GameDataBlockIDSuggestion(uint id, GameDataBlockBase<T> block)
        {
            _id = id;
            _secondarySignature = string.Empty;
            if (block != null)
                _secondarySignature = $" {block.name}";
            _completion = _id.ToString();
        }

        public bool MatchesPrompt(string prompt)
        {
            return prompt == _id.ToString();
        }

        public string GetCompletion(string prompt)
        {
            return _completion;
        }

        public string GetCompletionTail(string prompt)
        {
            return string.Empty;
        }

        public SuggestionContext? GetInnerSuggestionContext(SuggestionContext context)
        {
            return null;
        }
    }
}
