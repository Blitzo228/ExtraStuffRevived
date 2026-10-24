using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class RoomsSetup
    {
        public static RoomCategory OfficeCategory = EnumExtensions.ExtendEnum<RoomCategory>("BaldiOffice");
        public static RoomCategory UnlockedOfficeCategory = EnumExtensions.ExtendEnum<RoomCategory>("BaldiOfficeUnlock");
        public static RoomCategory McGonigalCategory = EnumExtensions.ExtendEnum<RoomCategory>("McGonigalRoom");
        public static RoomCategory BeltRoomCategory = EnumExtensions.ExtendEnum<RoomCategory>("BeltRoomCategory");
        public static RoomCategory OldLibraryCategory = EnumExtensions.ExtendEnum<RoomCategory>("OldLibraryCategory");
        public static RoomCategory OldLibraryRNGCategory = EnumExtensions.ExtendEnum<RoomCategory>("OldLibraryRNGCategory");

        public static WeightedRoomAsset[] officeLayouts = Array.Empty<WeightedRoomAsset>();
        public static WeightedRoomAsset[] unlockedOfficeLayouts = Array.Empty<WeightedRoomAsset>();

        public static bool OfficeSpawnedThisFloor = false;

        public static void LoadRooms(LevelLoaderPlugin loader, string modAssetsPath, AssetManager assetManager)
        {
            Texture2D? wallFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "Wall");
            Texture2D? floorFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "TileFloor");
            Texture2D? ceilFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "CeilingNoLight");
            Texture2D? carpetFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name.ToLower() == "carpet");

            if (carpetFallback == null)
            {
                var mat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m != null && m.name.ToLower().Contains("carpet"));
                if (mat != null) carpetFallback = mat.mainTexture as Texture2D;
            }

            if (wallFallback != null && !loader.roomTextureAliases.ContainsKey("Wall"))
                loader.roomTextureAliases.Add("Wall", wallFallback);
            if (floorFallback != null && !loader.roomTextureAliases.ContainsKey("TileFloor"))
                loader.roomTextureAliases.Add("TileFloor", floorFallback);
            if (ceilFallback != null && !loader.roomTextureAliases.ContainsKey("CeilingNoLight"))
                loader.roomTextureAliases.Add("CeilingNoLight", ceilFallback);
            if (carpetFallback != null && !loader.roomTextureAliases.ContainsKey("Carpet"))
                loader.roomTextureAliases.Add("Carpet", carpetFallback);

            Texture2D? wallTex = assetManager.ContainsKey("Bal_Office_Wall") ? assetManager.Get<Texture2D>("Bal_Office_Wall") : null;
            if (wallTex != null && !loader.roomTextureAliases.ContainsKey("Bal_Office_Wall"))
                loader.roomTextureAliases.Add("Bal_Office_Wall", wallTex);

            Texture2D? floorTex = assetManager.ContainsKey("Bal_Office_Floor") ? assetManager.Get<Texture2D>("Bal_Office_Floor") : null;
            if (floorTex != null && !loader.roomTextureAliases.ContainsKey("Bal_Office_Floor"))
                loader.roomTextureAliases.Add("Bal_Office_Floor", floorTex);

            Texture2D? ceilTex = assetManager.ContainsKey("Bal_Office_Ceiling") ? assetManager.Get<Texture2D>("Bal_Office_Ceiling") : null;
            if (ceilTex != null && !loader.roomTextureAliases.ContainsKey("Bal_Office_Ceiling"))
                loader.roomTextureAliases.Add("Bal_Office_Ceiling", ceilTex);

            Texture2D? openTex = assetManager.ContainsKey("OfficeClass_Open") ? assetManager.Get<Texture2D>("OfficeClass_Open") : null;
            Texture2D? closedTex = assetManager.ContainsKey("OfficeClass_Closed") ? assetManager.Get<Texture2D>("OfficeClass_Closed") : null;

            StandardDoorMats? officeDoorMats = null;
            if (openTex != null && closedTex != null)
                officeDoorMats = ObjectCreators.CreateDoorDataObject("OfficeClassDoor", openTex, closedTex);

            StandardDoorMats? standardDoorMats = Resources.FindObjectsOfTypeAll<RoomAsset>()
                .FirstOrDefault(x => x != null && x.category == RoomCategory.Class && x.doorMats != null)?.doorMats;

            if (standardDoorMats == null)
                standardDoorMats = officeDoorMats;

            if (!loader.roomSettings.ContainsKey("baldioffice"))
                loader.roomSettings.Add("baldioffice", new RoomSettings(OfficeCategory, (RoomType)2, Color.white, officeDoorMats, null));

            if (!loader.roomSettings.ContainsKey("baldiofficeunlock"))
                loader.roomSettings.Add("baldiofficeunlock", new RoomSettings(UnlockedOfficeCategory, (RoomType)2, Color.white, officeDoorMats, null));

            Texture2D? mcGWallTex = assetManager.ContainsKey("BasicRealWall") ? assetManager.Get<Texture2D>("BasicRealWall") : null;
            if (mcGWallTex != null && !loader.roomTextureAliases.ContainsKey("BasicRealWall"))
                loader.roomTextureAliases.Add("BasicRealWall", mcGWallTex);

            Texture2D? mcGFloorTex = assetManager.ContainsKey("BasicRealCarpet") ? assetManager.Get<Texture2D>("BasicRealCarpet") : null;
            if (mcGFloorTex != null && !loader.roomTextureAliases.ContainsKey("BasicRealCarpet"))
                loader.roomTextureAliases.Add("BasicRealCarpet", mcGFloorTex);

            Texture2D? mcGCeilTex = assetManager.ContainsKey("BasicRealCeiling") ? assetManager.Get<Texture2D>("BasicRealCeiling") : null;
            if (mcGCeilTex != null && !loader.roomTextureAliases.ContainsKey("BasicRealCeiling"))
                loader.roomTextureAliases.Add("BasicRealCeiling", mcGCeilTex);

            Texture2D? mcGOpenTex = assetManager.ContainsKey("McGonigal_Open") ? assetManager.Get<Texture2D>("McGonigal_Open") : null;
            Texture2D? mcGClosedTex = assetManager.ContainsKey("McGonigal_Closed") ? assetManager.Get<Texture2D>("McGonigal_Closed") : null;

            StandardDoorMats? mcGDoorMats = null;
            if (mcGOpenTex != null && mcGClosedTex != null)
                mcGDoorMats = ObjectCreators.CreateDoorDataObject("McGonigalDoor", mcGOpenTex, mcGClosedTex);

            if (mcGDoorMats == null) mcGDoorMats = standardDoorMats;

            if (!loader.roomSettings.ContainsKey("mcgonigalroom"))
                loader.roomSettings.Add("mcgonigalroom", new RoomSettings(McGonigalCategory, (RoomType)2, Color.black, mcGDoorMats, null));

            Texture2D? oldBookWallTex = assetManager.ContainsKey("Old_BookHolder") ? assetManager.Get<Texture2D>("Old_BookHolder") : null;
            if (oldBookWallTex != null && !loader.roomTextureAliases.ContainsKey("Old_BookHolder"))
                loader.roomTextureAliases.Add("Old_BookHolder", oldBookWallTex);

            if (!loader.roomSettings.ContainsKey("beltroom"))
                loader.roomSettings.Add("beltroom", new RoomSettings(BeltRoomCategory, (RoomType)2, Color.white, standardDoorMats, null));

            if (!loader.roomSettings.ContainsKey("oldlibrary"))
                loader.roomSettings.Add("oldlibrary", new RoomSettings(OldLibraryCategory, (RoomType)2, Color.cyan, standardDoorMats, null));

            Texture2D? oldWoodTex = assetManager.ContainsKey("OldWood") ? assetManager.Get<Texture2D>("OldWood") : null;
            if (oldWoodTex != null && !loader.roomTextureAliases.ContainsKey("OldWood"))
                loader.roomTextureAliases.Add("OldWood", oldWoodTex);

            Texture2D? oldFenceTex = assetManager.ContainsKey("Oldfence") ? assetManager.Get<Texture2D>("Oldfence") : null;
            if (oldFenceTex != null && !loader.roomTextureAliases.ContainsKey("Oldfence"))
                loader.roomTextureAliases.Add("Oldfence", oldFenceTex);

            if (!loader.roomSettings.ContainsKey("oldlibraryrng"))
                loader.roomSettings.Add("oldlibraryrng", new RoomSettings(OldLibraryRNGCategory, (RoomType)2, Color.cyan, standardDoorMats, null));

            List<WeightedRoomAsset> myRooms = new List<WeightedRoomAsset>();
            string layoutsPath = Path.Combine(modAssetsPath, "RoomLayouts", "BaldiOffice");

            if (Directory.Exists(layoutsPath))
            {
                string[] roomPaths = Directory.GetFiles(layoutsPath, "*.rbpl");
                foreach (string filePath in roomPaths)
                {
                    try
                    {
                        using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
                        {
                            BaldiRoomAsset formatAsset = BaldiRoomAsset.Read(reader);
                            ExtendedRoomAsset asset = LevelImporter.CreateRoomAsset(formatAsset, false);
                            if (asset != null)
                            {
                                RestrictOfficeItems((RoomAsset)(object)asset);
                                myRooms.Add(new WeightedRoomAsset { selection = (RoomAsset)(object)asset, weight = 100 });
                            }
                        }
                    }
                    catch { }
                }
            }

            if (myRooms.Count == 0)
            {
                RoomAsset? vanillaClass = Resources.FindObjectsOfTypeAll<RoomAsset>()
                    .FirstOrDefault(x => x != null && x.category == RoomCategory.Class);

                if (vanillaClass != null)
                {
                    RoomAsset? officeAsset = UnityEngine.Object.Instantiate(vanillaClass);
                    if (officeAsset != null)
                    {
                        officeAsset.name = "BaldiOfficeAsset";
                        officeAsset.category = OfficeCategory;
                        officeAsset.color = Color.white;
                        officeAsset.keepTextures = false;
                        if (officeDoorMats != null) officeAsset.doorMats = officeDoorMats;
                        RestrictOfficeItems(officeAsset);
                        myRooms.Add(new WeightedRoomAsset { selection = officeAsset, weight = 100 });
                    }
                }
            }

            officeLayouts = myRooms.ToArray();

            List<WeightedRoomAsset> unlockedRooms = new List<WeightedRoomAsset>();
            foreach (var weighted in officeLayouts)
            {
                var clonedAsset = UnityEngine.Object.Instantiate(weighted.selection);
                clonedAsset.name = "Unlocked" + clonedAsset.name;
                clonedAsset.category = UnlockedOfficeCategory;
                unlockedRooms.Add(new WeightedRoomAsset { selection = clonedAsset, weight = weighted.weight });
            }
            unlockedOfficeLayouts = unlockedRooms.ToArray();
        }

        public static void AddRooms(SceneObject scene, int levelNumber)
        {
            OfficeSpawnedThisFloor = false;
            if (levelNumber < 2) return;
            if (scene == null) return;

            if (officeLayouts != null && officeLayouts.Length > 0)
            {
                OfficeSpawnedThisFloor = true;
            }

            CustomLevelObject[]? customLevelObjects = CustomLevelObjectExtensions.GetCustomLevelObjects(scene);
            if (customLevelObjects == null || customLevelObjects.Length == 0) return;

            foreach (CustomLevelObject? custom in customLevelObjects)
            {
                if (custom == null) continue;

                if (officeLayouts != null && officeLayouts.Length > 0)
                {
                    bool exists = false;
                    if (custom.roomGroup != null)
                    {
                        foreach (var g in custom.roomGroup)
                            if (g != null && g.name == "BaldiOffice") { exists = true; break; }
                    }
                    if (!exists)
                    {
                        var group = CreateOfficeRoomGroup();
                        if (group != null)
                        {
                            if (custom.roomGroup == null)
                                custom.roomGroup = new RoomGroup[] { group };
                            else
                                custom.roomGroup = HarmonyLib.CollectionExtensions.AddRangeToArray(custom.roomGroup, new RoomGroup[] { group });
                        }
                    }
                }
            }
        }

        public static RoomGroup? CreateOfficeRoomGroup()
        {
            if (officeLayouts == null || officeLayouts.Length == 0) return null;

            RoomGroup group = new RoomGroup();
            group.name = "BaldiOffice";
            group.maxRooms = 1;
            group.minRooms = 0;
            group.stickToHallChance = 0f;
            group.potentialRooms = officeLayouts;

            Texture2D? wallTex = BasePlugin.assetManager.ContainsKey("Bal_Office_Wall") ? BasePlugin.assetManager.Get<Texture2D>("Bal_Office_Wall") : null;
            Texture2D? floorTex = BasePlugin.assetManager.ContainsKey("Bal_Office_Floor") ? BasePlugin.assetManager.Get<Texture2D>("Bal_Office_Floor") : null;
            Texture2D? ceilTex = BasePlugin.assetManager.ContainsKey("Bal_Office_Ceiling") ? BasePlugin.assetManager.Get<Texture2D>("Bal_Office_Ceiling") : null;

            if (wallTex != null) group.wallTexture = new[] { new WeightedTexture2D { selection = wallTex, weight = 100 } };
            if (floorTex != null) group.floorTexture = new[] { new WeightedTexture2D { selection = floorTex, weight = 100 } };
            if (ceilTex != null) group.ceilingTexture = new[] { new WeightedTexture2D { selection = ceilTex, weight = 100 } };

            Transform? lightPrefab = null;
            if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.lightTransforms != null)
            {
                if (LevelLoaderPlugin.Instance.lightTransforms.TryGetValue("StandardHangingLight", out var l1)) lightPrefab = l1;
                else if (LevelLoaderPlugin.Instance.lightTransforms.TryGetValue("fluorescent", out var l2)) lightPrefab = l2;
            }

            if (lightPrefab != null)
                group.light = new[] { new WeightedTransform { selection = lightPrefab, weight = 100 } };
            else if (officeLayouts[0].selection != null && officeLayouts[0].selection.lightPre != null)
                group.light = new[] { new WeightedTransform { selection = officeLayouts[0].selection.lightPre, weight = 100 } };
            else
                group.light = new WeightedTransform[0];

            return group;
        }

        private static void RestrictOfficeItems(RoomAsset room)
        {
            if (room == null) return;

            var allItems = Resources.FindObjectsOfTypeAll<ItemObject>();
            var apple = allItems.FirstOrDefault(x => x != null && x.name.ToLowerInvariant().Contains("apple"));
          
            room.itemList.Clear();
            room.items.Clear();

            if (apple != null) room.itemList.Add(apple);

        }

        [HarmonyPatch(typeof(LevelGenerator), "Generate")]
        internal static class ForceRoomGroupsPatch
        {
            static void Prefix(LevelGenerator __instance)
            {
                if (__instance == null) return;
                var ld = __instance.ld;
                if (ld == null) return;
                if (BasePlugin.Instance == null) return;

                bool hasOffice = false;
                var existing = ld.roomGroup;
                if (existing != null)
                {
                    foreach (var g in existing)
                    {
                        if (g != null && g.name == "BaldiOffice") hasOffice = true;
                    }
                }

                if (OfficeSpawnedThisFloor && !hasOffice)
                {
                    var group = CreateOfficeRoomGroup();
                    if (group != null)
                    {
                        if (existing == null) ld.roomGroup = new List<RoomGroup> { group };
                        else existing.Add(group);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CoreGameManager), "ReturnToMenu")]
        internal static class ResetOfficeFlagsPatch
        {
            static void Postfix()
            {
                OfficeSpawnedThisFloor = false;
            }
        }
    }
}

