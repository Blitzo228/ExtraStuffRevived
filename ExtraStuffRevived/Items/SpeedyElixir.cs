using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.UI;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_SpeedyElixir : Item
    {
        private HudGauge? gauge;
        private float timeLeft = 15f;
        private const float boostAmount = 20f;
        private PlayerMovementStatModifier? statModifier;
        private ValueModifier? modifier;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            pm.RuleBreak("Drinking", 0.8f, 0.25f);

            var drinkSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name.ToLower().Contains("drinkelixir"));

            if (drinkSound != null && Singleton<CoreGameManager>.Instance != null)
            {
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(drinkSound);
            }
            else if (BasePlugin.assetManager.ContainsKey("Sounds/drink_energy"))
            {
                var fallbackSound = BasePlugin.assetManager.Get<SoundObject>("Sounds/drink_energy");
                if (fallbackSound != null && Singleton<CoreGameManager>.Instance != null)
                    Singleton<CoreGameManager>.Instance.audMan.PlaySingle(fallbackSound);
            }

            pm.SetHidden(true);

            statModifier = pm.GetComponent<PlayerMovementStatModifier>();
            if (statModifier != null)
            {
                modifier = new ValueModifier(1f, boostAmount);
                statModifier.AddModifier("walkSpeed", modifier);
                statModifier.AddModifier("runSpeed", modifier);
            }

            Sprite? icon = null;
            if (BasePlugin.assetManager.ContainsKey("Sprites/SpeedyElixirIcon_Small"))
                icon = BasePlugin.assetManager.Get<Sprite>("Sprites/SpeedyElixirIcon_Small");

            gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(icon, timeLeft);
            pm.StartCoroutine(Timer(pm));

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator Timer(PlayerManager pm)
        {
            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime * pm.PlayerTimeScale;
                gauge?.SetValue(15f, timeLeft);
                yield return null;
            }

            if (statModifier != null && modifier != null) statModifier.RemoveModifier(modifier);
            gauge?.Deactivate();

            if (pm != null)
            {
                pm.SetHidden(false);
            }
        }
    }
}
