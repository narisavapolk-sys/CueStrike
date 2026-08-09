using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
using System;
using SaveSystem = CueStrike.Gameplay.SaveSystem;
using CueStrike.Gameplay.SaveSystem;

namespace CueStrike.Gameplay.Practice
{
    /// <summary>
    /// VR Laser Pointer System for Custom Ball Placement.
    /// Allows players to use VR controllers to place balls on the table with laser precision.
    /// Integrates with SaveSystem for custom drill creation and persistence.
    /// </summary>
    public class CueStrikeLaserPlacementSystem : MonoBehaviour
    {
        [Header("XR Interaction")]
        [Tooltip("XR Ray Interactor for laser pointing")]
        public XRRayInteractor rayInteractor;

        [Tooltip("XR Direct Interactor for grabbing placed balls")]
        public XRDirectInteractor directInteractor;

        [Header("Visual Settings")]
        [Tooltip("Material for laser line renderer")]
        public Material laserMaterial;

        [Tooltip("Color when placement is valid")]
        public Color validPlacementColor = Color.green;

        [Tooltip("Color when placement is invalid")]
        public Color invalidPlacementColor = Color.red;

        [Tooltip("Color for preview ball")]
        public Color previewBallColor = new Color(0f, 1f, 0f, 0.5f);

        [Tooltip("Maximum ray distance")]
        public float maxRayDistance = 5f;

        [Header("Table Bounds")]
        [Tooltip("Table surface collider for raycasting")]
        public Collider tableSurface;

        [Tooltip("Table bounds for validation")]
        public Bounds tableBounds;

        [Header("Ball Settings")]
        [Tooltip("Ball radius for placement")]
        public float ballRadius = 0.028575f;

        [Tooltip("Available ball types for placement")]
        public BallTypeData[] ballTypes;

        [Header("Custom Drill Builder")]
        [Tooltip("Maximum balls in a custom drill")]
        public int maxBallsInDrill = 21;

        [Tooltip("Reference to Practice Manager")]
        public CueStrikePracticeManager practiceManager;

        // Properties for UI access
        public BallTypeData[] BallTypes => ballTypes;
        public int MaxBallsInDrill => maxBallsInDrill;
        public CueStrike.Gameplay.PracticeRoutine ActiveRoutine => practiceManager != null ? practiceManager.ActiveRoutine : CueStrike.Gameplay.PracticeRoutine.FreePlacement;
        public int TableType => practiceManager != null ? practiceManager.TableType : 0;

        // Events
        public event Action<SaveSystem.BallPositionData> OnBallPlaced;
        public event Action<SaveSystem.BallPositionData> OnBallRemoved;
        public event Action<SaveSystem.CustomDrillData> OnDrillSaved;
        public event Action OnDrillCleared;
        public event Action<bool> OnPlacementModeChanged;

        // State
        private enum PlacementMode
        {
            None,
            PlaceCueBall,
            PlaceObjectBall,
            EditExisting
        }

        private PlacementMode _currentMode = PlacementMode.None;
        private int _selectedBallTypeIndex = 0;
        private GameObject _previewBall;
        private LineRenderer _laserLine;
        private List<SaveSystem.BallPositionData> _placedBalls = new List<SaveSystem.BallPositionData>();
        private List<GameObject> _placedBallObjects = new List<GameObject>();
        private SaveSystem.BallPositionData _editingBall = null;
        private GameObject _editingBallObject = null;
        private bool _isPlacementValid = false;
        private Vector3 _lastValidPosition;
        private int _nextBallId = 1;

        // Components
        private XRRayInteractor _rayInteractor;
        private LineRenderer _lineRenderer;

        // Input actions (XRIT 3.x uses Input System)
        private UnityEngine.InputSystem.InputAction _selectAction;
        private UnityEngine.InputSystem.InputAction _uiPressAction;
        private UnityEngine.InputSystem.InputAction _moveAction;

        private void Awake()
        {
            InitializeComponents();
            SetupLaserVisual();
            SetupInputActions();
        }

        private void Start()
        {
            // Auto-find components if not assigned
            if (_rayInteractor == null)
                _rayInteractor = GetComponent<XRRayInteractor>();
            
            if (rayInteractor == null)
                rayInteractor = _rayInteractor;

            if (tableSurface == null)
            {
                var tableObj = GameObject.Find("Table") ?? GameObject.Find("SnookerTable") ?? GameObject.Find("PoolTable");
                if (tableObj != null)
                    tableSurface = tableObj.GetComponent<Collider>();
            }

            if (practiceManager == null)
                practiceManager = FindFirstObjectByType<CueStrikePracticeManager>();

            // Load existing custom drill if in custom builder mode
            if (practiceManager != null && practiceManager.ActiveRoutine == CueStrike.Gameplay.PracticeRoutine.CustomBuilder)
            {
                LoadCustomDrillFromSave();
            }
        }

        private void InitializeComponents()
        {
            _rayInteractor = rayInteractor;
        }

        private void SetupInputActions()
        {
            if (_rayInteractor == null) return;

            try
            {
                // In XRIT 3.x, input actions are handled by the ActionBasedController component
                // rather than directly on the interactor itself.
                var actionController = _rayInteractor.transform.GetComponent<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
                if (actionController != null)
                {
                    // InputActionProperty is a struct - check .action property directly (no null-conditional on struct)
                    _selectAction = actionController.selectAction.action;
                    _uiPressAction = actionController.uiPressAction.action;
                    _moveAction = actionController.translateAnchorAction.action; // XRIT 3.x translation equivalent
                }
                
                // Fallback: Check legacy XRController if needed
                if (_selectAction == null || _uiPressAction == null)
                {
                    var controller = _rayInteractor.transform.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRController>();
                    if (controller != null)
                    {
                        // In Unity InputDevice, the property is 'isValid' (lowercase i, not a method)
                        if (controller.inputDevice.isValid)
                        {
                            // Assign legacy defaults
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LaserPlacement] Could not setup input actions: {e.Message}");
            }
        }

        private void SetupLaserVisual()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = 0.005f;
            _lineRenderer.endWidth = 0.002f;
            _lineRenderer.material = laserMaterial ?? new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.enabled = false;
            _lineRenderer.numCapVertices = 4;
        }

        private void Update()
        {
            if (_currentMode == PlacementMode.None) return;

            UpdateLaserVisual();
            UpdatePreviewBall();
            HandleInput();
        }

        private void UpdateLaserVisual()
        {
            if (_rayInteractor == null || !_rayInteractor.enabled) return;

            _lineRenderer.enabled = true;

            // Get ray origin and direction from interactor
            Vector3 origin = _rayInteractor.transform.position;
            Vector3 direction = _rayInteractor.transform.forward;

            // Raycast against table surface
            Ray ray = new Ray(origin, direction);
            bool hitTable = false;
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.up;

            if (tableSurface != null && tableSurface.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                hitTable = true;
                hitPoint = hit.point;
                hitNormal = hit.normal;
                _isPlacementValid = IsPlacementValid(hitPoint);
            }
            else
            {
                // Check against table bounds
                if (tableBounds != default && tableBounds.IntersectRay(ray, out float distance))
                {
                    if (distance <= maxRayDistance)
                    {
                        hitTable = true;
                        hitPoint = ray.GetPoint(distance);
                        _isPlacementValid = IsPlacementValid(hitPoint);
                    }
                }
            }

            // Update laser line
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, hitTable ? hitPoint : origin + direction * maxRayDistance);
            _lineRenderer.material.color = _isPlacementValid ? validPlacementColor : invalidPlacementColor;

            if (hitTable && _isPlacementValid)
            {
                _lastValidPosition = hitPoint;
            }
        }

        private void UpdatePreviewBall()
        {
            if (_previewBall == null)
            {
                CreatePreviewBall();
            }

            if (_previewBall != null && _isPlacementValid)
            {
                _previewBall.transform.position = _lastValidPosition + Vector3.up * ballRadius;
                _previewBall.SetActive(true);
            }
            else if (_previewBall != null)
            {
                _previewBall.SetActive(false);
            }
        }

        private void CreatePreviewBall()
        {
            _previewBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _previewBall.name = "PreviewBall";
            _previewBall.transform.localScale = Vector3.one * ballRadius * 2f;
            Destroy(_previewBall.GetComponent<Collider>());

            var renderer = _previewBall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = previewBallColor;
                renderer.material.SetFloat("_Surface", 1); // Transparent
                renderer.material.renderQueue = 3000;
            }
        }

        private void HandleInput()
        {
            if (_rayInteractor == null) return;

            // Trigger pressed - place ball
            if (_selectAction != null && _selectAction.WasPressedThisFrame())
            {
                if (_isPlacementValid && _currentMode != PlacementMode.EditExisting)
                {
                    PlaceBall(_lastValidPosition);
                }
            }

            // Grip pressed - remove ball / cancel edit
            if (_uiPressAction != null && _uiPressAction.WasPressedThisFrame())
            {
                if (_currentMode == PlacementMode.EditExisting && _editingBallObject != null)
                {
                    CancelEditMode();
                }
                else
                {
                    TryRemoveBallAtRay();
                }
            }

            // Thumbstick / touchpad for ball type selection
            if (_moveAction != null)
            {
                Vector2 scroll = _moveAction.ReadValue<Vector2>();
                if (scroll.y != 0)
                {
                    CycleBallType(scroll.y > 0 ? 1 : -1);
                }
            }
            else
            {
                // Fallback: Use keyboard input for testing in editor
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                {
                    CycleBallType(1);
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                {
                    CycleBallType(-1);
                }
            }
        }

        private bool IsPlacementValid(Vector3 position)
        {
            // Check table bounds
            if (tableBounds != default && !tableBounds.Contains(position))
                return false;

            // Check ball radius from edges
            Bounds inflatedBounds = tableBounds;
            inflatedBounds.Expand(-ballRadius * 2f);
            if (!inflatedBounds.Contains(position))
                return false;

            // Check collision with existing balls
            foreach (var ballObj in _placedBallObjects)
            {
                if (ballObj != null && ballObj != _editingBallObject)
                {
                    float dist = Vector3.Distance(position, ballObj.transform.position);
                    if (dist < ballRadius * 2.1f) // Slightly more than diameter
                        return false;
                }
            }

            // Check max ball count
            if (_placedBalls.Count >= maxBallsInDrill && _currentMode != PlacementMode.EditExisting)
                return false;

            return true;
        }

        private void PlaceBall(Vector3 position)
        {
            var ballType = ballTypes.Length > 0 ? ballTypes[_selectedBallTypeIndex] : new BallTypeData();
            
            Vector3 posVec = position + Vector3.up * ballRadius;
            SaveSystem.BallPositionData ballData = new SaveSystem.BallPositionData
            {
                ballId = _nextBallId++,
                ballName = ballType.ballName,
                position = new SaveSystem.Vector3Serializable(posVec.x, posVec.y, posVec.z),
                velocity = new SaveSystem.Vector3Serializable(0f, 0f, 0f),
                isActive = true,
                isPocketed = false,
                pocketIndex = -1
            };

            // Create visual ball
            GameObject ballObj = CreateBallVisual(position, ballType, ballData.ballId, ballData.ballName);
            _placedBallObjects.Add(ballObj);
            _placedBalls.Add(ballData);

            OnBallPlaced?.Invoke(ballData);
            
            Debug.Log($"[LaserPlacement] Placed ball: {ballData.ballName} at {position}");
        }

        private GameObject CreateBallVisual(Vector3 position, BallTypeData ballType, int ballId, string ballName)
        {
            GameObject ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = $"PlacedBall_{ballName}_{ballId}";
            ballObj.transform.position = position + Vector3.up * ballRadius;
            ballObj.transform.localScale = Vector3.one * ballRadius * 2f;
            ballObj.tag = "Ball";

            var rb = ballObj.AddComponent<Rigidbody>();
            rb.mass = 0.17f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.useGravity = false;
            rb.isKinematic = true; // Static during placement

            var collider = ballObj.GetComponent<SphereCollider>();
            if (collider != null)
            {
                var mat = new PhysicsMaterial
                {
                    bounciness = 0.85f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
                collider.material = mat;
            }

            var renderer = ballObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = ballType.ballColor;
            }

            // Add ball identifier
            var ballComp = ballObj.AddComponent<CueStrikeBall>();
            ballComp.BallId = ballId;
            ballComp.BallName = ballName;
            ballComp.Type = CueStrikeBall.BallType.ObjectBall;

            // Make interactable for XR grabbing
            var interactable = ballObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            interactable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;
            interactable.selectEntered.AddListener(OnBallGrabbed);
            interactable.selectExited.AddListener(OnBallReleased);

            return ballObj;
        }

        private void OnBallGrabbed(SelectEnterEventArgs args)
        {
            if (_currentMode == PlacementMode.None)
            {
                EnterEditMode(args.interactableObject.transform.gameObject);
            }
        }

        private void OnBallReleased(SelectExitEventArgs args)
        {
            if (_currentMode == PlacementMode.EditExisting && args.interactableObject.transform.gameObject == _editingBallObject)
            {
                // Update ball position in data
                UpdateEditingBallPosition();
                ExitEditMode();
            }
        }

        private void EnterEditMode(GameObject ballObj)
        {
            _currentMode = PlacementMode.EditExisting;
            _editingBallObject = ballObj;
            
            // Find the ball data
            int index = _placedBallObjects.IndexOf(ballObj);
            if (index >= 0 && index < _placedBalls.Count)
            {
                _editingBall = _placedBalls[index];
            }

            // Make ball kinematic for editing
            var rb = ballObj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Highlight ball
            var renderer = ballObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }

            OnPlacementModeChanged?.Invoke(true);
        }

        private void CancelEditMode()
        {
            if (_editingBallObject != null)
            {
                var renderer = _editingBallObject.GetComponent<Renderer>();
                if (renderer != null && _editingBall != null)
                {
                    var ballType = Array.Find(ballTypes, b => b.ballName == _editingBall.ballName);
                    renderer.material.color = ballType != null ? ballType.ballColor : Color.white;
                }

                var rb = _editingBallObject.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
            }

            _currentMode = PlacementMode.None;
            _editingBall = null;
            _editingBallObject = null;
            OnPlacementModeChanged?.Invoke(false);
        }

        private void ExitEditMode()
        {
            CancelEditMode();
        }

        private void UpdateEditingBallPosition()
        {
            if (_editingBall == null || _editingBallObject == null) return;

            Vector3 newPos = _editingBallObject.transform.position;
            _editingBall.position = new SaveSystem.Vector3Serializable(newPos.x, newPos.y, newPos.z);

            // Update in save system if it's a custom drill
            if (practiceManager != null && practiceManager.ActiveRoutine == CueStrike.Gameplay.PracticeRoutine.CustomBuilder)
            {
                // Will be saved when drill is saved
            }
        }

        private void TryRemoveBallAtRay()
        {
            if (_rayInteractor == null) return;

            Ray ray = new Ray(_rayInteractor.transform.position, _rayInteractor.transform.forward);
            
            foreach (var ballObj in _placedBallObjects)
            {
                if (ballObj == null) continue;
                
                var collider = ballObj.GetComponent<Collider>();
                if (collider != null && collider.Raycast(ray, out RaycastHit hit, maxRayDistance))
                {
                    RemoveBall(ballObj);
                    return;
                }
            }
        }

        private void RemoveBall(GameObject ballObj)
        {
            int index = _placedBallObjects.IndexOf(ballObj);
            if (index >= 0)
            {
                SaveSystem.BallPositionData removedBall = _placedBalls[index];
                _placedBalls.RemoveAt(index);
                _placedBallObjects.RemoveAt(index);
                Destroy(ballObj);
                
                OnBallRemoved?.Invoke(removedBall);
                Debug.Log($"[LaserPlacement] Removed ball: {removedBall.ballName}");
            }
        }

        private void CycleBallType(int direction)
        {
            if (ballTypes.Length == 0) return;
            
            _selectedBallTypeIndex = (_selectedBallTypeIndex + direction + ballTypes.Length) % ballTypes.Length;
            Debug.Log($"[LaserPlacement] Selected ball type: {ballTypes[_selectedBallTypeIndex].ballName}");
        }

        // Public API
        public void EnterPlacementMode(bool placeCueBall = false)
        {
            _currentMode = placeCueBall ? PlacementMode.PlaceCueBall : PlacementMode.PlaceObjectBall;
            OnPlacementModeChanged?.Invoke(true);
            
            if (_lineRenderer != null) _lineRenderer.enabled = true;
        }

        public void ExitPlacementMode()
        {
            _currentMode = PlacementMode.None;
            
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            if (_previewBall != null) _previewBall.SetActive(false);
            
            OnPlacementModeChanged?.Invoke(false);
        }

        public void SetSelectedBallType(int index)
        {
            if (index >= 0 && index < ballTypes.Length)
            {
                _selectedBallTypeIndex = index;
            }
        }

        public void ClearAllBalls()
        {
            foreach (var ball in _placedBallObjects)
            {
                if (ball != null) Destroy(ball);
            }
            _placedBallObjects.Clear();
            _placedBalls.Clear();
            _nextBallId = 1;
            
            OnDrillCleared?.Invoke();
        }

        public List<SaveSystem.BallPositionData> GetPlacedBalls()
        {
            return new List<SaveSystem.BallPositionData>(_placedBalls);
        }

        public SaveSystem.CustomDrillData CreateCustomDrill(string name, string description, int tableType, SaveSystem.DrillSettingsData settings = null)
        {
            var drill = new SaveSystem.CustomDrillData
            {
                drillId = System.Guid.NewGuid().ToString(),
                drillName = name,
                description = description,
                authorProfileId = CueStrikeSaveSystemIntegration.GetActiveProfile()?.profileId ?? "local",
                authorName = CueStrikeSaveSystemIntegration.GetActiveProfile()?.profileName ?? "Local Player",
                createdTimestamp = System.DateTime.UtcNow.ToString("o"),
                tableType = tableType,
                ballPositions = new List<SaveSystem.BallPositionData>(_placedBalls),
                settings = settings ?? new SaveSystem.DrillSettingsData(),
                stats = new SaveSystem.DrillStatsData()
                {
                    timesPlayed = 0,
                    timesCompleted = 0,
                    globalBestScore = 0,
                    globalBestTimeSeconds = float.MaxValue,
                    averageScore = 0f,
                    averageTimeSeconds = 0f
                },
                isPublic = false
            };

            // Save to system
            var savedDrill = CueStrikeSaveSystemIntegration.SaveCustomDrill(drill);
            OnDrillSaved?.Invoke(savedDrill);
            
            return savedDrill;
        }

        public void LoadCustomDrill(SaveSystem.CustomDrillData drill)
        {
            ClearAllBalls();
            
            foreach (var ballData in drill.ballPositions)
            {
                if (ballData.isActive && !ballData.isPocketed)
                {
                    GameObject ballObj = CreateBallVisual(
                        ballData.position.ToVector3(), 
                        GetBallTypeByName(ballData.ballName), 
                        ballData.ballId, 
                        ballData.ballName
                    );
                    
                    _placedBallObjects.Add(ballObj);
                    _placedBalls.Add(ballData);
                    
                    if (ballData.ballId >= _nextBallId)
                        _nextBallId = ballData.ballId + 1;
                }
            }
            
            Debug.Log($"[LaserPlacement] Loaded custom drill: {drill.drillName} with {_placedBalls.Count} balls");
        }

        private void LoadCustomDrillFromSave()
        {
            var recentDrills = CueStrikeSaveSystemIntegration.GetRecentDrills();
            if (recentDrills.Count > 0)
            {
                var lastDrill = CueStrikeSaveSystemIntegration.GetCustomDrill(recentDrills[0]);
                if (lastDrill != null)
                {
                    LoadCustomDrill(lastDrill);
                }
            }
        }

        private BallTypeData GetBallTypeByName(string name)
        {
            foreach (var bt in ballTypes)
            {
                if (bt.ballName == name) return bt;
            }
            return ballTypes.Length > 0 ? ballTypes[0] : new BallTypeData();
        }

        private void OnDestroy()
        {
            ClearAllBalls();
            if (_previewBall != null) Destroy(_previewBall);
            if (_lineRenderer != null) Destroy(_lineRenderer);
        }
    }

    /// <summary>
    /// Ball type definition for placement system.
    /// </summary>
    [Serializable]
    public class BallTypeData
    {
        public string ballName = "Red";
        public Color ballColor = Color.red;
        public int ballId = 1;
        public bool isCueBall = false;
    }

}