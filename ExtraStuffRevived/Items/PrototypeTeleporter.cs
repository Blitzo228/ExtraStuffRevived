using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_PrototypeTeleporter : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var cell = pm.ec.RandomCell(false, false, true);
            if (cell != null)
            {
                pm.transform.position = cell.FloorWorldPosition + Vector3.up * 5f;
                pm.plm.height = cell.FloorWorldPosition.y + 5f;
                var audTeleport = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x.name == "Teleport" || x.name == "AudTeleport");
                if (audTeleport != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audTeleport);
            }
            Object.Destroy(gameObject);
            return true;
        }
    }
}