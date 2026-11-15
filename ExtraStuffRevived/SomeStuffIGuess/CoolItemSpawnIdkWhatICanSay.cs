#nullable disable
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ExtraStuffRevived
{
    internal static class ItemSpawnHelper
    {
        public static List<WeightedItemObject> AddWeightedItems(List<WeightedItemObject> list, string[] keys, int weight)
        {
            if (list == null) list = new List<WeightedItemObject>();

            foreach (var key in keys)
            {
                if (BasePlugin.ModItems.TryGetValue(key, out var item) && item != null)
                {
                    if (!list.Any(x => x != null && x.selection == item))
                    {
                        list.Add(new WeightedItemObject { selection = item, weight = weight });
                    }
                }
            }
            return list;
        }
    }

    [HarmonyPatch(typeof(MysteryRoom), "AfterUpdateSetup")]
    internal static class MysteryRoomPatch
    {
        static void Prefix(MysteryRoom __instance)
        {
            var field = AccessTools.Field(typeof(MysteryRoom), "items");
            if (field == null) return;

            var items = field.GetValue(__instance) as WeightedItemObject[];
            var list = items != null ? new List<WeightedItemObject>(items) : new List<WeightedItemObject>();

            ItemSpawnHelper.AddWeightedItems(list, new string[] {
                "itm_coolplant", "itm_emerald", "itm_teacher_nametag", "itm_eventcontroller",
                "itm_speedyelixir", "itm_coolusefulteleporter", "itm_randomizer", "itm_backpack_full"
            }, 100);

            field.SetValue(__instance, list.ToArray());
        }
    }

    [HarmonyPatch(typeof(PartyEvent), "Initialize")]
    internal static class PartyEventPatch
    {
        static void Prefix(PartyEvent __instance)
        {
            var field = AccessTools.Field(typeof(PartyEvent), "potentialItems");
            if (field == null) return;

            var items = field.GetValue(__instance) as WeightedItemObject[];
            var list = items != null ? new List<WeightedItemObject>(items) : new List<WeightedItemObject>();

            ItemSpawnHelper.AddWeightedItems(list, new string[] {
                "itm_nutella", "itm_darkcoffee", "itm_energy_drink", "itm_pinkgum", "itm_oldcandy"
            }, 100);

            field.SetValue(__instance, list.ToArray());
        }
    }

    [HarmonyPatch(typeof(FieldTripBaseRoomFunction), "Initialize")]
    internal static class CampingPatch
    {
        static void Prefix(FieldTripBaseRoomFunction __instance)
        {
            var field = AccessTools.Field(typeof(FieldTripBaseRoomFunction), "potentialItems");
            if (field == null) return;

            var items = field.GetValue(__instance) as WeightedItemObject[];
            var list = items != null ? new List<WeightedItemObject>(items) : new List<WeightedItemObject>();

            ItemSpawnHelper.AddWeightedItems(list, new string[] {
 "itm_tastyburger", "itm_nutella", "itm_darkcoffee",
            "itm_lighter", "itm_instantbalder", "itm_backpack_unfull"
        }, 100);

            field.SetValue(__instance, list.ToArray());
        }
    }
}
