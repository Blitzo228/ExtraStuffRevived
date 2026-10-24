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
    public class BasePlugin : BaseUnityPlugin
    {
        public static BasePlugin Instance = null!;
        public static AssetManager assetManager = new AssetManager();
        public static Dictionary<string, ItemObject> ModItems = new Dictionary<string, ItemObject>();

        public static bool IsRewiredCompatInstalled { get; private set; }
        private static bool inputsRegistered = false;

        private string ModAssetsPath => Path.Combine(Application.streamingAssetsPath, "Modded", ModInfo.ID);

        private void Awake()
        {
            Instance = this;
            IsRewiredCompatInstalled = Chainloader.PluginInfos.ContainsKey("alexbw145.bbplus.rewiredcompat");

            Harmony harmony = new Harmony(ModInfo.ID);
            harmony.PatchAll();
            Harmony.CreateAndPatchAll(typeof(BasePlugin));

            LoadAssets();
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadMod(), LoadingEventOrder.Pre);

            GeneratorManagement.Register(this, GenerationModType.Finalizer, (levelName, levelNumber, scene) =>
            {
                ShopPatch.Apply(scene);
                RoomsSetup.AddRooms(scene, levelNumber);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RegisterRewiredInputs()
        {
            if (!IsRewiredCompatInstalled || inputsRegistered) return;
            BBPRewiredCompat.RewiredPlusManager.CreateNewInput(
                name: "UseDash",
                descriptionName: "Use Dash",
                behaviorID: BBPRewiredCompat.RewiredPlusManager.InputBehaviorID.Snap,
                categoryID: BBPRewiredCompat.RewiredPlusManager.InputMapCategory.Actions,
                key: KeyCode.R
            );
            inputsRegistered = true;
        }

        [HarmonyPatch(typeof(NameManager), "Awake")]
        [HarmonyPrefix]
        private static void RegisterInputsOnGameStart()
        {
            if (IsRewiredCompatInstalled)
            {
                RegisterRewiredInputs();
            }
        }

        private void LoadAssets()
        {
            string localizationPath = Path.Combine(ModAssetsPath, "Localization.json");
            if (File.Exists(localizationPath))
            {
                AssetLoader.LocalizationFromFile(localizationPath, Language.English);
            }

            string texDir = Path.Combine(ModAssetsPath, "Textures");
            string audioDir = Path.Combine(ModAssetsPath, "Audio");
            string otherDir = Path.Combine(ModAssetsPath, "OtherStuff");

            string officeWallPath = Path.Combine(otherDir, "Bal_Office_Wall.png");
            if (File.Exists(officeWallPath)) { var tex = AssetLoader.TextureFromFile(officeWallPath); if (tex != null) assetManager.Add("Bal_Office_Wall", tex); }

            string officeFloorPath = Path.Combine(otherDir, "Bal_Office_Floor.png");
            if (File.Exists(officeFloorPath)) { var tex = AssetLoader.TextureFromFile(officeFloorPath); if (tex != null) assetManager.Add("Bal_Office_Floor", tex); }

            string officeCeilPath = Path.Combine(otherDir, "Bal_Office_Ceiling.png");
            if (File.Exists(officeCeilPath)) { var tex = AssetLoader.TextureFromFile(officeCeilPath); if (tex != null) assetManager.Add("Bal_Office_Ceiling", tex); }

            string officeClosedPath = Path.Combine(otherDir, "Old_BaldisOffice_Closed.png");
            if (File.Exists(officeClosedPath)) { var tex = AssetLoader.TextureFromFile(officeClosedPath); if (tex != null) assetManager.Add("OfficeClass_Closed", tex); }

            string officeOpenPath = Path.Combine(otherDir, "Old_BaldisOffice_Open.png");
            if (File.Exists(officeOpenPath)) { var tex = AssetLoader.TextureFromFile(officeOpenPath); if (tex != null) assetManager.Add("OfficeClass_Open", tex); }

            string officeIconPath = Path.Combine(otherDir, "room_baldioffice.png");
            if (File.Exists(officeIconPath)) { var sprite = AssetLoader.SpriteFromFile(officeIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("Editor BaldiOffice", sprite); }

            string officeUnlockIconPath = Path.Combine(otherDir, "room_baldiofficeunlock.png");
            if (File.Exists(officeUnlockIconPath)) { var sprite = AssetLoader.SpriteFromFile(officeUnlockIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("Editor BaldiOfficeUnlock", sprite); }

            string mcGWallPath = Path.Combine(otherDir, "BasicRealWall.png");
            if (File.Exists(mcGWallPath)) { var tex = AssetLoader.TextureFromFile(mcGWallPath); if (tex != null) assetManager.Add("BasicRealWall", tex); }

            string mcGFloorPath = Path.Combine(otherDir, "BasicRealCarpet.png");
            if (File.Exists(mcGFloorPath)) { var tex = AssetLoader.TextureFromFile(mcGFloorPath); if (tex != null) assetManager.Add("BasicRealCarpet", tex); }

            string mcGCeilPath = Path.Combine(otherDir, "BasicRealCeiling.png");
            if (File.Exists(mcGCeilPath)) { var tex = AssetLoader.TextureFromFile(mcGCeilPath); if (tex != null) assetManager.Add("BasicRealCeiling", tex); }

            string mcGClosedPath = Path.Combine(otherDir, "McGonigal_Closed.png");
            if (File.Exists(mcGClosedPath)) { var tex = AssetLoader.TextureFromFile(mcGClosedPath); if (tex != null) assetManager.Add("McGonigal_Closed", tex); }

            string mcGOpenPath = Path.Combine(otherDir, "McGonigal_Open.png");
            if (File.Exists(mcGOpenPath)) { var tex = AssetLoader.TextureFromFile(mcGOpenPath); if (tex != null) assetManager.Add("McGonigal_Open", tex); }

            string mcGIconPath = Path.Combine(otherDir, "room_mcgonigaloffice.png");
            if (File.Exists(mcGIconPath)) { var sprite = AssetLoader.SpriteFromFile(mcGIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("Editor McGonigalRoom", sprite); }

            string beltIconPath = Path.Combine(otherDir, "room_beltroom.png");
            if (File.Exists(beltIconPath)) { var sprite = AssetLoader.SpriteFromFile(beltIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("Editor BeltRoom", sprite); }

            string oldBookWallPath = Path.Combine(otherDir, "Old_BookHolder.png");
            if (File.Exists(oldBookWallPath)) { var tex = AssetLoader.TextureFromFile(oldBookWallPath); if (tex != null) assetManager.Add("Old_BookHolder", tex); }

            string oldLibIconPath = Path.Combine(otherDir, "room_OldLibrary.png");
            if (File.Exists(oldLibIconPath)) { var sprite = AssetLoader.SpriteFromFile(oldLibIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("Editor OldLibraryRoom", sprite); }

            string mazeIconPath = Path.Combine(otherDir, "room_OldLibraryRNG.png");
            if (File.Exists(mazeIconPath)) { var sprite = AssetLoader.SpriteFromFile(mazeIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("Editor MazeRoom", sprite); }

            string oldWoodPath = Path.Combine(otherDir, "OldWood.png");
            if (File.Exists(oldWoodPath)) { var tex = AssetLoader.TextureFromFile(oldWoodPath); if (tex != null) assetManager.Add("OldWood", tex); }

            string oldFencePath = Path.Combine(otherDir, "Oldfence.png");
            if (File.Exists(oldFencePath)) { var tex = AssetLoader.TextureFromFile(oldFencePath); if (tex != null) assetManager.Add("Oldfence", tex); }

            string hntWindowsPath = Path.Combine(otherDir, "hnt_windows.png");
            if (File.Exists(hntWindowsPath)) { var tex = AssetLoader.TextureFromFile(hntWindowsPath); if (tex != null) assetManager.Add("hnt_windows", tex); }

            string testComicPath = Path.Combine(otherDir, "TestComic.png");
            if (File.Exists(testComicPath)) { var tex = AssetLoader.TextureFromFile(testComicPath); if (tex != null) assetManager.Add("TestComic", tex); }

            string testPosterPath = Path.Combine(otherDir, "TestPoster.png");
            if (File.Exists(testPosterPath)) { var tex = AssetLoader.TextureFromFile(testPosterPath); if (tex != null) assetManager.Add("TestPoster", tex); }

            string lockerEasterEggPath = Path.Combine(otherDir, "EasterEggBaldiInLocker.png");
            if (File.Exists(lockerEasterEggPath)) { var tex = AssetLoader.TextureFromFile(lockerEasterEggPath); if (tex != null) assetManager.Add("EasterEggLockerTex", tex); }

            string slotPath = Path.Combine(otherDir, "ESSlotIndividual.png");
            if (File.Exists(slotPath)) { var sprite = AssetLoader.SpriteFromFile(slotPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("ESSlotIndividual", sprite); }

            string classicMaskPath = Path.Combine(otherDir, "ClassicMask.png");
            if (File.Exists(classicMaskPath)) { var tex = AssetLoader.TextureFromFile(classicMaskPath); if (tex != null) assetManager.Add("ClassicMask", tex); }

            string classicWinPath = Path.Combine(otherDir, "ClassicWindow.png");
            if (File.Exists(classicWinPath)) { var tex = AssetLoader.TextureFromFile(classicWinPath); if (tex != null) assetManager.Add("ClassicWindow", tex); }

            string classicBrokenPath = Path.Combine(otherDir, "ClassicWindow_Broken.png");
            if (File.Exists(classicBrokenPath)) { var tex = AssetLoader.TextureFromFile(classicBrokenPath); if (tex != null) assetManager.Add("ClassicWindow_Broken", tex); }

            string classicWinIconPath = Path.Combine(otherDir, "ClassicWindowIcon.png");
            if (File.Exists(classicWinIconPath)) { var sprite = AssetLoader.SpriteFromFile(classicWinIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("ClassicWindowIcon", sprite); }

            string classMaskPath = Path.Combine(otherDir, "ClassicalWindow_Mask.png");
            if (File.Exists(classMaskPath)) { var tex = AssetLoader.TextureFromFile(classMaskPath); if (tex != null) assetManager.Add("ClassicalWindow_Mask", tex); }

            string classWinPath = Path.Combine(otherDir, "ClassicalWindow_Default.png");
            if (File.Exists(classWinPath)) { var tex = AssetLoader.TextureFromFile(classWinPath); if (tex != null) assetManager.Add("ClassicalWindow_Default", tex); }

            string classBrokenPath = Path.Combine(otherDir, "ClassicalWindow_Broken.png");
            if (File.Exists(classBrokenPath)) { var tex = AssetLoader.TextureFromFile(classBrokenPath); if (tex != null) assetManager.Add("ClassicalWindow_Broken", tex); }

            string classicalWinIconPath = Path.Combine(otherDir, "ClassicalWindowIcon.png");
            if (File.Exists(classicalWinIconPath)) { var sprite = AssetLoader.SpriteFromFile(classicalWinIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("ClassicalWindowIcon", sprite); }

            string winMaskPath = Path.Combine(otherDir, "WindowMask.png");
            if (File.Exists(winMaskPath)) { var tex = AssetLoader.TextureFromFile(winMaskPath); if (tex != null) assetManager.Add("WindowMask", tex); }

            string winTexPath = Path.Combine(otherDir, "WindowTexture.png");
            if (File.Exists(winTexPath)) { var tex = AssetLoader.TextureFromFile(winTexPath); if (tex != null) assetManager.Add("WindowTexture", tex); }

            string winBrokenPath = Path.Combine(otherDir, "WindowTexture_Broken.png");
            if (File.Exists(winBrokenPath)) { var tex = AssetLoader.TextureFromFile(winBrokenPath); if (tex != null) assetManager.Add("WindowTexture_Broken", tex); }

            string windowIconPath = Path.Combine(otherDir, "WindowIcon.png");
            if (File.Exists(windowIconPath)) { var sprite = AssetLoader.SpriteFromFile(windowIconPath, new Vector2(0.5f, 0.5f), 1f); if (sprite != null) assetManager.Add("WindowIcon", sprite); }

            string[] posterFiles = { "MyOfficePlusPoster", "MyOfficeRulesPoster", "MyOfficeToys", "MyOfficeWhiteboard", "MyOfficeWindow" };
            foreach (var f in posterFiles)
            {
                string path = Path.Combine(otherDir, f + ".png");
                if (File.Exists(path)) { var tex = AssetLoader.TextureFromFile(path); if (tex != null) assetManager.Add(f, tex); }
            }

            string coinDoorSmallPath = Path.Combine(texDir, "CoinDoorItemIcon_Small.png");
            if (File.Exists(coinDoorSmallPath)) { var sprite = AssetLoader.SpriteFromFile(coinDoorSmallPath, new Vector2(0.5f, 0.5f), 40f); if (sprite != null) assetManager.Add("Sprites/itm_coindoor_small", sprite); }

            string coinDoorLargePath = Path.Combine(texDir, "CoinDoorItemIcon_Large.png");
            if (File.Exists(coinDoorLargePath)) { var sprite = AssetLoader.SpriteFromFile(coinDoorLargePath, new Vector2(0.5f, 0.5f), 40f); if (sprite != null) assetManager.Add("Sprites/itm_coindoor_large", sprite); }

            string[] texFiles = {
                "YTP_Ruby.png", "YTP_Ruby_Small.png", "YTP_Emerald.png", "YTP_Emerald_Small.png", "YTP_Random.png", "YTP_Random_Small.png",
                "EnergyDrinkIcon_Small.png", "EnergyDrinkIcon_Large.png", "CleanChalkEraserIcon_Small.png", "CleanChalkEraserIcon_Large.png",
                "ReflexHammerIcon_Small.png", "ReflexHammerIcon_Large.png", "PrototypeTeleporterIcon_Small.png", "PrototypeTeleporterIcon_Large.png",
                "LighterIcon_Small.png", "LighterIcon_Large.png", "FireParticles.png", "WaterBottleIcon_Small.png", "WaterBottleIcon_Large.png", "WaterSpray.png",
                "StarFireProjectile.png", "StarFireIcon_Small.png", "StarFireIcon_Large.png",
                "CoolPlantIcon_Small.png", "CoolPlantIcon_Large.png", "PaintBucketIcon_Small.png", "PaintBucketIcon_Large.png", "PaintBucket_Blind.png",
                "GravityStickIcon_Small.png", "GravityStickIcon_Large.png", "DashIcon_Small.png", "DashIcon_Large.png",
                "BaldiOfficeKeyIcon_Small.png", "BaldiOfficeKeyIcon_Large.png"
            };

            foreach (var f in texFiles)
            {
                string path = Path.Combine(texDir, f);
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

                    if (!assetManager.ContainsKey(key))
                    {
                        var sprite = AssetLoader.SpriteFromFile(path, new Vector2(0.5f, 0.5f), 40f);
                        if (sprite != null) assetManager.Add(key, sprite);
                    }
                }
            }

            string[] audioFiles = { "Ruby.wav", "Emerald.wav", "DrinkEnergy.wav", "Random.wav", "Lighter.wav", "Fire.wav", "Splash.wav", "StarFire.wav" };
            foreach (var f in audioFiles)
            {
                string path = Path.Combine(audioDir, f);
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

                    if (!assetManager.ContainsKey(key))
                    {
                        var clip = AssetLoader.AudioClipFromFile(path);
                        if (clip != null) assetManager.Add(key, clip);
                    }
                }
            }
        }

        private IEnumerator LoadMod()
        {
            yield return 1;
            yield return "Preparing Custom rooms...";

            var loader = LevelLoaderPlugin.Instance;
            if (loader != null)
            {
                WindowsSetup.RegisterWindows(loader);
                PostersSetup.SetupPosters();
                RoomsSetup.LoadRooms(loader, ModAssetsPath, assetManager);
            }

            yield return "Creating Items...";
            CreateItems();

            yield return "Loading Decorations...";
            yield return DecorationsSetup.RegisterDecorations();

            yield return "Preparing editor stuff";
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                EditorPatches.SetupEditorObjects();
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudioloader"))
                EditorPatches.AddLoaderContent();
        }

        private void CreateItems()
        {
            SoundObject? rubySound = null;
            if (assetManager.ContainsKey("Sounds/ruby_sound"))
            {
                var clip = assetManager.Get<AudioClip>("Sounds/ruby_sound");
                if (clip != null)
                {
                    rubySound = ObjectCreators.CreateSoundObject(clip, "", SoundType.Effect, Color.white, -1f);
                    if (rubySound != null) rubySound.subtitle = false;
                }
            }

            SoundObject? emeraldSound = null;
            if (assetManager.ContainsKey("Sounds/emerald_sound"))
            {
                var clip = assetManager.Get<AudioClip>("Sounds/emerald_sound");
                if (clip != null)
                {
                    emeraldSound = ObjectCreators.CreateSoundObject(clip, "", SoundType.Effect, Color.white, -1f);
                    if (emeraldSound != null) emeraldSound.subtitle = false;
                }
            }

            SoundObject? energySound = null;
            if (assetManager.ContainsKey("Sounds/drink_energy"))
            {
                var clip = assetManager.Get<AudioClip>("Sounds/drink_energy");
                if (clip != null)
                {
                    energySound = ObjectCreators.CreateSoundObject(clip, "", SoundType.Effect, Color.white, -1f);
                    if (energySound != null) energySound.subtitle = false;
                    assetManager.Add("Sounds/drink_energy", energySound);
                }
            }

            ITM_YTPs? vanillaPoints = null;
            var allItems = Resources.FindObjectsOfTypeAll<ITM_YTPs>();
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i] != null && !allItems[i].gameObject.scene.IsValid()) { vanillaPoints = allItems[i]; break; }
            }

            if (assetManager.ContainsKey("Sprites/itm_ruby"))
            {
                Sprite? rubySpriteSmall = assetManager.ContainsKey("Sprites/YTP_Ruby_Small") ? assetManager.Get<Sprite>("Sprites/YTP_Ruby_Small") : assetManager.Get<Sprite>("Sprites/itm_ruby");
                Sprite? rubySpriteLarge = assetManager.Get<Sprite>("Sprites/itm_ruby");

                if (rubySpriteSmall != null && rubySpriteLarge != null)
                {
                    var rubyItem = new ItemBuilder(Info)
                        .SetEnum(Items.Points)
                        .SetNameAndDescription("Itm_YTP_Ruby", "Desc_YTP_Ruby")
                        .SetSprites(rubySpriteSmall, rubySpriteLarge)
                        .SetGeneratorCost(500)
                        .SetAsInstantUse()
                        .Build();

                    if (rubySound != null) rubyItem.audPickupOverride = rubySound;

                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "CustomRubyPrefab";
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
                Sprite? emeraldSpriteSmall = assetManager.ContainsKey("Sprites/YTP_Emerald_Small") ? assetManager.Get<Sprite>("Sprites/YTP_Emerald_Small") : assetManager.Get<Sprite>("Sprites/itm_emerald");
                Sprite? emeraldSpriteLarge = assetManager.Get<Sprite>("Sprites/itm_emerald");

                if (emeraldSpriteSmall != null && emeraldSpriteLarge != null)
                {
                    var emeraldItem = new ItemBuilder(Info)
                        .SetEnum(Items.Points)
                        .SetNameAndDescription("Itm_YTP_Emerald", "Desc_YTP_Emerald")
                        .SetSprites(emeraldSpriteSmall, emeraldSpriteLarge)
                        .SetGeneratorCost(750)
                        .SetAsInstantUse()
                        .Build();

                    if (emeraldSound != null) emeraldItem.audPickupOverride = emeraldSound;

                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "CustomEmeraldPrefab";
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
                Sprite? randomSpriteSmall = assetManager.ContainsKey("Sprites/YTP_Random_Small") ? assetManager.Get<Sprite>("Sprites/YTP_Random_Small") : assetManager.Get<Sprite>("Sprites/YTP_Random");
                Sprite? randomSpriteLarge = assetManager.Get<Sprite>("Sprites/YTP_Random");

                SoundObject? randomSound = null;
                if (assetManager.ContainsKey("Sounds/random_pickup"))
                {
                    var clip = assetManager.Get<AudioClip>("Sounds/random_pickup");
                    if (clip != null)
                    {
                        randomSound = ObjectCreators.CreateSoundObject(clip, "", SoundType.Effect, Color.white, -1f);
                        if (randomSound != null) randomSound.subtitle = false;
                    }
                }

                if (randomSpriteSmall != null && randomSpriteLarge != null)
                {
                    var randomItem = new ItemBuilder(Info)
                        .SetEnum("Itm_RandomYTP")
                        .SetNameAndDescription("Itm_YTP_Random", "Desc_YTP_Random")
                        .SetSprites(randomSpriteSmall, randomSpriteLarge)
                        .SetGeneratorCost(0)
                        .SetAsInstantUse()
                        .Build();

                    if (randomSound != null) randomItem.audPickupOverride = randomSound;

                    if (vanillaPoints != null)
                    {
                        var customPrefab = UnityEngine.Object.Instantiate(vanillaPoints.gameObject);
                        customPrefab.name = "CustomRandomYTPPrefab";
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

            if (assetManager.ContainsKey("Sprites/itm_energy_small") && assetManager.ContainsKey("Sprites/itm_energy_large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/itm_energy_small");
                var large = assetManager.Get<Sprite>("Sprites/itm_energy_large");
                if (small != null && large != null)
                {
                    var energyDrinkItem = new ItemBuilder(Info)
                        .SetEnum("Itm_Energy_Drink")
                        .SetNameAndDescription("Itm_EnergyDrink", "Desc_EnergyDrink")
                        .SetSprites(small, large)
                        .SetGeneratorCost(100)
                        .SetShopPrice(400)
                        .SetItemComponent<ITM_EnergyDrink>()
                        .SetMeta(ItemFlags.Persists, new string[] { "drink", "food" })
                        .Build();
                    ModItems.Add("itm_energy_drink", energyDrinkItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/itm_chalk_eraser_small") && assetManager.ContainsKey("Sprites/itm_chalk_eraser_large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/itm_chalk_eraser_small");
                var large = assetManager.Get<Sprite>("Sprites/itm_chalk_eraser_large");
                if (small != null && large != null)
                {
                    var chalkEraserItem = new ItemBuilder(Info)
                        .SetEnum("Itm_CleanChalkEraser")
                        .SetNameAndDescription("Itm_CleanChalkEraser", "Desc_CleanChalkEraser")
                        .SetSprites(small, large)
                        .SetGeneratorCost(50)
                        .SetShopPrice(600)
                        .SetItemComponent<ITM_CleanChalkEraser>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool" })
                        .Build();
                    ModItems.Add("itm_clean_chalk_eraser", chalkEraserItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/ReflexHammerIcon_Small") && assetManager.ContainsKey("Sprites/ReflexHammerIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/ReflexHammerIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/ReflexHammerIcon_Large");
                if (small != null && large != null)
                {
                    var hammerItem = new ItemBuilder(Info)
                        .SetEnum("Itm_HammerReflex")
                        .SetNameAndDescription("Itm_HammerReflex", "Desc_HammerReflex")
                        .SetSprites(small, large)
                        .SetGeneratorCost(150)
                        .SetShopPrice(550)
                        .SetItemComponent<ITM_HammerReflex>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon" })
                        .Build();
                    ModItems.Add("itm_hammer_reflex", hammerItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/PrototypeTeleporterIcon_Small") && assetManager.ContainsKey("Sprites/PrototypeTeleporterIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/PrototypeTeleporterIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/PrototypeTeleporterIcon_Large");
                if (small != null && large != null)
                {
                    var teleporterItem = new ItemBuilder(Info)
                        .SetEnum("Itm_PrototypeTeleporter")
                        .SetNameAndDescription("Itm_PrototypeTeleporter", "Desc_PrototypeTeleporter")
                        .SetSprites(small, large)
                        .SetGeneratorCost(100)
                        .SetShopPrice(300)
                        .SetItemComponent<ITM_PrototypeTeleporter>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "teleport" })
                        .Build();
                    ModItems.Add("itm_prototype_teleporter", teleporterItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/StarFireIcon_Small") && assetManager.ContainsKey("Sprites/StarFireIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/StarFireIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/StarFireIcon_Large");
                if (small != null && large != null)
                {
                    SoundObject? itemPickupSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && (x.name == "ItemPickup" || x.name == "ItemPickup_Default"));
                    var starFireItem = new ItemBuilder(Info)
                        .SetEnum("Itm_StarFire")
                        .SetNameAndDescription("Itm_StarFire", "Desc_StarFire")
                        .SetSprites(small, large)
                        .SetGeneratorCost(250)
                        .SetShopPrice(400)
                        .SetItemComponent<ITM_StarFire>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "weapon", "magic" })
                        .Build();
                    if (itemPickupSound != null) starFireItem.audPickupOverride = itemPickupSound;
                    ModItems.Add("itm_starfire", starFireItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/LighterIcon_Small") && assetManager.ContainsKey("Sprites/LighterIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/LighterIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/LighterIcon_Large");
                if (small != null && large != null)
                {
                    var lighterItem = new ItemBuilder(Info)
                        .SetEnum("Itm_Lighter")
                        .SetNameAndDescription("Itm_Lighter", "Desc_Lighter")
                        .SetSprites(small, large)
                        .SetGeneratorCost(150)
                        .SetShopPrice(800)
                        .SetItemComponent<ITM_Lighter>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "fire" })
                        .Build();
                    ModItems.Add("itm_lighter", lighterItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/WaterBottleIcon_Small") && assetManager.ContainsKey("Sprites/WaterBottleIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/WaterBottleIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/WaterBottleIcon_Large");
                if (small != null && large != null)
                {
                    var waterBottleItem = new ItemBuilder(Info)
                        .SetEnum("Itm_WaterBott")
                        .SetNameAndDescription("Itm_WaterBott", "Desc_WaterBott")
                        .SetSprites(small, large)
                        .SetGeneratorCost(100)
                        .SetShopPrice(750)
                        .SetItemComponent<ITM_WaterBott>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "drink" })
                        .Build();
                    ModItems.Add("itm_waterbott", waterBottleItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/itm_coindoor_small") && assetManager.ContainsKey("Sprites/itm_coindoor_large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/itm_coindoor_small");
                var large = assetManager.Get<Sprite>("Sprites/itm_coindoor_large");
                if (small != null && large != null)
                {
                    var coinDoorItem = new ItemBuilder(Info)
                        .SetEnum("Itm_CoinDoor")
                        .SetNameAndDescription("Itm_CoinDoor", "Desc_CoinDoor")
                        .SetSprites(small, large)
                        .SetGeneratorCost(300)
                        .SetShopPrice(300)
                        .SetItemComponent<ITM_CoinDoor>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "key" })
                        .Build();
                    ModItems.Add("itm_coindoor", coinDoorItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/CoolPlantIcon_Small") && assetManager.ContainsKey("Sprites/CoolPlantIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/CoolPlantIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/CoolPlantIcon_Large");
                if (small != null && large != null)
                {
                    var coolPlantItem = new ItemBuilder(Info)
                        .SetEnum("Itm_CoolPlant")
                        .SetNameAndDescription("Itm_CoolPlant", "Desc_CoolPlant")
                        .SetSprites(small, large)
                        .SetGeneratorCost(100)
                        .SetShopPrice(900)
                        .SetItemComponent<ITM_CoolPlant>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "plant" })
                        .Build();
                    ModItems.Add("itm_coolplant", coolPlantItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/PaintBucketIcon_Small") && assetManager.ContainsKey("Sprites/PaintBucketIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/PaintBucketIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/PaintBucketIcon_Large");
                if (small != null && large != null)
                {
                    var paintBucketItem = new ItemBuilder(Info)
                        .SetEnum("Itm_PaintBucket")
                        .SetNameAndDescription("Itm_PaintBucket", "Desc_PaintBucket")
                        .SetSprites(small, large)
                        .SetGeneratorCost(165)
                        .SetShopPrice(400)
                        .SetItemComponent<ITM_PaintBucket>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "blind" })
                        .Build();
                    ModItems.Add("itm_paintbucket", paintBucketItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/GravityStickIcon_Small") && assetManager.ContainsKey("Sprites/GravityStickIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/GravityStickIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/GravityStickIcon_Large");
                if (small != null && large != null)
                {
                    var gravityStickItem = new ItemBuilder(Info)
                        .SetEnum("Itm_GravityStick")
                        .SetNameAndDescription("Itm_GravityStick", "Desc_GravityStick")
                        .SetSprites(small, large)
                        .SetGeneratorCost(200)
                        .SetShopPrice(750)
                        .SetItemComponent<ITM_GravityStick>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "gravity" })
                        .Build();
                    ModItems.Add("itm_gravitystick", gravityStickItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/DashIcon_Small") && assetManager.ContainsKey("Sprites/DashIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/DashIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/DashIcon_Large");
                if (small != null && large != null)
                {
                    var dashItem = new ItemBuilder(Info)
                        .SetEnum("Itm_Dash")
                        .SetNameAndDescription("Itm_Dash", "Desc_Dash")
                        .SetSprites(small, large)
                        .SetGeneratorCost(150)
                        .SetShopPrice(555)
                        .SetItemComponent<ITM_Dash>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "movement" })
                        .Build();
                    ModItems.Add("itm_dash", dashItem);
                }
            }

            if (assetManager.ContainsKey("Sprites/BaldiOfficeKeyIcon_Small") && assetManager.ContainsKey("Sprites/BaldiOfficeKeyIcon_Large"))
            {
                var small = assetManager.Get<Sprite>("Sprites/BaldiOfficeKeyIcon_Small");
                var large = assetManager.Get<Sprite>("Sprites/BaldiOfficeKeyIcon_Large");
                if (small != null && large != null)
                {
                    var keyItem = new ItemBuilder(Info)
                        .SetEnum("Itm_BaldiOfficeKey")
                        .SetNameAndDescription("Itm_BaldiOfficeKey", "Desc_BaldiOfficeKey")
                        .SetSprites(small, large)
                        .SetGeneratorCost(40)
                        .SetShopPrice(450)
                        .SetItemComponent<ITM_BaldiOfficeKey>()
                        .SetMeta(ItemFlags.Persists, new string[] { "tool", "key" })
                        .Build();
                    ModItems.Add("itm_officekey", keyItem);
                }
            }
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

                if (_echoMixer == null)
                    _echoMixer = Resources.FindObjectsOfTypeAll<AudioMixer>().FirstOrDefault(x => x != null && x.GetFloat("EchoWetMix", out _));

                if (_echoMixer != null)
                    _echoMixer.SetFloat("EchoWetMix", 0f);
            }
        }

        public static void Unmute()
        {
            if (IsMutedByLibrary)
            {
                AudioListener.volume = DefaultVolume;
                IsMutedByLibrary = false;

                if (_echoMixer == null)
                    _echoMixer = Resources.FindObjectsOfTypeAll<AudioMixer>().FirstOrDefault(x => x != null && x.GetFloat("EchoWetMix", out _));

                if (_echoMixer != null)
                    _echoMixer.SetFloat("EchoWetMix", 0f);
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
            if (cell != null && cell.room != null && (cell.room.category == RoomsSetup.OldLibraryCategory || cell.room.category == RoomsSetup.OldLibraryRNGCategory))
            {
                AudioHelper.Mute();
            }
            else
            {
                AudioHelper.Unmute();
            }
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
                if (BasePlugin.ModItems.TryGetValue("itm_ruby", out var ruby) && ruby != null && !guaranteedList.Contains(ruby))
                    guaranteedList.Add(ruby);
                if (BasePlugin.ModItems.TryGetValue("itm_emerald", out var emerald) && emerald != null && !guaranteedList.Contains(emerald))
                    guaranteedList.Add(emerald);
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
                AddItem("itm_hammer_reflex", 20);
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
                __instance.ld.potentialItems = _originalItems!;
            }
        }
    }

    [HarmonyPatch(typeof(CoreGameManager), "ReturnToMenu")]
    internal static class ResetAudioFlagsPatch
    {
        static void Postfix()
        {
            AudioHelper.Unmute();
        }
    }

    public static class ModInfo
    {
        public const string ID = "blitzo.baldiplus.extrastuffrevived";
        public const string NAME = "Extra Stuff Revived";
        public const string VERSION = "1.0.0.0";
    }
}
