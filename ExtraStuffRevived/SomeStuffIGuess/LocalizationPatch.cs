using HarmonyLib;
using System.Collections.Generic;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(LocalizationManager), "GetLocalizedText")]
    [HarmonyPatch(new System.Type[] { typeof(string), typeof(bool) })]
    internal static class LocalizationPatch
    {
        static void Postfix(ref string __result, string key)
        {
            if (__result == key)
            {
                var manager = Singleton<LocalizationManager>.Instance;
                if (manager != null)
                {
                    var field = AccessTools.Field(typeof(LocalizationManager), "localizedText");
                    var dict = field?.GetValue(manager) as Dictionary<string, string>;

                    if (dict != null)
                    {
                        string prefixedKey = key.StartsWith("ExtraStuff_") ? key : "ExtraStuff_" + key;

                        if (dict.ContainsKey(prefixedKey))
                        {
                            __result = dict[prefixedKey];
                            return;
                        }

                        if (key.Contains(" ("))
                        {
                            string baseKey = key.Substring(0, key.IndexOf(" ("));
                            string prefixedBaseKey = baseKey.StartsWith("ExtraStuff_") ? baseKey : "ExtraStuff_" + baseKey;

                            if (dict.ContainsKey(prefixedBaseKey))
                            {
                                __result = dict[prefixedBaseKey];
                            }
                        }
                    }
                }
            }
        }
    }
}
