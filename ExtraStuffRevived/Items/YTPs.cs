using HarmonyLib;
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
}