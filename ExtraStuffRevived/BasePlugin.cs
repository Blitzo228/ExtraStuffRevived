using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using UnityEngine;
using UnityEngine.Audio;

namespace ExtraStuffRevived
{
    [BepInPlugin(ModInfo.ID, ModInfo.NAME, ModInfo.VERSION)]
    [BepInDependency(MTM101BaldiDevAPI.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("alexbw145.bbplus.rewiredcompat", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.criminalpackroot", BepInDependency.DependencyFlags.SoftDependency)]
    public class BasePlugin : BaseUnityPlugin
    {
        public static BasePlugin Instance = null!;
        public static AssetManager assetManager = new AssetManager();
        public static Dictionary<string, ItemObject> ModItems = new Dictionary<string, ItemObject>();

        public static bool IsRewiredCompatInstalled { get; private set; }
        private static bool inputsRegistered = false;

        public static Sticker NotebookSticker;
        public static Sprite? NotebookStickerSprite;
        public static Sticker BsodaBonusSticker;
        public static Sprite? BsodaBonusStickerSprite;

        public static string ModAssetsPath => Path.Combine(Application.streamingAssetsPath, "Modded", ModInfo.ID);

        public static string GetModFilePath(string folder, string filename)
        {
            string dir = Path.Combine(ModAssetsPath, folder);
            string path = Path.Combine(dir, filename);
            if (File.Exists(path)) return path;
            string prefixedPath = Path.Combine(dir, "ExtraStuff_" + filename);
            if (File.Exists(prefixedPath)) return prefixedPath;
            string typoPath = Path.Combine(dir, "ExtraSruff_" + filename);
            if (File.Exists(typoPath)) return typoPath;
            return path;
        }

        private void Awake()
        {
            Instance = this;
            IsRewiredCompatInstalled = Chainloader.PluginInfos.ContainsKey("alexbw145.bbplus.rewiredcompat");

            Harmony harmony = new Harmony(ModInfo.ID);

            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0 ||
                    type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
                {
                    try { harmony.CreateClassProcessor(type).Patch(); }
                    catch { }
                }
            }

            LoadAssets();
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadMod(), LoadingEventOrder.Pre);

            GeneratorManagement.Register(this, GenerationModType.Addend, (levelName, levelNumber, scene) =>
            {
                if (scene == null) return;
                ShopPatch.Apply(scene);
                RoomsSetup.AddRooms(levelName, levelNumber, scene);
                VendingMachinesSetup.AddVendingMachinesToScene(levelName, levelNumber, scene);

                if (NotebookSticker != Sticker.Nothing && scene.potentialStickers != null)
                {
                    if (!scene.potentialStickers.Any(s => s != null && s.selection == NotebookSticker))
                        scene.potentialStickers = HarmonyLib.CollectionExtensions.AddToArray(scene.potentialStickers, new WeightedSticker(NotebookSticker, 100));
                }
                if (BsodaBonusSticker != Sticker.Nothing && scene.potentialStickers != null)
                {
                    if (!scene.potentialStickers.Any(s => s != null && s.selection == BsodaBonusSticker))
                        scene.potentialStickers = HarmonyLib.CollectionExtensions.AddToArray(scene.potentialStickers, new WeightedSticker(BsodaBonusSticker, 100));
                }
            });

            Instance.gameObject.AddComponent<BackpackInjector>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RegisterRewiredInputs()
        {
            if (!IsRewiredCompatInstalled || inputsRegistered) return;
            BBPRewiredCompat.RewiredPlusManager.CreateNewInput(name: "UseDash", descriptionName: "Use Dash", behaviorID: BBPRewiredCompat.RewiredPlusManager.InputBehaviorID.Snap, categoryID: BBPRewiredCompat.RewiredPlusManager.InputMapCategory.Actions, key: KeyCode.R);
            BBPRewiredCompat.RewiredPlusManager.CreateNewInput(name: "UseBackpack", descriptionName: "Use Backpack", behaviorID: BBPRewiredCompat.RewiredPlusManager.InputBehaviorID.Snap, categoryID: BBPRewiredCompat.RewiredPlusManager.InputMapCategory.Actions, key: KeyCode.X);
            inputsRegistered = true;
        }

        [HarmonyPatch(typeof(NameManager), "Awake")]
        [HarmonyPrefix]
        private static void RegisterInputsOnGameStart()
        {
            if (IsRewiredCompatInstalled) RegisterRewiredInputs();
        }

        private void AddPrefixedAsset(string key, UnityEngine.Object asset)
        {
            if (asset == null) return;
            if (!asset.name.StartsWith("ExtraStuff_") && !asset.name.StartsWith("ExtraSruff_")) asset.name = "ExtraStuff_" + asset.name;
            if (!assetManager.ContainsKey(key)) assetManager.Add(key, asset);
        }

        private void LoadAssets()
        {
            string localizationPath = GetModFilePath("", "Localization.json");
            if (File.Exists(localizationPath)) AssetLoader.LocalizationFromFile(localizationPath, Language.English);

            string[] otherTexFiles = {
                "Bal_Office_Wall", "Bal_Office_Floor", "Bal_Office_Ceiling", "OfficeClass_Closed", "OfficeClass_Open",
                "MM12_BasicRealWall", "MM12_BasicRealFloor", "MM12_BasicRealCeiling", "McGonigal_Closed", "McGonigal_Open",
                "Old_BookHolder", "OldWood", "Oldfence", "hnt_windows", "TestComic", "TestPoster", "EasterEggLockerTex",
                "ClassicMask", "ClassicWindow", "ClassicWindow_Broken", "ClassicalWindow_Mask", "ClassicalWindow_Default",
                "ClassicalWindow_Broken", "WindowMask", "WindowTexture", "WindowTexture_Broken",
                "MyOfficePlusPoster", "MyOfficeRulesPoster", "MyOfficeToys", "MyOfficeWhiteboard", "MyOfficeWindow",
                "ExtraStuff_BaldiWindow_Default", "ExtraStuff_BaldiWindow_Broken", "ExtraStuff_BaldiWindow_Mask"
            };
            foreach (var f in otherTexFiles)
            {
                string path = GetModFilePath("OtherStuff", f + ".png");
                if (File.Exists(path))
                {
                    var tex = AssetLoader.TextureFromFile(path);
                    if (tex != null) AddPrefixedAsset(f, tex);
                }
            }

            string[] otherSpriteFiles = {
                "room_baldioffice", "room_baldiofficeunlock", "room_mcgonigaloffice", "room_beltroom", "room_OldLibrary", "room_OldLibraryRNG",
                "ESSlotIndividual", "ClassicWindowIcon", "ClassicalWindowIcon", "WindowIcon",
                "BBCCSSlotIndividual", "BBIALBOESlotIndividual", "TimesSlotIndividual", "BaldiFrenzySlotIndividual", "BaldTrophy", "TaldiSlotIndividual",
                "ExtraStuff_BaldiWindowIcon", "CaseohSlotIndividual"
            };
            foreach (var f in otherSpriteFiles)
            {
                string path = GetModFilePath("OtherStuff", f + ".png");
                if (File.Exists(path))
                {
                    var sprite = AssetLoader.SpriteFromFile(path, new Vector2(0.5f, 0.5f), 1f);
                    if (sprite != null)
                    {
                        if (f.StartsWith("room_")) AddPrefixedAsset("Editor " + (f == "room_baldioffice" ? "BaldiOffice" : f == "room_baldiofficeunlock" ? "BaldiOfficeUnlock" : f == "room_mcgonigaloffice" ? "McGonigalRoom" : f == "room_beltroom" ? "BeltRoom" : f == "room_OldLibrary" ? "OldLibraryRoom" : "MazeRoom"), sprite);
                        else AddPrefixedAsset(f, sprite);
                    }
                }
            }

            (string, string)[] specificSprites = {
                ("CoinDoorItemIcon_Small.png", "Sprites/itm_coindoor_small"), ("CoinDoorItemIcon_Large.png", "Sprites/itm_coindoor_large"),
                ("DetentionPassIcon_Small.png", "Sprites/DetentionPassIcon_Small"), ("DetentionPassIcon_Large.png", "Sprites/DetentionPassIcon_Large"),
                ("ReallyTeleporterIcon_Small.png", "Sprites/ReallyTeleporterIcon_Small"), ("ReallyTeleporterIcon_Large.png", "Sprites/ReallyTeleporterIcon_Large"),
                ("JuiceBottleIcon_Small.png", "Sprites/JuiceBottleIcon_Small"), ("JuiceBottleIcon_Large.png", "Sprites/JuiceBottleIcon_Large"),
                ("JuiceBottleproj.png", "Sprites/JuiceBottleproj"), ("Baldi_JuiceStunned.png", "Sprites/Baldi_JuiceStunned"),
                ("ChristmasYTP_Small.png", "Sprites/ChristmasYTP_Small"), ("ChristmasYTP.png", "Sprites/ChristmasYTP_Large"),
                ("YTPEvent_Small.png", "Sprites/YTPEvent_Small"), ("YTPEvent.png", "Sprites/YTPEvent_Large"),
                ("ControllerAlarmClockIcon_Small.png", "Sprites/ControllerAlarmClockIcon_Small"), ("ControllerAlarmClockIcon_Large.png", "Sprites/ControllerAlarmClockIcon_Large"),
                ("ControllerAlarmClockDropUnpress.png", "Sprites/ControllerAlarmClockDropUnpress"), ("ControllerAlarmClockDropPressed.png", "Sprites/ControllerAlarmClockDropPressed"),
                ("InstantBalderIcon_Small.png", "Sprites/InstantBalderIcon_Small"), ("InstantBalderIcon_Large.png", "Sprites/InstantBalderIcon_Large"),
                ("UnFullBackpackIcon_Small.png", "Sprites/UnFullBackpack_Small"), ("UnFullBackpackIcon_Large.png", "Sprites/UnFullBackpack_Large"),
                ("FullBackpackIcon_Small.png", "Sprites/FullBackpack_Small"), ("FullBackpackIcon_Large.png", "Sprites/FullBackpack_Large"),
                ("NutellaIcon_Small.png", "Sprites/NutellaIcon_Small"), ("NutellaIcon_Large.png", "Sprites/NutellaIcon_Large"),
                ("GumItemIcon_Small.png", "Sprites/GumItemIcon_Small"), ("GumItemIcon_Large.png", "Sprites/GumItemIcon_Large"),
                ("OldCandyIcon_Small.png", "Sprites/OldCandyIcon_Small"), ("OldCandyIcon_Large.png", "Sprites/OldCandyIcon_Large"),
                ("OldCandySplash.png", "Sprites/OldCandySplash"),
                ("SpeedyElixirIcon_Small.png", "Sprites/SpeedyElixirIcon_Small"), ("SpeedyElixirIcon_Large_Transparent.png", "Sprites/SpeedyElixirIcon_Large"),
                ("TeacherNametagIcon_Small.png", "Sprites/TeacherNametagIcon_Small"), ("TeacherNametagIcon_Large.png", "Sprites/TeacherNametagIcon_Large"),
                ("EventControllerIcon_Small.png", "Sprites/EventControllerIcon_Small"), ("EventControllerIcon_Large.png", "Sprites/EventControllerIcon_Large"),
                ("BlastBsodaIcon_Small.png", "Sprites/BlastBsodaIcon_Small"), ("BlastBsodaIcon_Large.png", "Sprites/BlastBsodaIcon_Large"),
                ("BlastBsodaSpray_Sprite_Transparent.png", "Sprites/BlastBsodaSpray_Sprite_Transparent"),
                ("BSODAPlusIcon_Small.png", "Sprites/BSODAPlusIcon_Small"),
                ("BSODAPlusIcon_Large.png", "Sprites/BSODAPlusIcon_Large"),
                ("BsodaSpray_Sprite_Transparent.png", "Sprites/BsodaSpray_Sprite_Transparent"),
                ("RandomizerItemIcon_Small.png", "Sprites/RandomizerItemIcon_Small"), ("RandomizerItemIcon_Large.png", "Sprites/RandomizerItemIcon_Large"),
                ("BaldiWhistleIcon_Small.png", "Sprites/BaldiWhistleIcon_Small"), ("BaldiWhistleIcon_Large.png", "Sprites/BaldiWhistleIcon_Large"),
                ("DarkCoffeeIcon_Small.png", "Sprites/DarkCoffeeIcon_Small"), ("DarkCoffeeIcon_Large.png", "Sprites/DarkCoffeeIcon_Large"),
                ("Sticker_NotebookSticker.png", "Sprites/Sticker_NotebookSticker"), ("Sticker_BsodaBonus.png", "Sprites/Sticker_BsodaBonus"),
                ("UsefulTeleporterIcon_Small.png", "Sprites/UsefulTeleporterIcon_Small"), ("UsefulTeleporterIcon_Large.png", "Sprites/UsefulTeleporterIcon_Large"),
                ("SafetyShurikenIcon_Small.png", "Sprites/SafetyShurikenIcon_Small"), ("SafetyShurikenIcon_Large.png", "Sprites/SafetyShurikenIcon_Large"),
                ("PSpecialTeapotSpray_Sprite_Transparent.png", "Sprites/PSpecialTeapotSpray_Sprite_Transparent"),
                ("PSpecialTeapotIcon_Small.png", "Sprites/PSpecialTeapotIcon_Small"), ("PSpecialTeapotIcon_Large.png", "Sprites/PSpecialTeapotIcon_Large"),
                ("TastyBurgerIcon_Small.png", "Sprites/TastyBurgerIcon_Small"), ("TastyBurgerIcon_Large.png", "Sprites/TastyBurgerIcon_Large")
            };
            foreach (var (f, key) in specificSprites)
            {
                string path = GetModFilePath("Textures", f);
                if (File.Exists(path))
                {
                    var sprite = AssetLoader.SpriteFromFile(path, new Vector2(0.5f, 0.5f), 40f);
                    if (sprite != null) AddPrefixedAsset(key, sprite);
                }
            }

            string[] texFiles = {
                "YTP_Ruby.png", "YTP_Ruby_Small.png", "YTP_Emerald.png", "YTP_Emerald_Small.png", "YTP_Random.png", "YTP_Random_Small.png",
                "EnergyDrinkIcon_Small.png", "EnergyDrinkIcon_Large.png", "CleanChalkEraserIcon_Small.png", "CleanChalkEraserIcon_Large.png",
                "ReflexHammerIcon_Small.png", "ReflexHammerIcon_Large.png", "PrototypeTeleporterIcon_Small.png", "PrototypeTeleporterIcon_Large.png",
                "LighterIcon_Small.png", "LighterIcon_Large.png", "FireParticles.png", "WaterBottleIcon_Small.png", "WaterBottleIcon_Large.png", "WaterSpray.png",
                "StarFireProjectile.png", "StarFireIcon_Small.png", "StarFireIcon_Large.png",
                "CoolPlantIcon_Small.png", "CoolPlantIcon_Large.png", "PaintBucketIcon_Small.png", "PaintBucketIcon_Large.png", "PaintBucket_Blind.png",
                "GravityStickIcon_Small.png", "GravityStickIcon_Large.png", "DashIcon_Small.png", "DashIcon_Large.png",
                "BaldiOfficeKeyIcon_Small.png", "BaldiOfficeKeyIcon_Large.png",
                "KimimColaIcon_Small.png", "KimimColaIcon_Large.png"
            };
            foreach (var f in texFiles)
            {
                string path = GetModFilePath("Textures", f);
                if (File.Exists(path))
                {
                    string key = "Sprites/" + Path.GetFileNameWithoutExtension(f).Replace("YTP_", "itm_").Replace("Icon", "itm").Replace("WaterBottle", "WaterBottle").Replace("_Small", "_small").Replace("_Large", "_large").Replace("_Small", "_Small");
                    if (f.Contains("YTP_Ruby") && f.Contains("Small")) key = "Sprites/YTP_Ruby_Small";
                    else if (f.Contains("YTP_Emerald") && f.Contains("Small")) key = "Sprites/YTP_Emerald_Small";
                    else if (f.Contains("YTP_Random") && f.Contains("Small")) key = "Sprites/YTP_Random_Small";
                    else if (f == "YTP_Ruby.png") key = "Sprites/itm_ruby";
                    else if (f == "YTP_Emerald.png") key = "Sprites/itm_emerald";
                    else if (f == "YTP_Random.png") key = "Sprites/YTP_Random";
                    else if (f == "EnergyDrinkIcon_Small.png") key = "Sprites/itm_energy_small";
                    else if (f == "EnergyDrinkIcon_Large.png") key = "Sprites/itm_energy_large";
                    else if (f == "CleanChalkEraserIcon_Small.png") key = "Sprites/itm_chalk_eraser_small";
                    else if (f == "CleanChalkEraserIcon_Large.png") key = "Sprites/itm_chalk_eraser_large";
                    else if (f == "ReflexHammerIcon_Small.png") key = "Sprites/ReflexHammerIcon_Small";
                    else if (f == "ReflexHammerIcon_Large.png") key = "Sprites/ReflexHammerIcon_Large";
                    else if (f == "PrototypeTeleporterIcon_Small.png") key = "Sprites/PrototypeTeleporterIcon_Small";
                    else if (f == "PrototypeTeleporterIcon_Large.png") key = "Sprites/PrototypeTeleporterIcon_Large";
                    else if (f == "LighterIcon_Small.png") key = "Sprites/LighterIcon_Small";
                    else if (f == "LighterIcon_Large.png") key = "Sprites/LighterIcon_Large";
                    else if (f == "FireParticles.png") key = "Sprites/FireParticles";
                    else if (f == "WaterBottleIcon_Small.png") key = "Sprites/WaterBottleIcon_Small";
                    else if (f == "WaterBottleIcon_Large.png") key = "Sprites/WaterBottleIcon_Large";
                    else if (f == "WaterSpray.png") key = "Sprites/WaterSpray";
                    else if (f == "StarFireProjectile.png") key = "Sprites/StarFireProjectile";
                    else if (f == "StarFireIcon_Small.png") key = "Sprites/StarFireIcon_Small";
                    else if (f == "StarFireIcon_Large.png") key = "Sprites/StarFireIcon_Large";
                    else if (f == "CoolPlantIcon_Small.png") key = "Sprites/CoolPlantIcon_Small";
                    else if (f == "CoolPlantIcon_Large.png") key = "Sprites/CoolPlantIcon_Large";
                    else if (f == "PaintBucketIcon_Small.png") key = "Sprites/PaintBucketIcon_Small";
                    else if (f == "PaintBucketIcon_Large.png") key = "Sprites/PaintBucketIcon_Large";
                    else if (f == "PaintBucket_Blind.png") key = "Sprites/PaintBucket_Blind";
                    else if (f == "GravityStickIcon_Small.png") key = "Sprites/GravityStickIcon_Small";
                    else if (f == "GravityStickIcon_Large.png") key = "Sprites/GravityStickIcon_Large";
                    else if (f == "DashIcon_Small.png") key = "Sprites/DashIcon_Small";
                    else if (f == "DashIcon_Large.png") key = "Sprites/DashIcon_Large";
                    else if (f == "BaldiOfficeKeyIcon_Small.png") key = "Sprites/BaldiOfficeKeyIcon_Small";
                    else if (f == "BaldiOfficeKeyIcon_Large.png") key = "Sprites/BaldiOfficeKeyIcon_Large";
                    else if (f == "KimimColaIcon_Small.png") key = "Sprites/KimimColaIcon_Small";
                    else if (f == "KimimColaIcon_Large.png") key = "Sprites/KimimColaIcon_Large";

                    if (!assetManager.ContainsKey(key))
                    {
                        var sprite = AssetLoader.SpriteFromFile(path, new Vector2(0.5f, 0.5f), 40f);
                        if (sprite != null) AddPrefixedAsset(key, sprite);
                    }
                }
            }

            string[] audioFiles = { "Ruby.wav", "Emerald.wav", "DrinkEnergy.wav", "Random.wav", "Lighter.wav", "Fire.wav", "Splash.wav", "StarFire.wav", "BAL_JuiceSplash.wav", "GlassJuiceBreak.ogg", "Christmas.wav", "EventYTP1.wav", "GumSpit.wav", "Coffee.wav", "BAL_YourGoodPraise.wav", "BAL_FantasticPraise.wav", "BAL_YouDidGreatPraise.wav", "ExtraStuff_NotebookSteal.wav", "ExtraStuff_NoteSad.wav" };
            foreach (var f in audioFiles)
            {
                string path = GetModFilePath("Audio", f);
                if (File.Exists(path))
                {
                    string key = "Sounds/" + Path.GetFileNameWithoutExtension(f).ToLower();
                    if (f == "Ruby.wav") key = "Sounds/ruby_sound";
                    else if (f == "Emerald.wav") key = "Sounds/emerald_sound";
                    else if (f == "DrinkEnergy.wav") key = "Sounds/drink_energy";
                    else if (f == "Random.wav") key = "Sounds/random_pickup";
                    else if (f == "Lighter.wav") key = "Sounds/lighter_use";
                    else if (f == "Fire.wav") key = "Sounds/fire_loop";
                    else if (f == "Splash.wav") key = "Sounds/splash";
                    else if (f == "StarFire.wav") key = "Sounds/starfire_use";
                    else if (f == "BAL_JuiceSplash.wav") key = "Sounds/bal_juice_splash";
                    else if (f == "GlassJuiceBreak.ogg") key = "Sounds/glass_break";
                    else if (f == "Christmas.wav") key = "Sounds/christmas_ytp";
                    else if (f == "EventYTP1.wav") key = "Sounds/event_ytp";
                    else if (f == "GumSpit.wav") key = "Sounds/GumSpit";
                    else if (f == "Coffee.wav") key = "Sounds/coffee";
                    else if (f == "ExtraStuff_NotebookSteal.wav") key = "Sounds/NotebookSteal";
                    else if (f == "ExtraStuff_NoteSad.wav") key = "Sounds/NoteSad";
                    else if (f.Contains("Praise")) key = "Sounds/" + Path.GetFileNameWithoutExtension(f);

                    if (!assetManager.ContainsKey(key))
                    {
                        var clip = AssetLoader.AudioClipFromFile(path);
                        if (clip != null)
                        {
                            bool isPraise = f.Contains("Praise");
                            string subtitleKey = isPraise ? Path.GetFileNameWithoutExtension(f) : "";
                            SoundType sType = isPraise ? SoundType.Voice : SoundType.Effect;
                            Color col = isPraise ? Color.green : Color.white;

                            var sound = ObjectCreators.CreateSoundObject(clip, subtitleKey, sType, col, -1f);
                            if (sound != null)
                            {
                                sound.subtitle = isPraise;
                                sound.name = "ExtraStuff_" + Path.GetFileNameWithoutExtension(f);
                                if (isPraise) sound.additionalKeys = new SubtitleTimedKey[0];
                                AddPrefixedAsset(key, sound);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator LoadMod()
        {
            yield return 1;
            yield return "Preparing Custom rooms...";

            var loader = LevelLoaderPlugin.Instance;
            if (loader != null) WindowsSetup.RegisterWindows(loader);
            PostersSetup.SetupPosters();
            RoomsSetup.LoadRooms(loader, ModAssetsPath);

            yield return "Loading Vending Machines...";
            VendingMachinesSetup.LoadData(ModAssetsPath);

            yield return "Creating Items...";
            CreateItems();

            yield return "Loading Decorations...";
            yield return DecorationsSetup.RegisterDecorations();

            yield return "Preparing editor stuff";
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudioloader"))
                EditorPatches.AddLoaderContent();

            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                EditorPatches.SetupEditorObjects();

            yield return "Registering Bonus Stickers...";
            if (BsodaBonusSticker != Sticker.Nothing)
            {
                var stickerManager = Resources.FindObjectsOfTypeAll<StickerManager>().FirstOrDefault();
                if (stickerManager != null)
                {
                    var bonusField = HarmonyLib.AccessTools.Field(typeof(StickerManager), "bonusStickers");
                    if (bonusField != null)
                    {
                        var bonusList = bonusField.GetValue(stickerManager) as List<Sticker>;
                        if (bonusList != null && !bonusList.Contains(BsodaBonusSticker))
                            bonusList.Add(BsodaBonusSticker);
                    }
                }
            }
        }

        private void CreateItems()
        {
            SoundObject? rubySound = assetManager.ContainsKey("Sounds/ruby_sound") ? assetManager.Get<SoundObject>("Sounds/ruby_sound") : null;
            SoundObject? emeraldSound = assetManager.ContainsKey("Sounds/emerald_sound") ? assetManager.Get<SoundObject>("Sounds/emerald_sound") : null;

            ITM_YTPs? vanillaPoints = null;
            var allItems = Resources.FindObjectsOfTypeAll<ITM_YTPs>();
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i] != null && !allItems[i].gameObject.scene.IsValid()) { vanillaPoints = allItems[i]; break; }
            }

            if (assetManager.ContainsKey("Sprites/itm_ruby"))
            {
                Sprite rubySpriteSmall = assetManager.ContainsKey("Sprites/YTP_Ruby_Small") ? assetManager.Get<Sprite>("Sprites/YTP_Ruby_Small") : assetManager.Get<Sprite>("Sprites/itm_ruby");
                Sprite rubySpriteLarge = assetManager.Get<Sprite>("Sprites/itm_ruby");
                if (rubySpriteSmall != null && rubySpriteLarge != null)
                {
                    var rubyItem = new ItemBuilder(Info).SetEnum("BaldiFrenzyRuby").SetNameAndDescription("ExtraStuff_Itm_YTP_Ruby", "ExtraStuff_Desc_YTP_Ruby").SetSprites(rubySpriteSmall, rubySpriteLarge).SetGeneratorCost(500).SetAsInstantUse().Build();
                    rubyItem.name = "ExtraStuff_Itm_YTP_Ruby";
                    if (rubySound != null) rubyItem.audPickupOverride = rubySound;
                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "ExtraStuff_CustomRubyPrefab";
                        customPrefab.SetActive(false);
                        var vanillaComp = customPrefab.GetComponent<ITM_YTPs>();
                        if (vanillaComp != null) UnityEngine.Object.DestroyImmediate(vanillaComp);
                        var customComp = customPrefab.AddComponent<ITM_Ruby>();
                        UnityEngine.Object.DontDestroyOnLoad(customPrefab);
                        rubyItem.item = customComp;
                        rubyItem.itemSpriteSmall = rubySpriteSmall;
                        rubyItem.itemSpriteLarge = rubySpriteLarge;
                        var spriteRenderer = customPrefab.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) { spriteRenderer.sprite = rubySpriteLarge; spriteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); }
                    }
                    ModItems.Add("itm_ruby", rubyItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/itm_emerald"))
            {
                Sprite emeraldSpriteSmall = assetManager.ContainsKey("Sprites/YTP_Emerald_Small") ? assetManager.Get<Sprite>("Sprites/YTP_Emerald_Small") : assetManager.Get<Sprite>("Sprites/itm_emerald");
                Sprite emeraldSpriteLarge = assetManager.Get<Sprite>("Sprites/itm_emerald");
                if (emeraldSpriteSmall != null && emeraldSpriteLarge != null)
                {
                    var emeraldItem = new ItemBuilder(Info).SetEnum("BaldiFrenzyEmerald").SetNameAndDescription("ExtraStuff_Itm_YTP_Emerald", "ExtraStuff_Desc_YTP_Emerald").SetSprites(emeraldSpriteSmall, emeraldSpriteLarge).SetGeneratorCost(750).SetAsInstantUse().Build();
                    emeraldItem.name = "ExtraStuff_Itm_YTP_Emerald";
                    if (emeraldSound != null) emeraldItem.audPickupOverride = emeraldSound;
                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "ExtraStuff_CustomEmeraldPrefab";
                        customPrefab.SetActive(false);
                        var vanillaComp = customPrefab.GetComponent<ITM_YTPs>();
                        if (vanillaComp != null) UnityEngine.Object.DestroyImmediate(vanillaComp);
                        var customComp = customPrefab.AddComponent<ITM_Emerald>();
                        UnityEngine.Object.DontDestroyOnLoad(customPrefab);
                        emeraldItem.item = customComp;
                        emeraldItem.itemSpriteSmall = emeraldSpriteSmall;
                        emeraldItem.itemSpriteLarge = emeraldSpriteLarge;
                        var spriteRenderer = customPrefab.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) { spriteRenderer.sprite = emeraldSpriteLarge; spriteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); }
                    }
                    ModItems.Add("itm_emerald", emeraldItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/YTP_Random"))
            {
                Sprite randomSpriteSmall = assetManager.ContainsKey("Sprites/YTP_Random_Small") ? assetManager.Get<Sprite>("Sprites/YTP_Random_Small") : assetManager.Get<Sprite>("Sprites/YTP_Random");
                Sprite randomSpriteLarge = assetManager.Get<Sprite>("Sprites/YTP_Random");
                SoundObject? randomSound = assetManager.ContainsKey("Sounds/random_pickup") ? assetManager.Get<SoundObject>("Sounds/random_pickup") : null;
                if (randomSpriteSmall != null && randomSpriteLarge != null)
                {
                    var randomItem = new ItemBuilder(Info).SetEnum("Itm_RandomYTP").SetNameAndDescription("ExtraStuff_Itm_YTP_Random", "ExtraStuff_Desc_YTP_Random").SetSprites(randomSpriteSmall, randomSpriteLarge).SetGeneratorCost(0).SetAsInstantUse().Build();
                    randomItem.name = "ExtraStuff_Itm_YTP_Random";
                    if (randomSound != null) randomItem.audPickupOverride = randomSound;
                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "ExtraStuff_CustomRandomYTPPrefab";
                        customPrefab.SetActive(false);
                        var vanillaComp = customPrefab.GetComponent<ITM_YTPs>();
                        if (vanillaComp != null) UnityEngine.Object.DestroyImmediate(vanillaComp);
                        var customComp = customPrefab.AddComponent<ITM_RandomYTP>();
                        UnityEngine.Object.DontDestroyOnLoad(customPrefab);
                        randomItem.item = customComp;
                        randomItem.itemSpriteSmall = randomSpriteSmall;
                        randomItem.itemSpriteLarge = randomSpriteLarge;
                        var spriteRenderer = customPrefab.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) { spriteRenderer.sprite = randomSpriteLarge; spriteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); }
                    }
                    ModItems.Add("itm_random_ytp", randomItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/ChristmasYTP_Small") && assetManager.ContainsKey("Sprites/ChristmasYTP_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/ChristmasYTP_Small");
                var large = assetManager.Get<Sprite>("Sprites/ChristmasYTP_Large");
                SoundObject? christmasSound = assetManager.ContainsKey("Sounds/christmas_ytp") ? assetManager.Get<SoundObject>("Sounds/christmas_ytp") : null;
                if (small != null && large != null)
                {
                    var christmasItem = new ItemBuilder(Info).SetEnum("BaldiFrenzyChristmasYTP").SetNameAndDescription("ExtraStuff_Itm_ChristmasYTP", "ExtraStuff_Desc_ChristmasYTP").SetSprites(small, large).SetGeneratorCost(125).SetAsInstantUse().Build();
                    christmasItem.name = "ExtraStuff_Itm_ChristmasYTP";
                    if (christmasSound != null) christmasItem.audPickupOverride = christmasSound;
                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "ExtraStuff_CustomChristmasYTPPrefab";
                        customPrefab.SetActive(false);
                        var vanillaComp = customPrefab.GetComponent<ITM_YTPs>();
                        if (vanillaComp != null) UnityEngine.Object.DestroyImmediate(vanillaComp);
                        var customComp = customPrefab.AddComponent<ITM_ChristmasYTP>();
                        UnityEngine.Object.DontDestroyOnLoad(customPrefab);
                        christmasItem.item = customComp;
                        christmasItem.itemSpriteSmall = small;
                        christmasItem.itemSpriteLarge = large;
                        var spriteRenderer = customPrefab.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) { spriteRenderer.sprite = large; spriteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); }
                    }
                    ModItems.Add("itm_christmasytp", christmasItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/YTPEvent_Small") && assetManager.ContainsKey("Sprites/YTPEvent_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/YTPEvent_Small");
                var large = assetManager.Get<Sprite>("Sprites/YTPEvent_Large");
                SoundObject? eventSound = assetManager.ContainsKey("Sounds/event_ytp") ? assetManager.Get<SoundObject>("Sounds/event_ytp") : null;
                if (small != null && large != null)
                {
                    var eventItem = new ItemBuilder(Info).SetEnum("Itm_EventYTP").SetNameAndDescription("ExtraStuff_Itm_EventYTP", "ExtraStuff_Desc_EventYTP").SetSprites(small, large).SetGeneratorCost(150).SetAsInstantUse().Build();
                    eventItem.name = "ExtraStuff_Itm_EventYTP";
                    if (eventSound != null) eventItem.audPickupOverride = eventSound;
                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "ExtraStuff_CustomEventYTPPrefab";
                        customPrefab.SetActive(false);
                        var vanillaComp = customPrefab.GetComponent<ITM_YTPs>();
                        if (vanillaComp != null) UnityEngine.Object.DestroyImmediate(vanillaComp);
                        var customComp = customPrefab.AddComponent<ITM_EventYTP>();
                        UnityEngine.Object.DontDestroyOnLoad(customPrefab);
                        eventItem.item = customComp;
                        eventItem.itemSpriteSmall = small;
                        eventItem.itemSpriteLarge = large;
                        var spriteRenderer = customPrefab.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) { spriteRenderer.sprite = large; spriteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); }
                    }
                    ModItems.Add("itm_eventytp", eventItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/itm_energy_small") && assetManager.ContainsKey("Sprites/itm_energy_large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/itm_energy_small");
                var large = assetManager.Get<Sprite>("Sprites/itm_energy_large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_Energy_Drink").SetNameAndDescription("ExtraStuff_Itm_EnergyDrink", "ExtraStuff_Desc_EnergyDrink").SetSprites(small, large).SetGeneratorCost(100).SetShopPrice(400).SetItemComponent<ITM_EnergyDrink>().SetMeta(ItemFlags.Persists, new string[] { "drink", "food" }).Build();
                    item.name = "ExtraStuff_Itm_Energy_Drink";
                    ModItems.Add("itm_energy_drink", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/itm_chalk_eraser_small") && assetManager.ContainsKey("Sprites/itm_chalk_eraser_large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/itm_chalk_eraser_small");
                var large = assetManager.Get<Sprite>("Sprites/itm_chalk_eraser_large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_CleanChalkEraser").SetNameAndDescription("ExtraStuff_Itm_CleanChalkEraser", "ExtraStuff_Desc_CleanChalkEraser").SetSprites(small, large).SetGeneratorCost(50).SetShopPrice(600).SetItemComponent<ITM_CleanChalkEraser>().SetMeta(ItemFlags.Persists, new string[] { "tool", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_CleanChalkEraser";
                    ModItems.Add("itm_clean_chalk_eraser", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/ReflexHammerIcon_Small") && assetManager.ContainsKey("Sprites/ReflexHammerIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/ReflexHammerIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/ReflexHammerIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_HammerReflex").SetNameAndDescription("ExtraStuff_Itm_HammerReflex", "ExtraStuff_Desc_HammerReflex").SetSprites(small, large).SetGeneratorCost(50).SetShopPrice(750).SetItemComponent<ITM_HammerReflex>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon" }).Build();
                    item.name = "ExtraStuff_Itm_HammerReflex";
                    ModItems.Add("itm_hammer_reflex", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/PrototypeTeleporterIcon_Small") && assetManager.ContainsKey("Sprites/PrototypeTeleporterIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/PrototypeTeleporterIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/PrototypeTeleporterIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_PrototypeTeleporter").SetNameAndDescription("ExtraStuff_Itm_PrototypeTeleporter", "ExtraStuff_Desc_PrototypeTeleporter").SetSprites(small, large).SetGeneratorCost(120).SetShopPrice(300).SetItemComponent<ITM_PrototypeTeleporter>().SetMeta(ItemFlags.Persists, new string[] { "tool", "teleport", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_PrototypeTeleporter";
                    ModItems.Add("itm_prototype_teleporter", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/StarFireIcon_Small") && assetManager.ContainsKey("Sprites/StarFireIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/StarFireIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/StarFireIcon_Large");
                if (small != null && large != null)
                {
                    SoundObject itemPickupSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && (x.name == "ItemPickup" || x.name == "ItemPickup_Default"));
                    var item = new ItemBuilder(Info).SetEnum("Itm_StarFire").SetNameAndDescription("ExtraStuff_Itm_StarFire", "ExtraStuff_Desc_StarFire").SetSprites(small, large).SetGeneratorCost(50).SetShopPrice(550).SetItemComponent<ITM_StarFire>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon", "magic", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_StarFire";
                    if (itemPickupSound != null) item.audPickupOverride = itemPickupSound;
                    ModItems.Add("itm_starfire", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/LighterIcon_Small") && assetManager.ContainsKey("Sprites/LighterIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/LighterIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/LighterIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_Lighter").SetNameAndDescription("ExtraStuff_Itm_Lighter", "ExtraStuff_Desc_Lighter").SetSprites(small, large).SetGeneratorCost(1000).SetShopPrice(800).SetItemComponent<ITM_Lighter>().SetMeta(ItemFlags.Persists, new string[] { "tool", "fire", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_Lighter";
                    ModItems.Add("itm_lighter", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/WaterBottleIcon_Small") && assetManager.ContainsKey("Sprites/WaterBottleIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/WaterBottleIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/WaterBottleIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_WaterBott").SetNameAndDescription("ExtraStuff_Itm_WaterBott", "ExtraStuff_Desc_WaterBott").SetSprites(small, large).SetGeneratorCost(130).SetShopPrice(750).SetItemComponent<ITM_WaterBott>().SetMeta(ItemFlags.Persists, new string[] { "tool", "drink" }).Build();
                    item.name = "ExtraStuff_Itm_WaterBott";
                    ModItems.Add("itm_waterbott", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/itm_coindoor_small") && assetManager.ContainsKey("Sprites/itm_coindoor_large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/itm_coindoor_small");
                var large = assetManager.Get<Sprite>("Sprites/itm_coindoor_large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_CoinDoor").SetNameAndDescription("ExtraStuff_Itm_CoinDoor", "ExtraStuff_Desc_CoinDoor").SetSprites(small, large).SetGeneratorCost(300).SetShopPrice(300).SetItemComponent<ITM_CoinDoor>().SetMeta(ItemFlags.Persists, new string[] { "tool", "key" }).Build();
                    item.name = "ExtraStuff_Itm_CoinDoor";
                    ModItems.Add("itm_coindoor", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/CoolPlantIcon_Small") && assetManager.ContainsKey("Sprites/CoolPlantIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/CoolPlantIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/CoolPlantIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_CoolPlant").SetNameAndDescription("ExtraStuff_Itm_CoolPlant", "ExtraStuff_Desc_CoolPlant").SetSprites(small, large).SetGeneratorCost(250).SetShopPrice(900).SetItemComponent<ITM_CoolPlant>().SetMeta(ItemFlags.Persists, new string[] { "tool", "plant", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_CoolPlant";
                    ModItems.Add("itm_coolplant", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/PaintBucketIcon_Small") && assetManager.ContainsKey("Sprites/PaintBucketIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/PaintBucketIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/PaintBucketIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_PaintBucket").SetNameAndDescription("ExtraStuff_Itm_PaintBucket", "ExtraStuff_Desc_PaintBucket").SetSprites(small, large).SetGeneratorCost(170).SetShopPrice(400).SetItemComponent<ITM_PaintBucket>().SetMeta(ItemFlags.Persists, new string[] { "tool", "blind" }).Build();
                    item.name = "ExtraStuff_Itm_PaintBucket";
                    ModItems.Add("itm_paintbucket", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/GravityStickIcon_Small") && assetManager.ContainsKey("Sprites/GravityStickIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/GravityStickIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/GravityStickIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_GravityStick").SetNameAndDescription("ExtraStuff_Itm_GravityStick", "ExtraStuff_Desc_GravityStick").SetSprites(small, large).SetGeneratorCost(200).SetShopPrice(750).SetItemComponent<ITM_GravityStick>().SetMeta(ItemFlags.Persists, new string[] { "tool", "gravity", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_GravityStick";
                    ModItems.Add("itm_gravitystick", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/DashIcon_Small") && assetManager.ContainsKey("Sprites/DashIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/DashIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/DashIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_Dash").SetNameAndDescription("ExtraStuff_Itm_Dash", "ExtraStuff_Desc_Dash").SetSprites(small, large).SetGeneratorCost(150).SetShopPrice(555).SetItemComponent<ITM_Dash>().SetMeta(ItemFlags.Persists, new string[] { "tool", "movement" }).Build();
                    item.name = "ExtraStuff_Itm_Dash";
                    ModItems.Add("itm_dash", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/BaldiOfficeKeyIcon_Small") && assetManager.ContainsKey("Sprites/BaldiOfficeKeyIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/BaldiOfficeKeyIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/BaldiOfficeKeyIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_BaldiOfficeKey").SetNameAndDescription("ExtraStuff_Itm_BaldiOfficeKey", "ExtraStuff_Desc_BaldiOfficeKey").SetSprites(small, large).SetGeneratorCost(40).SetShopPrice(450).SetItemComponent<ITM_BaldiOfficeKey>().SetMeta(ItemFlags.Persists, new string[] { "tool", "key" }).Build();
                    item.name = "ExtraStuff_Itm_BaldiOfficeKey";
                    ModItems.Add("itm_officekey", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/DetentionPassIcon_Small") && assetManager.ContainsKey("Sprites/DetentionPassIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/DetentionPassIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/DetentionPassIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_DetentionPass").SetNameAndDescription("ExtraStuff_Itm_DetentionPass", "ExtraStuff_Desc_DetentionPass").SetSprites(small, large).SetGeneratorCost(10).SetShopPrice(500).SetItemComponent<ITM_DetentionPass>().SetMeta(ItemFlags.Persists, new string[] { "tool", "key", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_DetentionPass";
                    ModItems.Add("itm_detentionpass", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/ReallyTeleporterIcon_Small") && assetManager.ContainsKey("Sprites/ReallyTeleporterIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/ReallyTeleporterIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/ReallyTeleporterIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_ReallyDangerousTeleporter").SetNameAndDescription("ExtraStuff_Itm_ReallyDangerousTeleporter", "ExtraStuff_Desc_ReallyDangerousTeleporter").SetSprites(small, large).SetGeneratorCost(200).SetShopPrice(1125).SetItemComponent<ITM_ReallyDangerousTeleporter>().SetMeta(ItemFlags.Persists, new string[] { "tool", "teleport", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_ReallyDangerousTeleporter";
                    ModItems.Add("itm_reallydangerousteleporter", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/JuiceBottleIcon_Small") && assetManager.ContainsKey("Sprites/JuiceBottleIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/JuiceBottleIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/JuiceBottleIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_JuiceBottle").SetNameAndDescription("ExtraStuff_Itm_JuiceBottle", "ExtraStuff_Desc_JuiceBottle").SetSprites(small, large).SetGeneratorCost(180).SetShopPrice(670).SetItemComponent<ITM_JuiceBottle>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_JuiceBottle";
                    ModItems.Add("itm_juicebottle", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/ControllerAlarmClockIcon_Small") && assetManager.ContainsKey("Sprites/ControllerAlarmClockIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/ControllerAlarmClockIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/ControllerAlarmClockIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_ControllerAlarmClock").SetNameAndDescription("ExtraStuff_Itm_ControllerAlarmClock", "ExtraStuff_Desc_ControllerAlarmClock").SetSprites(small, large).SetGeneratorCost(100).SetShopPrice(400).SetItemComponent<ITM_ControllerAlarmClock>().SetMeta(ItemFlags.Persists, new string[] { "tool" }).Build();
                    item.name = "ExtraStuff_Itm_ControllerAlarmClock";
                    ModItems.Add("itm_controlleralarmclock", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/InstantBalderIcon_Small") && assetManager.ContainsKey("Sprites/InstantBalderIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/InstantBalderIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/InstantBalderIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_InstantBalder").SetNameAndDescription("ExtraStuff_Itm_InstantBalder", "ExtraStuff_Desc_InstantBalder").SetSprites(small, large).SetGeneratorCost(200).SetShopPrice(600).SetItemComponent<ITM_InstantBalder>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_InstantBalder";
                    ModItems.Add("itm_instantbalder", item);

                    var item2 = new ItemBuilder(Info).SetEnum("Itm_InstantBalder_Infinite").SetNameAndDescription("ExtraStuff_Itm_InstantBalder_Infinite", "ExtraStuff_Desc_InstantBalder_Infinite").SetSprites(small, large).SetGeneratorCost(0).SetShopPrice(999999).SetItemComponent<ITM_InstantBalder_Infinite>().SetMeta(ItemFlags.Unobtainable, new string[] { "tool", "weapon", "crmp_contraband" }).Build();
                    item2.name = "ExtraStuff_Itm_InstantBalder_Infinite";
                    ModItems.Add("itm_instantbalder_infinite", item2);
                }
            }

            if (assetManager.ContainsKey("Sprites/BSODAPlusIcon_Small") && assetManager.ContainsKey("Sprites/BSODAPlusIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/BSODAPlusIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/BSODAPlusIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_BSODAPlus").SetNameAndDescription("ExtraStuff_Itm_BSODAPlus", "ExtraStuff_Desc_BSODAPlus").SetSprites(small, large).SetGeneratorCost(15).SetShopPrice(850).SetItemComponent<BSODAPlus>().SetMeta(ItemFlags.Persists, new string[] { "drink", "food", "StackableItems_NotAllowStacking" }).Build();
                    item.name = "ExtraStuff_Itm_BSODAPlus";
                    ModItems.Add("itm_bsodaplus", item);
                }
            }

            ItemObject? unfullBp = null;
            ItemObject? fullBp = null;

            if (assetManager.ContainsKey("Sprites/UnFullBackpack_Small") && assetManager.ContainsKey("Sprites/UnFullBackpack_Large"))
            {
                var ufs = assetManager.Get<Sprite>("Sprites/UnFullBackpack_Small");
                var ufl = assetManager.Get<Sprite>("Sprites/UnFullBackpack_Large");
                if (ufs != null && ufl != null)
                {
                    unfullBp = new ItemBuilder(Info).SetEnum("Itm_BackpackUnfull").SetNameAndDescription("ExtraStuff_Itm_BackpackUnfull", "ExtraStuff_Desc_Backpack").SetSprites(ufs, ufl).SetGeneratorCost(175).SetShopPrice(600).SetItemComponent<ITM_BackpackUnfull>().SetMeta(ItemFlags.Persists, new string[] { "tool", "StackableItems_NotAllowStacking" }).Build();
                    unfullBp.name = "ExtraStuff_Itm_BackpackUnfull";
                    ModItems.Add("itm_backpack_unfull", unfullBp);
                }
            }
            if (assetManager.ContainsKey("Sprites/FullBackpack_Small") && assetManager.ContainsKey("Sprites/FullBackpack_Large"))
            {
                var fs = assetManager.Get<Sprite>("Sprites/FullBackpack_Small");
                var fl = assetManager.Get<Sprite>("Sprites/FullBackpack_Large");
                if (fs != null && fl != null)
                {
                    fullBp = new ItemBuilder(Info).SetEnum("Itm_BackpackFull").SetNameAndDescription("ExtraStuff_Itm_BackpackFull", "ExtraStuff_Desc_BackpackFull").SetSprites(fs, fl).SetGeneratorCost(250).SetShopPrice(800).SetItemComponent<ITM_BackpackFull>().SetMeta(ItemFlags.Persists, new string[] { "tool", "StackableItems_NotAllowStacking" }).Build();
                    fullBp.name = "ExtraStuff_Itm_BackpackFull";
                    ModItems.Add("itm_backpack_full", fullBp);
                }
            }
            if (assetManager.ContainsKey("Sprites/NutellaIcon_Small") && assetManager.ContainsKey("Sprites/NutellaIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/NutellaIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/NutellaIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_Nutella").SetNameAndDescription("ExtraStuff_Itm_Nutella", "ExtraStuff_Desc_Nutella").SetSprites(small, large).SetGeneratorCost(100).SetShopPrice(500).SetItemComponent<ITM_Nutella>().SetMeta(ItemFlags.Persists, new string[] { "food", "StackableItems_NotAllowStacking" }).Build();
                    item.name = "ExtraStuff_Itm_Nutella";
                    ModItems.Add("itm_nutella", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/GumItemIcon_Small") && assetManager.ContainsKey("Sprites/GumItemIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/GumItemIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/GumItemIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_PinkGum").SetNameAndDescription("ExtraStuff_Itm_PinkGum", "ExtraStuff_Desc_PinkGum").SetSprites(small, large).SetGeneratorCost(125).SetShopPrice(325).SetItemComponent<ITM_PinkGum>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon" }).Build();
                    item.name = "ExtraStuff_Itm_PinkGum";
                    ModItems.Add("itm_pinkgum", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/OldCandyIcon_Small") && assetManager.ContainsKey("Sprites/OldCandyIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/OldCandyIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/OldCandyIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_OldCandy").SetNameAndDescription("ExtraStuff_Itm_OldCandy", "ExtraStuff_Desc_OldCandy").SetSprites(small, large).SetGeneratorCost(150).SetShopPrice(600).SetItemComponent<ITM_OldCandy>().SetMeta(ItemFlags.Persists, new string[] { "tool", "throwable" }).Build();
                    item.name = "ExtraStuff_Itm_OldCandy";
                    ModItems.Add("itm_oldcandy", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/SpeedyElixirIcon_Small") && assetManager.ContainsKey("Sprites/SpeedyElixirIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/SpeedyElixirIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/SpeedyElixirIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_SpeedyElixir").SetNameAndDescription("ExtraStuff_Itm_SpeedyElixir", "ExtraStuff_Desc_SpeedyElixir").SetSprites(small, large).SetGeneratorCost(300).SetShopPrice(800).SetItemComponent<ITM_SpeedyElixir>().SetMeta(ItemFlags.Persists, new string[] { "drink", "food", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_SpeedyElixir";
                    ModItems.Add("itm_speedyelixir", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/TeacherNametagIcon_Small") && assetManager.ContainsKey("Sprites/TeacherNametagIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/TeacherNametagIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/TeacherNametagIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_TeacherNametag").SetNameAndDescription("ExtraStuff_Itm_TeacherNametag", "ExtraStuff_Desc_TeacherNametag").SetSprites(small, large).SetGeneratorCost(200).SetShopPrice(600).SetItemComponent<ITM_TeacherNametag>().SetMeta(ItemFlags.Persists, new string[] { "tool", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_TeacherNametag";
                    ModItems.Add("itm_teacher_nametag", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/EventControllerIcon_Small") && assetManager.ContainsKey("Sprites/EventControllerIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/EventControllerIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/EventControllerIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_EventController").SetNameAndDescription("ExtraStuff_Itm_EventController (5/5)", "ExtraStuff_Desc_EventController").SetSprites(small, large).SetGeneratorCost(100).SetShopPrice(850).SetItemComponent<ITM_EventController>().SetMeta(ItemFlags.Persists, new string[] { "tool", "crmp_contraband", "StackableItems_NotAllowStacking" }).Build();
                    item.name = "ExtraStuff_Itm_EventController";
                    ModItems.Add("itm_eventcontroller", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/BlastBsodaIcon_Small") && assetManager.ContainsKey("Sprites/BlastBsodaIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/BlastBsodaIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/BlastBsodaIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_BlastBsoda").SetNameAndDescription("ExtraStuff_Itm_BlastBsoda", "ExtraStuff_Desc_BlastBsoda").SetSprites(small, large).SetGeneratorCost(75).SetShopPrice(350).SetItemComponent<ITM_BlastBsoda>().SetMeta(ItemFlags.Persists, new string[] { "drink", "food" }).Build();
                    item.name = "ExtraStuff_Itm_BlastBsoda";
                    ModItems.Add("itm_blastbsoda", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/RandomizerItemIcon_Small") && assetManager.ContainsKey("Sprites/RandomizerItemIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/RandomizerItemIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/RandomizerItemIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_Randomizer").SetNameAndDescription("ExtraStuff_Itm_Randomizer", "ExtraStuff_Desc_Randomizer").SetSprites(small, large).SetGeneratorCost(100).SetShopPrice(1000).SetItemComponent<ITM_Randomizer>().SetMeta(ItemFlags.Persists, new string[] { "tool", "crmp_contraband", "StackableItems_NotAllowStacking" }).Build();
                    item.name = "ExtraStuff_Itm_Randomizer";
                    ModItems.Add("itm_randomizer", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/BaldiWhistleIcon_Small") && assetManager.ContainsKey("Sprites/BaldiWhistleIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/BaldiWhistleIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/BaldiWhistleIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_BaldiWhistle").SetNameAndDescription("ExtraStuff_Itm_BaldiWhistle", "ExtraStuff_Desc_BaldiWhistle").SetSprites(small, large).SetGeneratorCost(0).SetShopPrice(999).SetItemComponent<ITM_BaldiWhistle>().SetMeta(ItemFlags.Persists, new string[] { "tool", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_BaldiWhistle";
                    ModItems.Add("itm_baldiwhistle", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/UsefulTeleporterIcon_Small") && assetManager.ContainsKey("Sprites/UsefulTeleporterIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/UsefulTeleporterIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/UsefulTeleporterIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_CoolUsefulTeleporter").SetNameAndDescription("ExtraStuff_Itm_CoolUsefulTeleporter", "ExtraStuff_Desc_CoolUsefulTeleporter").SetSprites(small, large).SetGeneratorCost(150).SetShopPrice(1500).SetItemComponent<ITM_CoolUsefulTeleporter>().SetMeta(ItemFlags.Persists, new string[] { "tool", "teleport", "crmp_contraband" }).Build();
                    item.name = "ExtraStuff_Itm_CoolUsefulTeleporter";
                    ModItems.Add("itm_coolusefulteleporter", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/SafetyShurikenIcon_Small") && assetManager.ContainsKey("Sprites/SafetyShurikenIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/SafetyShurikenIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/SafetyShurikenIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_SafetyShuriken").SetNameAndDescription("ExtraStuff_Itm_SafetyShuriken", "ExtraStuff_Desc_SafetyShuriken").SetSprites(small, large).SetGeneratorCost(150).SetShopPrice(550).SetItemComponent<ITM_SafetyShuriken>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon" }).Build();
                    item.name = "ExtraStuff_Itm_SafetyShuriken";
                    ModItems.Add("itm_safetyshuriken", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/DarkCoffeeIcon_Small") && assetManager.ContainsKey("Sprites/DarkCoffeeIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/DarkCoffeeIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/DarkCoffeeIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_DarkCoffee").SetNameAndDescription("ExtraStuff_Itm_DarkCoffee", "ExtraStuff_Desc_DarkCoffee").SetSprites(small, large).SetGeneratorCost(75).SetShopPrice(700).SetItemComponent<ITM_DarkCoffee>().SetMeta(ItemFlags.Persists, new string[] { "drink", "food" }).Build();
                    item.name = "ExtraStuff_Itm_DarkCoffee";
                    ModItems.Add("itm_darkcoffee", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/PSpecialTeapotIcon_Small") && assetManager.ContainsKey("Sprites/PSpecialTeapotIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/PSpecialTeapotIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/PSpecialTeapotIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_SpecialTeapot").SetNameAndDescription("ExtraStuff_Itm_SpecialTeapot", "ExtraStuff_Desc_SpecialTeapot").SetSprites(small, large).SetGeneratorCost(99).SetShopPrice(9999).SetItemComponent<ITM_SpecialTeapot>().SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon" }).Build();
                    item.name = "ExtraStuff_Itm_SpecialTeapot";
                    ModItems.Add("itm_specialteapot", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/TastyBurgerIcon_Small") && assetManager.ContainsKey("Sprites/TastyBurgerIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/TastyBurgerIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/TastyBurgerIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_TastyBurger").SetNameAndDescription("ExtraStuff_Itm_TastyBurger", "ExtraStuff_Desc_TastyBurger").SetSprites(small, large).SetGeneratorCost(150).SetShopPrice(450).SetItemComponent<ITM_TastyBurger>().SetMeta(ItemFlags.Persists, new string[] { "food" }).Build();
                    item.name = "ExtraStuff_Itm_TastyBurger";
                    ModItems.Add("itm_tastyburger", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/KimimColaIcon_Small") && assetManager.ContainsKey("Sprites/KimimColaIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/KimimColaIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/KimimColaIcon_Large");
                if (small != null && large != null)
                {
                    var item = new ItemBuilder(Info).SetEnum("Itm_KimCola").SetNameAndDescription("ExtraStuff_Itm_KimCola", "ExtraStuff_Desc_KimCola").SetSprites(small, large).SetGeneratorCost(0).SetShopPrice(9999).SetItemComponent<ITM_KimCola>().SetMeta(ItemFlags.Persists, new string[] { "drink", "food" }).Build();
                    item.name = "ExtraStuff_Itm_KimCola";
                    ModItems.Add("itm_kimcola", item);
                }
            }
            if (assetManager.ContainsKey("Sprites/Sticker_NotebookSticker"))
            {
                var sprite = assetManager.Get<Sprite>("Sprites/Sticker_NotebookSticker");
                var stickerData = new StickerBuilder<NotebookStickerData>(Info).SetEnum("NotebookSticker").SetDuplicateOddsMultiplier(0.5f).SetSprite(sprite).Build();
                BasePlugin.NotebookSticker = stickerData.sticker;
                BasePlugin.NotebookStickerSprite = sprite;
            }
            if (assetManager.ContainsKey("Sprites/Sticker_BsodaBonus"))
            {
                var sprite = assetManager.Get<Sprite>("Sprites/Sticker_BsodaBonus");
                var stickerData = new StickerBuilder<BsodaBonusStickerData>(Info).SetEnum("BsodaBonusSticker").SetDuplicateOddsMultiplier(0.5f).SetSprite(sprite).Build();
                BasePlugin.BsodaBonusSticker = stickerData.sticker;
                BasePlugin.BsodaBonusStickerSprite = sprite;
            }
            if (unfullBp != null && fullBp != null) BackpackManager.Setup(unfullBp, fullBp);

            foreach (var item in ModItems.Values)
            {
                if (item != null && item.name != null && !item.name.StartsWith("ExtraStuff_")) item.name = "ExtraStuff_" + item.name;
            }
        }

        public static class AudioHelper
        {
            public static float DefaultVolume = 1f;
            public static bool IsMutedByLibrary = false;
            private static AudioMixer? _echoMixer;

            public static void Mute()
            {
                if (!IsMutedByLibrary)
                {
                    DefaultVolume = AudioListener.volume;
                    AudioListener.volume = 0f;
                    IsMutedByLibrary = true;
                    if (_echoMixer == null) _echoMixer = Resources.FindObjectsOfTypeAll<AudioMixer>().FirstOrDefault(x => x != null && x.GetFloat("EchoWetMix", out _));
                    if (_echoMixer != null) _echoMixer.SetFloat("EchoWetMix", 0f);
                }
            }

            public static void Unmute()
            {
                if (IsMutedByLibrary)
                {
                    AudioListener.volume = DefaultVolume;
                    IsMutedByLibrary = false;
                    if (_echoMixer == null) _echoMixer = Resources.FindObjectsOfTypeAll<AudioMixer>().FirstOrDefault(x => x != null && x.GetFloat("EchoWetMix", out _));
                    if (_echoMixer != null) _echoMixer.SetFloat("EchoWetMix", 0f);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "Update")]
        internal static class OldLibraryAudioPatch
        {
            static void Postfix(PlayerManager __instance)
            {
                if (__instance == null || __instance.ec == null) return;
                var cell = __instance.ec.CellFromPosition(__instance.transform.position);
                if (cell != null && cell.room != null && (cell.room.category == RoomsSetup.OldLibraryCategory || cell.room.category == RoomsSetup.OldLibraryRNGCategory)) AudioHelper.Mute();
                else AudioHelper.Unmute();
            }
        }

        [HarmonyPatch(typeof(FieldTripBaseRoomFunction), "Initialize")]
        internal static class FieldTripLootPatch
        {
            [HarmonyPrefix]
            static void Prefix(FieldTripBaseRoomFunction __instance)
            {
                if (__instance == null) return;
                var guaranteedField = AccessTools.Field(typeof(FieldTripBaseRoomFunction), "guaranteedItems");
                if (guaranteedField?.GetValue(__instance) is List<ItemObject> guaranteedList)
                {
                    if (BasePlugin.ModItems.TryGetValue("itm_ruby", out var ruby) && ruby != null && !guaranteedList.Contains(ruby)) guaranteedList.Add(ruby);
                    if (BasePlugin.ModItems.TryGetValue("itm_emerald", out var emerald) && emerald != null && !guaranteedList.Contains(emerald)) guaranteedList.Add(emerald);
                }
            }
        }

        [HarmonyPatch(typeof(LevelGenerator), "Generate")]
        internal static class ItemsSpawnPatch
        {
            private static WeightedItemObject[]? _originalItems;

            [HarmonyPrefix]
            static void Prefix(LevelGenerator __instance)
            {
                if (__instance == null || __instance.ld == null) return;
                _originalItems = __instance.ld.potentialItems;
                if (_originalItems != null)
                {
                    var list = _originalItems.ToList();

                    void AddItem(string key, int weight)
                    {
                        if (BasePlugin.ModItems.TryGetValue(key, out var item) && item != null && !list.Exists(x => x.selection == item))
                        {
                            list.Add(new WeightedItemObject { selection = item, weight = weight });
                        }
                    }

                    AddItem("itm_random_ytp", 50);
                    AddItem("itm_energy_drink", 40);
                    AddItem("itm_clean_chalk_eraser", 15);
                    AddItem("itm_hammer_reflex", 10);
                    AddItem("itm_prototype_teleporter", 47);
                    AddItem("itm_starfire", 23);
                    AddItem("itm_lighter", 24);
                    AddItem("itm_waterbott", 30);
                    AddItem("itm_coindoor", 5);
                    AddItem("itm_coolplant", 5);
                    AddItem("itm_paintbucket", 29);
                    AddItem("itm_gravitystick", 11);
                    AddItem("itm_dash", 25);
                    AddItem("itm_officekey", 20);
                    AddItem("itm_detentionpass", 10);
                    AddItem("itm_juicebottle", 25);
                    AddItem("itm_eventytp", 67);
                    AddItem("itm_controlleralarmclock", 15);
                    AddItem("itm_christmasytp", 30);
                    AddItem("itm_backpack_unfull", 15);
                    AddItem("itm_backpack_full", 5);
                    AddItem("itm_instantbalder", 9);
                    AddItem("itm_nutella", 15);
                    AddItem("itm_gum", 43);
                    AddItem("itm_speedyelixir", 18);
                    AddItem("itm_teacher_nametag", 20);
                    AddItem("itm_oldcandy", 20);
                    AddItem("itm_eventcontroller", 10);
                    AddItem("itm_randomizer", 20);
                    AddItem("itm_blastbsoda", 70);
                    AddItem("itm_darkcoffee", 40);
                    AddItem("itm_coolusefulteleporter", 15);
                    AddItem("itm_safetyshuriken", 20);
                    AddItem("itm_specialteapot", 25);
                    AddItem("itm_kimcola", 0);
                    AddItem("itm_tastyburger", 30);
                    AddItem("itm_bsodaplus", 20); 

                    __instance.ld.potentialItems = list.ToArray();
                }
            }

            [HarmonyPostfix]
            static void Postfix(LevelGenerator __instance, ref IEnumerator __result)
            {
                var original = __result;
                __result = Wrapper(__instance, original);
            }

            static IEnumerator Wrapper(LevelGenerator __instance, IEnumerator original)
            {
                yield return original;
                if (__instance != null && __instance.ld != null && _originalItems != null)
                {
                    __instance.ld.potentialItems = _originalItems;
                }
            }
        }

        [HarmonyPatch(typeof(CoreGameManager), "ReturnToMenu")]
        internal static class ResetAudioFlagsPatch
        {
            static void Postfix() { AudioHelper.Unmute(); }
        }

        public static class ModInfo
        {
            public const string ID = "blitzo.baldiplus.extrastuffrevived";
            public const string NAME = "Extra Stuff Revived";
            public const string VERSION = "1.1.0.0";
        }
    }
}