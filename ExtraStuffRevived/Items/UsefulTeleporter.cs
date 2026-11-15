using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public class ITM_CoolUsefulTeleporter : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var tpComp = pm.gameObject.AddComponent<UsefulTeleportComponent>();
            tpComp.StartCoroutine(tpComp.TeleportRoutine(pm));

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class UsefulTeleportComponent : MonoBehaviour
    {
        public IEnumerator TeleportRoutine(PlayerManager pm)
        {
            int teleports = 10;
            var rng = new System.Random();
            var freezeMod = new MovementModifier(Vector3.zero, 0f);

            if (pm != null && pm.plm != null && pm.plm.am != null)
                pm.plm.am.moveMods.Add(freezeMod);

            var audTeleport = Resources.FindObjectsOfTypeAll<SoundObject>()
                .FirstOrDefault(x => x != null && (x.name == "Teleport" || x.name == "AudTeleport"));

            var baldi = pm.ec.GetBaldi();
            Vector3 baldiPos = baldi != null ? baldi.transform.position : Vector3.zero;
            bool hasBaldi = baldi != null;

            var candidateCells = new List<Cell>();

            var notebooks = Object.FindObjectsOfType<Notebook>().Where(n => n != null && !n.collected).ToList();
            foreach (var nb in notebooks)
            {
                var cell = pm.ec.CellFromPosition(nb.transform.position);
                if (cell != null && !cell.Null) candidateCells.Add(cell);
            }

            var pomp = Object.FindObjectsOfType<NoLateTeacher>().FirstOrDefault();
            if (pomp != null && pomp.TargetClassRoom != null)
            {
                candidateCells.AddRange(pomp.TargetClassRoom.cells);
            }

            var safeRooms = pm.ec.rooms.Where(r => r != null && r.functionObject != null && r.functionObject.GetComponent<BlockNavigationRoomFunction>() != null).ToList();
            foreach (var room in safeRooms)
            {
                candidateCells.AddRange(room.cells);
            }

            candidateCells = candidateCells.Where(c => c != null && !c.Null).Distinct().ToList();

            Cell? targetCell = null;

            if (candidateCells.Count > 0)
            {
                if (hasBaldi)
                {
                    var safeCells = candidateCells.Where(c => Vector3.Distance(c.FloorWorldPosition, baldiPos) > 30f).ToList();
                    if (safeCells.Count > 0)
                        targetCell = safeCells[rng.Next(0, safeCells.Count)];
                    else
                        targetCell = candidateCells.OrderByDescending(c => Vector3.Distance(c.FloorWorldPosition, baldiPos)).First();
                }
                else
                {
                    targetCell = candidateCells[rng.Next(0, candidateCells.Count)];
                }
            }

            Cell? finalSafeCell = null;
            if (targetCell != null)
            {
                var neighbors = new List<Cell>();
                foreach (var dir in Directions.All())
                {
                    var n = pm.ec.CellFromPosition(targetCell.position + dir.ToIntVector2());
                    if (n != null && !n.Null && n.open)
                    {
                        neighbors.Add(n);
                    }
                }

                if (neighbors.Count > 0)
                    finalSafeCell = neighbors[rng.Next(0, neighbors.Count)];
                else
                    finalSafeCell = targetCell;
            }

            for (int i = 0; i < teleports; i++)
            {
                Cell? currentTarget = null;

                if (i == teleports - 1 && finalSafeCell != null)
                {
                    currentTarget = finalSafeCell;
                }
                else
                {
                    currentTarget = pm.ec.RandomCell(false, false, true);
                }

                if (currentTarget != null)
                {
                    pm.transform.position = currentTarget.FloorWorldPosition + Vector3.up * 5f;
                    pm.plm.height = currentTarget.FloorWorldPosition.y + 5f;

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
