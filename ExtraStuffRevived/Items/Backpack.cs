#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI.Registers;

namespace ExtraStuffRevived
{
    public class ITM_BackpackBase : Item
    {
        public override bool Use(PlayerManager pm)
        {
            if (pm == null || pm.itm == null || pm.itm.items == null) return false;

            var itemObject = pm.itm.items[pm.itm.selectedItem];
            if (itemObject != null && BackpackManager.Inventories.TryGetValue(itemObject, out var items))
            {
                if (items.Count > 0)
                {
                    var room = pm.ec.CellFromPosition(pm.transform.position).room;
                    foreach (var item in items)
                    {
                        if (item != null)
                            pm.ec.CreateItem(room, item, new Vector2(pm.transform.position.x, pm.transform.position.z));
                    }
                    items.Clear();
                    var manager = pm.GetComponent<BackpackManager>();
                    if (manager != null)
                        manager.UpdateState(itemObject, items, pm, manager.GetMaxCapacity());
                    var sound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "ItemPickup");
                    if (sound != null && Singleton<CoreGameManager>.Instance != null)
                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle(sound);
                }
            }
            UnityEngine.Object.Destroy(gameObject);
            return false;
        }
    }

    public class ITM_BackpackUnfull : ITM_BackpackBase { }
    public class ITM_BackpackFull : ITM_BackpackBase { }

    [HarmonyPatch(typeof(Pickup), nameof(Pickup.AssignItem))]
    internal static class PickupAssignItemPatch
    {
        static void Prefix(ref ItemObject item)
        {
            if (item == BackpackManager.UnfullItem || item == BackpackManager.FullItem)
                BackpackManager.SetupClonedBackpack(item, ref item);
        }
    }

    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.AddItem), new System.Type[] { typeof(ItemObject) })]
    internal static class ItemManagerAddItemPatch
    {
        static void Prefix(ref ItemObject item)
        {
            if (item == BackpackManager.UnfullItem || item == BackpackManager.FullItem)
                BackpackManager.SetupClonedBackpack(item, ref item);
        }
    }

    public class BackpackInjector : MonoBehaviour
    {
        private void Update()
        {
            var core = Singleton<CoreGameManager>.Instance;
            if (core != null)
            {
                var pm = core.GetPlayer(0);
                if (pm != null && pm.GetComponent<BackpackManager>() == null)
                    pm.gameObject.AddComponent<BackpackManager>();
            }
        }
    }

    public class BackpackManager : MonoBehaviour
    {
        public static Dictionary<ItemObject, List<ItemObject>> Inventories = new Dictionary<ItemObject, List<ItemObject>>();
        private bool pressedLastFrame = false;

        public static ItemObject UnfullItem = null;
        public static ItemObject FullItem = null;

        public static void Setup(ItemObject unfull, ItemObject full)
        {
            UnfullItem = unfull;
            FullItem = full;
        }

        public int GetMaxCapacity()
        {
            int cap = 2;
            if (Singleton<StickerManager>.Instance != null)
                cap += Singleton<StickerManager>.Instance.StickerValue(Sticker.InventorySlot);
            return cap;
        }

        private static bool IsItemRestricted(ItemObject item)
        {
            if (item == null) return true;
            if (item.item is ITM_BackpackBase) return true;
            if (item.itemType == Items.Points || item.itemType == Items.StickerPack) return true;
            if (item.name != null && item.name.ToLower().Contains("key")) return true;
            return false;
        }

        public static void SetupClonedBackpack(ItemObject original, ref ItemObject cloned)
        {
            bool isFull = original == FullItem;
            var instance = UnityEngine.Object.Instantiate(original);
            if (instance == null) return;

            cloned = instance;
            cloned.name = cloned.name.Replace("(Clone)", "").Trim() + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 4);

            var originalMeta = original.GetMeta();
            if (originalMeta != null)
            {
                var plugin = BasePlugin.Instance != null ? BasePlugin.Instance.Info : originalMeta.info;
                var cloneMeta = new ItemMetaData(plugin, cloned);
                cloneMeta.flags = originalMeta.flags;
                if (originalMeta.tags != null)
                {
                    foreach (var tag in originalMeta.tags)
                        cloneMeta.tags.Add(tag);
                }
                ItemMetaStorage.Instance.Add(cloneMeta);
            }

            if (isFull)
            {
                var list = new List<ItemObject>();
                var allItems = Resources.FindObjectsOfTypeAll<ItemObject>()
                    .Where(x => x != null && x.item != null && !IsItemRestricted(x))
                    .ToList();
                for (int i = 0; i < 2; i++)
                {
                    if (allItems.Count > 0)
                        list.Add(allItems[UnityEngine.Random.Range(0, allItems.Count)]);
                }
                Inventories[cloned] = list;
            }
            else
            {
                Inventories[cloned] = new List<ItemObject>();
            }
        }

        private void Update()
        {
            try
            {
                var pm = GetComponent<PlayerManager>();
                if (pm == null || pm.itm == null || pm.itm.items == null) return;
                if (UnfullItem == null || FullItem == null) return;

                int selectedIndex = pm.itm.selectedItem;
                if (selectedIndex < 0 || selectedIndex >= pm.itm.items.Length) return;

                var heldItem = pm.itm.items[selectedIndex];
                int maxCapacity = GetMaxCapacity();

                if (heldItem != null && heldItem.item is ITM_BackpackBase)
                {
                    if (!Inventories.ContainsKey(heldItem)) Inventories[heldItem] = new List<ItemObject>();
                    UpdateState(heldItem, Inventories[heldItem], pm, maxCapacity);
                }

                ItemObject targetBackpack = null;
                if (heldItem != null && heldItem.item is ITM_BackpackBase)
                {
                    targetBackpack = heldItem;
                }
                else if (heldItem != null && heldItem.itemType != Items.None)
                {
                    for (int i = 0; i <= pm.itm.maxItem; i++)
                    {
                        if (i < 0 || i >= pm.itm.items.Length) continue;
                        var it = pm.itm.items[i];
                        if (it != null && it.item is ITM_BackpackBase)
                        {
                            if (!Inventories.ContainsKey(it)) Inventories[it] = new List<ItemObject>();
                            if (Inventories[it].Count < maxCapacity)
                            {
                                targetBackpack = it;
                                break;
                            }
                        }
                    }
                }

                if (targetBackpack == null) return;
                if (!Inventories.ContainsKey(targetBackpack))
                    Inventories[targetBackpack] = new List<ItemObject>();

                var storedItems = Inventories[targetBackpack];
                if (storedItems == null) return;

                bool isPressed = Input.GetKeyDown(KeyCode.X);
                if (!isPressed && BasePlugin.IsRewiredCompatInstalled)
                {
                    try
                    {
                        if (InputManager.Instance != null)
                            isPressed = InputManager.Instance.GetDigitalInput("UseBackpack", true);
                    }
                    catch { }
                }

                if (isPressed && !pressedLastFrame)
                {
                    var core = Singleton<CoreGameManager>.Instance;
                    if (core == null) return;

                    if (heldItem == targetBackpack)
                    {
                        var cam = core.GetCamera(pm.playerNumber);
                        bool itemStored = false;

                        if (cam != null)
                        {
                            int layerMask = Physics.DefaultRaycastLayers;
                            if (pm.pc != null)
                                layerMask = pm.pc.ClickLayers;

                            if (Physics.Raycast(pm.transform.position, cam.transform.forward, out RaycastHit hit, 10f, layerMask, QueryTriggerInteraction.Ignore))
                            {
                                var pickup = hit.transform?.GetComponentInParent<Pickup>();
                                if (pickup != null && pickup.item != null)
                                {
                                    if (IsItemRestricted(pickup.item))
                                    {
                                        PlayErrorSound(core);
                                        pressedLastFrame = isPressed;
                                        return;
                                    }
                                    if (storedItems.Count < maxCapacity)
                                    {
                                        storedItems.Add(pickup.item);
                                        UnityEngine.Object.Destroy(pickup.gameObject);
                                        PlaySound(pm);
                                        UpdateState(targetBackpack, storedItems, pm, maxCapacity);
                                        itemStored = true;
                                    }
                                    else PlayErrorSound(core);
                                }
                            }
                        }

                        if (!itemStored && storedItems.Count > 0)
                        {
                            if (!pm.itm.InventoryFull())
                            {
                                var itemToPull = storedItems[storedItems.Count - 1];
                                storedItems.RemoveAt(storedItems.Count - 1);
                                pm.itm.AddItem(itemToPull);
                                PlaySound(pm);
                                UpdateState(targetBackpack, storedItems, pm, maxCapacity);
                            }
                            else PlayErrorSound(core);
                        }
                    }
                    else if (heldItem != null && heldItem.itemType != Items.None)
                    {
                        if (IsItemRestricted(heldItem))
                        {
                            PlayErrorSound(core);
                            pressedLastFrame = isPressed;
                            return;
                        }
                        if (storedItems.Count < maxCapacity)
                        {
                            storedItems.Add(heldItem);
                            pm.itm.RemoveItem(selectedIndex);
                            PlaySound(pm);
                            UpdateState(targetBackpack, storedItems, pm, maxCapacity);
                        }
                        else PlayErrorSound(core);
                    }
                }

                pressedLastFrame = isPressed;
            }
            catch { }
        }

        public void UpdateState(ItemObject backpack, List<ItemObject> storedItems, PlayerManager pm, int maxCapacity)
        {
            if (backpack == null || FullItem == null || UnfullItem == null) return;

            string newNameKey;
            if (storedItems.Count >= maxCapacity)
            {
                backpack.itemSpriteSmall = FullItem.itemSpriteSmall;
                backpack.itemSpriteLarge = FullItem.itemSpriteLarge;
                newNameKey = $"ExtraStuff_Itm_BackpackFull ({storedItems.Count}/{maxCapacity})";
                backpack.descKey = "ExtraStuff_Desc_BackpackFull";
            }
            else
            {
                backpack.itemSpriteSmall = UnfullItem.itemSpriteSmall;
                backpack.itemSpriteLarge = UnfullItem.itemSpriteLarge;
                newNameKey = $"ExtraStuff_Itm_BackpackUnfull ({storedItems.Count}/{maxCapacity})";
                backpack.descKey = "ExtraStuff_Desc_Backpack";
            }

            if (backpack.nameKey != newNameKey)
            {
                backpack.nameKey = newNameKey;
                if (pm != null && pm.itm != null)
                {
                    pm.itm.UpdateItems();
                    pm.itm.UpdateSelect();
                }
            }
        }

        private void PlaySound(PlayerManager pm)
        {
            var sound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "ItemPickup");
            if (sound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(sound);
        }

        private void PlayErrorSound(CoreGameManager core)
        {
            var err = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "ErrorMaybe");
            if (err != null && core.audMan != null)
                core.audMan.PlaySingle(err);
        }
    }

    [HarmonyPatch]
    internal static class ScannerOnTriggerEnterPatch
    {
        static bool Prepare()
        {
            return AccessTools.TypeByName("CriminalPack.ItemScanner") != null;
        }

        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("CriminalPack.ItemScanner");
            return AccessTools.Method(type, "OnTriggerEnter", new Type[] { typeof(Collider) });
        }

        static bool Prefix(MonoBehaviour __instance, Collider other)
        {
            var type = __instance.GetType();

            var poweredProp = AccessTools.Property(type, "powered");
            if (poweredProp != null && !(bool)poweredProp.GetValue(__instance)) return true;

            var timeTilResetField = AccessTools.Field(type, "timeTilReset");
            var timeTilProcessField = AccessTools.Field(type, "timeTilProcess");
            var foundContrabandField = AccessTools.Field(type, "foundContraband");
            var playerItemCountField = AccessTools.Field(type, "playerItemCount");

            if (timeTilResetField != null && (float)timeTilResetField.GetValue(__instance) != 0f) return true;
            if (timeTilProcessField != null && (float)timeTilProcessField.GetValue(__instance) != 0f) return true;

            if (!other.gameObject.CompareTag("Player")) return true;

            PlayerManager pm = other.GetComponent<PlayerManager>();
            if (pm == null || pm.Tagged) return true;

            var foundContraband = foundContrabandField != null ? (List<Items>)foundContrabandField.GetValue(__instance) : null;
            if (foundContraband == null) return true;

            var slotLockedField = AccessTools.Field(typeof(ItemManager), "slotLocked");
            var slotLocked = slotLockedField != null ? (bool[])slotLockedField.GetValue(pm.itm) : null;

            foundContraband.Clear();
            int playerItemCount = 0;

            for (int i = 0; i < pm.itm.maxItem + 1; i++)
            {
                if (slotLocked != null && slotLocked[i]) continue;
                ItemObject currentItem = pm.itm.items[i];
                if (currentItem == null) continue;

                ItemMetaData meta = currentItem.GetMeta();
                if (meta == null || meta.id == Items.None) continue;

                playerItemCount++;

                bool hasContraband = meta.tags != null && meta.tags.Contains("crmp_contraband");

                if (BackpackManager.Inventories.TryGetValue(currentItem, out var storedItems))
                {
                    foreach (var stored in storedItems)
                    {
                        if (stored == null) continue;
                        var storedMeta = stored.GetMeta();
                        if (storedMeta != null && storedMeta.tags != null && storedMeta.tags.Contains("crmp_contraband"))
                        {
                            hasContraband = true;
                            if (!foundContraband.Contains(stored.itemType))
                                foundContraband.Add(stored.itemType);
                        }
                    }
                }

                if (hasContraband)
                {
                    if (!foundContraband.Contains(currentItem.itemType))
                        foundContraband.Add(currentItem.itemType);
                }
            }

            if (playerItemCountField != null) playerItemCountField.SetValue(__instance, playerItemCount);

            var activateMethod = AccessTools.Method(type, "ActivateForPlayer");
            activateMethod?.Invoke(__instance, new object[] { pm });

            return false;
        }
    }
}
