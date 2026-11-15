using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_OldCandy : Item
    {
        internal Entity entity = null!;

        public void SetupPrefab()
        {
            gameObject.layer = LayerStorage.standardEntities;

            var sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/OldCandyIcon_Large");
            var renderer = ObjectCreationExtensions.CreateSpriteBillboard(sprite);
            renderer.transform.SetParent(transform);
            renderer.name = "OldCandyVisual";

            entity = gameObject.CreateEntity(0.5f, 5f, renderer.transform);
            entity.SetGrounded(false);
        }

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var tossSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CampfireToss");
            if (tossSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(tossSound);

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            var projObj = new GameObject("OldCandyProjectile");
            projObj.SetActive(false);

            var comp = projObj.AddComponent<ITM_OldCandy>();
            comp.SetupPrefab();

            projObj.SetActive(true);
            comp.Initialize(pm.transform.position, cam.transform.forward, pm.ec);

            Object.Destroy(gameObject);
            return true;
        }

        public void Initialize(Vector3 pos, Vector3 dir, EnvironmentController ec)
        {
            entity.Initialize(ec, pos);
            entity.SetActive(true);
            entity.OnEntityMoveInitialCollision += (hit) => Splat(hit.point, ec);

            StartCoroutine(FlyRoutine(dir, ec));
        }

        private IEnumerator FlyRoutine(Vector3 dir, EnvironmentController ec)
        {
            float life = 5f;
            float speed = 35f;
            float height = 1.5f;
            float verticalSpeed = 15f;
            float gravity = -25f;
            var renderer = GetComponentInChildren<SpriteRenderer>();

            while (life > 0f)
            {
                if (renderer != null && ec != null)
                {
                    float light = ec.LightLevel(transform.position);
                    renderer.color = new Color(light, light, light, 1f);
                }

                verticalSpeed += gravity * Time.deltaTime * ec.EnvironmentTimeScale;
                height += verticalSpeed * Time.deltaTime * ec.EnvironmentTimeScale;

                if (height <= 0.1f)
                {
                    Splat(transform.position, ec);
                    yield break;
                }

                entity.SetHeight(height);
                entity.UpdateInternalMovement(dir * speed * ec.EnvironmentTimeScale);
                transform.Rotate(Vector3.forward * 720f * Time.deltaTime);

                life -= Time.deltaTime * ec.EnvironmentTimeScale;
                yield return null;
            }

            Splat(transform.position, ec);
        }

        private void Splat(Vector3 pos, EnvironmentController ec)
        {
            var splatSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "Ben_Splat");
            if (splatSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(splatSound);

            Vector3 floorPos = pos;
            if (ec != null)
            {
                var cell = ec.CellFromPosition(pos);
                if (cell != null && !cell.Null)
                    floorPos = cell.FloorWorldPosition;
            }

            GameObject puddleObj = new GameObject("OldCandyPuddle");
            puddleObj.transform.position = new Vector3(floorPos.x, 0.01f, floorPos.z);
            puddleObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            puddleObj.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);

            var puddleRenderer = puddleObj.AddComponent<SpriteRenderer>();
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/OldCandySplash"))
            {
                puddleRenderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/OldCandySplash");
            }

            puddleRenderer.material = new Material(Shader.Find("Sprites/Default"));
            puddleRenderer.sortingLayerName = "NPCs";
            puddleRenderer.sortingOrder = 1;

            puddleObj.AddComponent<OldCandyLighting>();

            var trigger = puddleObj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.5f;

            var puddleComp = puddleObj.AddComponent<OldCandyPuddle>();
            puddleComp.Initialize(20f);

            Destroy(gameObject);
        }
    }

    public class OldCandyLighting : MonoBehaviour
    {
        private SpriteRenderer? sr;
        private EnvironmentController? ec;

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            if (BaseGameManager.Instance != null) ec = BaseGameManager.Instance.Ec;
        }

        private void Update()
        {
            if (sr != null && ec != null)
            {
                float light = ec.LightLevel(transform.position);
                sr.color = new Color(light, light, light, 1f);
            }
        }
    }

    public class OldCandyPuddle : MonoBehaviour
    {
        private float timer;
        private readonly MovementModifier slowMod = new MovementModifier(Vector3.zero, 0.15f);
        private readonly List<PlayerManager> affectedPlayers = new List<PlayerManager>();
        private readonly List<NPC> affectedNpcs = new List<NPC>();
        private AudioSource? audioSource;

        public void Initialize(float duration)
        {
            timer = duration;

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sounds/splash"))
            {
                var soundObj = BasePlugin.assetManager.Get<SoundObject>("Sounds/splash");
                if (soundObj != null && soundObj.soundClip != null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.clip = soundObj.soundClip;
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                    audioSource.minDistance = 2f;
                    audioSource.maxDistance = 20f;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var player = other.GetComponent<PlayerManager>();
                if (player != null && !affectedPlayers.Contains(player))
                {
                    affectedPlayers.Add(player);
                    player.plm?.am?.moveMods.Add(slowMod);
                    PlaySplash();
                }
            }
            else if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null && !affectedNpcs.Contains(npc) && npc.Navigator != null)
                {
                    affectedNpcs.Add(npc);
                    npc.Navigator.Am?.moveMods.Add(slowMod);
                    PlaySplash();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var player = other.GetComponent<PlayerManager>();
                if (player != null && affectedPlayers.Contains(player))
                {
                    affectedPlayers.Remove(player);
                    player.plm?.am?.moveMods.Remove(slowMod);
                }
            }
            else if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null && affectedNpcs.Contains(npc))
                {
                    affectedNpcs.Remove(npc);
                    npc.Navigator?.Am?.moveMods.Remove(slowMod);
                }
            }
        }

        private void PlaySplash()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(audioSource.clip);
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                foreach (var p in affectedPlayers)
                    p?.plm?.am?.moveMods.Remove(slowMod);

                foreach (var n in affectedNpcs)
                    n?.Navigator?.Am?.moveMods.Remove(slowMod);

                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (var p in affectedPlayers)
                p?.plm?.am?.moveMods.Remove(slowMod);

            foreach (var n in affectedNpcs)
                n?.Navigator?.Am?.moveMods.Remove(slowMod);
        }
    }
}