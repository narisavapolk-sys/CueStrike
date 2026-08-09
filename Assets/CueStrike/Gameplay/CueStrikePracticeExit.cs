using UnityEngine;
using UnityEngine.UI;
using CueStrike.VR;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Adds a VR-friendly Return-to-Menu button inside the offline Practice Hub.
    /// Provides smooth exit transition back to the Main Menu.
    /// </summary>
    public class CueStrikePracticeExit : MonoBehaviour
    {
        private void Start()
        {
            CreateExitCanvas();
        }

        private void CreateExitCanvas()
        {
            // 1. Create Canvas GO in world space
            var canvasGO = new GameObject("PracticeExitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.position = new Vector3(-1.2f, 1.1f, -1.8f);
            canvasGO.transform.rotation = Quaternion.Euler(20f, 45f, 0f);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 5f;

            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 120f);
            rect.localScale = Vector3.one * 0.003f;

            // 2. Button Background Image
            var btnGO = new GameObject("ExitButton", typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvasGO.transform, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.sizeDelta = Vector2.zero;

            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.8f, 0.15f, 0.15f, 0.7f);

            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(OnExitClicked);

            // 3. Text Label
            var textGO = new GameObject("Label", typeof(Text));
            textGO.transform.SetParent(btnGO.transform, false);
            var txtRect = textGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txt = textGO.GetComponent<Text>();
            txt.text = "RETURN TO MENU";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void OnExitClicked()
        {
            Debug.Log("[CueStrike Practice] Exiting Practice Mode, loading MainMenu...");
            CueStrikeLoadingScreen.LoadScene("MainMenu");
        }
    }
}