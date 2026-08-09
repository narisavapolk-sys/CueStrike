using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ball Labels System - Floating number labels for billiard balls
/// Supports 8-Ball and 9-Ball numbering with dynamic visibility
/// </summary>
public class CueStrikeBallLabels : MonoBehaviour
{
    [Header("Label Settings")]
    [SerializeField] private bool showLabels = true;

    /// <summary>
    /// Public property to access showLabels for reflection/UI binding
    /// </summary>
    public bool ShowLabels
    {
        get => showLabels;
        set => showLabels = value;
    }
    [SerializeField] private float labelHeight = 0.05f;
    [SerializeField] private float labelScale = 0.02f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Font labelFont;

    [Header("Visibility")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float fadeDistance = 5f;
    [SerializeField] private bool faceCamera = true;

    [Header("Ball References")]
    [SerializeField] private List<BallLabelData> ballLabels = new List<BallLabelData>();

    private Camera mainCamera;
    private Dictionary<int, GameObject> labelObjects = new Dictionary<int, GameObject>();

    [System.Serializable]
    public class BallLabelData
    {
        public int ballNumber;
        public Transform ballTransform;
        public string customText;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        CreateLabelObjects();
    }

    private void Start()
    {
        UpdateVisibility();
    }

    private void LateUpdate()
    {
        if (!showLabels) return;
        if (mainCamera == null) return;

        UpdateLabelPositions();
        UpdateVisibility();
    }

    private void CreateLabelObjects()
    {
        foreach (var ballData in ballLabels)
        {
            if (ballData.ballTransform == null) continue;

            GameObject labelObj = new GameObject($"BallLabel_{ballData.ballNumber}");
            labelObj.transform.SetParent(ballData.ballTransform, false);
            labelObj.transform.localPosition = Vector3.up * labelHeight;

            var canvas = labelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = Vector3.one * labelScale;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(labelObj.transform, false);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = string.IsNullOrEmpty(ballData.customText) ? ballData.ballNumber.ToString() : ballData.customText;
            textMesh.fontSize = fontSize;
            textMesh.color = labelColor;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontStyle = FontStyle.Bold;

            if (labelFont != null)
            {
                textMesh.font = labelFont;
            }

            // Add outline effect using a duplicate text mesh
            var outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(labelObj.transform, false);
            outlineObj.transform.localPosition = new Vector3(0.002f, -0.002f, 0.001f);

            var outlineMesh = outlineObj.AddComponent<TextMesh>();
            outlineMesh.text = textMesh.text;
            outlineMesh.fontSize = fontSize;
            outlineMesh.color = outlineColor;
            outlineMesh.anchor = TextAnchor.MiddleCenter;
            outlineMesh.alignment = TextAlignment.Center;
            outlineMesh.fontStyle = FontStyle.Bold;
            if (labelFont != null)
            {
                outlineMesh.font = labelFont;
            }

            labelObjects[ballData.ballNumber] = labelObj;
        }
    }

    private void UpdateLabelPositions()
    {
        if (!faceCamera || mainCamera == null) return;

        foreach (var kvp in labelObjects)
        {
            if (kvp.Value != null)
            {
                kvp.Value.transform.LookAt(mainCamera.transform);
                kvp.Value.transform.Rotate(0, 180, 0);
            }
        }
    }

    private void UpdateVisibility()
    {
        if (mainCamera == null) return;

        foreach (var kvp in labelObjects)
        {
            if (kvp.Value == null) continue;

            var ballData = ballLabels.Find(b => b.ballNumber == kvp.Key);
            if (ballData == null || ballData.ballTransform == null) continue;

            float distance = Vector3.Distance(mainCamera.transform.position, ballData.ballTransform.position);
            float alpha = 1f;

            if (distance > maxDistance)
            {
                alpha = 0f;
            }
            else if (distance > fadeDistance)
            {
                alpha = 1f - ((distance - fadeDistance) / (maxDistance - fadeDistance));
            }

            var canvas = kvp.Value.GetComponent<Canvas>();
            if (canvas != null)
            {
                var canvasGroup = kvp.Value.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = kvp.Value.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = alpha;
            }
        }
    }

    /// <summary>
    /// Register a ball for labeling
    /// </summary>
    public void RegisterBall(int ballNumber, Transform ballTransform, string customText = "")
    {
        // Remove existing if any
        UnregisterBall(ballNumber);

        var ballData = new BallLabelData
        {
            ballNumber = ballNumber,
            ballTransform = ballTransform,
            customText = customText
        };

        ballLabels.Add(ballData);

        // Create label object for this ball
        CreateLabelForBall(ballData);
    }

    private void CreateLabelForBall(BallLabelData ballData)
    {
        if (ballData.ballTransform == null) return;

        GameObject labelObj = new GameObject($"BallLabel_{ballData.ballNumber}");
        labelObj.transform.SetParent(ballData.ballTransform, false);
        labelObj.transform.localPosition = Vector3.up * labelHeight;

        var canvas = labelObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.localScale = Vector3.one * labelScale;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform, false);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;

        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = string.IsNullOrEmpty(ballData.customText) ? ballData.ballNumber.ToString() : ballData.customText;
        textMesh.fontSize = fontSize;
        textMesh.color = labelColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;

        if (labelFont != null)
        {
            textMesh.font = labelFont;
        }

        // Outline
        var outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(labelObj.transform, false);
        outlineObj.transform.localPosition = new Vector3(0.002f, -0.002f, 0.001f);

        var outlineMesh = outlineObj.AddComponent<TextMesh>();
        outlineMesh.text = textMesh.text;
        outlineMesh.fontSize = fontSize;
        outlineMesh.color = outlineColor;
        outlineMesh.anchor = TextAnchor.MiddleCenter;
        outlineMesh.alignment = TextAlignment.Center;
        outlineMesh.fontStyle = FontStyle.Bold;
        if (labelFont != null)
        {
            outlineMesh.font = labelFont;
        }

        labelObjects[ballData.ballNumber] = labelObj;
    }

    /// <summary>
    /// Unregister a ball from labeling
    /// </summary>
    public void UnregisterBall(int ballNumber)
    {
        var existing = ballLabels.Find(b => b.ballNumber == ballNumber);
        if (existing != null)
        {
            ballLabels.Remove(existing);
        }

        if (labelObjects.TryGetValue(ballNumber, out var labelObj))
        {
            if (labelObj != null)
            {
                Destroy(labelObj);
            }
            labelObjects.Remove(ballNumber);
        }
    }

    /// <summary>
    /// Enable/Disable all labels
    /// </summary>
    public void SetLabelsVisible(bool visible)
    {
        showLabels = visible;
        foreach (var kvp in labelObjects)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// Set label color
    /// </summary>
    public void SetLabelColor(Color color)
    {
        labelColor = color;
        foreach (var kvp in labelObjects)
        {
            if (kvp.Value != null)
            {
                var textMesh = kvp.Value.GetComponentInChildren<TextMesh>();
                if (textMesh != null && textMesh.name == "Text")
                {
                    textMesh.color = color;
                }
            }
        }
    }

    /// <summary>
    /// Auto-detect and label all balls in scene with PoolBall component
    /// </summary>
    public void AutoDetectBalls()
    {
        var poolBalls = FindObjectsByType<PoolBall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ball in poolBalls)
        {
            RegisterBall(ball.BallNumber, ball.transform);
        }
        Debug.Log($"[CueStrikeBallLabels] Auto-detected {poolBalls.Length} balls");
    }
}

/// <summary>
/// Simple Pool Ball component for identification
/// </summary>
public class PoolBall : MonoBehaviour
{
    [SerializeField] private int ballNumber = 1;

    public int BallNumber
    {
        get => ballNumber;
        set => ballNumber = Mathf.Clamp(value, 1, 15);
    }
}