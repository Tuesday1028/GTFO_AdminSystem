using Player;

namespace Hikaria.AdminSystem.Utilities
{
    public static class AdminUtils
    {
        public static bool TryGetPlayerAgentFromSlotIndex(int slot, out PlayerAgent player)
        {
            slot--;
            if (!PlayerManager.TryGetPlayerAgent(ref slot, out player))
            {
                return false;
            }
            return true;
        }

        public static PlayerAgent LocalPlayerAgent => PlayerManager.GetLocalPlayerAgent();
    }
}
