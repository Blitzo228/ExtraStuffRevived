using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_StarFire : Item
    {
        private static SoundObject? starFireSound;
        private static ITM_BSODA? bsodaPrefab;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            if (starFireSound == null)
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Modded", ModInfo.ID, "Audio", "StarFire.wav");
                if (File.Exists(path))
                {
                    starFireSound = ScriptableObject.CreateInstance<SoundObject>();
                    starFireSound.soundClip = AssetLoader.AudioClipFromFile(path);
                    starFireSound.soundType = SoundType.Effect;
                    starFireSound.subtitle = false;
                }
            }

            if (starFireSound != null && starFireSound.soundClip != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(starFireSound);

            if (bsodaPrefab == null)
            {
                bsodaPrefab = Resources.FindObjectsOfTypeAll<ITM_BSODA>().FirstOrDefault();
            }

            if (bsodaPrefab != null)
            {
                var projObj = Object.Instantiate(bsodaPrefab.gameObject);
                projObj.name = "StarFireProjectile";

                var oldColliders = projObj.GetComponentsInChildren<Collider>();
                foreach (var c in oldColliders) c.enabled = false;

                var trigger = projObj.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.5f;

                var renderer = projObj.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null && BasePlugin.assetManager.ContainsKey("Sprites/StarFireProjectile"))
                {
                    renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/StarFireProjectile");
                    renderer.transform.localScale = new Vector3(1f, 1f, 1f);
                }

                var bsodaComp = projObj.GetComponent<ITM_BSODA>();

                var timeField = HarmonyLib.AccessTools.Field(typeof(ITM_BSODA), "time");
                if (timeField != null) timeField.SetValue(bsodaComp, 30f);

                var soundField = HarmonyLib.AccessTools.Field(typeof(ITM_BSODA), "sound");
                if (soundField != null && starFireSound != null)
                    soundField.SetValue(bsodaComp, starFireSound);

                projObj.AddComponent<StarFireBehavior>();
                projObj.AddComponent<StarFireAfterimage>();

                bsodaComp.Use(pm);
            }

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class StarFireBehavior : MonoBehaviour
    {
        private bool hasHit = false;
        private static SoundObject? _slapSound;

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit || other == null) return;

            if (other.CompareTag("NPC"))
            {
                hasHit = true;
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null)
                {
                    if (_slapSound == null) _slapSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name.ToLowerInvariant().Contains("slap"));
                    if (_slapSound != null && _slapSound.soundClip != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(_slapSound);

                    var stunManager = new GameObject("StunManager").AddComponent<StunManager>();
                    stunManager.StartCoroutine(stunManager.StunNpc(npc, 10f));
                }

                var bsoda = GetComponent<ITM_BSODA>();
                if (bsoda != null) bsoda.enabled = false;

                Destroy(gameObject);
            }
            else if (other.CompareTag("Wall") || other.name.ToLower().Contains("wall"))
            {
                hasHit = true;
                KillBsoda();
            }
        }

        private void KillBsoda()
        {
            var bsoda = GetComponent<ITM_BSODA>();
            if (bsoda != null)
            {
                var timeField = HarmonyLib.AccessTools.Field(typeof(ITM_BSODA), "time");
                if (timeField != null) timeField.SetValue(bsoda, 0.01f);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public class StarFireAfterimage : MonoBehaviour
    {
        private float timer = 0f;
        private SpriteRenderer? sr;

        private void Start()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (sr == null || sr.sprite == null) return;

            timer += Time.deltaTime;
            if (timer >= 0.045f)
            {
                timer = 0f;
                SpawnGhost();
            }
        }

        private void SpawnGhost()
        {
            if (sr == null) return;

            GameObject ghostObj = new GameObject("StarFire_Ghost");
            ghostObj.transform.SetPositionAndRotation(sr.transform.position, sr.transform.rotation);
            ghostObj.transform.localScale = sr.transform.lossyScale;

            SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
            ghostSr.sprite = sr.sprite;
            ghostSr.flipX = sr.flipX;
            ghostSr.flipY = sr.flipY;
            ghostSr.sortingLayerID = sr.sortingLayerID;
            ghostSr.sortingOrder = sr.sortingOrder;

            if (sr.sharedMaterial != null)
                ghostSr.material = new Material(sr.sharedMaterial);

            Color startColor = sr.color;
            startColor.a *= 0.45f;
            ghostSr.color = startColor;

            var anim = ghostObj.AddComponent<AfterimageFader>();
            anim.Initialize(ghostSr, 0.3f, startColor);
        }
    }

    public class AfterimageFader : MonoBehaviour
    {
        private SpriteRenderer? sr;
        private float lifetime;
        private float age;
        private Color originalColor;

        public void Initialize(SpriteRenderer renderer, float life, Color col)
        {
            sr = renderer;
            lifetime = life;
            originalColor = col;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(age / lifetime));

            if (sr != null)
            {
                Color color = originalColor;
                color.a *= currentAlpha;
                sr.color = color;
            }

            if (age >= lifetime)
            {
                if (sr != null && sr.material != null)
                    Destroy(sr.material);
                Destroy(gameObject);
            }
        }
    }

    public class StunManager : MonoBehaviour
    {
        public IEnumerator StunNpc(NPC npc, float duration)
        {
            if (npc == null) yield break;

            if (npc.Navigator != null)
            {
                if (npc.Navigator.Am != null)
                {
                    npc.Navigator.Am.moveMods.Clear();
                }
                npc.Navigator.SetSpeed(0f);
                npc.Navigator.ClearDestination();
                npc.Navigator.enabled = false;
            }
            npc.enabled = false;

            var colliders = npc.GetComponentsInChildren<Collider>();
            var originalColliders = new List<bool>();
            foreach (var col in colliders) { originalColliders.Add(col.enabled); col.enabled = false; }

            var sprites = npc.GetComponentsInChildren<SpriteRenderer>();
            var originalColors = new List<Color>();
            foreach (var sr in sprites) { originalColors.Add(sr.color); sr.color = Color.yellow; }

            yield return new WaitForSeconds(duration);

            if (npc != null)
            {
                npc.enabled = true;
                if (npc.Navigator != null) { npc.Navigator.enabled = true; npc.Navigator.SetSpeed(1f); }

                for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = originalColliders[i];
                for (int i = 0; i < sprites.Length; i++) if (sprites[i] != null) sprites[i].color = originalColors[i];
            }
            Destroy(gameObject);
        }
    }
}