using System.Linq;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using UnityEngine;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(BaseGameManager), "CollectNotebook")]
    internal static class NotebookStickerPatch
    {
        static void Postfix()
        {
            var stickerManager = Singleton<StickerManager>.Instance;
            var core = Singleton<CoreGameManager>.Instance;

            if (stickerManager != null && core != null && core.sceneObject != null && core.sceneObject.potentialStickers != null)
            {
                int stickerCount = stickerManager.TotalInPosession(BasePlugin.NotebookSticker);

                if (stickerCount > 0)
                {
                    var validStickers = core.sceneObject.potentialStickers
                        .Where(s => s != null && s.selection != BasePlugin.NotebookSticker)
                        .Where(s => {
                            var meta = StickerMetaStorage.Instance?.Get(s.selection);
                            return meta == null || meta.tags == null || !meta.tags.Contains("tms_daredevil");
                        })
                        .ToArray();

                    if (validStickers.Length > 0)
                    {
                        stickerManager.GiveRandomStickers(validStickers, StickerPackType.Normal, stickerCount, true);
                    }
                }
            }
        }
    }
}