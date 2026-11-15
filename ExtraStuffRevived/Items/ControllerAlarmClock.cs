using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_ControllerAlarmClock : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var vanillaPrefab = Resources.FindObjectsOfTypeAll<ITM_AlarmClock>()
                .FirstOrDefault(x => x != null && !x.gameObject.scene.IsValid());

            if (vanillaPrefab == null) return false;

            GameObject clone = Object.Instantiate(vanillaPrefab.gameObject);
            clone.SetActive(false);
            clone.name = "ControllerAlarmClockWorld";

            clone.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

            var vanillaScript = clone.GetComponent<ITM_AlarmClock>();
            if (vanillaScript != null) Object.DestroyImmediate(vanillaScript);

            var customScript = clone.AddComponent<ControllerAlarmClockObject>();
            customScript.entity = clone.GetComponent<Entity>();
            customScript.audMan = clone.GetComponent<PropagatedAudioManager>();
            customScript.spriteRenderer = clone.GetComponentInChildren<SpriteRenderer>();

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/ControllerAlarmClockDropUnpress"))
            {
                var sprite = BasePlugin.assetManager.Get<Sprite>("Sprites/ControllerAlarmClockDropUnpress");
                if (sprite != null && customScript.spriteRenderer != null)
                {
                    customScript.spriteRenderer.sprite = sprite;
                }
            }

            Cell cell = pm.ec.CellFromPosition(pm.transform.position);
            float floorY = cell != null ? cell.FloorWorldPosition.y : 0f;


            Vector3 spawnPos = new Vector3(pm.transform.position.x, floorY + 1.5f, pm.transform.position.z);
            clone.transform.position = spawnPos;

            if (customScript.entity != null)
            {
                customScript.entity.Initialize(pm.ec, spawnPos);

                customScript.entity.SetHeight(5.5f);
                customScript.entity.SetGrounded(true); 

                customScript.entity.CopyStatusEffects(pm.plm.Entity);
            }

            clone.SetActive(true);

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class ControllerAlarmClockObject : MonoBehaviour, IClickable<int>
    {
        public Entity? entity;
        public PropagatedAudioManager? audMan;
        public SpriteRenderer? spriteRenderer;

        private SoundObject? audRing;
        private SoundObject? audTick;
        private Sprite? spritePressed;
        private bool finished;

        public void Start()
        {
            audRing = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "AlarmClockBell");
            audTick = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "AlarmClockTick");

            if (BasePlugin.assetManager != null && BasePlugin.assetManager.ContainsKey("Sprites/ControllerAlarmClockDropPressed"))
            {
                spritePressed = BasePlugin.assetManager.Get<Sprite>("Sprites/ControllerAlarmClockDropPressed");
            }

            if (audTick != null && audMan != null)
            {
                audMan.QueueAudio(audTick);
                audMan.SetLoop(true);
            }
        }

        public void Clicked(int playerNumber)
        {
            if (finished) return;
            finished = true;

            if (spriteRenderer != null && spritePressed != null)
                spriteRenderer.sprite = spritePressed;

            if (audMan != null)
                audMan.FlushQueue(true);

            float ringLength = 2f;
            if (audRing != null && audMan != null)
            {
                audMan.PlaySingle(audRing);
                if (audRing.soundClip != null)
                    ringLength = audRing.soundClip.length;
            }

            if (entity != null && entity.Ec != null)
                entity.Ec.MakeNoise(transform.position, 112, false);

            StartCoroutine(WaitAndDestroy(ringLength));
        }

        private IEnumerator WaitAndDestroy(float delay)
        {
            yield return new WaitForSeconds(delay + 0.5f);
            Destroy(gameObject);
        }

        public void ClickableSighted(int player) { }
        public void ClickableUnsighted(int player) { }
        public bool ClickableHidden() => finished;
        public bool ClickableRequiresNormalHeight() => true;
    }
}