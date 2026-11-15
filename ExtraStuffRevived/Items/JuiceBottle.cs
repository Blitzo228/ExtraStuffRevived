using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_JuiceBottle : Item
    {
        public static SoundObject? splashSound;
        public static SoundObject? breakSound;

        internal Entity entity = null!;

        public void SetupPrefab()
        {
            gameObject.layer = LayerStorage.standardEntities;

            var sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/JuiceBottleproj");
            var renderer = ObjectCreationExtensions.CreateSpriteBillboard(sprite);
            renderer.transform.SetParent(transform);
            renderer.name = "JuiceBottleVisual";

            entity = gameObject.CreateEntity(0.5f, 5f, renderer.transform);
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

            var projObj = new GameObject("JuiceBottleProjectile");
            projObj.SetActive(false);

            var comp = projObj.AddComponent<ITM_JuiceBottle>();
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

            StartCoroutine(FlyRoutine(dir, ec));
        }

        private IEnumerator FlyRoutine(Vector3 dir, EnvironmentController ec)
        {
            float life = 15f;
            float speed = 30f;
            var renderer = GetComponentInChildren<SpriteRenderer>();

            while (life > 0f)
            {
                if (renderer != null && ec != null)
                {
                    float light = ec.LightLevel(transform.position);
                    renderer.color = new Color(light, light, light, 1f);
                }

                float stepDist = speed * Time.deltaTime * ec.EnvironmentTimeScale;
                Vector3 castOrigin = transform.position + Vector3.up * 1.5f;

                if (Physics.SphereCast(castOrigin, 0.5f, dir, out RaycastHit hit, stepDist + 0.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    if (hit.transform != null && !hit.transform.CompareTag("Player") && hit.transform.GetComponentInParent<PlayerManager>() == null)
                    {
                        HandleHit(hit.collider, hit.point, ec);
                        yield break;
                    }
                }

                entity.UpdateInternalMovement(dir * speed * ec.EnvironmentTimeScale);
                life -= Time.deltaTime * ec.EnvironmentTimeScale;
                yield return null;
            }

            HandleHit(null, transform.position, ec);
        }

        private void HandleHit(Collider? other, Vector3 hitPos, EnvironmentController ec)
        {
            if (other != null)
            {
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null && npc.Character == Character.Baldi)
                {
                    if (npc.GetComponent<BaldiJuiceStun>() == null)
                    {
                        var stunComp = npc.gameObject.AddComponent<BaldiJuiceStun>();
                        stunComp.StartCoroutine(stunComp.StunRoutine(npc, 7.5f));
                    }
                }
            }

            PlayBreakSound(hitPos);
            Destroy(gameObject);
        }

        private void PlayBreakSound(Vector3 position)
        {
            if (breakSound == null)
            {
                string path = BasePlugin.GetModFilePath("Audio", "GlassJuiceBreak.ogg");
                if (File.Exists(path))
                {
                    var clip = AssetLoader.AudioClipFromFile(path);
                    if (clip != null)
                    {
                        breakSound = ObjectCreators.CreateSoundObject(clip, "Glass_JuiceBreak", SoundType.Effect, Color.white, -1f);
                        if (breakSound != null) breakSound.subtitle = true;
                    }
                }
            }

            if (breakSound != null && breakSound.soundClip != null)
            {
                var audioObj = new GameObject("JuiceBreakAudio");
                audioObj.transform.position = position;
                var source = audioObj.AddComponent<AudioSource>();
                source.clip = breakSound.soundClip;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 5f;
                source.maxDistance = 30f;
                source.Play();
                Destroy(audioObj, source.clip.length);
            }
        }
    }

    public class BaldiJuiceStun : MonoBehaviour
    {
        private SpriteRenderer? spriteRenderer;
        private Sprite? originalSprite;
        private Vector3 originalScale;
        private Animator? animator;
        private Collider? baldiCollider;
        private CharacterController? baldiController;
        private float currentDuration;
        private bool isStunned = false;

        public void RefreshStun(float duration)
        {
            currentDuration = duration;
        }

        public IEnumerator StunRoutine(NPC npc, float duration)
        {
            if (npc == null) yield break;

            currentDuration = duration;

            if (!isStunned)
            {
                isStunned = true;
                Baldi? baldi = npc as Baldi;

                baldiCollider = npc.GetComponent<Collider>();
                if (baldiCollider != null) baldiCollider.enabled = false;

                baldiController = npc.GetComponent<CharacterController>();
                if (baldiController != null) baldiController.enabled = false;

                if (npc.Navigator != null)
                {
                    if (npc.Navigator.Am != null) npc.Navigator.Am.moveMods.Clear();
                    npc.Navigator.SetSpeed(0f);
                    npc.Navigator.ClearDestination();
                    npc.Navigator.enabled = false;
                }
                npc.enabled = false;

                if (npc.Entity != null && Singleton<CoreGameManager>.Instance != null)
                {
                    var player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
                    if (player != null && player.plm != null && player.plm.Entity != null)
                    {
                        npc.Entity.IgnoreEntity(player.plm.Entity, true);
                    }
                }

                animator = npc.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;

                spriteRenderer = npc.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null && BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/Baldi_JuiceStunned"))
                {
                    originalSprite = spriteRenderer.sprite;
                    originalScale = spriteRenderer.transform.localScale;

                    spriteRenderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/Baldi_JuiceStunned");
                    spriteRenderer.transform.localScale = originalScale * 1.2f;
                }

                if (ITM_JuiceBottle.splashSound == null)
                {
                    string path = BasePlugin.GetModFilePath("Audio", "BAL_JuiceSplash.wav");
                    if (File.Exists(path))
                    {
                        var clip = AssetLoader.AudioClipFromFile(path);
                        if (clip != null)
                        {
                            var voiceSound = ObjectCreators.CreateSoundObject(clip, "Baldi_JuiceSplash_Sub", SoundType.Voice, Color.green, -1f);
                            if (voiceSound != null)
                            {
                                voiceSound.subtitle = true;
                                voiceSound.soundKey = "Baldi_JuiceSplash_Sub";
                                voiceSound.additionalKeys = new SubtitleTimedKey[0];
                                ITM_JuiceBottle.splashSound = voiceSound;
                            }
                        }
                    }
                }

                if (ITM_JuiceBottle.splashSound != null)
                {
                    if (baldi != null && baldi.AudMan != null)
                        baldi.AudMan.PlaySingle(ITM_JuiceBottle.splashSound);
                    else if (Singleton<CoreGameManager>.Instance != null)
                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle(ITM_JuiceBottle.splashSound);
                }
            }

            while (currentDuration > 0f)
            {
                currentDuration -= Time.deltaTime;
                yield return null;
            }

            if (npc != null)
            {
                npc.enabled = true;
                if (baldiCollider != null) baldiCollider.enabled = true;
                if (baldiController != null) baldiController.enabled = true;
                if (animator != null) animator.enabled = true;
                if (npc.Navigator != null)
                {
                    npc.Navigator.enabled = true;
                    npc.Navigator.SetSpeed(1f);
                }

                if (npc.Entity != null && Singleton<CoreGameManager>.Instance != null)
                {
                    var player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
                    if (player != null && player.plm != null && player.plm.Entity != null)
                    {
                        npc.Entity.IgnoreEntity(player.plm.Entity, false);
                    }
                }

                if (spriteRenderer != null)
                {
                    if (originalSprite != null) spriteRenderer.sprite = originalSprite;
                    spriteRenderer.transform.localScale = originalScale;
                }

                if (npc is Baldi baldiNpc)
                {
                    baldiNpc.behaviorStateMachine.ChangeState(new Baldi_Chase(baldiNpc, baldiNpc));
                    var player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
                    if (player != null)
                    {
                        baldiNpc.behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(baldiNpc, 63, player.transform.position));
                    }
                }
            }
            Destroy(this);
        }
    }
}