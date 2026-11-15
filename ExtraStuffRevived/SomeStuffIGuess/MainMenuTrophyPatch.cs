using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(MainMenu), "Start")]
    internal static class MainMenuTrophyPatch
    {
        static void Postfix(MainMenu __instance)
        {
            var fileManager = Singleton<PlayerFileManager>.Instance;
            if (fileManager == null || fileManager.clearedLevels == null) return;

            bool clearedSeeds = fileManager.clearedLevels.Length > 2 && fileManager.clearedLevels[2];
            bool clearedAnything = fileManager.clearedLevels.Any(x => x) ||
                                  (fileManager.wonChallenges != null && fileManager.wonChallenges.Any(x => x));

            if (!clearedSeeds && !clearedAnything) return;

            if (BasePlugin.assetManager == null || !BasePlugin.assetManager.ContainsKey("BaldTrophy")) return;
            Sprite trophySprite = BasePlugin.assetManager.Get<Sprite>("BaldTrophy");
            if (trophySprite == null) return;

            Transform bgTransform = __instance.transform.Find("Image");
            Transform parentTransform = bgTransform != null ? bgTransform : __instance.transform;

            if (parentTransform.Find("BaldTrophyUI") != null) return;

            GameObject trophyObj = new GameObject("BaldTrophyUI");
            trophyObj.transform.SetParent(parentTransform, false);

            Image image = trophyObj.AddComponent<Image>();
            image.sprite = trophySprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-27f, 30f);
            rect.sizeDelta = new Vector2(64f, 64f);
            rect.localScale = Vector3.one;

            CursorController.Instance?.transform?.SetAsLastSibling();
        }
    }
}
