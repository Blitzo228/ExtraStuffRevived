using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;

namespace ExtraStuffRevived
{
    public class ITM_SpecialTeapot : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            Vector3 spawnPos = pm.transform.position;
            var cell = pm.ec.CellFromPosition(spawnPos);
            if (cell != null) spawnPos = cell.FloorWorldPosition;

            GameObject puddleObj = new GameObject("SpecialTeapotPuddle");
            puddleObj.transform.position = new Vector3(spawnPos.x, 0.01f, spawnPos.z);
            puddleObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            puddleObj.transform.localScale = new Vector3(4f, 4f, 4f);

            var renderer = puddleObj.AddComponent<SpriteRenderer>();
            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/PSpecialTeapotSpray_Sprite_Transparent"))
            {
                renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/PSpecialTeapotSpray_Sprite_Transparent");
            }

            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingLayerName = "NPCs";
            renderer.sortingOrder = 1;
            renderer.color = new Color(0.5f, 0.8f, 1f, 0.8f);

            puddleObj.AddComponent<WaterLighting>();

            var trigger = puddleObj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2f;

            var puddleComp = puddleObj.AddComponent<SpecialTeapotPuddle>();
            puddleComp.Initialize(15f);

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class SpecialTeapotPuddle : MonoBehaviour
    {
        private float timer;
        private readonly HashSet<Entity> pushedEntities = new HashSet<Entity>();

        public void Initialize(float duration)
        {
            timer = duration;
        }

        private Entity? GetEntity(Collider other)
        {
            if (other == null) return null;

            if (other.CompareTag("Player"))
            {
                var pm = other.GetComponent<PlayerManager>();
                if (pm != null) return pm.plm?.Entity;
            }
            else if (other.CompareTag("NPC"))
            {
                var npc = other.GetComponentInParent<NPC>();
                if (npc != null) return npc.Navigator?.Entity;
            }
            return null;
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity? entity = GetEntity(other);

            if (entity != null && !pushedEntities.Contains(entity))
            {
                pushedEntities.Add(entity);

                Vector3 dir = (entity.transform.position - transform.position).normalized;
                dir.y = 0;

                if (dir.sqrMagnitude < 0.1f)
                {
                    var pm = entity.GetComponent<PlayerManager>();
                    if (pm != null)
                        dir = pm.transform.forward;
                    else
                        dir = entity.transform.forward;
                }
                dir.y = 0;
                dir.Normalize();

                entity.AddForce(new Force(dir, 60f, -15f));

                if (entity.ExternalActivity != null)
                {
                    var mod = new MovementModifier(Vector3.zero, 0.1f);
                    entity.ExternalActivity.moveMods.Add(mod);
                    StartCoroutine(RemoveSlowMod(entity, mod, 1.5f));
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Entity? entity = GetEntity(other);
            if (entity != null)
            {
                pushedEntities.Remove(entity);
            }
        }

        private IEnumerator RemoveSlowMod(Entity entity, MovementModifier mod, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (entity != null && entity.ExternalActivity != null)
                entity.ExternalActivity.moveMods.Remove(mod);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
