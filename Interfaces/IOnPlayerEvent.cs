using SNetwork;

namespace Hikaria.AdminSystem.Interfaces
{
    public interface IOnPlayerEvent
    {
        void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason);
    }
}
