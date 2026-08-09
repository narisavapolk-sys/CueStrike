using System;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.Gameplay.Tutorial
{
    /// <summary>
    /// Visual overlay for tutorial steps - highlights target objects, shows arrows/indicators.
    /// </summary>
    public class CueStrikeTutorialOverlay : MonoBehaviour
    {
        // Singleton
        public static CueStrikeTutorialOverlay Instance { get; private set; }

        [Header("Overlay Elements")]
        [SerializeField] private GameObject _highlightRingPrefab;
        [SerializeField] private GameObject _arrowIndicatorPrefab;
        [SerializeField] private GameObject _targetMarkerPrefab;
        [SerializeField] private Canvas _overlayCanvas;
        [SerializeField] private RectTransform _overlayContainer;
        [SerializeField] private Image _dimBackground;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseScaleMin = 0.9f;
        [SerializeField] private float _pulseScaleMax = 1.1f;

        // Active overlay objects
        private GameObject _currentHighlight;
        private GameObject _currentArrow;
        private GameObject _currentTargetMarker;
        private Coroutine _pulseCoroutine;
        private Coroutine _fadeCoroutine;

        // State
        private bool _isVisible = false;
        private CueStrikeTutorialSteps.TutorialStep _currentStep;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure overlay starts hidden
            if (_overlayCanvas != null) _overlayCanvas.enabled = false;
            if (_dimBackground != null) _dimBackground.enabled = false;

            // Create default prefabs if not assigned
            EnsureDefaultPrefabs();
        }

        private void EnsureDefaultPrefabs()
        {
            if (_highlightRingPrefab == null)
            {
                _highlightRingPrefab = CreateDefaultHighlightRing();
            }
            if (_arrowIndicatorPrefab == null)
            {
                _arrowIndicatorPrefab = CreateDefaultArrowIndicator();
            }
            if (_targetMarkerPrefab == null)
            {
                _targetMarkerPrefab = CreateDefaultTargetMarker();
            }
        }

        private GameObject CreateDefaultHighlightRing()
        {
            var go = new GameObject("TutorialHighlightRing");
            go.transform.SetParent(transform);
            
            var ring = go.AddComponent<Image>();
            ring.color = new Color(1f, 0.8f, 0f, 0.8f); // Gold highlight
            ring.raycastTarget = false;
            
            // Create ring using a sprite or draw procedurally
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 0.5f, 1f);
            outline.effectDistance = new Vector2(4f, 4f);
            
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 200f);
            
            return go;
        }

        private GameObject CreateDefaultArrowIndicator()
        {
            var go = new GameObject("TutorialArrowIndicator");
            go.transform.SetParent(transform);
            
            var arrow = go.AddComponent<Image>();
            arrow.color = new Color(0f, 1f, 0.5f, 1f); // Green arrow
            arrow.raycastTarget = false;
            
            // Simple triangle arrow using a sprite or rotate rect
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(60f, 100f);
            
            return go;
        }

        private GameObject CreateDefaultTargetMarker()
        {
            var go = new GameObject("TutorialTargetMarker");
            go.transform.SetParent(transform);
            
            var marker = go.AddComponent<Image>();
            marker.color = new Color(1f, 0.5f, 0f, 1f); // Orange target
            marker.raycastTarget = false;
            
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 80f);
            
            return go;
        }

        /// <summary>
        /// Shows the overlay for a tutorial step.
        /// </summary>
        public void ShowStep(CueStrikeTutorialSteps.TutorialStep step)
        {
            _currentStep = step;
            _isVisible = true;

            if (_overlayCanvas != null) _overlayCanvas.enabled = true;
            if (_dimBackground != null) _dimBackground.enabled = true;

            // Fade in background
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeBackground(0f, 0.4f, _fadeInDuration));

            // Show highlight on target object
            if (!string.IsNullOrEmpty(step.targetObjectName))
            {
                ShowHighlight(step.targetObjectName, step.highlightPosition, step.highlightRadius);
            }

            // Show arrow pointing to target (for VR/3D guidance)
            if (step.highlightPosition != Vector3.zero && step.highlightRadius > 0)
            {
                ShowArrow(step.highlightPosition);
            }

            // Show target marker at specific position
            if (step.requiredBallId >= 0)
            {
                ShowTargetMarker(step.highlightPosition);
            }

            // Start pulse animation
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseAnimation());
        }

        /// <summary>
        /// Hides the overlay.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;

            // Fade out background
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeBackground(0.4f, 0f, _fadeOutDuration, () => 
            {
                if (_overlayCanvas != null) _overlayCanvas.enabled = false;
                if (_dimBackground != null) _dimBackground.enabled = false;
            }));

            // Stop pulse
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            // Clean up overlay objects
            ClearOverlays();
        }

        /// <summary>
        /// Shows a highlight ring around a target GameObject.
        /// </summary>
        private void ShowHighlight(string objectName, Vector3 worldPosition, float radius)
        {
            ClearHighlight();

            // Find target object
            var targetObj = GameObject.Find(objectName);
            if (targetObj == null) return;

            // Create highlight ring
            _currentHighlight = Instantiate(_highlightRingPrefab, _overlayContainer);
            _currentHighlight.name = "TutorialHighlight";

            // Position in world space (convert to canvas space)
            PositionWorldObjectOnCanvas(_currentHighlight, worldPosition);

            // Scale based on radius
            var rect = _currentHighlight.GetComponent<RectTransform>();
            float scale = Mathf.Max(radius * 50f, 100f); // Convert world units to pixels
            rect.sizeDelta = new Vector2(scale, scale);
        }

        /// <summary>
        /// Shows an arrow indicator pointing to a world position.
        /// </summary>
        private void ShowArrow(Vector3 worldPosition)
        {
            ClearArrow();

            _currentArrow = Instantiate(_arrowIndicatorPrefab, _overlayContainer);
            _currentArrow.name = "TutorialArrow";

            // Position arrow at screen edge pointing to target
            PositionArrowAtScreenEdge(_currentArrow, worldPosition);
        }

        /// <summary>
        /// Shows a target marker at a specific position.
        /// </summary>
        private void ShowTargetMarker(Vector3 worldPosition)
        {
            ClearTargetMarker();

            _currentTargetMarker = Instantiate(_targetMarkerPrefab, _overlayContainer);
            _currentTargetMarker.name = "TutorialTargetMarker";

            PositionWorldObjectOnCanvas(_currentTargetMarker, worldPosition);
        }

        /// <summary>
        /// Positions a UI element at a world position on the canvas.
        /// </summary>
        private void PositionWorldObjectOnCanvas(GameObject uiElement, Vector3 worldPosition)
        {
            if (_overlayCanvas == null || Camera.main == null) return;

            var rect = uiElement.GetComponent<RectTransform>();
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
            
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayContainer, screenPos, _overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out localPos);
            
            rect.anchoredPosition = localPos;
        }

        /// <summary>
        /// Positions arrow at screen edge pointing toward target.
        /// </summary>
        private void PositionArrowAtScreenEdge(GameObject arrow, Vector3 worldTargetPos)
        {
            if (Camera.main == null) return;

            Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldTargetPos);
            
            // If target is on screen, place arrow near it
            if (viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1 && viewportPos.z > 0)
            {
                PositionWorldObjectOnCanvas(arrow, worldTargetPos + Vector3.up * 0.5f);
                
                // Rotate arrow to point down at target
                var rect = arrow.GetComponent<RectTransform>();
                rect.rotation = Quaternion.Euler(0, 0, 180f);
            }
            else
            {
                // Target off-screen: place arrow at screen edge pointing toward target
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 targetScreen = Camera.main.WorldToScreenPoint(worldTargetPos);
                Vector2 direction = (targetScreen - screenCenter).normalized;
                
                // Place arrow at edge
                float margin = 100f;
                Vector2 edgePos = screenCenter + direction * (Mathf.Min(Screen.width, Screen.height) * 0.5f - margin);
                
                var rect = arrow.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _overlayContainer, edgePos, _overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out Vector2 localPos);
                rect.anchoredPosition = localPos;
                
                // Rotate to point toward target
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                rect.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// Pulsing animation for highlights.
        /// </summary>
        private System.Collections.IEnumerator PulseAnimation()
        {
            float timer = 0f;
            while (_isVisible)
            {
                timer += Time.deltaTime * _pulseSpeed;
                float pulse = Mathf.Lerp(_pulseScaleMin, _pulseScaleMax, (Mathf.Sin(timer * Mathf.PI) + 1f) * 0.5f);

                if (_currentHighlight != null)
                {
                    _currentHighlight.transform.localScale = Vector3.one * pulse;
                }
                if (_currentTargetMarker != null)
                {
                    _currentTargetMarker.transform.localScale = Vector3.one * pulse;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Fades the background dim overlay.
        /// </summary>
        private System.Collections.IEnumerator FadeBackground(float from, float to, float duration, Action onComplete = null)
        {
            if (_dimBackground == null) yield break;

            float timer = 0f;
            Color color = _dimBackground.color;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                color.a = Mathf.Lerp(from, to, t);
                _dimBackground.color = color;
                yield return null;
            }

            color.a = to;
            _dimBackground.color = color;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Clears all overlay objects.
        /// </summary>
        private void ClearOverlays()
        {
            ClearHighlight();
            ClearArrow();
            ClearTargetMarker();
        }

        private void ClearHighlight()
        {
            if (_currentHighlight != null)
            {
                Destroy(_currentHighlight);
                _currentHighlight = null;
            }
        }

        private void ClearArrow()
        {
            if (_currentArrow != null)
            {
                Destroy(_currentArrow);
                _currentArrow = null;
            }
        }

        private void ClearTargetMarker()
        {
            if (_currentTargetMarker != null)
            {
                Destroy(_currentTargetMarker);
                _currentTargetMarker = null;
            }
        }

        /// <summary>
        /// Updates overlay for VR mode (world-space canvas).
        /// </summary>
        public void UpdateForVR(Camera vrCamera)
        {
            if (!_isVisible || _overlayCanvas == null) return;

            // Switch canvas to world space for VR
            if (_overlayCanvas.renderMode != RenderMode.WorldSpace)
            {
                _overlayCanvas.renderMode = RenderMode.WorldSpace;
                _overlayCanvas.worldCamera = vrCamera;
                
                // Position canvas in front of player
                transform.position = vrCamera.transform.position + vrCamera.transform.forward * 1.5f;
                transform.rotation = vrCamera.transform.rotation;
                transform.localScale = Vector3.one * 0.01f; // Scale down for world space
            }
        }

        /// <summary>
        /// Updates overlay for desktop mode (screen-space overlay).
        /// </summary>
        public void UpdateForDesktop()
        {
            if (!_isVisible || _overlayCanvas == null) return;

            if (_overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _overlayCanvas.worldCamera = null;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
        }

        public bool IsVisible => _isVisible;
        public CueStrikeTutorialSteps.TutorialStep CurrentStep => _currentStep;
    }
}