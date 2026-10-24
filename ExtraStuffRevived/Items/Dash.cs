using MTM101BaldAPI.Registers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExtraStuffRevived
{
    public class ITM_Dash : Item
    {
        public override bool Use(PlayerManager pm)
        {
            base.pm = pm;
            if (pm == null) return false;

            var snapSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "GloveSnapOn");
            if (snapSound != null && Singleton<CoreGameManager>.Instance != null)
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(snapSound);

            var dashComp = pm.GetComponent<DashComponent>();
            if (dashComp == null)
            {
                dashComp = pm.gameObject.AddComponent<DashComponent>();
                dashComp.Initialize(pm, 5);
            }
            else
            {
                dashComp.AddDashes(5);
            }

            Object.Destroy(gameObject);
            return true;
        }
    }

    public class DashComponent : MonoBehaviour
    {
        private PlayerManager? pm;
        private int dashesLeft = 0;
        private float dashCooldown = 0f;
        private int initialLives = 0;

        private GameObject? uiObj;
        private TextMeshProUGUI? uiText;
        private float textTimer = 0f;
        private static SoundObject? tossSound;

        public void Initialize(PlayerManager player, int count)
        {
            pm = player;
            dashesLeft = count;
            dashCooldown = 0f;
            if (Singleton<CoreGameManager>.Instance != null)
                initialLives = Singleton<CoreGameManager>.Instance.Lives;
            ShowHudText();
        }

        public void AddDashes(int count)
        {
            dashesLeft += count;
            ShowHudText();
        }

        private void Update()
        {
            if (pm == null || pm.plm == null || pm.plm.Entity == null || pm.ec == null) return;

            var core = Singleton<CoreGameManager>.Instance;
            if (core == null || core.Lives < initialLives || !pm.ec.Active || Singleton<BaseGameManager>.Instance.Ec != pm.ec)
            {
                Destroy(this);
                return;
            }

            if (dashCooldown > 0f)
                dashCooldown -= Time.deltaTime;

            bool isPaused = core.Paused;
            bool isMapOpen = core.MapOpen;

            if (uiText != null && uiObj != null)
            {
                if (isPaused || isMapOpen)
                {
                    uiObj.SetActive(false);
                }
                else
                {
                    uiObj.SetActive(true);
                    textTimer -= Time.deltaTime;
                    if (textTimer <= 0f)
                    {
                        Object.Destroy(uiObj);
                        uiObj = null;
                        uiText = null;
                    }
                }
            }

            bool isDashPressed = BasePlugin.IsRewiredCompatInstalled
                ? InputManager.Instance.GetDigitalInput("UseDash", true)
                : Input.GetKeyDown(KeyCode.R);

            if (isDashPressed && dashesLeft > 0 && dashCooldown <= 0f && !pm.plm.Entity.Flipped)
            {
                if (pm.plm.stamina <= 5f)
                {
                    var err = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "ErrorMaybe");
                    if (err != null && core != null)
                        core.audMan.PlaySingle(err);
                    return;
                }

                float dashCost = 15f;
                float dashPower = Mathf.Clamp01(pm.plm.stamina / dashCost);

                dashesLeft--;
                dashCooldown = 0.5f;

                pm.plm.stamina = Mathf.Max(0f, pm.plm.stamina - dashCost);

                if (tossSound == null)
                {
                    tossSound = Resources.FindObjectsOfTypeAll<SoundObject>().FirstOrDefault(x => x != null && x.name == "CampfireToss");
                }

                if (tossSound != null && core != null)
                    core.audMan.PlaySingle(tossSound);

                ShowHudText();
                StartCoroutine(DashRoutine(dashPower));
            }

            if (dashesLeft <= 0 && textTimer <= 0f)
            {
                Destroy(this);
            }
        }

        private void ShowHudText()
        {
            if (pm == null || Singleton<CoreGameManager>.Instance == null) return;

            string localizedText = "Dash_HUD_Text";
            var locManager = Singleton<LocalizationManager>.Instance;
            if (locManager != null)
            {
                var text = locManager.GetLocalizedText("Dash_HUD_Text");
                if (!string.IsNullOrEmpty(text)) localizedText = text;
            }

            string finalText = localizedText.Contains("{0}")
                ? string.Format(localizedText, dashesLeft)
                : localizedText + "\n" + dashesLeft;

            if (uiObj == null || uiText == null)
            {
                var hud = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber);
                if (hud == null) return;

                uiObj = new GameObject("DashHUDText", typeof(RectTransform));
                uiObj.transform.SetParent(hud.Canvas().transform, false);

                uiText = uiObj.AddComponent<TextMeshProUGUI>();

                TMP_FontAsset? fontAsset = null;
                var hudText = hud.GetComponentInChildren<TMP_Text>();
                if (hudText != null) fontAsset = hudText.font;
                if (fontAsset != null) uiText.font = fontAsset;

                uiText.fontSize = 28f;
                uiText.alignment = TextAlignmentOptions.Bottom;
                uiText.color = Color.black;
                uiText.enableWordWrapping = false;
                uiText.overflowMode = TextOverflowModes.Overflow;

                RectTransform rect = uiObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);

                rect.anchoredPosition = new Vector2(0, 50f);
                rect.sizeDelta = new Vector2(800, 100);
            }

            if (uiText != null)
            {
                uiText.text = finalText;
                uiText.color = Color.black;
            }
            textTimer = 3f;
        }

        private IEnumerator DashRoutine(float power)
        {
            if (pm == null || pm.plm == null || pm.plm.Entity == null) yield break;

            var allNpcs = Object.FindObjectsOfType<NPC>();
            var ignoredNpcs = new List<NPC>();

            foreach (var npc in allNpcs)
            {
                if (npc != null && npc.Navigator != null && npc.Navigator.Entity != null)
                {
                    npc.Navigator.Entity.IgnoreEntity(pm.plm.Entity, true);
                    ignoredNpcs.Add(npc);
                }
            }

            float duration = 0.35f * Mathf.Max(0.2f, power);
            float elapsed = 0f;

            Vector3 dashDir = pm.transform.forward;
            float dashSpeed = 45f * power;

            var moveMod = new MovementModifier(Vector3.zero, 1f);
            pm.plm.am.moveMods.Add(moveMod);

            float startHeight = pm.plm.height;
            float jumpHeight = 4f * power;

            while (elapsed < duration)
            {
                if (pm == null || pm.plm == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                pm.plm.height = startHeight + Mathf.Sin(t * Mathf.PI) * jumpHeight;
                moveMod.movementAddend = pm.transform.forward * dashSpeed;

                yield return null;
            }

            if (pm != null && pm.plm != null)
            {
                pm.plm.height = startHeight;
                pm.plm.am.moveMods.Remove(moveMod);
            }

            foreach (var npc in ignoredNpcs)
            {
                if (npc != null && npc.Navigator != null && npc.Navigator.Entity != null && pm.plm != null && pm.plm.Entity != null)
                {
                    npc.Navigator.Entity.IgnoreEntity(pm.plm.Entity, false);
                }
            }
        }
    }
}