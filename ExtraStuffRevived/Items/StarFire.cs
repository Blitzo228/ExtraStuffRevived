#nullable disable
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_StarFire : Item
    {
        private static SoundObject _starFireSound;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            if (_starFireSound == null)
            {
                string path = BasePlugin.GetModFilePath("Audio", "StarFire.wav");
                if (File.Exists(path))
                {
                    _starFireSound = ScriptableObject.CreateInstance<SoundObject>();
                    _starFireSound.soundClip = AssetLoader.AudioClipFromFile(path);
                    _starFireSound.soundType = SoundType.Effect;
                    _starFireSound.subtitle = false;
                    _starFireSound.name = "ExtraStuff_StarFireSound";
                }
            }

            if (_starFireSound != null && _starFireSound.soundClip != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(_starFireSound);

            GameObject projObj = new GameObject("StarFireProjectile");
            projObj.SetActive(false);

            var projComp = projObj.AddComponent<StarFireProjectile>();
            projComp.Initialize(pm, cam.transform.forward);

            projObj.SetActive(true);

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class StarFireProjectile : MonoBehaviour
    {
        private Vector3 _dir;
        private float _life = 30f;
        private bool _hasHit = false;
        private float _ghostTimer = 0f;
        private EnvironmentController _ec;
        private SpriteRenderer _sr;
        private float _speed = 35f;

        private static SoundObject _slapSound;

        public void Initialize(PlayerManager pm, Vector3 dir)
        {
            _dir = dir;
            _ec = pm.ec;

            Sprite sprite = null;
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/StarFireProjectile"))
                sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/StarFireProjectile");

            _sr = ObjectCreationExtensions.CreateSpriteBillboard(sprite);
            _sr.transform.SetParent(transform);
            _sr.name = "StarFireVisual";
            _sr.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            gameObject.layer = LayerStorage.standardEntities;

            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.8f;

            transform.position = pm.transform.position + Vector3.up * 1.8f;

            if (_slapSound == null)
            {
                _slapSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name.ToLowerInvariant().Contains("slap"));
                if (_slapSound != null)
                {
                    _slapSound = Object.Instantiate(_slapSound);
                    _slapSound.name = "ExtraStuff_Slap";
                }
            }
        }

        private void Update()
        {
            if (_hasHit || _ec == null) return;

            _life -= Time.deltaTime * _ec.EnvironmentTimeScale;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 oldPos = transform.position;
            Vector3 newPos = oldPos + _dir * _speed * Time.deltaTime * _ec.EnvironmentTimeScale;

            if (Physics.SphereCast(oldPos, 0.8f, _dir, out RaycastHit hit, Vector3.Distance(oldPos, newPos) + 0.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var hitTransform = hit.transform;
                if (hitTransform != null && hitTransform.gameObject.layer != 2)
                {
                    var npc = hitTransform.GetComponentInParent<NPC>();
                    if (npc != null)
                    {
                        OnHitNpc(npc);
                        return;
                    }

                    var window = hitTransform.GetComponentInParent<Window>();
                    if (window != null && !window.open)
                    {
                        window.Break(true);
                    }

                    OnWallHit();
                    return;
                }
            }

            transform.position = newPos;

            if (_sr != null)
            {
                float light = _ec.LightLevel(transform.position);
                _sr.color = new Color(light, light, light, 1f);
            }

            _ghostTimer += Time.deltaTime;
            if (_ghostTimer >= 0.05f)
            {
                _ghostTimer = 0f;
                SpawnGhost();
            }
        }

        private void OnHitNpc(NPC npc)
        {
            if (_hasHit) return;
            _hasHit = true;

            if (_slapSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(_slapSound);

            var stunManager = new GameObject("StunManager").AddComponent<StunManager>();
            stunManager.StartCoroutine(stunManager.StunNpc(npc, 30f));

            Destroy(gameObject);
        }

        private void SpawnGhost()
        {
            if (_sr == null || _sr.sprite == null) return;

            GameObject ghostObj = new GameObject("StarFire_Ghost");
            ghostObj.transform.position = _sr.transform.position;
            ghostObj.transform.rotation = _sr.transform.rotation;
            ghostObj.transform.localScale = _sr.transform.lossyScale;

            SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
            ghostSr.sprite = _sr.sprite;
            ghostSr.flipX = _sr.flipX;
            ghostSr.flipY = _sr.flipY;
            ghostSr.sortingLayerID = _sr.sortingLayerID;
            ghostSr.sortingOrder = _sr.sortingOrder;

            if (_sr.sharedMaterial != null)
                ghostSr.material = new Material(_sr.sharedMaterial);

            Color startColor = _sr.color;
            startColor.a *= 0.45f;
            ghostSr.color = startColor;

            var anim = ghostObj.AddComponent<AfterimageFader>();
            anim.Initialize(ghostSr, 0.3f, startColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit || other == null) return;

            if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null) OnHitNpc(npc);
            }
            else if (other.GetComponentInParent<Gum>() != null)
            {
                var gum = other.GetComponentInParent<Gum>();
                if (gum != null && gum.beans != null)
                {
                    _hasHit = true;
                    gum.beans.GumHit(gum, false);
                    gum.Hide();
                    Destroy(gameObject);
                }
            }
            else if (other.GetComponentInParent<FirstPrize>() != null)
            {
                _hasHit = true;
                var fp = other.GetComponentInParent<FirstPrize>();
                if (fp != null) fp.CutWires();
                Destroy(gameObject);
            }
            else if (other.GetComponentInParent<Playtime>() != null)
            {
                _hasHit = true;
                var playtime = other.GetComponentInParent<Playtime>();
                if (playtime != null)
                {
                    var jumprope = Object.FindObjectsOfType<Jumprope>().FirstOrDefault(j => j.playtime == playtime);
                    if (jumprope != null) jumprope.Destroy();
                    playtime.BecomeSad();
                    playtime.behaviorStateMachine?.ChangeState(new Playtime_Cooldown(playtime, playtime, playtime.InitialCooldown));
                }
                Destroy(gameObject);
            }
        }

        private void OnWallHit()
        {
            if (_hasHit) return;
            _hasHit = true;
            Destroy(gameObject);
        }
    }

    public class AfterimageFader : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _lifetime;
        private float _age;
        private Color _originalColor;

        public void Initialize(SpriteRenderer renderer, float life, Color col)
        {
            _sr = renderer;
            _lifetime = life;
            _originalColor = col;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(_age / _lifetime));

            if (_sr != null)
            {
                Color color = _originalColor;
                color.a *= currentAlpha;
                _sr.color = color;
            }

            if (_age >= _lifetime)
            {
                if (_sr != null && _sr.material != null)
                    Destroy(_sr.material);
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
                if (npc.Navigator.Am != null) npc.Navigator.Am.moveMods.Clear();
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