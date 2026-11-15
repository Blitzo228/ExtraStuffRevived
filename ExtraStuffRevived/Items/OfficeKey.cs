using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraStuffRevived
{
    [HarmonyPatch(typeof(Door), "Initialize")]
    internal static class DoorInitPatch
    {
        static void Postfix(Door __instance)
        {
            if (__instance == null || __instance.ec == null) return;

            if ((__instance.aTile != null && __instance.aTile.room != null && __instance.aTile.room.category == RoomsSetup.OfficeCategory) ||
                (__instance.bTile != null && __instance.bTile.room != null && __instance.bTile.room.category == RoomsSetup.OfficeCategory))
            {
                if (__instance.GetComponent<BaldiOfficeDoorScript>() == null)
                    __instance.gameObject.AddComponent<BaldiOfficeDoorScript>();
            }
        }
    }

    [HarmonyPatch(typeof(Door), nameof(Door.Open))]
    internal static class OfficeDoorNoisePatch
    {
        static void Prefix(Door __instance, ref bool makeNoise)
        {
            if (__instance != null)
            {
                bool isOfficeDoor = false;
                if (__instance.aTile != null && __instance.aTile.room != null && __instance.aTile.room.category == RoomsSetup.OfficeCategory)
                    isOfficeDoor = true;
                else if (__instance.bTile != null && __instance.bTile.room != null && __instance.bTile.room.category == RoomsSetup.OfficeCategory)
                    isOfficeDoor = true;

                if (isOfficeDoor) makeNoise = true;
            }
        }
    }

    [HarmonyPatch(typeof(LevelGenerator), "Generate")]
    internal static class ResetOfficeDoorsPatch
    {
        static void Prefix()
        {
            BaldiOfficeDoorScript.UnlockedRooms.Clear();
        }
    }

    public class BaldiOfficeDoorScript : MonoBehaviour
    {
        public static HashSet<RoomController> UnlockedRooms = new HashSet<RoomController>();

        private Door? door;
        private bool unlocked = false;

        private void Start()
        {
            door = GetComponent<Door>();
            if (door != null)
            {
                door.Shut();
                door.Lock(true);
            }
        }

        private void Update()
        {
            if (door == null) return;

            var room = GetRoom();
            if (room == null) return;

            if (UnlockedRooms.Contains(room))
            {
                if (!unlocked) UnlockPermanently();
                return;
            }

            if (unlocked) return;

            var ec = door.ec;
            if (ec != null && ec.Players != null)
            {
                foreach (var player in ec.Players)
                {
                    if (player == null || player.plm == null) continue;

                    var playerCell = ec.CellFromPosition(player.transform.position);

                    if (playerCell != null && (playerCell.room == room || room.containsPosition(player.transform.position)))
                    {
                        UnlockPermanently();
                        break;
                    }
                }
            }

            if (!unlocked)
            {
                bool hasNpc = false;
                if (ec != null && ec.Npcs != null)
                {
                    foreach (var npc in ec.Npcs)
                    {
                        if (npc != null && room.containsPosition(npc.transform.position))
                        {
                            hasNpc = true;
                            break;
                        }
                    }
                }

                if (!hasNpc)
                {
                    if (!door.locked) door.Lock(true);
                    if (door.open) door.Shut();
                }
            }
        }

        public RoomController? GetRoom()
        {
            if (door == null) return null;
            if (door.aTile != null && door.aTile.room != null && door.aTile.room.category == RoomsSetup.OfficeCategory) return door.aTile.room;
            if (door.bTile != null && door.bTile.room != null && door.bTile.room.category == RoomsSetup.OfficeCategory) return door.bTile.room;
            return null;
        }

        public void UnlockPermanently()
        {
            unlocked = true;
            if (door != null)
            {
                door.Unlock();
                door.Shut();
            }
        }
    }

    public class ITM_BaldiOfficeKey : Item
    {
        private static SoundObject? unlockSound;

        public override bool Use(PlayerManager pm)
        {
            if (pm == null || pm.ec == null) return false;

            var cam = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber);
            if (cam == null) return false;

            if (Physics.Raycast(pm.transform.position, cam.transform.forward, out RaycastHit hit, pm.pc.reach + 1f))
            {
                var door = hit.transform.GetComponentInParent<Door>();
                if (door != null)
                {
                    var script = door.GetComponent<BaldiOfficeDoorScript>();
                    if (script != null)
                    {
                        var room = script.GetRoom();
                        if (room != null)
                        {
                            BaldiOfficeDoorScript.UnlockedRooms.Add(room);
                        }

                        script.UnlockPermanently();

                        if (unlockSound == null)
                            unlockSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "DoorUnlock");

                        if (unlockSound != null && Singleton<CoreGameManager>.Instance != null)
                            Singleton<CoreGameManager>.Instance.audMan.PlaySingle(unlockSound);

                        door.Open(true, true);
                        Object.Destroy(gameObject);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}