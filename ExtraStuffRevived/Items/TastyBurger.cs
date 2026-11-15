#nullable disable
using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.UI;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_TastyBurger : Item
    {
        public override bool Use(PlayerManager pm)
        {
            if (pm?.plm == null) return false;
            pm.plm.stamina = pm.plm.StaminaMax;

            var audEat = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x?.name.Contains("ChipCrunch") == true);
            if (audEat != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audEat);

            var statMod = pm.GetComponent<PlayerMovementStatModifier>();
            if (statMod != null)
            {
                var mod = new ValueModifier(1f, 5f);
                statMod.AddModifier("walkSpeed", mod);
                statMod.AddModifier("runSpeed", mod);
                pm.StartCoroutine(RemoveMod(pm, statMod, mod));
            }
            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator RemoveMod(PlayerManager pm, PlayerMovementStatModifier statMod, ValueModifier mod)
        {
            float maxTime = 30f;
            float t = maxTime;

            Sprite gaugeIcon = null;
            if (BasePlugin.assetManager != null)
            {
                if (BasePlugin.assetManager.ContainsKey("Sprites/TastyBurgerIcon_Small"))
                    gaugeIcon = BasePlugin.assetManager.Get<Sprite>("Sprites/TastyBurgerIcon_Small");
            }

            if (gaugeIcon == null && BasePlugin.ModItems.TryGetValue("itm_tastyburger", out var itemObj) && itemObj != null)
            {
                gaugeIcon = itemObj.itemSpriteSmall ?? itemObj.itemSpriteLarge;
            }

            var gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(gaugeIcon, t);

            while (t > 0f)
            {
                t -= Time.deltaTime * pm.PlayerTimeScale;
                gauge?.SetValue(maxTime, t);
                yield return null;
            }

            statMod.RemoveModifier(mod);
            gauge?.Deactivate();
        }
    }
}