using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class PostersSetup
    {
        public static void SetupPosters()
        {
            var loader = LevelLoaderPlugin.Instance;
            if (loader == null) return;

            string[] simplePosters = { "TestComic", "TestPoster", "MyOfficePlusPoster", "MyOfficeRulesPoster", "MyOfficeToys", "MyOfficeWhiteboard", "MyOfficeWindow", "hnt_windows" };

            foreach (var pKey in simplePosters)
            {
                if (BasePlugin.assetManager.ContainsKey(pKey) && !loader.posterAliases.ContainsKey(pKey))
                {
                    var tex = BasePlugin.assetManager.Get<Texture2D>(pKey);
                    if (tex != null)
                    {
                        var poster = ObjectCreators.CreatePosterObject(tex, new PosterTextData[0]);
                        poster.name = pKey;
                        loader.posterAliases[pKey] = poster;
                    }
                }
            }
        }
    }
}