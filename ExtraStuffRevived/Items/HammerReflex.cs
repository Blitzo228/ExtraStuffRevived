using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace ExtraStuffRevived
{
    public class ITM_HammerReflex : Item
    {
        public static Dictionary<ItemObject, int> Uses = new Dictionary<ItemObject, int>();
        private const int MAX_USES = 20;
        private RaycastHit hit;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            var heldItem = pm.itm.items[pm.itm.selectedItem];
            if (heldItem == null) return false;

            if (!Uses.ContainsKey(heldItem))
                Uses[heldItem] = MAX_USES;

            var drReflex = Object.FindObjectsOfType<DrReflex>().FirstOrDefault();
            if (drReflex != null) drReflex.HammerSound();
            else
            {
                var audHammer = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && (x.name == "DrR_Hammer" || x.name == "Hammer"));
                if (audHammer != null && Singleton<CoreGameManager>.Instance != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audHammer);
            }

            bool hitSomething = false;
            bool hitNpc = false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam != null && Physics.Raycast(pm.transform.position, cam.transform.forward, out hit, pm.pc.reach))
            {
                if (hit.transform != null)
                {
                    var hitTrans = hit.transform;

                    if (hitTrans.CompareTag("Window"))
                    {
                        var window = hitTrans.GetComponent<Window>();
                        if (window != null && !window.open)
                        {
                            window.Break(true);
                            hitSomething = true;
                        }
                    }
                    else if (hitTrans.GetComponentInParent<GameLock>() != null)
                    {
                        var gameLock = hitTrans.GetComponentInParent<GameLock>();
                        if (gameLock != null)
                        {
                            gameLock.Remove();
                            hitSomething = true;
                        }
                    }
                    else if (hitTrans.GetComponentInParent<SodaMachine>() != null)
                    {
                        var machine = hitTrans.GetComponentInParent<SodaMachine>();
                        if (machine != null)
                        {
                            if (machine.ItemFits(Items.Quarter))
                            {
                                machine.InsertItem(pm, pm.ec);
                                hitSomething = true;
                            }
                        }
                    }
                    else if (hitTrans.GetComponentInParent<CoinDoor>() != null)
                    {
                        var coinDoor = hitTrans.GetComponentInParent<CoinDoor>();
                        var door = hitTrans.GetComponentInParent<Door>();
                        if (coinDoor != null && door != null)
                        {
                            coinDoor.InsertItem(pm, pm.ec);
                            door.Open(true, true);
                            hitSomething = true;
                        }
                    }
                    else if (hitTrans.GetComponentInParent<Door>() != null)
                    {
                        var door = hitTrans.GetComponentInParent<Door>();
                        if (door != null && door.locked)
                        {
                            door.Unlock();
                            door.Open(true, true);
                            hitSomething = true;
                        }
                    }
                    else if (hitTrans.CompareTag("NPC"))
                    {
                        var firstPrize = hitTrans.GetComponentInParent<FirstPrize>();
                        if (firstPrize != null)
                        {
                            firstPrize.CutWires();
                            hitSomething = true;
                            hitNpc = true;
                        }
                        else
                        {
                            var npc = hitTrans.GetComponent<NPC>();
                            if (npc != null)
                            {
                                var balder = hitTrans.GetComponent<Balder_Entity>();
                                if (balder != null) balder.Crumble(true);
                                else if (npc.Entity != null) npc.Entity.Squish(30f);
                                hitSomething = true;
                                hitNpc = true;
                            }
                        }
                    }
                }
            }

            if (hitSomething)
            {
                int cost = hitNpc ? 5 : 1;
                Uses[heldItem] -= cost;

                if (Uses[heldItem] <= 0)
                {
                    Uses.Remove(heldItem);
                    pm.itm.RemoveItem(pm.itm.selectedItem);
                    pm.itm.UpdateItems();
                    pm.itm.UpdateSelect();
                }
                else
                {
                    pm.itm.UpdateItems();
                    pm.itm.UpdateSelect();
                }
            }

            return false;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Pickup), nameof(Pickup.AssignItem))]
    internal static class HammerPickupPatch
    {
        static void Prefix(ref ItemObject item)
        {
            if (item != null && item.name != null && item.name.Contains("HammerReflex"))
            {
                var clone = Object.Instantiate(item);
                clone.name = item.name + "_Clone_" + System.Guid.NewGuid().ToString("N").Substring(0, 4);
                clone.nameKey = "Itm_HammerReflex";
                ITM_HammerReflex.Uses[clone] = 20;
                item = clone;
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(ItemManager), nameof(ItemManager.AddItem), new System.Type[] { typeof(ItemObject) })]
    internal static class HammerAddPatch
    {
        static void Prefix(ref ItemObject item)
        {
            if (item != null && item.name != null && item.name.Contains("HammerReflex"))
            {
                var clone = Object.Instantiate(item);
                clone.name = item.name + "_Clone_" + System.Guid.NewGuid().ToString("N").Substring(0, 4);
                clone.nameKey = "Itm_HammerReflex";
                ITM_HammerReflex.Uses[clone] = 20;
                item = clone;
            }
        }
    }
}