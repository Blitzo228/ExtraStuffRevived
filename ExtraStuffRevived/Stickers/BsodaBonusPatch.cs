using System.Collections.Generic;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class BsodaBonusTracker
    {
        public static int HitCount = 0;

        public static void Reset()
        {
            HitCount = 0;
        }
    }

    [HarmonyPatch(typeof(ITM_BSODA), nameof(ITM_BSODA.EntityTriggerEnter))]
    internal static class BsodaHitPatch
    {
        private static readonly HashSet<int> HitBsodas = new HashSet<int>();

        static void Postfix(ITM_BSODA __instance, Entity otherEntity, Collider other, bool validCollision)
        {
            if (!validCollision || other == null || !other.CompareTag("NPC")) return;
            if (BasePlugin.BsodaBonusSticker == Sticker.Nothing) return;

            int bsodaId = __instance.GetInstanceID();
            if (HitBsodas.Contains(bsodaId)) return;

            var stickerManager = Singleton<StickerManager>.Instance;
            if (stickerManager != null)
            {
                int stickerCount = stickerManager.StickerValue(BasePlugin.BsodaBonusSticker);
                if (stickerCount > 0)
                {
                    HitBsodas.Add(bsodaId);
                    BsodaBonusTracker.HitCount += stickerCount;
                }
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), "FinishLevel")]
    internal static class BsodaBonusEndLevelPatch
    {
        static void Prefix()
        {
            if (BsodaBonusTracker.HitCount > 0)
            {
                var core = Singleton<CoreGameManager>.Instance;
                if (core != null)
                {
                    core.AddPoints(20 * BsodaBonusTracker.HitCount, 0, true);
                }
                BsodaBonusTracker.Reset();
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), "RestartLevel")]
    internal static class BsodaBonusRestartPatch
    {
        static void Prefix()
        {
            BsodaBonusTracker.Reset();
        }
    }

    [HarmonyPatch(typeof(CoreGameManager), "ReturnToMenu")]
    internal static class BsodaBonusMenuPatch
    {
        static void Postfix()
        {
            BsodaBonusTracker.Reset();
        }
    }
}