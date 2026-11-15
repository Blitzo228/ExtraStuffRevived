using System.Collections;
using System.Linq;
using UnityEngine;
using MTM101BaldAPI;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;

namespace ExtraStuffRevived
{
    public class ITM_SafetyShuriken : Item, IEntityTrigger
    {
        internal Entity entity = null!;
        private SpriteRenderer? visualRenderer;
        private bool hasHit = false;
        private float currentRotation = 0f;
        private MaterialPropertyBlock? propertyBlock;

        public void SetupPrefab()
        {
            gameObject.layer = LayerStorage.standardEntities;

            var sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/SafetyShurikenIcon_Large");
            visualRenderer = ObjectCreationExtensions.CreateSpriteBillboard(sprite);
            visualRenderer.transform.SetParent(transform);
            visualRenderer.name = "ShurikenVisual";
            propertyBlock = new MaterialPropertyBlock();

            entity = gameObject.CreateEntity(0.5f, 5f, visualRenderer.transform);
            entity.SetGrounded(false);
            entity.SetHeight(4.5f);
        }

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var tossSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CampfireToss");
            if (tossSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(tossSound);

            var projObj = new GameObject("SafetyShurikenProjectile");
            projObj.SetActive(false);

            var comp = projObj.AddComponent<ITM_SafetyShuriken>();
            comp.SetupPrefab();

            projObj.SetActive(true);

            var cam = Singleton<CoreGameManager>.Instance?.GetCamera(pm.playerNumber);
            Vector3 dir = cam != null ? cam.transform.forward : pm.transform.forward;

            comp.Initialize(pm.transform.position, dir, pm.ec);

            Object.Destroy(gameObject);
            return true;
        }

        public void Initialize(Vector3 pos, Vector3 dir, EnvironmentController ec)
        {
            entity.Initialize(ec, pos);
            entity.SetActive(true);
            entity.OnEntityMoveInitialCollision += (hit) => OnHit(hit.transform);
            StartCoroutine(FlyRoutine(dir, ec));
        }

        private IEnumerator FlyRoutine(Vector3 dir, EnvironmentController ec)
        {
            float life = 10f;
            float speed = 45f;

            while (life > 0f && !hasHit)
            {
                if (visualRenderer != null && ec != null)
                {
                    float light = ec.LightLevel(transform.position);
                    visualRenderer.color = new Color(light, light, light, 1f);

                    if (propertyBlock != null)
                    {
                        visualRenderer.GetPropertyBlock(propertyBlock);
                        currentRotation = (currentRotation + 600f * Time.deltaTime * ec.EnvironmentTimeScale) % 360f;
                        propertyBlock.SetFloat("_SpriteRotation", currentRotation);
                        visualRenderer.SetPropertyBlock(propertyBlock);
                    }
                }

                entity.UpdateInternalMovement(dir * speed * ec.EnvironmentTimeScale);
                entity.SetHeight(4.5f);

                life -= Time.deltaTime * ec.EnvironmentTimeScale;
                yield return null;
            }

            if (!hasHit) Destroy(gameObject);
        }

        private void OnHit(Transform hitTransform)
        {
            if (hasHit || hitTransform == null) return;

            bool hitSomething = false;

            var window = hitTransform.GetComponentInParent<Window>();
            if (window != null && !window.open)
            {
                window.Break(true);
                hitSomething = true;
            }

            var playtime = hitTransform.GetComponentInParent<Playtime>();
            if (playtime != null)
            {
                InterruptPlaytime(playtime);
                hitSomething = true;
            }

            var firstPrize = hitTransform.GetComponentInParent<FirstPrize>();
            if (firstPrize != null)
            {
                firstPrize.CutWires();
                hitSomething = true;
            }

            var gum = hitTransform.GetComponentInParent<Gum>();
            if (gum != null)
            {
                DestroyGum(gum);
                hitSomething = true;
            }

            if (hitSomething || hitTransform.gameObject.layer != 2)
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }

        public void EntityTriggerEnter(Entity otherEntity, Collider other, bool validCollision)
        {
            if (hasHit || !validCollision || other == null) return;

            var window = other.GetComponentInParent<Window>();
            if (window != null && !window.open)
            {
                window.Break(true);
                hasHit = true;
                Destroy(gameObject);
                return;
            }

            var gum = other.GetComponentInParent<Gum>();
            if (gum != null)
            {
                DestroyGum(gum);
                hasHit = true;
                Destroy(gameObject);
                return;
            }

            var firstPrize = other.GetComponentInParent<FirstPrize>();
            if (firstPrize != null)
            {
                firstPrize.CutWires();
                hasHit = true;
                Destroy(gameObject);
                return;
            }

            var playtime = other.GetComponentInParent<Playtime>();
            if (playtime != null)
            {
                InterruptPlaytime(playtime);
                hasHit = true;
                Destroy(gameObject);
                return;
            }
        }

        private void InterruptPlaytime(Playtime playtime)
        {
            var jumprope = Object.FindObjectsOfType<Jumprope>().FirstOrDefault(j => j.playtime == playtime);

            if (jumprope != null)
            {
                jumprope.Destroy();
            }

            playtime.BecomeSad();
            playtime.behaviorStateMachine?.ChangeState(new Playtime_Cooldown(playtime, playtime, playtime.InitialCooldown));
            playtime.Navigator?.SetSpeed(12f);
        }

        private void DestroyGum(Gum gum)
        {
            var splatSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "Ben_Splat");
            if (splatSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(splatSound);

            if (gum.beans != null)
                gum.beans.GumHit(gum, false);

            gum.Hide();
        }

        public void EntityTriggerStay(Entity otherEntity, Collider other, bool validCollision) { }
        public void EntityTriggerExit(Entity otherEntity, Collider other, bool validCollision) { }
    }
}