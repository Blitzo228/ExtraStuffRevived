using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExtraStuffRevived
{
    public class ITM_Randomizer : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var allItems = Resources.FindObjectsOfTypeAll<ItemObject>()
                .Where(x => x != null && x.item != null && x.itemType != Items.None)
                .Where(x => x.itemType != Items.Points)
                .Where(x => x.itemType != Items.Quarter)
                .Where(x => x.itemType != Items.BusPass)
                .Where(x => x.itemType != Items.Map)
                .Where(x => x.itemType != Items.StickerPack)
                .Where(x => x.itemType != Items.CircleKey)
                .Where(x => x.itemType != Items.TriangleKey)
                .Where(x => x.itemType != Items.SquareKey)
                .Where(x => x.itemType != Items.PentagonKey)
                .Where(x => x.itemType != Items.HexagonKey)
                .Where(x => x.itemType != Items.WeirdKey)
                .Where(x => x.itemType != Items.DetentionKey)
                .Where(x => !x.name.ToLower().Contains("lostitem"))
                .Where(x => !x.name.ToLower().Contains("key"))
                .Where(x => !x.name.ToLower().Contains("randomizer"))
                .ToList();

            if (allItems.Count > 0)
            {
                int slot = pm.itm.selectedItem;

                pm.itm.SetItem(pm.itm.nothing, slot);
                pm.itm.UpdateItems();
                pm.itm.UpdateSelect();

                pm.StartCoroutine(SpinRoutine(pm, allItems, slot));
            }

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator SpinRoutine(PlayerManager pm, List<ItemObject> allItems, int slot)
        {
            var hud = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber);
            if (hud == null) yield break;

            var itemSpritesField = HarmonyLib.AccessTools.Field(typeof(HudManager), "itemSprites");
            var itemSprites = itemSpritesField?.GetValue(hud) as Image[];

            if (itemSprites == null || slot >= itemSprites.Length) yield break;

            var boinkSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name.ToLower().Contains("boink"));

            float time = 3f;
            float elapsed = 0f;
            float nextChange = 0f;
            float interval = 0.05f;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= nextChange)
                {
                    var randomItem = allItems[Random.Range(0, allItems.Count)];

                    if (itemSprites[slot] != null)
                    {
                        itemSprites[slot].sprite = randomItem.itemSpriteSmall;
                    }

                    if (boinkSound != null && Singleton<CoreGameManager>.Instance != null)
                    {
                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle(boinkSound);
                    }

                    nextChange = elapsed + interval;

                    if (elapsed > time - 1f)
                    {
                        interval = Mathf.Lerp(0.05f, 0.3f, (elapsed - (time - 1f)) / 1f);
                    }
                }
                yield return null;
            }

            var finalItem = allItems[Random.Range(0, allItems.Count)];

            pm.itm.SetItem(finalItem, slot);

            hud.UpdateItemIcon(slot, finalItem.itemSpriteSmall);

            hud.SetItemSelect(slot, finalItem.nameKey);

            pm.itm.UpdateItems();
            pm.itm.UpdateSelect();

            var itemPickupSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "ItemPickup");
            if (itemPickupSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(itemPickupSound);
        }
    }
}