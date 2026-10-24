using HarmonyLib;
using UnityEngine;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(Cell), nameof(Cell.AddRenderer))]
    internal static class LockerEasterEggPatch
    {
        [HarmonyPostfix]
        static void Postfix(Renderer renderer)
        {
            if (renderer == null) return;

            bool isLocker = renderer.name.ToLower().Contains("locker");
            if (!isLocker && renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null)
            {
                isLocker = renderer.sharedMaterial.mainTexture.name.ToLower().Contains("locker");
            }

            if (!isLocker) return;

            if (UnityEngine.Random.value > 0.02f) return;

            if (BasePlugin.assetManager == null || !BasePlugin.assetManager.ContainsKey("EasterEggLockerTex")) return;

            var tex = BasePlugin.assetManager.Get<Texture2D>("EasterEggLockerTex");
            if (tex == null) return;

            var mat = renderer.material;
            if (mat == null) return;

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);

            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);

            renderer.material = mat;
        }
    }
}