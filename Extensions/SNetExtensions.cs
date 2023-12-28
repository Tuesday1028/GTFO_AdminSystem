using SNetwork;

namespace Hikaria.AdminSystem.Extensions
{
    public static class SNetExtensions
    {
        public static bool IsValid(this SNet_Player player)
        {
            if (player.IsBot)
            {
                return true;
            }
            else if (player.Lookup >= 72057594037927936UL)
            {
                return true;
            }
            return false;
        }
    }
}
