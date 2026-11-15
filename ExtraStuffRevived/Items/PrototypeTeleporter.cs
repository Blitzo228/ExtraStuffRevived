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
                pm.Teleport(cell.FloorWorldPosition);

                var audTeleport = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && (x.name == "Teleport" || x.name == "AudTeleport"));
                if (audTeleport != null) Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audTeleport);
            }

            Object.Destroy(gameObject);
            return true;
        }
    }
}