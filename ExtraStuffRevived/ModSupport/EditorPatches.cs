#nullable disable
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    if (!studio.selectableTextures.Contains("MM12_BasicRealWall")) studio.selectableTextures.Add("MM12_BasicRealWall");
                    if (!studio.selectableTextures.Contains("MM12_BasicRealFloor")) studio.selectableTextures.Add("MM12_BasicRealFloor");
                    if (!studio.selectableTextures.Contains("MM12_BasicRealCeiling")) studio.selectableTextures.Add("MM12_BasicRealCeiling");
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
                        studio.defaultRoomTextures.Add("mcgonigalroom", new TextureContainer("MM12_BasicRealFloor", "MM12_BasicRealWall", "MM12_BasicRealCeiling"));
                    if (!studio.defaultRoomTextures.ContainsKey("beltroom"))
                        studio.defaultRoomTextures.Add("beltroom", new TextureContainer("DiamondPlateFloor", "DiamondPlateFloor", "DiamondPlateFloor"));
                    if (!studio.defaultRoomTextures.ContainsKey("oldlibrary"))
                        studio.defaultRoomTextures.Add("oldlibrary", new TextureContainer("Carpet", "Old_BookHolder", "CeilingNoLight"));
                    if (!studio.defaultRoomTextures.ContainsKey("oldlibraryrng"))
                        studio.defaultRoomTextures.Add("oldlibraryrng", new TextureContainer("Carpet", "Old_BookHolder", "CeilingNoLight"));
                }

                if (studio.selectableStickers != null && studio.stickerSprites != null)
                {
                    if (BasePlugin.NotebookSticker != Sticker.Nothing && BasePlugin.NotebookStickerSprite != null)
                    {
                        if (!studio.stickerSprites.ContainsKey("notebooksticker"))
                            studio.stickerSprites.Add("notebooksticker", BasePlugin.NotebookStickerSprite);
                        if (!studio.selectableStickers.Contains("notebooksticker"))
                            studio.selectableStickers.Add("notebooksticker");
                    }

                    if (BasePlugin.BsodaBonusSticker != Sticker.Nothing && BasePlugin.BsodaBonusStickerSprite != null)
                    {
                        if (!studio.stickerSprites.ContainsKey("bsodabonussticker"))
                            studio.stickerSprites.Add("bsodabonussticker", BasePlugin.BsodaBonusStickerSprite);
                        if (!studio.selectableStickers.Contains("bsodabonussticker"))
                            studio.selectableStickers.Add("bsodabonussticker");
                    }
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
            if (mode == null) return;

            Sprite defaultSlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("ESSlotIndividual"))
                defaultSlot = BasePlugin.assetManager.Get<Sprite>("ESSlotIndividual");

            Sprite frenzySlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("BaldiFrenzySlotIndividual"))
                frenzySlot = BasePlugin.assetManager.Get<Sprite>("BaldiFrenzySlotIndividual");

            Sprite timesSlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("TimesSlotIndividual"))
                timesSlot = BasePlugin.assetManager.Get<Sprite>("TimesSlotIndividual");

            Sprite alboeSlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("BBIALBOESlotIndividual"))
                alboeSlot = BasePlugin.assetManager.Get<Sprite>("BBIALBOESlotIndividual");

            Sprite bbccsSlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("BBCCSSlotIndividual"))
                bbccsSlot = BasePlugin.assetManager.Get<Sprite>("BBCCSSlotIndividual");

            Sprite taldiSlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("TaldiSlotIndividual"))
                taldiSlot = BasePlugin.assetManager.Get<Sprite>("TaldiSlotIndividual");

            Sprite caseohSlot = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("ExtraSruff_CaseohSlotIndividual"))
                caseohSlot = BasePlugin.assetManager.Get<Sprite>("ExtraSruff_CaseohSlotIndividual");
            else if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("CaseohSlotIndividual"))
                caseohSlot = BasePlugin.assetManager.Get<Sprite>("CaseohSlotIndividual");

            if (BasePlugin.assetManager != null)
            {
                Sprite npcIcon = null;
                if (BasePlugin.assetManager.ContainsKey("Sprites/ExtraStuff_FakeNotebookerIcon"))
                    npcIcon = BasePlugin.assetManager.Get<Sprite>("Sprites/ExtraStuff_FakeNotebookerIcon");
                else if (BasePlugin.assetManager.ContainsKey("Sprites/ExtraStuff_FakerNotebooker"))
                    npcIcon = BasePlugin.assetManager.Get<Sprite>("Sprites/ExtraStuff_FakerNotebooker");

                if (npcIcon != null)
                {
                    var npcTool = new NPCTool("NotebookerFaker", npcIcon);
                    if (defaultSlot != null) npcTool.frameOverride = defaultSlot;
                    EditorInterfaceModes.AddToolToCategory(mode, "npcs", npcTool, false);
                }

                if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.posterAliases != null && LevelLoaderPlugin.Instance.posterAliases.ContainsKey("fakenotebooker_rule"))
                {
                    var posterTool = new PosterTool("fakenotebooker_rule");
                    if (defaultSlot != null) posterTool.frameOverride = defaultSlot;
                    EditorInterfaceModes.AddToolToCategory(mode, "posters", posterTool, false);
                }
            }

            if (BasePlugin.ModItems != null)
            {
                foreach (string key in BasePlugin.ModItems.Keys)
                {
                    if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.itemObjects != null && !LevelLoaderPlugin.Instance.itemObjects.ContainsKey(key))
                        continue;

                    var tool = new ItemTool(key);
                    Sprite currentSlot = defaultSlot;

                    if (key == "itm_ruby" || key == "itm_emerald" || key == "itm_christmasytp")
                        currentSlot = frenzySlot ?? defaultSlot;
                    else if (key == "itm_pinkgum")
                        currentSlot = timesSlot ?? defaultSlot;
                    else if (key == "itm_darkcoffee")
                        currentSlot = alboeSlot ?? defaultSlot;
                    else if (key == "itm_juicebottle" || key == "itm_detentionpass")
                        currentSlot = bbccsSlot ?? defaultSlot;
                    else if (key == "itm_safetyshuriken")
                        currentSlot = taldiSlot ?? defaultSlot;
                    else if (key == "itm_bsodaplus") 
                        currentSlot = caseohSlot ?? defaultSlot;

                    if (currentSlot != null)
                        tool.frameOverride = currentSlot;

                    EditorInterfaceModes.AddToolToCategory(mode, "items", tool, false);
                }
            }

            if (BasePlugin.assetManager != null)
            {
                if (BasePlugin.assetManager.ContainsKey("Editor BaldiOffice"))
                {
                    Sprite icon = BasePlugin.assetManager.Get<Sprite>("Editor BaldiOffice");
                    if (icon != null)
                    {
                        var tool = new RoomTool("baldioffice", icon);
                        if (defaultSlot != null) tool.frameOverride = defaultSlot;
                        EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                    }
                }

                if (BasePlugin.assetManager.ContainsKey("Editor BaldiOfficeUnlock"))
                {
                    Sprite icon = BasePlugin.assetManager.Get<Sprite>("Editor BaldiOfficeUnlock");
                    if (icon != null)
                    {
                        var tool = new RoomTool("baldiofficeunlock", icon);
                        if (defaultSlot != null) tool.frameOverride = defaultSlot;
                        EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                    }
                }

                if (BasePlugin.assetManager.ContainsKey("Editor McGonigalRoom"))
                {
                    Sprite icon = BasePlugin.assetManager.Get<Sprite>("Editor McGonigalRoom");
                    if (icon != null)
                    {
                        var tool = new RoomTool("mcgonigalroom", icon);
                        if (defaultSlot != null) tool.frameOverride = defaultSlot;
                        EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                    }
                }

                if (BasePlugin.assetManager.ContainsKey("Editor BeltRoom"))
                {
                    Sprite icon = BasePlugin.assetManager.Get<Sprite>("Editor BeltRoom");
                    if (icon != null)
                    {
                        var tool = new RoomTool("beltroom", icon);
                        if (defaultSlot != null) tool.frameOverride = defaultSlot;
                        EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                    }
                }

                if (BasePlugin.assetManager.ContainsKey("Editor OldLibraryRoom"))
                {
                    Sprite icon = BasePlugin.assetManager.Get<Sprite>("Editor OldLibraryRoom");
                    if (icon != null)
                    {
                        var tool = new RoomTool("oldlibrary", icon);
                        if (defaultSlot != null) tool.frameOverride = defaultSlot;
                        EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                    }
                }

                Sprite mazeIcon = null;
                if (BasePlugin.assetManager.ContainsKey("Editor MazeRoom"))
                    mazeIcon = BasePlugin.assetManager.Get<Sprite>("Editor MazeRoom");
                if (mazeIcon == null && BasePlugin.assetManager.ContainsKey("Editor OldLibraryRoom"))
                    mazeIcon = BasePlugin.assetManager.Get<Sprite>("Editor OldLibraryRoom");

                if (mazeIcon != null)
                {
                    var tool = new RoomTool("oldlibraryrng", mazeIcon);
                    if (defaultSlot != null) tool.frameOverride = defaultSlot;
                    EditorInterfaceModes.AddToolToCategory(mode, "rooms", tool, false);
                }
            }

            string[] posterKeys = { "MyOfficePlusPoster", "MyOfficeRulesPoster", "MyOfficeToys", "MyOfficeWhiteboard", "MyOfficeWindow", "TestComic", "TestPoster", "hnt_windows" };
            foreach (var key in posterKeys)
            {
                if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.posterAliases != null && LevelLoaderPlugin.Instance.posterAliases.ContainsKey(key))
                {
                    var tool = new PosterTool(key);
                    if (defaultSlot != null) tool.frameOverride = defaultSlot;
                    EditorInterfaceModes.AddToolToCategory(mode, "posters", tool, false);
                }
            }

            WindowsSetup.AddEditorTools(mode, defaultSlot);
        }

        public static void AddLoaderContent()
        {
            if (LevelLoaderPlugin.Instance == null) return;

            if (LevelLoaderPlugin.Instance.itemObjects != null && BasePlugin.ModItems != null)
            {
                foreach (KeyValuePair<string, ItemObject> item in BasePlugin.ModItems)
                {
                    if (item.Value != null && !LevelLoaderPlugin.Instance.itemObjects.ContainsKey(item.Key))
                        LevelLoaderPlugin.Instance.itemObjects.Add(item.Key, item.Value);
                }
            }

            if (LevelLoaderPlugin.Instance.stickerAliases != null)
            {
                if (BasePlugin.NotebookSticker != Sticker.Nothing && !LevelLoaderPlugin.Instance.stickerAliases.ContainsKey("notebooksticker"))
                    LevelLoaderPlugin.Instance.stickerAliases.Add("notebooksticker", BasePlugin.NotebookSticker);

                if (BasePlugin.BsodaBonusSticker != Sticker.Nothing && !LevelLoaderPlugin.Instance.stickerAliases.ContainsKey("bsodabonussticker"))
                    LevelLoaderPlugin.Instance.stickerAliases.Add("bsodabonussticker", BasePlugin.BsodaBonusSticker);
            }
        }
    }
}