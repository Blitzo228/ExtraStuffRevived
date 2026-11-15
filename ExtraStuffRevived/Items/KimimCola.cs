#nullable disable
using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.UI;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_KimCola : Item
    {
        private HudGauge _gauge;
        private float _timeLeft = 10f;
        private const float BoostAmount = 50f;
        private PlayerMovementStatModifier _statModifier;
        private ValueModifier _modifier;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.plm == null) return false;

            pm.RuleBreak("Drinking", 0.8f, 0.25f);

            var drinkSound = Resources.FindObjectsOfTypeAll<SoundObject>()
                .FirstOrDefault(x => x != null && x.name.ToLowerInvariant().Contains("drinkelixir"));

            if (drinkSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(drinkSound);

            _statModifier = pm.GetComponent<PlayerMovementStatModifier>();
            if (_statModifier != null)
            {
                _modifier = new ValueModifier(1f, BoostAmount);
                _statModifier.AddModifier("walkSpeed", _modifier);
                _statModifier.AddModifier("runSpeed", _modifier);
            }

            Sprite icon = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/KimimColaIcon_Small"))
                icon = BasePlugin.assetManager.Get<Sprite>("Sprites/KimimColaIcon_Small");

            _gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(icon, _timeLeft);
            pm.StartCoroutine(Timer(pm));

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator Timer(PlayerManager pm)
        {
            while (_timeLeft > 0f)
            {
                _timeLeft -= Time.deltaTime * pm.PlayerTimeScale;
                _gauge?.SetValue(10f, _timeLeft);
                yield return null;
            }

            if (_statModifier != null && _modifier != null)
                _statModifier.RemoveModifier(_modifier);

            _gauge?.Deactivate();

            yield return null;
            Application.Quit();
        }
    }
}
