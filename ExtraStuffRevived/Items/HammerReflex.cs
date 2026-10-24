using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_HammerReflex : Item
    {
        private RaycastHit hit;
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            var drReflex = Object.FindObjectsOfType<DrReflex>().FirstOrDefault();
            if (drReflex != null) drReflex.HammerSound();
            else
            {
                var audHammer = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x.name == "DrR_Hammer" || x.name == "Hammer");
                if (audHammer != null && Singleton<CoreGameManager>.Instance != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audHammer);
            }

            if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out hit, pm.pc.reach))
            {
                if (hit.transform.CompareTag("Window"))
                {
                    var window = hit.transform.GetComponent<Window>();
                    if (window != null && !window.open) window.Break(true);
                    return false;
                }
                else if (hit.transform.CompareTag("NPC"))
                {
                    var firstPrize = hit.transform.GetComponentInParent<FirstPrize>();
                    if (firstPrize != null)
                    {
                        firstPrize.CutWires();
                        Object.Destroy(gameObject);
                        return true;
                    }

                    var npc = hit.transform.GetComponent<NPC>();
                    if (npc != null)
                    {
                        var balder = hit.transform.GetComponent<Balder_Entity>();
                        if (balder != null) balder.Crumble(true);
                        else npc.Entity.Squish(30f);
                        Object.Destroy(gameObject);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}