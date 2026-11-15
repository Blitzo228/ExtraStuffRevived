using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_DarkCoffee : Item
    {
        private static SoundObject? _coffeeSound;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.plm == null) return false;

            pm.plm.stamina = pm.plm.StaminaMax * 5f;

            if (_coffeeSound == null)
            {
                if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sounds/coffee"))
                {
                    _coffeeSound = BasePlugin.assetManager.Get<SoundObject>("Sounds/coffee");
                }
            }

            if (_coffeeSound != null && Singleton<CoreGameManager>.Instance != null)
            {
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(_coffeeSound);
            }

            Object.Destroy(gameObject);
            return true;
        }
    }
}
