using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(Baldi), "Initialize")]
    internal static class BaldiPraisePatch
    {
        static void Postfix(Baldi __instance)
        {
            var correctSoundsField = AccessTools.Field(typeof(Baldi), "correctSounds");
            if (correctSoundsField == null) return;

            var newSounds = new List<WeightedSoundObject>();

            string[] praiseKeys = { "Sounds/BAL_YourGoodPraise", "Sounds/BAL_FantasticPraise", "Sounds/BAL_YouDidGreatPraise" };
            foreach (var key in praiseKeys)
            {
                if (BasePlugin.assetManager.ContainsKey(key))
                {
                    newSounds.Add(new WeightedSoundObject
                    {
                        selection = BasePlugin.assetManager.Get<SoundObject>(key),
                        weight = 100
                    });
                }
            }

            if (newSounds.Count > 0)
            {
                correctSoundsField.SetValue(__instance, newSounds.ToArray());
            }
        }
    }
}