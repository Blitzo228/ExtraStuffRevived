using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_EventController : Item
    {
        public static Dictionary<ItemObject, int> Uses = new Dictionary<ItemObject, int>();
        private const int MAX_USES = 5;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var heldItem = pm.itm.items[pm.itm.selectedItem];
            if (heldItem == null) return false;

            if (!Uses.ContainsKey(heldItem))
                Uses[heldItem] = MAX_USES;

            var evs = pm.ec.GetComponentsInChildren<RandomEvent>(true);

            var activeEvents = evs
                .Where(e => e != null && e.Active && !e.GetType().Name.Contains("TimeOut"))
                .ToList();

            RandomEvent? targetEvent = null;

            if (activeEvents.Count > 0)
            {
                targetEvent = activeEvents[UnityEngine.Random.Range(0, activeEvents.Count)];
                targetEvent.End();

                if (Singleton<CoreGameManager>.Instance != null)
                {
                    var hud = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber);
                    if (hud != null && hud.BaldiTv != null)
                    {
                        var tv = hud.BaldiTv;
                        tv.StopAllCoroutines();

                        var queue = HarmonyLib.AccessTools.Field(typeof(BaldiTV), "queuedEnumerators").GetValue(tv) as System.Collections.Generic.List<System.Collections.IEnumerator>;
                        queue?.Clear();

                        HarmonyLib.AccessTools.Field(typeof(BaldiTV), "busy").SetValue(tv, false);

                        var audMan = HarmonyLib.AccessTools.Field(typeof(BaldiTV), "baldiTvAudioManager").GetValue(tv) as AudioManager;
                        audMan?.FlushQueue(true);

                        var anim = HarmonyLib.AccessTools.Field(typeof(BaldiTV), "baldiTvAnimator").GetValue(tv) as Animator;
                        anim?.SetBool("Active", false);

                        var resetScreen = HarmonyLib.AccessTools.Method(typeof(BaldiTV), "ResetScreen");
                        resetScreen?.Invoke(tv, null);
                    }
                }
            }
            else
            {
                var validEvents = evs
                    .Where(e => e != null && !e.Active && e.gameObject.activeInHierarchy)
                    .Where(e => !e.GetType().Name.Contains("TimeOut"))
                    .ToList();

                if (validEvents.Count > 0)
                {
                    targetEvent = validEvents[UnityEngine.Random.Range(0, validEvents.Count)];
                    targetEvent.Begin();

                    if (targetEvent.EventIntro != null && Singleton<CoreGameManager>.Instance != null)
                    {
                        var hud = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber);
                        if (hud != null && hud.BaldiTv != null)
                        {
                            hud.BaldiTv.AnnounceEvent(targetEvent.EventIntro);
                        }
                    }
                }
            }

            if (targetEvent == null)
            {
                return false;
            }

            var pressSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "Sfx_Button_Press");
            if (pressSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(pressSound);

            Uses[heldItem]--;

            if (Uses[heldItem] <= 0)
            {
                Uses.Remove(heldItem);
                pm.itm.RemoveItem(pm.itm.selectedItem);
                pm.itm.UpdateItems();
                pm.itm.UpdateSelect();
            }
            else
            {
                heldItem.nameKey = $"ExtraStuff_Itm_EventController ({Uses[heldItem]}/5)";
                pm.itm.UpdateItems();
                pm.itm.UpdateSelect();
            }

            return false;
        }
    }
}