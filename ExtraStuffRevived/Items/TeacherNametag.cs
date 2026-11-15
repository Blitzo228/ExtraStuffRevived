using System.Collections;
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace ExtraStuffRevived
{
    public static class NametagManager
    {
        public static bool Active = false;
    }

    public class ITM_TeacherNametag : Item
    {
        private HudGauge? gauge;
        private float timeLeft = 30f;

        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            NametagManager.Active = true;
            pm.SetNametag(true);
            pm.invincible = true;

            var npcs = Object.FindObjectsOfType<NPC>();
            foreach (var npc in npcs)
            {
                if (npc != null)
                {
                    if (npc is Baldi baldi)
                    {
                        baldi.Distract();
                    }

                    if (npc.looker != null)
                    {
                        npc.looker.enabled = false;
                    }

                    if (npc.Navigator != null && npc.Navigator.Entity != null && pm.plm != null && pm.plm.Entity != null)
                    {
                        npc.Navigator.Entity.IgnoreEntity(pm.plm.Entity, true);
                    }
                }
            }

            Sprite? icon = null;
            if (BasePlugin.assetManager.ContainsKey("Sprites/TeacherNametagIcon_Small"))
                icon = BasePlugin.assetManager.Get<Sprite>("Sprites/TeacherNametagIcon_Small");

            gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(icon, timeLeft);
            pm.StartCoroutine(Timer(pm));

            Object.Destroy(gameObject);
            return true;
        }

        private IEnumerator Timer(PlayerManager pm)
        {
            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime * pm.PlayerTimeScale;
                gauge?.SetValue(30f, timeLeft);
                yield return null;
            }

            NametagManager.Active = false;

            if (pm != null)
            {
                pm.SetNametag(false);
                pm.invincible = false;

                var npcs = Object.FindObjectsOfType<NPC>();
                foreach (var npc in npcs)
                {
                    if (npc != null)
                    {
                        if (npc.looker != null)
                        {
                            npc.looker.enabled = true;
                        }

                        if (npc.Navigator != null && npc.Navigator.Entity != null && pm.plm != null && pm.plm.Entity != null)
                        {
                            npc.Navigator.Entity.IgnoreEntity(pm.plm.Entity, false);
                        }
                    }
                }
            }

            gauge?.Deactivate();
        }
    }

    [HarmonyPatch(typeof(NPC), "Hear")]
    internal static class NametagHearPatch
    {
        static bool Prefix() => !NametagManager.Active;
    }

    [HarmonyPatch(typeof(Baldi), "Hear", new System.Type[] { typeof(GameObject), typeof(Vector3), typeof(int), typeof(bool) })]
    internal static class NametagBaldiHearPatch
    {
        static bool Prefix() => !NametagManager.Active;
    }

    [HarmonyPatch(typeof(NPC), "PlayerInSight")]
    internal static class NametagSightPatch
    {
        static bool Prefix() => !NametagManager.Active;
    }

    [HarmonyPatch(typeof(NPC), "PlayerSighted")]
    internal static class NametagSightedPatch
    {
        static bool Prefix() => !NametagManager.Active;
    }
}