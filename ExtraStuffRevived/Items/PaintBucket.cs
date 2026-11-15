using MTM101BaldAPI.AssetTools;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_PaintBucket : Item
    {
        private static SoundObject? splashSound;
        private static bool splashTried;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var core = Singleton<CoreGameManager>.Instance;
            if (core == null) return false;

            NPC? targetNpc = null;
            var cam = core.GetCamera(pm.playerNumber);
            if (cam != null && pm.pc != null &&
                Physics.Raycast(pm.transform.position, cam.transform.forward,
                                out RaycastHit hit, pm.pc.reach + 2f))
            {
                var hitTransform = hit.transform;
                if (hitTransform != null && hitTransform.CompareTag("NPC"))
                {
                    var parent = hitTransform.GetComponentInParent<NPC>();
                    if (parent != null) targetNpc = parent;
                }
            }

            if (targetNpc == null)
            {
                PlayErrorSound(core);
                return false;
            }

            PlaySplashSound(core);

            targetNpc.StartCoroutine(BlindNpc(targetNpc, 10f));
            Object.Destroy(gameObject);
            return true;
        }

        private static SoundObject? GetSplashSound()
        {
            if (splashTried) return splashSound;
            splashTried = true;

            string path = BasePlugin.GetModFilePath("Audio", "Splash.wav");
            if (!System.IO.File.Exists(path)) return null;

            var clip = AssetLoader.AudioClipFromFile(path);
            if (clip == null) return null;

            var so = ScriptableObject.CreateInstance<SoundObject>();
            if (so == null) return null;

            so.soundClip = clip;
            so.soundType = SoundType.Effect;
            so.subtitle = false;
            so.name = "ExtraStuff_SplashSound";
            splashSound = so;
            return splashSound;
        }

        private static void PlaySplashSound(CoreGameManager core)
        {
            if (core == null || core.audMan == null) return;

            var sound = GetSplashSound();
            if (sound != null)
                core.audMan.PlaySingle(sound);
        }

        private static void PlayErrorSound(CoreGameManager core)
        {
            if (core == null || core.audMan == null) return;

            var err = Resources.FindObjectsOfTypeAll<SoundObject>()
                .FirstOrDefault(x => x != null && x.name == "ErrorMaybe");

            if (err != null) core.audMan.PlaySingle(err);
        }

        public static IEnumerator BlindNpc(NPC npc, float duration)
        {
            if (npc == null) yield break;

            var looker = npc.looker;
            if (looker == null) yield break;

            float originalDistance = looker.distance;
            looker.distance = 0f;

            var effectObj = new GameObject("BlindEffect");
            var effectTr = effectObj.transform;
            if (effectTr != null)
            {
                effectTr.SetParent(npc.transform);
                effectTr.localPosition = Vector3.up * 1.5f;
            }

            var effectRenderer = effectObj.AddComponent<SpriteRenderer>();
            if (effectRenderer != null)
            {
                if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/PaintBucket_Blind"))
                {
                    var sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/PaintBucket_Blind");
                    if (sprite != null) effectRenderer.sprite = sprite;
                }

                effectRenderer.sortingLayerName = "NPCs";
                effectRenderer.sortingOrder = 10;
            }

            effectObj.AddComponent<BlindEffectFollower>();

            float t = duration;
            while (t > 0f)
            {
                if (npc == null) yield break;
                t -= Time.deltaTime * npc.TimeScale;
                yield return null;
            }

            if (looker != null)
                looker.distance = originalDistance;

            if (effectObj != null)
                Object.Destroy(effectObj);
        }
    }

    public class BlindEffectFollower : MonoBehaviour
    {
        private Camera? _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            var cam = _cam;
            if (cam != null)
                transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        }
    }
}