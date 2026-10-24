using MTM101BaldAPI.AssetTools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_Lighter : Item
    {
        private static SoundObject? lighterSound;
        private static SoundObject? fireSound;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            AppleTree? targetTree = null;
            NPC? targetNpc = null;
            GameObject? targetObj = null;

            if (Physics.Raycast(pm.transform.position, cam.transform.forward, out RaycastHit hit, pm.pc.reach + 3f))
            {
                if (hit.transform != null)
                {
                    targetTree = hit.transform.GetComponentInParent<AppleTree>();
                    targetNpc = hit.transform.GetComponentInParent<NPC>();

                    if (hit.transform.name.ToLowerInvariant().Contains("tree")) targetObj = hit.transform.gameObject;
                    else if (hit.transform.parent != null && hit.transform.parent.name.ToLowerInvariant().Contains("tree")) targetObj = hit.transform.parent.gameObject;
                    else if (targetTree != null) targetObj = targetTree.gameObject;
                    else targetObj = hit.transform.gameObject;
                }
            }

            if (targetTree == null && targetNpc == null && (targetObj == null || !targetObj.name.ToLowerInvariant().Contains("tree"))) return false;

            if (pm.ec != null)
            {
                var flood = pm.ec.GetComponentInChildren<FloodEvent>();
                if (flood != null && flood.gameObject.activeInHierarchy)
                {
                    var buzzSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x.name == "Buzzer" || x.name == "Error");
                    if (buzzSound != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(buzzSound);
                    return false;
                }
            }

            if (lighterSound == null)
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Modded", ModInfo.ID, "Audio", "Lighter.wav");
                if (File.Exists(path))
                {
                    lighterSound = ScriptableObject.CreateInstance<SoundObject>();
                    lighterSound.soundClip = AssetLoader.AudioClipFromFile(path);
                    lighterSound.soundType = SoundType.Effect;
                    lighterSound.subtitle = false;
                }
            }

            if (fireSound == null)
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Modded", ModInfo.ID, "Audio", "Fire.wav");
                if (File.Exists(path))
                {
                    fireSound = ScriptableObject.CreateInstance<SoundObject>();
                    fireSound.soundClip = AssetLoader.AudioClipFromFile(path);
                    fireSound.soundType = SoundType.Effect;
                    fireSound.subtitle = false;
                }
            }

            if (lighterSound != null && lighterSound.soundClip != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(lighterSound);

            pm.RuleBreak("Bullying", 2f, 0.5f);

            var baldi = pm.ec.GetBaldi();
            if (baldi != null && baldi.Navigator != null && baldi.Navigator.Am != null)
            {
                var speedMod = new MovementModifier(Vector3.zero, 0.2f);
                baldi.Navigator.Am.moveMods.Add(speedMod);
                pm.StartCoroutine(RemoveBaldiSpeedMod(baldi, speedMod, 5f));
            }

            if (targetTree != null)
            {
                var appleField = HarmonyLib.AccessTools.Field(typeof(AppleTree), "apple");
                var droppedField = HarmonyLib.AccessTools.Field(typeof(AppleTree), "dropped");
                if (appleField != null && droppedField != null)
                {
                    var apple = appleField.GetValue(targetTree) as Pickup;
                    bool isDropped = (bool)droppedField.GetValue(targetTree);
                    if (!isDropped && apple != null)
                    {
                        apple.transform.SetParent(null, true);
                        apple.transform.position -= Vector3.up * (apple.transform.position.y - 5f);
                        droppedField.SetValue(targetTree, true);
                    }
                }
            }

            if (targetObj != null && (targetTree != null || targetObj.name.ToLowerInvariant().Contains("tree")))
            {
                var burnComp = targetObj.GetComponent<TreeBurn>();
                if (burnComp == null)
                {
                    burnComp = targetObj.AddComponent<TreeBurn>();
                    burnComp.StartBurning(30f, fireSound);
                }
                else burnComp.SpeedUpBurn(5f);
            }

            if (targetNpc != null)
            {
                var npcBurn = targetNpc.gameObject.GetComponent<NpcBurn>();
                if (npcBurn == null)
                {
                    npcBurn = targetNpc.gameObject.AddComponent<NpcBurn>();
                    npcBurn.StartBurning(15f);
                }
            }

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator RemoveBaldiSpeedMod(Baldi baldi, MovementModifier mod, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (baldi != null && baldi.Navigator != null && baldi.Navigator.Am != null)
            {
                baldi.Navigator.Am.moveMods.Remove(mod);
            }
        }
    }

    public class NpcBurn : MonoBehaviour
    {
        private float timer;
        private bool isBurning;
        private float particleTimer;
        private NPC? npc;
        private List<GameObject> activeParticles = new List<GameObject>();
        private Vector3 targetDestination;

        public void StartBurning(float duration)
        {
            if (isBurning) return;
            isBurning = true;
            timer = duration;
            npc = GetComponent<NPC>();
            if (npc != null && npc.Navigator != null)
            {
                npc.Navigator.SetSpeed(40f);
                npc.Navigator.ClearDestination();
                npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
            }
        }

        private void Update()
        {
            if (!isBurning || npc == null) return;

            if (npc.Navigator != null)
            {
                npc.Navigator.SetSpeed(40f);
                if (Vector3.Distance(npc.transform.position, targetDestination) < 2f)
                {
                    npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
                    targetDestination = npc.transform.position;
                }
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isBurning = false;
                foreach (var p in activeParticles) if (p != null) Destroy(p);
                if (npc != null) npc.Despawn();
                return;
            }

            particleTimer -= Time.deltaTime;
            if (particleTimer <= 0f)
            {
                SpawnParticle();
                SpawnParticle();
                particleTimer = 0.05f;
            }
        }

        private void SpawnParticle()
        {
            var particleObj = new GameObject("FireParticle");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 3f), Random.Range(-1f, 1f));

            var renderer = particleObj.AddComponent<SpriteRenderer>();
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/FireParticles")) renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/FireParticles");

            var billboardMaterial = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "SpriteStandard_Billboard_NoGlitch" || m.name == "SpriteStandard_Billboard");
            if (billboardMaterial != null) renderer.material = billboardMaterial;

            renderer.sortingLayerName = "NPCs";
            renderer.sortingOrder = 5;
            particleObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            var anim = particleObj.AddComponent<FireParticle>();
            anim.Initialize(renderer, Random.Range(3f, 5f));
            activeParticles.Add(particleObj);
        }
    }

    public class TreeBurn : MonoBehaviour
    {
        private float timer;
        private float totalDuration;
        private bool isBurning;
        private GameObject? audioObj;
        private float particleTimer;
        private List<GameObject> activeParticles = new List<GameObject>();

        public void StartBurning(float duration, SoundObject? fireSound)
        {
            if (isBurning) return;
            isBurning = true;
            timer = duration;
            totalDuration = duration;

            if (fireSound == null) fireSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "Fire");

            if (fireSound != null && fireSound.soundClip != null)
            {
                audioObj = new GameObject("FireAudio");
                audioObj.transform.SetParent(transform);
                audioObj.transform.localPosition = Vector3.up * 2f;
                audioObj.SetActive(true);

                var source = audioObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.clip = fireSound.soundClip;
                source.volume = 0.8f;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = 40f;
                source.Play();
            }
        }

        public void Extinguish()
        {
            if (!isBurning) return;
            isBurning = false;
            if (audioObj != null) Destroy(audioObj);
            foreach (var p in activeParticles) if (p != null) Destroy(p);
            activeParticles.Clear();
            Destroy(this);
        }

        public void SpeedUpBurn(float amount)
        {
            timer -= amount;
            if (timer <= 0f) timer = 0.1f;
        }

        private void Update()
        {
            if (!isBurning) return;

            var bsodas = Object.FindObjectsOfType<ITM_BSODA>();
            if (bsodas != null)
            {
                foreach (var bsoda in bsodas)
                {
                    if (bsoda != null && Vector3.Distance(transform.position, bsoda.transform.position) < 4f)
                    {
                        Extinguish();
                        return;
                    }
                }
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isBurning = false;
                if (audioObj != null) Destroy(audioObj);
                foreach (var p in activeParticles) if (p != null) Destroy(p);

                if (gameObject.name.ToLowerInvariant().Contains("tree")) Destroy(gameObject);
                else Destroy(this);
                return;
            }

            particleTimer -= Time.deltaTime;
            if (particleTimer <= 0f)
            {
                float burnProgress = 1f - (timer / totalDuration);
                int particleCount = (int)Mathf.Lerp(3f, 8f, burnProgress);
                for (int i = 0; i < particleCount; i++) SpawnParticle();
                particleTimer = Mathf.Lerp(0.08f, 0.02f, burnProgress);
            }
        }

        private void SpawnParticle()
        {
            var particleObj = new GameObject("FireParticle");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0f, 5f), Random.Range(-1.5f, 1.5f));

            var renderer = particleObj.AddComponent<SpriteRenderer>();
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/FireParticles")) renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/FireParticles");

            var billboardMaterial = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "SpriteStandard_Billboard_NoGlitch" || m.name == "SpriteStandard_Billboard");
            if (billboardMaterial != null) renderer.material = billboardMaterial;

            renderer.sortingLayerName = "NPCs";
            renderer.sortingOrder = 5;
            particleObj.transform.localScale = new Vector3(1f, 1f, 1f);

            var anim = particleObj.AddComponent<FireParticle>();
            anim.Initialize(renderer, Random.Range(3f, 5f));
            activeParticles.Add(particleObj);
        }
    }

    public class FireParticle : MonoBehaviour
    {
        private SpriteRenderer? sr;
        private float speed;
        private float lifeTime;
        private Vector3 velocity;

        public void Initialize(SpriteRenderer renderer, float riseSpeed)
        {
            sr = renderer;
            speed = riseSpeed;
            lifeTime = 2.5f;
            velocity = new Vector3(Random.Range(-0.5f, 0.5f), speed, Random.Range(-0.5f, 0.5f));
        }

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f) { Destroy(gameObject); return; }

            if (sr != null)
            {
                float t = lifeTime / 2.5f;
                sr.color = new Color(1f, t, 0f, t);
            }

            if (Camera.main != null)
            {
                Vector3 camDir = Camera.main.transform.position - transform.position;
                camDir.y = 0;
                if (camDir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(-camDir);
            }
        }
    }
}