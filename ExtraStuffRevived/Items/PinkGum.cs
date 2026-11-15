using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_PinkGum : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            var vanillaGum = Resources.FindObjectsOfTypeAll<Gum>()
                .FirstOrDefault(x => x != null && !x.gameObject.scene.IsValid());
            if (vanillaGum == null) return false;

            var gumObj = Object.Instantiate(vanillaGum.gameObject);
            gumObj.name = "PinkGumProjectile";

            // Спавним на уровне камеры (глаз)
            gumObj.transform.position = cam.transform.position;
            gumObj.transform.rotation = cam.transform.rotation;

            var gumwad = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(x => x != null && x.name == "beans_gumwad");
            if (gumwad != null)
            {
                foreach (var sr in gumObj.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.sprite = gumwad;
            }

            var dormantGum = gumObj.GetComponent<Gum>();
            if (dormantGum != null)
            {
                dormantGum.beans = null;
                dormantGum.ec = null;
            }

            var behavior = gumObj.AddComponent<PinkGumBehavior>();
            behavior.Initialize(pm, gumObj.GetComponent<Entity>());

            Object.Destroy(gameObject);
            return true;
        }
    }

    [HarmonyPatch(typeof(Gum), nameof(Gum.EntityTriggerExit))]
    internal static class GumExitNREPatch
    {
        static bool Prefix(Gum __instance) => __instance.beans != null;
    }

    public class PinkGumBehavior : MonoBehaviour, IEntityTrigger
    {
        private PlayerManager? pm;
        private EnvironmentController? ec;
        private Entity? entity;
        private Entity? stuckEntity;
        private Transform? owner;
        private Transform? flyingSprite;
        private Transform? groundedSprite;

        private PropagatedAudioManager? audMan;
        private SoundObject? audFly;
        private SoundObject? audSplat;
        private SoundObject? audSpit;

        private bool flying = true;
        private bool leftOwner = false;

        private readonly MovementModifier playerMod = new MovementModifier(Vector3.zero, 0.25f);
        private readonly MovementModifier npcMod = new MovementModifier(Vector3.zero, 0.1f);
        private const float Speed = 30f;
        private const float LifeTime = 15f;

        private static readonly Vector3 NpcStickOffset = Vector3.up * 1.5f;

        public void Initialize(PlayerManager player, Entity ent)
        {
            pm = player;
            ec = player.ec;
            entity = ent;
            owner = player != null ? player.transform : null;

            flyingSprite = transform.Find("RendererBase/Sprite_Flying");
            groundedSprite = transform.Find("RendererBase/Sprite_Grounded");

            audMan = GetComponent<PropagatedAudioManager>();
            audFly = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "Ben_Gum_Whoosh");
            audSplat = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "Ben_Splat");
            audSpit = BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sounds/GumSpit")
                ? BasePlugin.assetManager.Get<SoundObject>("Sounds/GumSpit")
                : null;

            if (audSpit != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audSpit);
            if (audFly != null && audMan != null)
            {
                audMan.QueueAudio(audFly);
                audMan.SetLoop(true);
            }

            if (entity != null && ec != null)
            {
                entity.Initialize(ec, transform.position);
                entity.SetActive(true);
                entity.SetFrozen(false);
                entity.SetInteractionState(true);
                entity.SetHeight(5f);
                entity.SetGrounded(false);
                entity.OnEntityMoveInitialCollision += OnHit;
            }

            if (groundedSprite != null) groundedSprite.gameObject.SetActive(false);
            if (flyingSprite != null) flyingSprite.gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other) => TryHit(other);

        private void OnTriggerExit(Collider other)
        {
            if (!flying || other == null || owner == null) return;
            if (other.transform == owner || other.transform.IsChildOf(owner))
                leftOwner = true;
        }

        public void EntityTriggerEnter(Entity otherEntity, Collider other, bool validCollision)
        {
            if (validCollision) TryHit(other);
        }

        private void TryHit(Collider other)
        {
            if (!flying || other == null) return;

            if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null) HitNpc(npc);
            }
            else if (other.CompareTag("Player"))
            {
                if (!leftOwner) return;
                var player = other.GetComponent<PlayerManager>();
                if (player != null && player.plm != null) HitPlayer(player);
            }
        }

        private void HitNpc(NPC npc)
        {
            if (npc == null || npc.Navigator == null) return;
            var e = npc.Navigator.Entity;
            if (e == null) return;

            flying = false;
            stuckEntity = e;

            if (e.ExternalActivity != null)
                e.ExternalActivity.moveMods.Add(npcMod);

            SetGroundedSprite("beans_enemywad");

            if (audMan != null) { audMan.FlushQueue(true); if (audSplat != null) audMan.PlaySingle(audSplat); }

            if (pm != null) pm.RuleBreak("Bullying", 2f);

            if (entity != null) { entity.SetFrozen(true); entity.SetInteractionState(false); }

            transform.position = npc.transform.position + NpcStickOffset;

            StartCoroutine(Timer(e, npcMod));
        }

        private void HitPlayer(PlayerManager player)
        {
            var e = player.plm?.Entity;
            if (e == null || e.ExternalActivity == null) return;

            e.ExternalActivity.moveMods.Add(playerMod);
            flying = false;
            stuckEntity = e;

            SetGroundedSprite("beans_gumwad");

            if (audMan != null) { audMan.FlushQueue(true); if (audSplat != null) audMan.PlaySingle(audSplat); }

            if (entity != null) { entity.SetFrozen(true); entity.SetInteractionState(false); }

            transform.position = player.transform.position + NpcStickOffset;

            StartCoroutine(Timer(e, playerMod));
        }

        private void OnHit(RaycastHit hit)
        {
            if (!flying || hit.transform == null || hit.transform.gameObject.layer == 2) return;

            flying = false;

            if (entity != null) { entity.SetFrozen(true); entity.SetInteractionState(false); }

            if (flyingSprite != null) flyingSprite.gameObject.SetActive(false);
            if (groundedSprite != null) groundedSprite.gameObject.SetActive(false);

            if (audMan != null) { audMan.FlushQueue(true); if (audSplat != null) audMan.PlaySingle(audSplat); }

            Destroy(gameObject, 0.5f);
        }

        private void SetGroundedSprite(string spriteName)
        {
            if (flyingSprite != null) flyingSprite.gameObject.SetActive(false);
            if (groundedSprite != null)
            {
                groundedSprite.gameObject.SetActive(true);
                var sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x != null && x.name == spriteName);
                if (sprite != null)
                {
                    var sr = groundedSprite.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.sprite = sprite;
                }
            }
        }

        public void EntityTriggerStay(Entity otherEntity, Collider other, bool validCollision) { }
        public void EntityTriggerExit(Entity otherEntity, Collider other, bool validCollision) { }

        private IEnumerator Timer(Entity? target, MovementModifier? mod)
        {
            float t = LifeTime;
            while (t > 0f)
            {
                if (ec == null) yield break;
                t -= Time.deltaTime * ec.EnvironmentTimeScale;
                yield return null;
            }

            if (target != null && mod != null && target.ExternalActivity != null)
                target.ExternalActivity.moveMods.Remove(mod);

            Destroy(gameObject);
        }

        private void Update()
        {
            if (flying && entity != null && ec != null)
            {
                entity.UpdateInternalMovement(transform.forward * Speed * ec.EnvironmentTimeScale);
            }
            else if (stuckEntity != null && entity != null)
            {
                entity.UpdateInternalMovement(Vector3.zero);
                entity.Teleport(stuckEntity.transform.position + NpcStickOffset);
                entity.CopyStatusEffects(stuckEntity);
            }
        }

        private void OnDestroy()
        {
            if (entity != null)
                entity.OnEntityMoveInitialCollision -= OnHit;
        }
    }
}