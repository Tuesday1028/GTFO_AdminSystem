using Hikaria.QC;
using Player;

namespace Hikaria.AdminSystem.Suggestion
{
    public sealed class PlayerSlotIndexSuggestion : IQcSuggestion
    {
        private readonly int _slot;
        private readonly string _completion;
        private readonly string _secondarySignature;

        public string FullSignature => _slot.ToString();
        public string PrimarySignature => _slot.ToString();
        public string SecondarySignature => _secondarySignature;

        public PlayerSlotIndexSuggestion(int slot, PlayerAgent player)
        {
            _slot = slot;
            if (player != null)
                _secondarySignature = $" {player.GetColoredName()}";
            else
                _secondarySignature = string.Empty;

            _completion = _slot.ToString();
        }

        public bool MatchesPrompt(string prompt)
        {
            return prompt == _slot.ToString();
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
