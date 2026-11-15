using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_CoinDoor : Item
    {
        public override bool Use(PlayerManager pm)
        {
            if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out RaycastHit hit, pm.pc.Reach, pm.pc.ClickLayers))
            {
                var door = hit.transform.GetComponentInParent<SwingDoor>();

                if (door != null && door.GetComponent<CoinDoorMarker>() == null)
                {
                    var marker = door.gameObject.AddComponent<CoinDoorMarker>();
                    marker.Initialize();
                    door.Lock(true);

                    var sound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CoinInsert");
                    if (sound == null) sound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CashRegister");
                    if (sound != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(sound);

                    Object.Destroy(gameObject);
                    return true;
                }
            }

            Object.Destroy(gameObject);
            return false;
        }
    }

    public class CoinDoorMarker : MonoBehaviour
    {
        private SwingDoor? door;
        private Material[]? originalLocked;
        private Material[]? originalShut;

        public void Initialize()
        {
            door = GetComponent<SwingDoor>();
            if (door == null) return;

            originalLocked = door.overlayLocked;
            originalShut = door.overlayShut;

            Texture2D tex = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x.name == "CoinDoor_Transparent");

            if (tex != null)
            {
                Material? baseMat = null;
                if (originalLocked != null && originalLocked.Length > 0 && originalLocked[0] != null)
                    baseMat = originalLocked[0];
                else if (originalShut != null && originalShut.Length > 0 && originalShut[0] != null)
                    baseMat = originalShut[0];

                Material? coinMat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Sprites/Default"));
                coinMat.mainTexture = tex;

                if (door.overlayLocked != null)
                {
                    door.overlayLocked = new Material[originalLocked.Length];
                    for (int i = 0; i < originalLocked.Length; i++) door.overlayLocked[i] = coinMat;
                }

                if (door.overlayShut != null)
                {
                    door.overlayShut = new Material[originalShut.Length];
                    for (int i = 0; i < originalShut.Length; i++) door.overlayShut[i] = coinMat;
                }
            }
        }

        public void RestoreVisuals()
        {
            if (door == null) return;
            if (originalLocked != null) door.overlayLocked = originalLocked;
            if (originalShut != null) door.overlayShut = originalShut;
            door.UpdateTextures();
        }
    }

    [HarmonyPatch(typeof(SwingDoor), nameof(SwingDoor.ItemFits))]
    internal static class SwingDoorItemFitsPatch
    {
        static void Postfix(SwingDoor __instance, Items item, ref bool __result)
        {
            if (__instance.GetComponent<CoinDoorMarker>() != null)
            {
                __result = (item == Items.Quarter);
            }
        }
    }

    [HarmonyPatch(typeof(SwingDoor), nameof(SwingDoor.InsertItem))]
    internal static class SwingDoorInsertItemPatch
    {
        static bool Prefix(SwingDoor __instance)
        {
            var marker = __instance.GetComponent<CoinDoorMarker>();
            if (marker != null)
            {
                var sound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CoinInsert");
                if (sound == null) sound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CashRegister");
                if (sound != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(sound);

                marker.RestoreVisuals();
                __instance.Unlock();
                __instance.Open(true, true);

                Object.Destroy(marker);
                return false;
            }
            return true;
        }
    }
}
