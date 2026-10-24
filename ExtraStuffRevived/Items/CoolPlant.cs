using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_CoolPlant : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            Vector3 spawnPos = pm.transform.position + pm.transform.forward * 2f;
            var cell = pm.ec.CellFromPosition(spawnPos);
            if (cell != null) spawnPos = cell.FloorWorldPosition;

            GameObject plantObj;
            var plantPrefab = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(x => x != null && x.name.ToLowerInvariant() == "plant");

            if (plantPrefab != null)
            {
                plantObj = Object.Instantiate(plantPrefab);
                foreach (var c in plantObj.GetComponentsInChildren<Collider>())
                {
                    Object.Destroy(c);
                }
                plantObj.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                plantObj = new GameObject("CoolPlant");
                var renderer = plantObj.AddComponent<SpriteRenderer>();
                if (BasePlugin.assetManager.ContainsKey("Sprites/CoolPlantIcon_Large"))
                    renderer.sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/CoolPlantIcon_Large");

                var mat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "SpriteStandard_Billboard_NoGlitch" || m.name == "SpriteStandard_Billboard");
                if (mat != null) renderer.material = mat;

                renderer.sortingLayerName = "NPCs";
                renderer.sortingOrder = 1;
                plantObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }

            plantObj.layer = LayerMask.NameToLayer("StandardEntities");
            plantObj.transform.position = spawnPos;

            var trigger = plantObj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2f;

            var behavior = plantObj.AddComponent<CoolPlantObject>();
            behavior.Initialize(pm);

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class CoolPlantObject : MonoBehaviour
    {
        private float timer = 30f;
        private PlayerManager? pm;
        private bool isHiding = false;
        private float pickupDelay = 1f;

        public void Initialize(PlayerManager player)
        {
            pm = player;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            pickupDelay -= Time.deltaTime;

            if (timer <= 0f)
            {
                if (isHiding && pm != null && pm.plm != null && pm.plm.Entity != null)
                {
                    pm.plm.Entity.SetHidden(false);
                    pm.plm.Entity.SetVisible(true);
                }
                Destroy(gameObject);
                return;
            }

            if (pm != null && pm.plm != null && pm.plm.Entity != null)
            {
                float dist = Vector3.Distance(transform.position, pm.transform.position);
                if (dist <= 10f && !isHiding)
                {
                    isHiding = true;
                    pm.plm.Entity.SetHidden(true);
                    pm.plm.Entity.SetVisible(false);
                }
                else if (dist > 10f && isHiding)
                {
                    isHiding = false;
                    pm.plm.Entity.SetHidden(false);
                    pm.plm.Entity.SetVisible(true);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (pickupDelay > 0f) return;

            if (other.CompareTag("Player"))
            {
                var p = other.GetComponent<PlayerManager>();
                if (p != null)
                {
                    p.itm.AddItem(BasePlugin.ModItems["itm_coolplant"]);
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (isHiding && pm != null && pm.plm != null && pm.plm.Entity != null)
            {
                pm.plm.Entity.SetHidden(false);
                pm.plm.Entity.SetVisible(true);
            }
        }
    }
}