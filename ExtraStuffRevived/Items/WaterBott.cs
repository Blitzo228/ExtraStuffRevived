using MTM101BaldAPI.AssetTools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_WaterBott : Item
    {
        private static SoundObject? splashSound;
        private static ITM_BSODA? bsodaPrefab;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            pm.RuleBreak("Drinking", 0.8f, 0.25f);

            if (splashSound == null)
            {
                string path = BasePlugin.GetModFilePath("Audio", "Splash.wav");
                if (File.Exists(path))
                {
                    splashSound = ScriptableObject.CreateInstance<SoundObject>();
                    splashSound.soundClip = AssetLoader.AudioClipFromFile(path);
                    splashSound.soundType = SoundType.Effect;
                    splashSound.subtitle = false;
                    splashSound.name = "ExtraStuff_WaterSplashSound";
                }
            }

            if (bsodaPrefab == null)
            {
                bsodaPrefab = Resources.FindObjectsOfTypeAll<ITM_BSODA>()
                    .FirstOrDefault(x => x != null && !x.gameObject.scene.IsValid() && x.GetType().Assembly == typeof(ITM_BSODA).Assembly);
            }

            if (bsodaPrefab != null)
            {
                var projObj = Object.Instantiate(bsodaPrefab.gameObject);
                projObj.name = "WaterBottleProjectile";

                var renderer = projObj.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null && BasePlugin.assetManager.ContainsKey("Sprites/WaterSpray"))
                {
                    renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/WaterSpray");
                    renderer.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                }

                var bsodaComp = projObj.GetComponent<ITM_BSODA>();

                var timeField = HarmonyLib.AccessTools.Field(typeof(ITM_BSODA), "time");
                if (timeField != null) timeField.SetValue(bsodaComp, 30f);

                var soundField = HarmonyLib.AccessTools.Field(typeof(ITM_BSODA), "sound");
                if (soundField != null && splashSound != null)
                    soundField.SetValue(bsodaComp, splashSound);

                var behavior = projObj.AddComponent<WaterBottleBehavior>();
                behavior.Initialize(splashSound);

                bsodaComp.Use(pm);
            }

            var pmCell = pm.ec.CellFromPosition(pm.transform.position);
            if (pmCell != null)
            {
                CreatePuddle(pmCell.FloorWorldPosition, 2.5f, 10f, splashSound);
            }

            Object.Destroy(gameObject);
            return true;
        }

        private void CreatePuddle(Vector3 position, float size, float duration, SoundObject? sound)
        {
            var puddleObj = new GameObject("WaterPuddle");
            puddleObj.transform.position = new Vector3(position.x, 0.01f, position.z);
            puddleObj.transform.localScale = new Vector3(size, size, size);
            puddleObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var renderer = puddleObj.AddComponent<SpriteRenderer>();
            if (BasePlugin.assetManager.ContainsKey("Sprites/WaterSpray"))
                renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/WaterSpray");

            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingLayerName = "NPCs";
            renderer.sortingOrder = 1;

            puddleObj.AddComponent<WaterLighting>();

            var puddleComp = puddleObj.AddComponent<WaterPuddle>();
            puddleComp.Initialize(size, duration, sound);
        }
    }

    public class WaterBottleBehavior : MonoBehaviour
    {
        private SoundObject? splashSound;
        private bool hasHit = false;

        public void Initialize(SoundObject? sound)
        {
            splashSound = sound;
        }

        private void Update()
        {
            if (hasHit) return;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1.5f))
            {
                if (hit.transform != null && (hit.transform.CompareTag("Wall") || hit.transform.name.ToLower().Contains("wall")))
                {
                    hasHit = true;
                    CreateLargePuddle(transform.position);
                    KillBsoda();
                }
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

        private void CreateLargePuddle(Vector3 hitPoint)
        {
            var pm = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (pm == null || pm.ec == null) return;

            var cell = pm.ec.CellFromPosition(hitPoint);
            if (cell == null) return;

            Vector3 floorPos = cell.FloorWorldPosition;

            var puddleObj = new GameObject("WaterPuddleLarge");
            puddleObj.transform.position = new Vector3(floorPos.x, 0.01f, floorPos.z);
            puddleObj.transform.localScale = new Vector3(4.5f, 4.5f, 4.5f);
            puddleObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var renderer = puddleObj.AddComponent<SpriteRenderer>();
            if (BasePlugin.assetManager.ContainsKey("Sprites/WaterSpray"))
                renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/WaterSpray");

            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingLayerName = "NPCs";
            renderer.sortingOrder = 1;

            puddleObj.AddComponent<WaterLighting>();

            var puddleComp = puddleObj.AddComponent<WaterPuddle>();
            puddleComp.Initialize(4.5f, 15f, splashSound);
        }
    }

    public class WaterLighting : MonoBehaviour
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

    public class WaterPuddle : MonoBehaviour
    {
        private float timer;
        private List<PlayerManager> affectedPlayers = new List<PlayerManager>();
        private List<NPC> affectedNpcs = new List<NPC>();
        private MovementModifier slowMod = new MovementModifier(Vector3.zero, 0.3f);
        private SphereCollider trigger = null!;
        private AudioSource? audioSource;

        public void Initialize(float size, float duration, SoundObject? sound)
        {
            timer = duration;

            trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = size * 0.5f;

            if (sound != null && sound.soundClip != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = sound.soundClip;
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 15f;
                audioSource.volume = 1f;
                audioSource.dopplerLevel = 0f;
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
                    player.plm.am.moveMods.Add(slowMod);

                    if (audioSource != null && audioSource.clip != null)
                        audioSource.PlayOneShot(audioSource.clip);
                }
            }
            else if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponent<NPC>();
                if (npc != null && !affectedNpcs.Contains(npc))
                {
                    affectedNpcs.Add(npc);
                    if (npc.Navigator != null) npc.Navigator.Am.moveMods.Add(slowMod);
                    if (audioSource != null && audioSource.clip != null) audioSource.PlayOneShot(audioSource.clip);
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
                    player.plm.am.moveMods.Remove(slowMod);
                }
            }
            else if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponent<NPC>();
                if (npc != null && affectedNpcs.Contains(npc))
                {
                    affectedNpcs.Remove(npc);
                    if (npc.Navigator != null) npc.Navigator.Am.moveMods.Remove(slowMod);
                }
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                foreach (var player in affectedPlayers) if (player != null) player.plm.am.moveMods.Remove(slowMod);
                foreach (var npc in affectedNpcs) if (npc != null && npc.Navigator != null) npc.Navigator.Am.moveMods.Remove(slowMod);

                affectedPlayers.Clear();
                affectedNpcs.Clear();
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (var player in affectedPlayers) if (player != null) player.plm.am.moveMods.Remove(slowMod);
            foreach (var npc in affectedNpcs) if (npc != null && npc.Navigator != null) npc.Navigator.Am.moveMods.Remove(slowMod);
        }
    }
}