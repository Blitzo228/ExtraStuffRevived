using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class WindowsSetup
    {
        public static void RegisterWindows(LevelLoaderPlugin loader)
        {
            RegisterWindow(loader, "classicwindow", "ClassicWindow", "ClassicWindow_Broken", "ClassicMask");
            RegisterWindow(loader, "classicalwindow", "ClassicalWindow_Default", "ClassicalWindow_Broken", "ClassicalWindow_Mask");
            RegisterWindow(loader, "stdwindow", "WindowTexture", "WindowTexture_Broken", "WindowMask");
            RegisterWindow(loader, "baldiwindow", "ExtraStuff_BaldiWindow_Default", "ExtraStuff_BaldiWindow_Broken", "ExtraStuff_BaldiWindow_Mask");
        }

        private static void RegisterWindow(LevelLoaderPlugin loader, string id, string normalKey, string brokenKey, string maskKey)
        {
            if (!BasePlugin.assetManager.ContainsKey(normalKey) ||
                !BasePlugin.assetManager.ContainsKey(brokenKey) ||
                !BasePlugin.assetManager.ContainsKey(maskKey))
                return;

            Texture2D normalTex = BasePlugin.assetManager.Get<Texture2D>(normalKey);
            Texture2D brokenTex = BasePlugin.assetManager.Get<Texture2D>(brokenKey);
            Texture2D maskTex = BasePlugin.assetManager.Get<Texture2D>(maskKey);

            if (normalTex == null || brokenTex == null || maskTex == null) return;

            var win = ObjectCreators.CreateWindowObject(id, normalTex, brokenTex, maskTex);
            if (win == null) return;

            win.allowSoundThrough = true;
            win.breakable = true;

            loader.windowObjects[id] = win;

            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                RegisterEditorWindow(id, win);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterEditorWindow(string id, WindowObject win)
        {
            EditorInterface.AddWindow(id, win);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddEditorTools(EditorMode mode, Sprite slotSprite)
        {
            AddWindowTool(mode, "classicwindow", "ClassicWindowIcon", slotSprite);
            AddWindowTool(mode, "classicalwindow", "ClassicalWindowIcon", slotSprite);
            AddWindowTool(mode, "stdwindow", "WindowIcon", slotSprite);
            AddWindowTool(mode, "baldiwindow", "ExtraStuff_BaldiWindowIcon", slotSprite);
        }

        private static void AddWindowTool(EditorMode mode, string windowType, string iconKey, Sprite slotSprite)
        {
            if (BasePlugin.assetManager == null || !BasePlugin.assetManager.ContainsKey(iconKey)) return;

            Sprite icon = BasePlugin.assetManager.Get<Sprite>(iconKey);
            if (icon == null) return;

            var tool = new WindowTool(windowType, icon);
            if (slotSprite != null) tool.frameOverride = slotSprite;

            EditorInterfaceModes.AddToolToCategory(mode, "doors", tool, false);
        }
    }
}