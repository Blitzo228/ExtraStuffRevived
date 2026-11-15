using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_ReallyDangerousTeleporter : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var tpComp = pm.gameObject.AddComponent<DangerousTeleportComponent>();
            tpComp.StartCoroutine(tpComp.TeleportRoutine(pm));

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class DangerousTeleportComponent : MonoBehaviour
    {
        public IEnumerator TeleportRoutine(PlayerManager pm)
        {
            int teleports = 20;
            var rng = new System.Random();
            var freezeMod = new MovementModifier(Vector3.zero, 0f);

            if (pm != null && pm.plm != null && pm.plm.am != null)
                pm.plm.am.moveMods.Add(freezeMod);

            var audTeleport = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && (x.name == "Teleport" || x.name == "AudTeleport"));

            for (int i = 0; i < teleports; i++)
            {
                Cell? targetCell = null;

                bool goToBaldi = (i == teleports - 1) && (rng.NextDouble() < 0.7);

                if (goToBaldi)
                {
                    var baldi = pm.ec.GetBaldi();
                    if (baldi != null)
                    {
                        var baldiCell = pm.ec.CellFromPosition(baldi.transform.position);
                        if (baldiCell != null)
                        {
                            var neighbors = new List<Cell>();
                            foreach (var dir in Directions.All())
                            {
                                var n = pm.ec.CellFromPosition(baldiCell.position + dir.ToIntVector2());
                                if (n != null && !n.Null && n.open)
                                {
                                    neighbors.Add(n);
                                }
                            }

                            if (neighbors.Count > 0)
                                targetCell = neighbors[rng.Next(0, neighbors.Count)];
                            else
                                targetCell = baldiCell;
                        }
                    }
                }

                if (targetCell == null)
                {
                    targetCell = pm.ec.RandomCell(false, false, true);
                }

                if (targetCell != null)
                {
                    pm.transform.position = targetCell.FloorWorldPosition + Vector3.up * 5f;
                    pm.plm.height = targetCell.FloorWorldPosition.y + 5f;

                    if (audTeleport != null && Singleton<CoreGameManager>.Instance != null)
                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle(audTeleport);
                }

                yield return new WaitForSeconds(0.2f);
            }

            if (pm != null && pm.plm != null && pm.plm.am != null)
                pm.plm.am.moveMods.Remove(freezeMod);

            Destroy(this);
        }
    }
}