using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.Linq;

namespace ExtraStuffRevived
{
    public static class ShopPatch
    {
        public static void Apply(SceneObject scene)
        {
            if (BasePlugin.Instance == null || scene == null || scene.shopItems == null) return;

            void AddToShop(string key, int weight)
            {
                if (BasePlugin.ModItems.TryGetValue(key, out var item) && item != null)
                {
                    var list = scene.shopItems.ToList();
                    if (!list.Exists(x => x.selection == item))
                    {
                        list.Add(new WeightedItemObject { selection = item, weight = weight });
                        scene.shopItems = list.ToArray();
                    }
                }
            }

            AddToShop("itm_energy_drink", 110);
            AddToShop("itm_clean_chalk_eraser", 45);
            AddToShop("itm_hammer_reflex", 99);
            AddToShop("itm_prototype_teleporter", 100);
            AddToShop("itm_starfire", 67);
            AddToShop("itm_lighter", 20);
            AddToShop("itm_waterbott", 91);
            AddToShop("itm_coindoor", 5);
            AddToShop("itm_coolplant", 30);
            AddToShop("itm_paintbucket", 85);
            AddToShop("itm_gravitystick", 76);
            AddToShop("itm_dash", 70);
            AddToShop("itm_officekey", 80);
            AddToShop("itm_detentionpass", 30);
            AddToShop("itm_controlleralarmclock", 30);
            AddToShop("itm_instantbalder", 50);
            AddToShop("itm_juicebottle", 60);
            AddToShop("itm_backpack_unfull", 20);
            AddToShop("itm_backpack_full", 10);
            AddToShop("itm_gum", 30);
            AddToShop("itm_nutella", 40);
            AddToShop("itm_speedyelixir", 50);
            AddToShop("itm_teacher_nametag", 50);
            AddToShop("itm_oldcandy", 50);
            AddToShop("itm_blastbsoda", 80);
            AddToShop("itm_eventcontroller", 50);
            AddToShop("itm_baldiwhistle", 50);
            AddToShop("itm_darkcoffee", 60);
            AddToShop("itm_coolusefulteleporter", 60);
            AddToShop("itm_safetyshuriken", 40);
            AddToShop("itm_randomizer", 50);
            AddToShop("itm_bsodaplus", 30);
            AddToShop("itm_tastyburger", 40);
        }
    }
}
