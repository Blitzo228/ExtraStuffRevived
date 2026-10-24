using MTM101BaldAPI.ObjectCreation;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class EditorPatches
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SetupEditorObjects()
        {
            var studio = LevelStudioPlugin.Instance;
            if (studio != null)
            {
                if (studio.selectableTextures != null)
                {
                    if (!studio.selectableTextures.Contains("Bal_Office_Wall")) studio.selectableTextures.Add("Bal_Office_Wall");
                    if (!studio.selectableTextures.Contains("Bal_Office_Floor")) studio.selectableTextures.Add("Bal_Office_Floor");
                    if (!studio.selectableTextures.Contains("Bal_Office_Ceiling")) studio.selectableTextures.Add("Bal_Office_Ceiling");

                    if (!studio.selectableTextures.Contains("BasicRealWall")) studio.selectableTextures.Add("BasicRealWall");
                    if (!studio.selectableTextures.Contains("BasicRealCarpet")) studio.selectableTextures.Add("BasicRealCarpet");
                    if (!studio.selectableTextures.Contains("BasicRealCeiling")) studio.selectableTextures.Add("BasicRealCeiling");

                    if (!studio.selectableTextures.Contains("DiamondPlateFloor")) studio.selectableTextures.Add("DiamondPlateFloor");
                    if (!studio.selectableTextures.Contains("Carpet")) studio.selectableTextures.Add("Carpet");
                    if (!studio.selectableTextures.Contains("CeilingNoLight")) studio.selectableTextures.Add("CeilingNoLight");
                    if (!studio.selectableTextures.Contains("Old_BookHolder")) studio.selectableTextures.Add("Old_BookHolder");
                    if (!studio.selectableTextures.Contains("OldWood")) studio.selectableTextures.Add("OldWood");
                    if (!studio.selectableTextures.Contains("Oldfence")) studio.selectableTextures.Add("Oldfence");
                }

                if (studio.defaultRoomTextures != null)
                {
                    if (!studio.defaultRoomTextures.ContainsKey("baldioffice"))
                        studio.defaultRoomTextures.Add("baldioffice", new TextureContainer("Bal_Office_Floor", "Bal_Office_Wall", "Bal_Office_Ceiling"));

                    if (!studio.defaultRoomTextures.ContainsKey("baldiofficeunlock"))
                        studio.defaultRoomTextures.Add("baldiofficeunlock", new TextureContainer("Bal_Office_Floor", "Bal_Office_Wall", "Bal_Office_Ceiling"));

                    if (!studio.defaultRoomTextures.ContainsKey("mcgonigalroom"))
                        studio.defaultRoomTextures.Add("mcgonigalroom", new TextureContainer("BasicRealCarpet", "BasicRealWall", "BasicRealCeiling"));

                    if (!studio.defaultRoomTextures.ContainsKey("beltroom"))
                        studio.defaultRoomTextures.Add("beltroom", new TextureContainer("DiamondPlateFloor", "DiamondPlateFloor", "DiamondPlateFloor"));

                    if (!studio.defaultRoomTextures.ContainsKey("oldlibrary"))
                        studio.defaultRoomTextures.Add("oldlibrary", new TextureContainer("Carpet", "Old_BookHolder", "CeilingNoLight"));

                    if (!studio.defaultRoomTextures.ContainsKey("oldlibraryrng"))
                        studio.defaultRoomTextures.Add("oldlibraryrng", new TextureContainer("Carpet", "Old_BookHolder", "CeilingNoLight"));
                }
            }

            if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.roomSettings != null)
            {
                if (!LevelLoaderPlugin.Instance.roomSettings.ContainsKey("baldioffice"))
                    LevelLoaderPlugin.Instance.roomSettings.Add("baldioffice", new RoomSettings(RoomsSetup.OfficeCategory, (RoomType)2, Color.white, null, null));

                if (!LevelLoaderPlugin.Instance.roomSettings.ContainsKey("baldiofficeunlock"))
                    LevelLoaderPlugin.Instance.roomSettings.Add("baldiofficeunlock", new RoomSettings(RoomsSetup.UnlockedOfficeCategory, (RoomType)2, Color.white, null, null));

                if (!LevelLoaderPlugin.Instance.roomSettings.ContainsKey("mcgonigalroom"))
                    LevelLoaderPlugin.Instance.roomSettings.Add("mcgonigalroom", new RoomSettings(RoomsSetup.McGonigalCategory, (RoomType)2, Color.black, null, null));

                if (!LevelLoaderPlugin.Instance.roomSettings.ContainsKey("beltroom"))
                    LevelLoaderPlugin.Instance.roomSettings.Add("beltroom", new RoomSettings(RoomsSetup.BeltRoomCategory, (RoomType)2, Color.white, null, null));

                if (!LevelLoaderPlugin.Instance.roomSettings.ContainsKey("oldlibrary"))
                    LevelLoaderPlugin.Instance.roomSettings.Add("oldlibrary", new RoomSettings(RoomsSetup.OldLibraryCategory, (RoomType)2, Color.cyan, null, null));

                if (!LevelLoaderPlugin.Instance.roomSettings.ContainsKey("oldlibraryrng"))
                    LevelLoaderPlugin.Instance.roomSettings.Add("oldlibraryrng", new RoomSettings(RoomsSetup.OldLibraryRNGCategory, (RoomType)2, Color.cyan, null, null));
            }

            EditorInterfaceModes.AddModeCallback((Action<EditorMode, bool>)AddContent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddContent(EditorMode mode, bool vanilla)
        {
            Sprite? slotSprite = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("ESSlotIndividual"))
                slotSprite = BasePlugin.assetManager.Get<Sprite>("ESSlotIndividual");

            if (BasePlugin.ModItems != null)
            {
                foreach (string key in BasePlugin.ModItems.Keys)
                {
                    var tool = new ItemTool(key);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "items", tool, false);
                }
            }

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor BaldiOffice"))
            {
                Sprite? icon = BasePlugin.assetManager.Get<Sprite>("Editor BaldiOffice");
                if (icon != null)
                {
                    var tool = new RoomTool("baldioffice", icon);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                }
            }

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor BaldiOfficeUnlock"))
            {
                Sprite? icon = BasePlugin.assetManager.Get<Sprite>("Editor BaldiOfficeUnlock");
                if (icon != null)
                {
                    var tool = new RoomTool("baldiofficeunlock", icon);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                }
            }

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor McGonigalRoom"))
            {
                Sprite? icon = BasePlugin.assetManager.Get<Sprite>("Editor McGonigalRoom");
                if (icon != null)
                {
                    var tool = new RoomTool("mcgonigalroom", icon);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                }
            }

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor BeltRoom"))
            {
                Sprite? icon = BasePlugin.assetManager.Get<Sprite>("Editor BeltRoom");
                if (icon != null)
                {
                    var tool = new RoomTool("beltroom", icon);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                }
            }

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor OldLibraryRoom"))
            {
                Sprite? icon = BasePlugin.assetManager.Get<Sprite>("Editor OldLibraryRoom");
                if (icon != null)
                {
                    var tool = new RoomTool("oldlibrary", icon);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                }
            }

            Sprite? mazeIcon = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor MazeRoom"))
                mazeIcon = BasePlugin.assetManager.Get<Sprite>("Editor MazeRoom");

            if (mazeIcon == null && BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Editor OldLibraryRoom"))
                mazeIcon = BasePlugin.assetManager.Get<Sprite>("Editor OldLibraryRoom");

            if (mazeIcon != null)
            {
                var tool = new RoomTool("oldlibraryrng", mazeIcon);
                if (slotSprite != null) tool.frameOverride = slotSprite;
                EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
            }

            string[] posterKeys = { "MyOfficePlusPoster", "MyOfficeRulesPoster", "MyOfficeToys", "MyOfficeWhiteboard", "MyOfficeWindow", "TestComic", "TestPoster", "hnt_windows" };
            foreach (var key in posterKeys)
            {
                if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.posterAliases != null && LevelLoaderPlugin.Instance.posterAliases.ContainsKey(key))
                {
                    var tool = new PosterTool(key);
                    if (slotSprite != null) tool.frameOverride = slotSprite;
                    EditorInterfaceModes.AddToolToCategory(mode, "posters", tool, false);
                }
            }

            WindowsSetup.AddEditorTools(mode, slotSprite);
        }

        public static void AddLoaderContent()
        {
            if (LevelLoaderPlugin.Instance == null || LevelLoaderPlugin.Instance.itemObjects == null) return;

            if (BasePlugin.ModItems != null)
            {
                foreach (KeyValuePair<string, ItemObject> item in BasePlugin.ModItems)
                {
                    if (item.Value != null && !LevelLoaderPlugin.Instance.itemObjects.ContainsKey(item.Key))
                        LevelLoaderPlugin.Instance.itemObjects.Add(item.Key, item.Value);
                }
            }
        }
    }
}