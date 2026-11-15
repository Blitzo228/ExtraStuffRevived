using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_DetentionPass : Item
    {
        private static SoundObject? unlockSound;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var cell = pm.ec.CellFromPosition(pm.transform.position);
            if (cell == null || cell.room == null) return false;

            var detentionFunc = cell.room.functionObject?.GetComponent<DetentionRoomFunction>();
            if (detentionFunc == null) return false;

            var timeField = AccessTools.Field(typeof(DetentionRoomFunction), "time");
            var activeField = AccessTools.Field(typeof(DetentionRoomFunction), "active");
            var gaugeField = AccessTools.Field(typeof(DetentionRoomFunction), "gauge");

            if (timeField == null || activeField == null) return false;

            float currentTime = (float)timeField.GetValue(detentionFunc);
            if (currentTime <= 0f) return false;

            timeField.SetValue(detentionFunc, 0f);
            activeField.SetValue(detentionFunc, false);

            var gauge = gaugeField?.GetValue(detentionFunc) as HudGauge;
            if (gauge != null && gauge.Active) gauge.Deactivate();

            foreach (var door in cell.room.doors)
            {
                if (door != null)
                {
                    door.Unlock();
                    door.Open(true, false);
                }
            }

            if (unlockSound == null)
                unlockSound = Resources.FindObjectsOfTypeAll<SoundObject>()
                    .FirstOrDefault(x => x != null && (x.name == "DoorUnlock" || x.name == "Unlock"));

            if (unlockSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(unlockSound);

            Object.Destroy(gameObject);
            return true;
        }
    }
}
