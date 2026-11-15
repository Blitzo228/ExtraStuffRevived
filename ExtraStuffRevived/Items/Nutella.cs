using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_Nutella : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            pm.plm.stamina = 400f;

            var audEat = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name.Contains("ChipCrunch"));
            if (audEat != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audEat);

            var slowMod = new MovementModifier(Vector3.zero, 0.6f);
            pm.plm.am.moveMods.Add(slowMod);
            pm.StartCoroutine(EffectRoutine(pm, slowMod, 10f));

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator EffectRoutine(PlayerManager pm, MovementModifier mod, float time)
        {
            Sprite? gaugeIcon = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/NutellaIcon_Small"))
            {
                gaugeIcon = BasePlugin.assetManager.Get<Sprite>("Sprites/NutellaIcon_Small");
            }

            var gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(gaugeIcon, time);

            float timer = time;
            while (timer > 0f)
            {
                if (pm == null || pm.plm == null) break;
                timer -= Time.deltaTime * pm.PlayerTimeScale;
                if (gauge != null) gauge.SetValue(time, timer);
                yield return null;
            }

            if (pm != null && pm.plm != null && pm.plm.am != null)
                pm.plm.am.moveMods.Remove(mod);

            if (gauge != null) gauge.Deactivate();
        }
    }
}