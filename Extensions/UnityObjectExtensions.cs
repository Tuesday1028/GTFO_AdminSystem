using UnityEngine;

namespace Hikaria.AdminSystem.Extensions
{
    internal static class UnityObjectExtensions
    {
        public static void DoNotDestroyAndSetHideFlags(this UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            UnityEngine.Object.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        }
    }
}
