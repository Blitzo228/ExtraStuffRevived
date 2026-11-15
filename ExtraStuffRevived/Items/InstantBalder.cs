#nullable disable
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using PixelInternalAPI.Classes;
using PixelInternalAPI.Extensions;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_InstantBalder : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            var prefab = Resources.FindObjectsOfTypeAll<Balder_Entity>()
                .FirstOrDefault(x => x != null && !x.gameObject.scene.IsValid());

            if (prefab == null) return false;

            GameObject clone = Object.Instantiate(prefab.gameObject);
            clone.name = "InstantBalderProjectile";
            clone.SetActive(false);
            clone.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            var balder = clone.GetComponent<Balder_Entity>();
            if (balder != null)
            {
                if (balder.Navigator != null) balder.Navigator.enabled = false;
                balder.enabled = false;
            }

            var entity = clone.GetComponent<Entity>();
            if (entity != null) entity.enabled = false;

            foreach (var col in clone.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            Vector3 spawnPos = pm.transform.position + cam.transform.forward * 2f;
            Cell cell = pm.ec.CellFromPosition(spawnPos);
            if (cell != null) spawnPos = cell.FloorWorldPosition;

            spawnPos.y += 3f;
            spawnPos.y -= 0f;
            clone.transform.position = spawnPos;
            clone.transform.forward = cam.transform.forward;
            clone.layer = LayerMask.NameToLayer("StandardEntities");

            var roller = clone.AddComponent<InstantBalderRoller>();
            roller.Initialize(balder, pm.ec, cam.transform.forward);

            clone.SetActive(true);

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class InstantBalderRoller : MonoBehaviour
    {
        private Balder_Entity _balder;
        private EnvironmentController _ec;
        private Vector3 moveDir;
        private float speed = 30f;
        private int hitCount = 0;
        private int maxHits;
        private float cooldown = 0f;
        private bool isCrumbled = false;
        private const float BounceOffset = 0.35f;
        private const float CrumbleLifetime = 3f;

        private static SoundObject _hitSound;
        private static bool _soundsLoaded;

        private PropagatedAudioManager _audMan;

        public void Initialize(Balder_Entity b, EnvironmentController environment, Vector3 dir)
        {
            _balder = b;
            _ec = environment;
            moveDir = dir;
            moveDir.y = 0;
            moveDir.Normalize();
            maxHits = Random.Range(6, 15);
            _audMan = GetComponent<PropagatedAudioManager>();
            LoadSounds();
        }

        private static void LoadSounds()
        {
            if (_soundsLoaded) return;
            _soundsLoaded = true;
            _hitSound = Resources.FindObjectsOfTypeAll<SoundObject>()
                .FirstOrDefault(x => x != null && x.name == "BalderCrush");
        }

        private void Update()
        {
            if (isCrumbled) return;

            if (_balder == null || _balder.Crumbled)
            {
                isCrumbled = true;
                StartCoroutine(DestroyAfterCrumble());
                return;
            }

            if (cooldown > 0f) cooldown -= Time.deltaTime;

            float stepDist = speed * Time.deltaTime;
            float checkDist = stepDist + 0.5f;

            if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, checkDist))
            {
                var hitTransform = hit.transform;
                if (hitTransform == null) return;

                var window = hitTransform.GetComponentInParent<Window>();
                if (window != null && !window.open)
                {
                    window.Break(true);
                }
                else
                {
                    var door = hitTransform.GetComponentInParent<Door>();
                    if (door != null)
                    {
                        if (door.locked) door.Unlock();
                        door.Open(true, true);
                    }

                    var npc = hitTransform.GetComponentInParent<NPC>();
                    if (npc != null && npc != _balder && npc.Entity != null)
                    {
                        npc.Entity.Squish(10f);
                        PlayHitSound();
                    }
                }

                Vector3 newDir = Vector3.Reflect(moveDir, hit.normal);
                newDir.y = 0;
                if (newDir.sqrMagnitude < 0.001f) newDir = -moveDir;
                newDir.Normalize();
                moveDir = newDir;

                Vector3 safePos = transform.position + moveDir * BounceOffset;
                if (_ec != null)
                {
                    var safeCell = _ec.CellFromPosition(safePos);
                    if (safeCell == null || safeCell.Null)
                        safePos = transform.position + hit.normal * BounceOffset;
                }

                transform.position = safePos;
                RegisterHit();
            }
            else
            {
                Vector3 oldPos = transform.position;
                Vector3 newPos = oldPos + moveDir * stepDist;

                if (_ec != null)
                {
                    var newCell = _ec.CellFromPosition(newPos);
                    if (newCell == null || newCell.Null)
                    {
                        Vector3 backDir = -moveDir;
                        backDir.y = 0;
                        if (backDir.sqrMagnitude < 0.001f) backDir = Vector3.back;
                        moveDir = backDir.normalized;

                        transform.position = oldPos + moveDir * BounceOffset;
                        RegisterHit();
                        goto rotate;
                    }
                }

                transform.position = newPos;
            }

        rotate:
            transform.Rotate(Vector3.back * speed * 50f * Time.deltaTime);
        }

        private void RegisterHit()
        {
            hitCount++;
            cooldown = 0.15f;

            if (hitCount >= maxHits && _balder != null && !_balder.Crumbled)
            {
                isCrumbled = true;
                _balder.Crumble(true);
                StartCoroutine(DestroyAfterCrumble());
            }
        }

        private void PlayHitSound()
        {
            if (_hitSound == null) return;

            if (_audMan != null)
            {
                _audMan.PlaySingle(_hitSound);
                return;
            }

            if (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.audMan != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(_hitSound);
        }

        private IEnumerator DestroyAfterCrumble()
        {
            yield return new WaitForSeconds(CrumbleLifetime);
            if (gameObject != null) Destroy(gameObject);
        }
    }
}