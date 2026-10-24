using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.UI;
using System.Collections;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_EnergyDrink : Item
    {
        private HudGauge? gauge;
        private float timeLeft = 25f;
        private const float boostAmount = 10f;
        private PlayerMovementStatModifier? statModifier;
        private ValueModifier? modifier;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            pm.RuleBreak("Drinking", 0.8f, 0.25f);

            if (BasePlugin.assetManager.ContainsKey("Sounds/drink_energy") && pm != null)
            {
                var sound = BasePlugin.assetManager.Get<SoundObject>("Sounds/drink_energy");
                if (sound != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(sound);
            }

            if (pm != null)
            {
                statModifier = pm.GetComponent<PlayerMovementStatModifier>();
                if (statModifier != null)
                {
                    modifier = new ValueModifier(1f, boostAmount);
                    statModifier.AddModifier("walkSpeed", modifier);
                    statModifier.AddModifier("runSpeed", modifier);
                }

                gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(BasePlugin.assetManager.Get<Sprite>("Sprites/itm_energy_small"), timeLeft);
                pm.StartCoroutine(Timer(pm));
            }

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator Timer(PlayerManager pm)
        {
            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime * pm.PlayerTimeScale;
                gauge?.SetValue(25f, timeLeft);
                yield return null;
            }

            if (statModifier != null && modifier != null) statModifier.RemoveModifier(modifier);
            gauge?.Deactivate();
        }
    }
}