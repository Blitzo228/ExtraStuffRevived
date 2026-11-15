using HarmonyLib;
using MTM101BaldAPI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(EnvironmentController), "BeginPlay")]
    internal static class BraidedMazePatch
    {
        static void Postfix(EnvironmentController __instance)
        {
            if (__instance == null || __instance.rooms == null) return;

            int seed;
            try { seed = CoreGameManager.Instance != null ? CoreGameManager.Instance.Seed() : System.Environment.TickCount; }
            catch { seed = System.Environment.TickCount; }

            var rng = new System.Random(seed);

            foreach (var room in __instance.rooms)
            {
                if (room == null) continue;
                if (room.category != RoomsSetup.OldLibraryRNGCategory) continue;
                MazeGenerator.GenerateBraided(room, rng);
            }
        }
    }

    public static class MazeGenerator
    {
        public static void GenerateBraided(RoomController room, System.Random rng)
        {
            if (room == null || rng == null) return;
            var ec = room.ec;
            if (ec == null) return;

            Cell[] cells = ec.AllCells().Where(c => c.room == room).ToArray();
            if (cells.Length == 0) return;

            foreach (var cell in cells)
            {
                foreach (var dir in Directions.All())
                {
                    if (!cell.doorDirs.Contains(dir))
                    {
                        ec.CloseCell(cell.position, dir);
                    }
                }
            }

            var visited = new HashSet<Cell>();
            var doors = cells.Where(c => c.doorDirs.Count > 0).ToList();
            var startCell = doors.Count > 0 ? doors[rng.Next(0, doors.Count)] : cells[rng.Next(0, cells.Length)];

            var stack = new Stack<Cell>();
            stack.Push(startCell);
            visited.Add(startCell);

            while (visited.Count < cells.Length)
            {
                Cell? current = stack.Count > 0 ? stack.Peek() : null;
                if (current == null)
                {
                    current = cells.FirstOrDefault(c => !visited.Contains(c));
                    if (current == null) break;
                    stack.Push(current);
                    visited.Add(current);
                }

                var neighbors = new List<Cell>();
                foreach (var dir in Directions.All())
                {
                    var n = ec.CellFromPosition(current.position + dir.ToIntVector2());
                    if (n != null && !n.Null && n.room == room && !visited.Contains(n))
                        neighbors.Add(n);
                }

                if (neighbors.Count > 0)
                {
                    var next = neighbors[rng.Next(0, neighbors.Count)];
                    var dir = GetDirection(current, next);
                    ec.ConnectCells(current.position, dir);
                    ec.UpdateCell(current.position);
                    stack.Push(next);
                    visited.Add(next);
                }
                else
                {
                    stack.Pop();
                }
            }

            for (int pass = 0; pass < 3; pass++)
            {
                bool changed = false;
                foreach (var cell in cells)
                {
                    int openToRoom = 0;
                    foreach (var dir in Directions.All())
                    {
                        if (cell.ConstBin.ContainsDirection(dir)) continue;
                        var n = ec.CellFromPosition(cell.position + dir.ToIntVector2());
                        if (n != null && !n.Null && n.room == room) openToRoom++;
                    }
                    if (openToRoom > 1) continue;

                    var candidates = new List<Direction>();
                    foreach (var dir in Directions.All())
                    {
                        if (!cell.ConstBin.ContainsDirection(dir)) continue;
                        var n = ec.CellFromPosition(cell.position + dir.ToIntVector2());
                        if (n != null && !n.Null && n.room == room)
                            candidates.Add(dir);
                    }
                    if (candidates.Count == 0) continue;
                    var pick = candidates[rng.Next(0, candidates.Count)];
                    ec.ConnectCells(cell.position, pick);
                    ec.UpdateCell(cell.position);
                    changed = true;
                }
                if (!changed) break;
            }

            foreach (var cell in cells)
            {
                ec.RefreshCell(cell.position);
            }

            if (ec.map != null)
            {
                foreach (var cell in cells)
                {
                    ec.map.Find(cell.position.x, cell.position.z, cell.ConstBin, room);
                }
            }
        }

        private static Direction GetDirection(Cell from, Cell to)
        {
            var diff = to.position - from.position;
            if (!Directions.Vectors.Contains(diff)) return Direction.Null;
            return Directions.All().FirstOrDefault(d => d.ToIntVector2() == diff);
        }
    }
}