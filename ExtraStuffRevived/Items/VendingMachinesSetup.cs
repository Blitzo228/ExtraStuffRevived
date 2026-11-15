#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using Newtonsoft.Json;
using PixelInternalAPI.Extensions;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class VendingMachineData
    {
        public int sodaMachineWeight = 100;
        public int usesLeft = 1;
        public string normalTextureFileName = string.Empty;
        public string outOfStockTextureFileName = string.Empty;
        public VendingMachineItem[] items = Array.Empty<VendingMachineItem>();
        public string[] acceptableItems = Array.Empty<string>();
        public string[] allowedLevels = Array.Empty<string>();
        public string[] allowedLevelTypes = Array.Empty<string>();

        [JsonIgnore] public SodaMachine machine;
        [JsonIgnore] public HashSet<LevelType> convertedLevelTypes = new HashSet<LevelType>();
    }

    public class VendingMachineItem
    {
        public string item = "None";
        public int weight = 100;
        public string mod_guid = string.Empty;
    }

    public static class VendingMachinesSetup
    {
        public static List<VendingMachineData> SodaMachines = new List<VendingMachineData>();

        public static void LoadData(string modPath)
        {
            string path = Path.Combine(modPath, "VendingMachines");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<VendingMachineData>(File.ReadAllText(file));
                    if (data != null) SodaMachines.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to load vending machine JSON: " + Path.GetFileName(file) + "\n" + e.Message);
                }
            }
        }

        public static void InitializeMachines()
        {
            foreach (var data in SodaMachines)
            {
                if (data.machine != null) continue;

                try
                {
                    Texture2D normalTex = null;
                    if (!string.IsNullOrEmpty(data.normalTextureFileName))
                    {
                        string p = Path.Combine(BasePlugin.ModAssetsPath, "VendingMachines", data.normalTextureFileName + ".png");
                        if (File.Exists(p)) normalTex = AssetLoader.TextureFromFile(p);
                        else if (BasePlugin.assetManager.ContainsKey(data.normalTextureFileName))
                            normalTex = BasePlugin.assetManager.Get<Texture2D>(data.normalTextureFileName);
                    }

                    Texture2D outOfStockTex = null;
                    if (!string.IsNullOrEmpty(data.outOfStockTextureFileName))
                    {
                        string p = Path.Combine(BasePlugin.ModAssetsPath, "VendingMachines", data.outOfStockTextureFileName + ".png");
                        if (File.Exists(p)) outOfStockTex = AssetLoader.TextureFromFile(p);
                        else if (BasePlugin.assetManager.ContainsKey(data.outOfStockTextureFileName))
                            outOfStockTex = BasePlugin.assetManager.Get<Texture2D>(data.outOfStockTextureFileName);
                    }

                    if (normalTex == null)
                    {
                        var vanilla = Resources.FindObjectsOfTypeAll<SodaMachine>().FirstOrDefault(x => x.name.Contains("SodaMachine"));
                        if (vanilla != null)
                            normalTex = vanilla.GetComponentInChildren<Renderer>()?.material?.mainTexture as Texture2D;
                    }
                    if (normalTex == null) continue;

                    WeightedItemObject[] weightedItems = new WeightedItemObject[data.items.Length];
                    for (int i = 0; i < data.items.Length; i++)
                    {
                        ItemObject item = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(data.items[i].mod_guid))
                            {
                                var meta = ItemMetaStorage.Instance.FindByEnumFromMod(EnumExtensions.GetFromExtendedName<Items>(data.items[i].item), Chainloader.PluginInfos[data.items[i].mod_guid]);
                                item = meta?.value;
                            }
                            else
                            {
                                var meta = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>(data.items[i].item));
                                item = meta?.value;
                            }
                        }
                        catch { }

                        if (item == null && BasePlugin.ModItems.ContainsKey(data.items[i].item))
                            item = BasePlugin.ModItems[data.items[i].item];

                        if (item != null)
                            weightedItems[i] = new WeightedItemObject { weight = data.items[i].weight, selection = item };
                    }

                    weightedItems = weightedItems.Where(x => x != null).ToArray();
                    if (weightedItems.Length == 0) continue;

                    data.machine = ObjectCreationExtensions.CreateSodaMachineInstance(normalTex, outOfStockTex ?? normalTex);
                    data.machine.name = "ExtraStuff_VendingMachine_" + Path.GetFileNameWithoutExtension(data.normalTextureFileName);
                    data.machine.SetUses(data.usesLeft);
                    data.machine.SetPotentialItems(weightedItems);

                    if (data.acceptableItems.Length > 0)
                    {
                        HashSet<Items> acceptable = new HashSet<Items>();
                        foreach (var acc in data.acceptableItems)
                        {
                            try { acceptable.Add(EnumExtensions.GetFromExtendedName<Items>(acc)); } catch { }
                        }
                        if (acceptable.Count > 0) data.machine.SetRequiredItems(acceptable.ToArray());
                    }

                    if (data.allowedLevelTypes.Length == 0)
                    {
                        var nums = EnumExtensions.GetValues<LevelType>();
                        for (int i = 0; i < nums.Length; i++)
                            data.convertedLevelTypes.Add((LevelType)nums[i]);
                    }
                    else
                    {
                        foreach (var type in data.allowedLevelTypes)
                        {
                            try { data.convertedLevelTypes.Add(EnumExtensions.GetFromExtendedName<LevelType>(type)); } catch { }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to create vending machine: " + e.Message);
                }
            }
        }

        public static void AddVendingMachinesToScene(string floorName, int floorNumber, SceneObject sceneObject)
        {
            if (sceneObject == null) return;

            if (SodaMachines.Any(m => m.machine == null))
                InitializeMachines();

            var available = SodaMachines.Where(m => m.machine != null && (m.allowedLevels.Length == 0 || m.allowedLevels.Contains(floorName))).ToList();
            if (available.Count == 0) return;

            var placer = Resources.FindObjectsOfTypeAll<Structure_EnvironmentObjectPlacer>().FirstOrDefault(x => x.name == "Structure_EnvironmentObjectBuilder_Weighted");
            if (placer == null) return;

            CustomLevelObject[] customLevelObjects = CustomLevelObjectExtensions.GetCustomLevelObjects(sceneObject);
            if (customLevelObjects == null) return;

            foreach (var customLevel in customLevelObjects)
            {
                var validMachines = available.Where(m => m.convertedLevelTypes.Contains(customLevel.type)).ToArray();
                if (validMachines.Length == 0) continue;

                var weightArray = validMachines.Select(m => new WeightedGameObject { selection = m.machine.gameObject, weight = m.sodaMachineWeight }).ToArray();

                customLevel.forcedStructures = HarmonyLib.CollectionExtensions.AddToArray(customLevel.forcedStructures, new StructureWithParameters
                {
                    prefab = placer,
                    parameters = new StructureParameters
                    {
                        minMax = new IntVector2[] { new IntVector2(1, 3) },
                        chance = new float[] { 0.5f },
                        prefab = weightArray
                    }
                });
            }
        }
    }
}