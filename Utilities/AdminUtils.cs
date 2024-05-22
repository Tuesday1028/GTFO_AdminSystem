using Il2CppInterop.Runtime.InteropTypes;
using Player;
using System.Linq;
using System.Reflection;

namespace Hikaria.AdminSystem.Utility
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

        public static LocalPlayerAgent LocalPlayerAgent => PlayerManager.GetLocalPlayerAgent()?.TryCast<LocalPlayerAgent>();

        public static T CopyProperties<T>(T source, T target)
        {
            PropertyInfo[] properties = source.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo sourceProp = properties[i];
                if (target.GetType().GetProperties().Any((PropertyInfo targetProp) => targetProp.Name == sourceProp.Name && targetProp.GetType() == sourceProp.GetType() && targetProp.CanWrite))
                {
                    object value = sourceProp.GetValue(source);
                    PropertyInfo property = target.GetType().GetProperty(sourceProp.Name);
                    if (property.PropertyType != typeof(Il2CppObjectBase) || property.PropertyType != typeof(UnityEngine.Object))
                    {
                        property.SetValue(target, value);
                    }
                }
            }
            return target;
        }
    }
}
