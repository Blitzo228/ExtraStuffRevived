using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class DecorationsSetup
    {
        private static AssetManager assetMan => BasePlugin.assetManager;
        private static bool hasStudio => Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio");
        private const string PREFIX = "ESR_Decoration_";

        public static IEnumerator RegisterDecorations()
        {
            yield return new WaitUntil(() => LevelLoaderPlugin.Instance != null);
            for (int i = 0; i < 10; i++) yield return null;

            string modPath = Path.Combine(Application.streamingAssetsPath, "Modded", ModInfo.ID);
            string otherDir = Path.Combine(modPath, "OtherStuff");

            Sprite? slotSprite = null;
            if (assetMan.ContainsKey("ESSlotIndividual"))
            {
                slotSprite = assetMan.Get<Sprite>("ESSlotIndividual");
            }

            yield return "Apple";
            Texture2D? appleTex = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t != null && t.name == "AppleIcon_Large");
            if (appleTex != null)
            {
                Sprite appleSpr = Sprite.Create(appleTex, new Rect(0, 0, appleTex.width, appleTex.height), new Vector2(0.5f, 0.5f), 65f);
                GameObject apple = new GameObject("AppleDecoration");

                SpriteRenderer? ren = apple.AddComponent<SpriteRenderer>();
                if (ren != null)
                {
                    ren.sprite = appleSpr;
                    Material? billboardMat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m != null && (m.name == "SpriteStandard_Billboard_NoGlitch" || m.name == "SpriteStandard_Billboard"));
                    if (billboardMat != null) ren.material = billboardMat;
                    else ren.material = ObjectCreators.SpriteMaterial;

                    ren.sortingLayerName = "Default";
                }

                apple.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

                BoxCollider? col = apple.AddComponent<BoxCollider>();
                if (col != null)
                {
                    col.isTrigger = true;
                    col.size = new Vector3(1f, 1f, 0.5f);
                }

                RendererContainer? rc = apple.AddComponent<RendererContainer>();
                if (rc != null && ren != null)
                {
                    rc.renderers = new Renderer[] { ren };
                }

                AddObjectToEditor(apple);

                if (hasStudio)
                {
                    string key = PREFIX + apple.name;
                    GameObject? prefab = assetMan.Get<GameObject>($"editorPrefab_{apple.name}");
                    if (prefab != null)
                    {
                        RegisterStudioApple(key, prefab, appleSpr, slotSprite);
                    }
                }
            }

            yield return "Globes";
            GameObject? globeOrig = Resources.FindObjectsOfTypeAll<RendererContainer>().FirstOrDefault(x => x != null && x.name == "Decor_Globe")?.gameObject;
            if (globeOrig != null)
            {
                GameObject giantBase = new GameObject("GiantGlobe");
                GameObject? giantGlobe = Object.Instantiate(globeOrig, giantBase.transform);
                if (giantGlobe != null)
                {
                    giantGlobe.transform.localScale *= 3f;
                    giantGlobe.transform.localPosition += Vector3.down * 5f;
                    giantGlobe.name = "Decor_Globe";

                    BoxCollider? gCol = giantBase.AddComponent<BoxCollider>();
                    if (gCol != null)
                    {
                        gCol.size = new Vector3(6f, 8f, 6f);
                        gCol.isTrigger = true;
                    }

                    Renderer[] gRends = giantGlobe.GetComponentsInChildren<Renderer>();
                    RendererContainer? gRc = giantBase.AddComponent<RendererContainer>();
                    if (gRc != null)
                    {
                        gRc.renderers = gRends;
                    }

                    AddObjectToEditor(giantBase);

                    if (hasStudio)
                    {
                        string gKey = PREFIX + giantBase.name;
                        GameObject? gPrefab = assetMan.Get<GameObject>($"editorPrefab_{giantBase.name}");
                        if (gPrefab != null)
                        {
                            Sprite? gIcon = LoadEditorSprite(otherDir, "GiantGlobeIcon");
                            if (gIcon == null && gRends.Length > 0 && gRends[0] != null && gRends[0].material != null && gRends[0].material.mainTexture is Texture2D tex)
                                gIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

                            if (gIcon != null)
                            {
                                RegisterStudioGiantGlobe(gKey, gPrefab, gIcon, slotSprite);
                            }
                        }
                    }
                }

                GameObject notBase = new GameObject("NotGiantGlobe");
                GameObject? notGlobe = Object.Instantiate(globeOrig, notBase.transform);
                if (notGlobe != null)
                {
                    notGlobe.transform.localScale *= 1.5f;
                    notGlobe.transform.localPosition += Vector3.down * 2.25f;
                    notGlobe.name = "Decor_Globe";

                    BoxCollider? nCol = notBase.AddComponent<BoxCollider>();
                    if (nCol != null)
                    {
                        nCol.size = new Vector3(3f, 4f, 3f);
                        nCol.isTrigger = true;
                    }

                    Renderer[] nRends = notGlobe.GetComponentsInChildren<Renderer>();
                    RendererContainer? nRc = notBase.AddComponent<RendererContainer>();
                    if (nRc != null)
                    {
                        nRc.renderers = nRends;
                    }

                    AddObjectToEditor(notBase);

                    if (hasStudio)
                    {
                        string nKey = PREFIX + notBase.name;
                        GameObject? nPrefab = assetMan.Get<GameObject>($"editorPrefab_{notBase.name}");
                        if (nPrefab != null)
                        {
                            Sprite? nIcon = LoadEditorSprite(otherDir, "NotGiantGlobeIcon");
                            if (nIcon == null && nRends.Length > 0 && nRends[0] != null && nRends[0].material != null && nRends[0].material.mainTexture is Texture2D tex2)
                                nIcon = Sprite.Create(tex2, new Rect(0, 0, tex2.width, tex2.height), new Vector2(0.5f, 0.5f), 100f);

                            if (nIcon != null)
                            {
                                RegisterStudioNotGiantGlobe(nKey, nPrefab, nIcon, slotSprite);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterStudioApple(string key, GameObject prefab, Sprite appleSpr, Sprite? slotSprite)
        {
            EditorInterface.AddObjectVisual(key, prefab, true);
            EditorInterfaceModes.AddModeCallback((mode, _) =>
            {
                var tool = new ObjectTool(key, appleSpr, 0.5f);
                if (slotSprite != null) tool.frameOverride = slotSprite;
                EditorInterfaceModes.AddToolToCategory(mode, "objects", tool);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterStudioGiantGlobe(string key, GameObject prefab, Sprite icon, Sprite? slotSprite)
        {
            EditorInterface.AddObjectVisualWithCustomSphereCollider(key, prefab, 2.5f, Vector3.up * 5f);
            EditorInterfaceModes.AddModeCallback((mode, _) =>
            {
                var tool = new ObjectToolNoRotation(key, icon);
                if (slotSprite != null) tool.frameOverride = slotSprite;
                EditorInterfaceModes.AddToolToCategory(mode, "objects", tool);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterStudioNotGiantGlobe(string key, GameObject prefab, Sprite icon, Sprite? slotSprite)
        {
            EditorInterface.AddObjectVisualWithCustomSphereCollider(key, prefab, 1.5f, Vector3.up * 2.25f);
            EditorInterfaceModes.AddModeCallback((mode, _) =>
            {
                var tool = new ObjectToolNoRotation(key, icon);
                if (slotSprite != null) tool.frameOverride = slotSprite;
                EditorInterfaceModes.AddToolToCategory(mode, "objects", tool);
            });
        }

        private static void AddObjectToEditor(GameObject obj)
        {
            string key = PREFIX + obj.name;
            if (LevelLoaderPlugin.Instance != null && LevelLoaderPlugin.Instance.basicObjects != null)
            {
                if (!LevelLoaderPlugin.Instance.basicObjects.ContainsKey(key))
                    LevelLoaderPlugin.Instance.basicObjects.Add(key, obj);
            }

            assetMan.Add($"editorPrefab_{obj.name}", obj);
            obj.ConvertToPrefab(true);
        }

        private static Sprite? LoadEditorSprite(string dir, string name)
        {
            string path = Path.Combine(dir, name + ".png");
            if (!File.Exists(path)) return null;
            Texture2D? tex = AssetLoader.TextureFromFile(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}