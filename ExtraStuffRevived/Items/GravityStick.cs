using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_GravityStick : Item
    {
        private static SoundObject? flipSound;
        public static readonly HashSet<int> FlippedByStick = new HashSet<int>();

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null || pm.plm == null || pm.plm.Entity == null) return false;

            if (flipSound == null)
            {
                flipSound = Resources.FindObjectsOfTypeAll<SoundObject>()
                    .FirstOrDefault(x => x != null && x.name == "MatchBalloon_Revealed");
            }

            var playerCell = pm.ec.CellFromPosition(pm.transform.position);
            if (playerCell == null) return false;

            var npcs = Object.FindObjectsOfType<NPC>();
            bool hitAnyone = false;

            foreach (var npc in npcs)
            {
                if (npc == null || npc.Navigator == null || npc.Navigator.Entity == null) continue;

                var npcCell = pm.ec.CellFromPosition(npc.transform.position);
                if (npcCell != null)
                {
                    int dx = Mathf.Abs(playerCell.position.x - npcCell.position.x);
                    int dz = Mathf.Abs(playerCell.position.z - npcCell.position.z);

                    if (dx <= 1 && dz <= 1)
                    {
                        if (FlippedByStick.Contains(npc.GetInstanceID())) continue;

                        hitAnyone = true;
                        FlippedByStick.Add(npc.GetInstanceID());

                        npc.Navigator.Entity.IgnoreEntity(pm.plm.Entity, true);
                        npc.Entity.Flip();
                        pm.StartCoroutine(UnflipNpcRoutine(npc, pm, 15f));
                    }
                }
            }

            if (hitAnyone)
            {
                if (flipSound != null && Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.audMan != null)
                    Singleton<CoreGameManager>.Instance.audMan.PlaySingle(flipSound);
            }
            else
            {
                var err = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "ErrorMaybe");
                if (err != null && Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.audMan != null)
                    Singleton<CoreGameManager>.Instance.audMan.PlaySingle(err);
                return false;
            }

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator UnflipNpcRoutine(NPC npc, PlayerManager pm, float duration)
        {
            float timer = duration;

            while (timer > 0f)
            {
                if (npc == null) yield break;
                timer -= Time.deltaTime * npc.TimeScale;
                yield return null;
            }

            if (npc != null)
            {
                FlippedByStick.Remove(npc.GetInstanceID());

                if (npc.Entity != null)
                {
                    npc.Entity.Flip();
                }

                if (npc.Navigator != null && npc.Navigator.Entity != null && pm != null && pm.plm != null && pm.plm.Entity != null)
                {
                    npc.Navigator.Entity.IgnoreEntity(pm.plm.Entity, false);
                }
            }
        }
    }
}