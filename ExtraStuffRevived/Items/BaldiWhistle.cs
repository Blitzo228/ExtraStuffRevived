using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_BaldiWhistle : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var baldi = pm.ec.GetBaldi();
            if (baldi != null)
            {
                pm.ec.MakeNoise(pm.transform.position, 127);
                baldi.GetAngry(100f);
                baldi.GetExtraAnger(100f);
            }

            var audWhistle = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "PriWhistle");
            if (audWhistle != null && Singleton<CoreGameManager>.Instance != null)
            {
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audWhistle);
            }

            Object.Destroy(gameObject);
            return true;
        }
    }
}