#nullable disable
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
        public static RoomCategory OldLibraryRNGCategory = EnumExtensions.ExtendEnum<RoomCategory>("OldLibraryRNG");

        public static WeightedRoomAsset[] officeLayouts = Array.Empty<WeightedRoomAsset>();
        public static WeightedRoomAsset[] unlockedOfficeLayouts = Array.Empty<WeightedRoomAsset>();

        public static bool OfficeSpawnedThisFloor = false;

        public static void LoadRooms(LevelLoaderPlugin loader, string modAssetsPath)
        {
            if (loader == null || string.IsNullOrEmpty(modAssetsPath)) return;

            var assetManager = BasePlugin.assetManager;
            if (assetManager == null) return;

            Texture2D wallFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "Wall");
            Texture2D floorFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "TileFloor");
            Texture2D ceilFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "CeilingNoLight");
            Texture2D carpetFallback = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name.ToLower() == "carpet");

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

            Texture2D wallTex = assetManager.ContainsKey("Bal_Office_Wall") ? assetManager.Get<Texture2D>("Bal_Office_Wall") : null;
            if (wallTex != null && !loader.roomTextureAliases.ContainsKey("Bal_Office_Wall"))
                loader.roomTextureAliases.Add("Bal_Office_Wall", wallTex);

            Texture2D floorTex = assetManager.ContainsKey("Bal_Office_Floor") ? assetManager.Get<Texture2D>("Bal_Office_Floor") : null;
            if (floorTex != null && !loader.roomTextureAliases.ContainsKey("Bal_Office_Floor"))
                loader.roomTextureAliases.Add("Bal_Office_Floor", floorTex);

            Texture2D ceilTex = assetManager.ContainsKey("Bal_Office_Ceiling") ? assetManager.Get<Texture2D>("Bal_Office_Ceiling") : null;
            if (ceilTex != null && !loader.roomTextureAliases.ContainsKey("Bal_Office_Ceiling"))
                loader.roomTextureAliases.Add("Bal_Office_Ceiling", ceilTex);

            Texture2D openTex = assetManager.ContainsKey("Old_BaldisOffice_Open") ? assetManager.Get<Texture2D>("Old_BaldisOffice_Open") : null;
            if (openTex == null) openTex = assetManager.ContainsKey("OfficeClass_Open") ? assetManager.Get<Texture2D>("OfficeClass_Open") : null;
            if (openTex == null)
            {
                string p = BasePlugin.GetModFilePath("OtherStuff", "Old_BaldisOffice_Open.png");
                if (File.Exists(p)) openTex = AssetLoader.TextureFromFile(p);
            }

            Texture2D closedTex = assetManager.ContainsKey("Old_BaldisOffice_Closed") ? assetManager.Get<Texture2D>("Old_BaldisOffice_Closed") : null;
            if (closedTex == null) closedTex = assetManager.ContainsKey("OfficeClass_Closed") ? assetManager.Get<Texture2D>("OfficeClass_Closed") : null;
            if (closedTex == null)
            {
                string p = BasePlugin.GetModFilePath("OtherStuff", "Old_BaldisOffice_Closed.png");
                if (File.Exists(p)) closedTex = AssetLoader.TextureFromFile(p);
            }

            StandardDoorMats officeDoorMats = null;
            if (openTex != null && closedTex != null)
                officeDoorMats = ObjectCreators.CreateDoorDataObject("OfficeClassDoor", openTex, closedTex);

            StandardDoorMats standardDoorMats = Resources.FindObjectsOfTypeAll<RoomAsset>()
                .FirstOrDefault(x => x != null && x.category == RoomCategory.Class && x.doorMats != null)?.doorMats;

            if (standardDoorMats == null) standardDoorMats = officeDoorMats;

            if (!loader.roomSettings.ContainsKey("baldioffice"))
                loader.roomSettings.Add("baldioffice", new RoomSettings(OfficeCategory, RoomType.Room, Color.white, officeDoorMats, null));

            if (!loader.roomSettings.ContainsKey("baldiofficeunlock"))
                loader.roomSettings.Add("baldiofficeunlock", new RoomSettings(UnlockedOfficeCategory, RoomType.Room, Color.white, officeDoorMats, null));

            if (!loader.roomSettings.ContainsKey("mcgonigalroom"))
                loader.roomSettings.Add("mcgonigalroom", new RoomSettings(McGonigalCategory, RoomType.Room, Color.black, standardDoorMats, null));

            if (!loader.roomSettings.ContainsKey("beltroom"))
                loader.roomSettings.Add("beltroom", new RoomSettings(BeltRoomCategory, RoomType.Room, Color.white, standardDoorMats, null));

            if (!loader.roomSettings.ContainsKey("oldlibrary"))
                loader.roomSettings.Add("oldlibrary", new RoomSettings(OldLibraryCategory, RoomType.Room, Color.cyan, standardDoorMats, null));

            if (!loader.roomSettings.ContainsKey("oldlibraryrng"))
                loader.roomSettings.Add("oldlibraryrng", new RoomSettings(OldLibraryRNGCategory, RoomType.Room, Color.cyan, standardDoorMats, null));

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
                                RoomAsset room = (RoomAsset)(object)asset;

                                if (officeDoorMats != null) room.doorMats = officeDoorMats;

                                if (loader.windowObjects.ContainsKey("baldiwindow"))
                                    room.windowObject = loader.windowObjects["baldiwindow"];

                                RestrictOfficeItems(room);
                                myRooms.Add(new WeightedRoomAsset { selection = room, weight = 100 });
                            }
                        }
                    }
                    catch { }
                }
            }

            if (myRooms.Count == 0)
            {
                RoomAsset vanillaClass = Resources.FindObjectsOfTypeAll<RoomAsset>()
                    .FirstOrDefault(x => x != null && x.category == RoomCategory.Class);

                if (vanillaClass != null)
                {
                    RoomAsset officeAsset = UnityEngine.Object.Instantiate(vanillaClass);
                    if (officeAsset != null)
                    {
                        officeAsset.name = "BaldiOfficeAsset";
                        officeAsset.category = OfficeCategory;
                        officeAsset.color = Color.white;
                        officeAsset.keepTextures = false;
                        if (officeDoorMats != null) officeAsset.doorMats = officeDoorMats;

                        if (loader.windowObjects.ContainsKey("baldiwindow"))
                            officeAsset.windowObject = loader.windowObjects["baldiwindow"];

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

        public static void AddRooms(string floorName, int floorNumber, SceneObject sceneObject)
        {
            OfficeSpawnedThisFloor = false;
            if (floorNumber < 2 || sceneObject == null) return;

            if (officeLayouts != null && officeLayouts.Length > 0)
            {
                OfficeSpawnedThisFloor = true;
            }

            CustomLevelObject[] customLevelObjects = CustomLevelObjectExtensions.GetCustomLevelObjects(sceneObject);
            if (customLevelObjects == null || customLevelObjects.Length == 0) return;

            foreach (CustomLevelObject custom in customLevelObjects)
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

        public static RoomGroup CreateOfficeRoomGroup()
        {
            if (officeLayouts == null || officeLayouts.Length == 0) return null;

            RoomGroup group = new RoomGroup();
            group.name = "BaldiOffice";
            group.maxRooms = 1;
            group.minRooms = 0;
            group.stickToHallChance = 1f;
            group.potentialRooms = officeLayouts;

            Texture2D wallTex = BasePlugin.assetManager.ContainsKey("Bal_Office_Wall") ? BasePlugin.assetManager.Get<Texture2D>("Bal_Office_Wall") : null;
            Texture2D floorTex = BasePlugin.assetManager.ContainsKey("Bal_Office_Floor") ? BasePlugin.assetManager.Get<Texture2D>("Bal_Office_Floor") : null;
            Texture2D ceilTex = BasePlugin.assetManager.ContainsKey("Bal_Office_Ceiling") ? BasePlugin.assetManager.Get<Texture2D>("Bal_Office_Ceiling") : null;

            if (wallTex == null) wallTex = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "Wall");
            if (floorTex == null) floorTex = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "TileFloor");
            if (ceilTex == null) ceilTex = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x != null && x.name == "CeilingNoLight");

            if (wallTex != null) group.wallTexture = new[] { new WeightedTexture2D { selection = wallTex, weight = 100 } };
            if (floorTex != null) group.floorTexture = new[] { new WeightedTexture2D { selection = floorTex, weight = 100 } };
            if (ceilTex != null) group.ceilingTexture = new[] { new WeightedTexture2D { selection = ceilTex, weight = 100 } };

            Transform lightPrefab = null;
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

            if (room.itemList != null) room.itemList.Clear();
            if (room.items != null) room.items.Clear();

            if (apple != null && room.itemList != null) room.itemList.Add(apple);
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

        [HarmonyPatch(typeof(LevelGenerator), "Generate")]
        internal static class OfficeFixesPatch
        {
            static void Postfix(LevelGenerator __instance, ref IEnumerator __result)
            {
                var original = __result;
                __result = Wrapper(__instance, original);
            }

            static IEnumerator Wrapper(LevelGenerator __instance, IEnumerator original)
            {
                yield return original;

                EnvironmentController ec = __instance != null ? __instance.Ec : null;
                if (ec != null)
                {
                    var room = ec.rooms.FirstOrDefault(r => r != null && (r.category == RoomsSetup.OfficeCategory || r.category == RoomsSetup.UnlockedOfficeCategory));
                    if (room != null)
                    {
                        var windowObj = LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.windowObjects.ContainsKey("baldiwindow") ? LevelLoaderPlugin.Instance.windowObjects["baldiwindow"] : null;
                        if (windowObj != null)
                        {
                            List<Cell> tilesOfShape = room.GetTilesOfShape((TileShapeMask)6, true);
                            if (tilesOfShape != null)
                            {
                                for (int i = 0; i < tilesOfShape.Count; i++)
                                {
                                    if (!tilesOfShape[i].HasHardFreeWall)
                                    {
                                        tilesOfShape.RemoveAt(i);
                                        i--;
                                    }
                                }

                                int amount = UnityEngine.Random.Range(room.size.x + room.size.z / 6, room.size.x + room.size.z / 3 + 1);
                                for (int j = 0; j < amount; j++)
                                {
                                    if (tilesOfShape.Count == 0) break;
                                    int idx = UnityEngine.Random.Range(0, tilesOfShape.Count);
                                    Cell cell = tilesOfShape[idx];

                                    Direction dir = Direction.Null;
                                    List<Direction> validDirs = new List<Direction>();
                                    foreach (Direction d in Directions.All())
                                    {
                                        if (cell.ConstBin.ContainsDirection(d))
                                            validDirs.Add(d);
                                    }
                                    if (validDirs.Count > 0)
                                        dir = validDirs[UnityEngine.Random.Range(0, validDirs.Count)];

                                    if (dir == Direction.Null)
                                    {
                                        tilesOfShape.RemoveAt(idx);
                                        j--;
                                        continue;
                                    }

                                    var otherCell = ec.CellFromPosition(cell.position + dir.ToIntVector2());
                                    if (otherCell == null || otherCell.WallHardCovered(dir.GetOpposite()) || otherCell.room == room)
                                    {
                                        tilesOfShape.RemoveAt(idx);
                                        j--;
                                    }
                                    else
                                    {
                                        ec.BuildWindow(cell, dir, windowObj);
                                        tilesOfShape.RemoveAt(idx);
                                    }
                                }
                            }
                        }
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