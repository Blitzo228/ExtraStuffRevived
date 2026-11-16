#nullable disable
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class BSODAPlus : Item
    {
        public static Dictionary<ItemObject, int> Uses = new Dictionary<ItemObject, int>();

        public override bool Use(PlayerManager pm)
        {
            if (pm == null || pm.ec == null) return false;

            var itemObject = pm.itm.items[pm.itm.selectedItem];
            if (itemObject == null) return false;

            bool isMini = itemObject.name.ToLower().Contains("mini");

            if (!Uses.ContainsKey(itemObject))
                Uses[itemObject] = 3;

            var bsodaPrefab = Resources.FindObjectsOfTypeAll<ITM_BSODA>()
                .FirstOrDefault(x => x != null && !x.gameObject.scene.IsValid() && x.GetType().Assembly == typeof(ITM_BSODA).Assembly);

            if (bsodaPrefab != null)
            {
                var projObj = Object.Instantiate(bsodaPrefab.gameObject);
                projObj.name = isMini ? "BSODAPlusMiniProjectile" : "BSODAPlusProjectile";

                var bsodaComp = projObj.GetComponent<ITM_BSODA>();
                if (bsodaComp != null)
                {
                    var speedField = AccessTools.Field(typeof(ITM_BSODA), "speed");
                    if (speedField != null) speedField.SetValue(bsodaComp, isMini ? 8f : 12f);

                    var timeField = AccessTools.Field(typeof(ITM_BSODA), "time");
                    if (timeField != null) timeField.SetValue(bsodaComp, isMini ? 20f : 60f);

                    var srField = AccessTools.Field(typeof(ITM_BSODA), "spriteRenderer");
                    if (srField != null)
                    {
                        var sr = srField.GetValue(bsodaComp) as SpriteRenderer;
                        if (sr != null)
                        {
                            if (BasePlugin.assetManager.ContainsKey("Sprites/BsodaSpray_Sprite_Transparent"))
                            {
                                sr.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/BsodaSpray_Sprite_Transparent");
                            }
                            sr.transform.localScale = isMini ? new Vector3(1.5625f, 1.5625f, 1.5625f) : new Vector3(2.5f, 2.5f, 2.5f);
                        }
                    }

                    bsodaComp.Use(pm);
                }
            }

            Uses[itemObject]--;

            if (Uses[itemObject] <= 0)
            {
                Uses.Remove(itemObject);
                return true;
            }
            else
            {
                string baseName = isMini ? "ExtraStuff_Itm_BSODAPlusMini" : "ExtraStuff_Itm_BSODAPlus";
                itemObject.nameKey = $"{baseName} ({Uses[itemObject]})";
                pm.itm.UpdateItems();
                pm.itm.UpdateSelect();
                return false;
            }
        }
    }
}