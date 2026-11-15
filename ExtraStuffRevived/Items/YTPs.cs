using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_Ruby : ITM_YTPs
    {
        public override bool Use(PlayerManager pm)
        {
            var field = AccessTools.Field(typeof(ITM_YTPs), "value");
            if (field != null) field.SetValue(this, 500);
            return base.Use(pm);
        }
    }

    public class ITM_Emerald : ITM_YTPs
    {
        public override bool Use(PlayerManager pm)
        {
            var field = AccessTools.Field(typeof(ITM_YTPs), "value");
            if (field != null) field.SetValue(this, 750);
            return base.Use(pm);
        }
    }

    public class ITM_RandomYTP : ITM_YTPs
    {
        public override bool Use(PlayerManager pm)
        {
            int points = UnityEngine.Random.Range(-300, 300);

            var field = AccessTools.Field(typeof(ITM_YTPs), "value");
            if (field != null) field.SetValue(this, points);

            return base.Use(pm);
        }
    }

    public class ITM_ChristmasYTP : ITM_YTPs
    {
        public override bool Use(PlayerManager pm)
        {
            var field = AccessTools.Field(typeof(ITM_YTPs), "value");
            if (field != null) field.SetValue(this, 125);
            return base.Use(pm);
        }
    }

    public class ITM_EventYTP : ITM_YTPs
    {
        public override bool Use(PlayerManager pm)
        {
            int points = 100;

            if (pm != null && pm.ec != null)
            {
                var validEvents = pm.ec.GetComponentsInChildren<RandomEvent>(true)
                    .Where(e => e != null && !e.Active && e.gameObject.activeInHierarchy)
                    .Where(e => !e.GetType().Name.Contains("TimeOut"))
                    .ToList();

                if (validEvents.Count > 0)
                {
                    points = 200;
                    var ev = validEvents[UnityEngine.Random.Range(0, validEvents.Count)];

                    ev.Begin();

                    if (ev.EventIntro != null && Singleton<CoreGameManager>.Instance != null)
                    {
                        var hud = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber);
                        if (hud != null && hud.BaldiTv != null)
                        {
                            hud.BaldiTv.AnnounceEvent(ev.EventIntro);
                        }
                    }
                }
            }

            var field = HarmonyLib.AccessTools.Field(typeof(ITM_YTPs), "value");
            if (field != null) field.SetValue(this, points);

            return base.Use(pm);
        }
    }
}
