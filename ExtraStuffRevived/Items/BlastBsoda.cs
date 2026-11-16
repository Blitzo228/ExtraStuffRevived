#nullable disable
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace ExtraStuffRevived
{
    public class ITM_BlastBsoda : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var itemObject = pm.itm.items[pm.itm.selectedItem];
            bool isMini = itemObject != null && itemObject.name.ToLower().Contains("mini");

            var bsodaPrefab = Resources.FindObjectsOfTypeAll<ITM_BSODA>()
                .FirstOrDefault(x => x != null && !x.gameObject.scene.IsValid() && x.GetType().Assembly == typeof(ITM_BSODA).Assembly);

            if (bsodaPrefab == null) return false;

            var projObj = Object.Instantiate(bsodaPrefab.gameObject);
            projObj.name = isMini ? "BlastBsodaMiniProjectile" : "BlastBsodaProjectile";

            var bsodaComp = projObj.GetComponent<ITM_BSODA>();
            if (bsodaComp == null) return false;

            var speedField = AccessTools.Field(typeof(ITM_BSODA), "speed");
            if (speedField != null) speedField.SetValue(bsodaComp, isMini ? 25f : 45f);

            var timeField = AccessTools.Field(typeof(ITM_BSODA), "time");
            if (timeField != null) timeField.SetValue(bsodaComp, isMini ? 1.5f : 3f);

            var srField = AccessTools.Field(typeof(ITM_BSODA), "spriteRenderer");
            if (srField != null)
            {
                var sr = srField.GetValue(bsodaComp) as SpriteRenderer;
                if (sr != null)
                {
                    if (BasePlugin.assetManager.ContainsKey("Sprites/BlastBsodaSpray_Sprite_Transparent"))
                    {
                        sr.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/BlastBsodaSpray_Sprite_Transparent");
                    }
                    sr.transform.localScale = isMini ? new Vector3(1.5625f, 1.5625f, 1.5625f) : new Vector3(2.5f, 2.5f, 2.5f);
                }
            }

            bsodaComp.Use(pm);

            Object.Destroy(gameObject);
            return true;
        }
    }
}