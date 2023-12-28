using System.Collections.Generic;

namespace Hikaria.AdminSystem.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AutoAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.TryAdd(key, value))
            {
                return;
            }
            dict[key] = value;
        }

    }
}
