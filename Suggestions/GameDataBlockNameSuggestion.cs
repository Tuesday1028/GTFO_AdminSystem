using GameData;
using Hikaria.QC;

namespace Hikaria.AdminSystem.Suggestion
{
    public sealed class GameDataBlockNameSuggestion<T> : IQcSuggestion where T : GameDataBlockBase<T>
    {
        private readonly string _name;
        private readonly string _completion;
        private readonly string _secondarySignature;

        public string FullSignature => _name;
        public string PrimarySignature => _name;
        public string SecondarySignature => _secondarySignature;

        public GameDataBlockNameSuggestion(string name, GameDataBlockBase<T> block)
        {
            _name = name;
            _secondarySignature = string.Empty;
            if (block != null)
                _secondarySignature = $" [{block.persistentID}]";
            _completion = _name;
        }

        public bool MatchesPrompt(string prompt)
        {
            return prompt == _name;
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
