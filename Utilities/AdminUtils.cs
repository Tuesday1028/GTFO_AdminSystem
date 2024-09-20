using Enemies;
using Hikaria.AdminSystem.Extensions;
using Il2CppInterop.Runtime.InteropTypes;
using Player;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

        public static bool CanFireHitObject(Vector3 sourcePos, GameObject targetObj)
        {
            return Physics.Raycast(sourcePos, targetObj.transform.position - sourcePos, out var hit, Vector3.Distance(targetObj.transform.position, sourcePos), LayerManager.MASK_BULLETWEAPON_RAY) && hit.transform.IsChildOf(targetObj.gameObject.transform);
        }

        public static bool CanSeeEnemyPlus(Vector3 sourcePos, EnemyAgent enemy)
        {
            foreach (var limb in enemy.Damage.DamageLimbs)
            {
                if (limb == null) continue;
                if (!Physics.Raycast(sourcePos, limb.DamageTargetPos - sourcePos, out _, Vector3.Distance(limb.DamageTargetPos, sourcePos), LayerManager.MASK_WORLD))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
