using HarmonyLib;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    public static class ChalkFaceGuard
    {
        public static readonly HashSet<int> ErasedFaces = new HashSet<int>();
        public static bool IsFaceErased(ChalkFace face) => face != null && ErasedFaces.Contains(face.GetInstanceID());
        public static bool StateIsErased(ChalkFace_StateBase state)
        {
            if (state == null) return false;
            var field = AccessTools.Field(typeof(ChalkFace_StateBase), "chalkles");
            var face = field?.GetValue(state) as ChalkFace;
            return face != null && ErasedFaces.Contains(face.GetInstanceID());
        }
        public static bool FaceIsActive(ChalkFace face)
        {
            if (face == null) return false;
            if (face.state is ChalkFace_Laughing) return false;
            return face.state != null && !(face.state is ChalkFace_Idle);
        }
        public static bool ChalkboardFaceDead(Chalkboard board)
        {
            if (board == null) return true;
            var field = AccessTools.Field(typeof(Chalkboard), "chalkFace");
            var face = field?.GetValue(board) as ChalkFace;
            return face == null;
        }
    }

    [HarmonyPatch(typeof(Chalkboard), nameof(Chalkboard.OnPlayerEnter))]
    internal static class ChalkboardOnPlayerEnterPatch { static bool Prefix(Chalkboard __instance) => !ChalkFaceGuard.ChalkboardFaceDead(__instance); }
    [HarmonyPatch(typeof(Chalkboard), nameof(Chalkboard.OnPlayerExit))]
    internal static class ChalkboardOnPlayerExitPatch { static bool Prefix(Chalkboard __instance) => !ChalkFaceGuard.ChalkboardFaceDead(__instance); }
    [HarmonyPatch(typeof(Chalkboard), nameof(Chalkboard.OnActivityProgress))]
    internal static class ChalkboardOnActivityProgressPatch { static bool Prefix(Chalkboard __instance) => !ChalkFaceGuard.ChalkboardFaceDead(__instance); }
    [HarmonyPatch(typeof(Chalkboard), nameof(Chalkboard.OnActivityCompletion))]
    internal static class ChalkboardOnActivityCompletionPatch { static bool Prefix(Chalkboard __instance) => !ChalkFaceGuard.ChalkboardFaceDead(__instance); }
    [HarmonyPatch(typeof(Chalkboard), "Update")]
    internal static class ChalkboardUpdatePatch { static bool Prefix(Chalkboard __instance) => !ChalkFaceGuard.ChalkboardFaceDead(__instance); }
    [HarmonyPatch(typeof(ChalkFace), nameof(ChalkFace.Cancelled))]
    internal static class ChalkFaceCancelledPatch { static bool Prefix(ChalkFace __instance) => !ChalkFaceGuard.IsFaceErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace), nameof(ChalkFace.Teleport))]
    internal static class ChalkFaceTeleportPatch { static bool Prefix(ChalkFace __instance) => !ChalkFaceGuard.IsFaceErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace), nameof(ChalkFace.Activate))]
    internal static class ChalkFaceActivatePatch { static bool Prefix(ChalkFace __instance) => !ChalkFaceGuard.IsFaceErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_StateBase), nameof(ChalkFace_StateBase.Cancel))]
    internal static class ChalkFaceStateCancelPatch { static bool Prefix(ChalkFace_StateBase __instance) => !ChalkFaceGuard.StateIsErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_StateBase), nameof(ChalkFace_StateBase.Teleport))]
    internal static class ChalkFaceStateTeleportPatch { static bool Prefix(ChalkFace_StateBase __instance) => !ChalkFaceGuard.StateIsErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_Idle), nameof(ChalkFace_Idle.Enter))]
    internal static class ChalkFaceIdleEnterPatch { static bool Prefix(ChalkFace_Idle __instance) => !ChalkFaceGuard.StateIsErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_Laughing), nameof(ChalkFace_Laughing.Enter))]
    internal static class ChalkFaceLaughingEnterPatch { static bool Prefix(ChalkFace_Laughing __instance) => !ChalkFaceGuard.StateIsErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_Laughing), nameof(ChalkFace_Laughing.Update))]
    internal static class ChalkFaceLaughingUpdatePatch { static bool Prefix(ChalkFace_Laughing __instance) => !ChalkFaceGuard.StateIsErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_Charging), nameof(ChalkFace_Charging.Enter))]
    internal static class ChalkFaceChargingEnterPatch { static bool Prefix(ChalkFace_Charging __instance) => !ChalkFaceGuard.StateIsErased(__instance); }
    [HarmonyPatch(typeof(ChalkFace_Charging), nameof(ChalkFace_Charging.Update))]
    internal static class ChalkFaceChargingUpdatePatch { static bool Prefix(ChalkFace_Charging __instance) => !ChalkFaceGuard.StateIsErased(__instance); }

    public class ITM_CleanChalkEraser : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null || pm.ec == null) return false;

            var cell = pm.ec.CellFromPosition(pm.transform.position);
            if (cell == null || cell.room == null || cell.room.category == RoomCategory.Hall) return false;

            var roomFaces = Object.FindObjectsOfType<ChalkFace>()
                .Where(f => f != null && f.ec != null && f.ec.CellFromPosition(f.transform.position)?.room == cell.room).ToArray();

            if (roomFaces.Length == 0) return false;
            if (roomFaces.Any(f => f != null && f.state is ChalkFace_Laughing)) return false;

            bool anyActive = roomFaces.Any(ChalkFaceGuard.FaceIsActive);
            bool anyLocked = Object.FindObjectsOfType<Door>()
                .Where(d => d != null && d.ec != null && d.GetType() == typeof(Door))
                .Any(d => d.locked && d.ec.CellFromPosition(d.transform.position)?.room == cell.room);

            if (!anyActive && !anyLocked) return false;

            foreach (var face in roomFaces)
            {
                if (!ChalkFaceGuard.IsFaceErased(face))
                {
                    Singleton<CoreGameManager>.Instance.StartCoroutine(RespawnChalkles(face, 15f));
                }
            }

            ForceOpenDoors(cell.room, true);
            Singleton<CoreGameManager>.Instance.StartCoroutine(KeepDoorsOpen(cell.room));

            var chalkSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x.name == "ChalkEraser" || x.name == "ChalkEraser_Use");
            if (chalkSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(chalkSound);

            Object.Destroy(gameObject);
            return true;
        }

        private static IEnumerator RespawnChalkles(ChalkFace face, float delay)
        {
            if (face == null) yield break;

            ChalkFaceGuard.ErasedFaces.Add(face.GetInstanceID());

            var nav = face.Navigator;
            if (nav != null)
            {
                nav.ClearDestination();
                nav.SetSpeed(0f);
                nav.enabled = false;
            }

            var renderers = face.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = false;
            }

            yield return new WaitForSeconds(delay);

            if (face != null)
            {
                ChalkFaceGuard.ErasedFaces.Remove(face.GetInstanceID());

                var boardField = AccessTools.Field(typeof(ChalkFace), "chalkboard") ?? AccessTools.Field(typeof(ChalkFace), "board");
                if (boardField != null && boardField.GetValue(face) is Chalkboard board)
                {
                    face.transform.position = board.transform.position;
                }

                foreach (var r in renderers)
                {
                    if (r != null) r.enabled = true;
                }

                var idle = new ChalkFace_Idle(face);
                face.state = idle;
                face.behaviorStateMachine.ChangeState(idle);

                if (nav != null)
                {
                    nav.enabled = true;
                }
            }
        }

        private static IEnumerator KeepDoorsOpen(RoomController room)
        {
            float t = 0f;
            while (t < 3f)
            {
                t += Time.deltaTime;
                ForceOpenDoors(room, false);
                yield return null;
            }
            ForceOpenDoors(room, false);
        }

        private static void ForceOpenDoors(RoomController room, bool withNoise)
        {
            if (room == null) return;
            foreach (var door in Object.FindObjectsOfType<Door>())
            {
                if (door == null || door.ec == null || door.GetType() != typeof(Door)) continue;
                var doorRoom = door.ec.CellFromPosition(door.transform.position)?.room;
                if (doorRoom != room || (!door.locked && door.open)) continue;

                door.locked = false;
                door.Block(false);
                door.Open(true, withNoise);
            }
        }
    }
}
