using UnityEngine;

namespace _Scripts.BreathGuideSystem
{
    /// <summary>
    /// Breathing guidance beam segment with simplified distance-based detection.
    /// - Uses distance calculation to detect ANY controller proximity
    /// - While inside range: play hit particles + send haptics
    /// - Auto-completes when progress reaches threshold
    /// </summary>
    public class BeamSegment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject tipBall;
        [SerializeField] private GameObject tailBall;
        [SerializeField] private int lineSegments = 20;
        [SerializeField] private ParticleSystem hitParticles;

        [Header("Beam Flow Settings")]
        [SerializeField] private float beamFlowSpeed = 2f;
        [SerializeField] private float breathDuration = 4f;

        [Header("Control Point Settings")]
        [SerializeField] private float cp2PathSpeed = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float cp1FollowRatio = 0.8f;
        [SerializeField] private float cp1SmoothTime = 0.08f;

        [Header("Lifecycle")]
        [SerializeField] private float flyAwaySpeed = 6f;
        [SerializeField] private float flyAwayDuration = 1.2f;
        [SerializeField] private float extraLifetime = 1.0f;
        [SerializeField] private float autoCompleteAtProgress = 0.9f; 

        [Header("VFX")]
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Haptic Feedback")]
        [SerializeField] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticInterval = 0.1f;

        [Header("Distance Detection")]
        [SerializeField] private float detectionRadius = 0.15f;
        [SerializeField] private int detectionSamplePoints = 10;
        [SerializeField] private float distanceCheckInterval = 0.05f;
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private float chestInnerRatio = 0.1f;

        private static System.Action<bool> OnBeamCompleted;

        private MovementType movementType { get; set; }
        private bool isLeftHand { get; set; }

        // Control Points (World Space)
        private Vector3 _cp1Position;
        private Vector3 _cp2Position;
        private Vector3 _cp1Velocity;

        // Path Configuration
        private Vector3 _spawnWorldPos;
        private Vector3 _forwardDir;

        // Movement Tracking
        private float _elapsed;
        private float _beamTravelDistance;
        
        private bool _isTouching;  
        private float _hapticTimer;
        private float _distanceCheckTimer;

        // Lifecycle
        private bool _flyAwayStarted;
        private float _flyAwayStartTime;
        private bool _isDestroyed;
        private bool _hasCompleted; 

        // Ring / path
        private Vector3 _ringCenter;
        private float _ringRadius;
        private Vector3 _ringRight;
        void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = Mathf.Max(2, lineSegments);
                lineRenderer.useWorldSpace = true;
            }
        }

        void Start()
        {
            FindControllerTransforms();

            if (leftHandTransform == null || rightHandTransform == null)
            {
                Debug.LogWarning("[BeamSegment] Controller transforms not found! Please assign manually or check controller names.");
            }
        }

        public void Init(
            Vector3 forwardDirWorld,
            MovementType type,
            bool isLeft,
            Vector3 ringCenter,
            float ringRadius,
            Vector3 ringRightWorld,
            float phaseDuration
        )
        {
            _spawnWorldPos = transform.position;
            _forwardDir = forwardDirWorld.normalized;
            movementType = type;
            isLeftHand = isLeft;
            _ringCenter = ringCenter;
            _ringRadius = ringRadius;
            _ringRight = ringRightWorld.normalized;
            breathDuration = Mathf.Max(0.01f, phaseDuration);

            // Reset runtime state
            _elapsed = 0f;
            _beamTravelDistance = 0f;
            _flyAwayStarted = false;
            _flyAwayStartTime = 0f;
            _isDestroyed = false;

            // Reset detection state - 简化
            _isTouching = false;
            _hapticTimer = 0f;
            _distanceCheckTimer = 0f;
            _hasCompleted = false;

            // Init CPs at t=0
            Vector3 p0 = PathCalculator.GetCP2OnPathXY(
                _ringCenter,
                _ringRadius,
                movementType,
                isLeftHand,
                0f,
                _ringRight,
                chestInnerRatio
            );
            _cp2Position = p0;
            _cp1Position = p0;
            _cp1Velocity = Vector3.zero;

            // Ensure particles off initially
            if (hitParticles != null)
                hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void Update()
        {
            if (_isDestroyed) return;

            _elapsed += Time.deltaTime;

            UpdateBeamFlow();
            UpdateControlPoints();
            UpdateLineRenderer();

            if (tipBall != null) tipBall.transform.position = _cp1Position;
            if (tailBall != null) tailBall.transform.position = _cp2Position;

            // Distance detection
            if (!_hasCompleted)
            {
                UpdateDistanceDetection();
                UpdateTouchFeedback();
            }

            // Auto completion check
            CheckAutoCompletion();

            ManageLifecycle();
        }

        void UpdateBeamFlow()
        {
            if (!_flyAwayStarted)
            {
                _beamTravelDistance += beamFlowSpeed * Time.deltaTime;
            }
            else
            {
                float flyT = (_elapsed - _flyAwayStartTime) / Mathf.Max(0.01f, flyAwayDuration);
                float flySpeed = Mathf.Lerp(beamFlowSpeed, flyAwaySpeed, flyT);
                _beamTravelDistance += flySpeed * Time.deltaTime;
            }
        }

        void UpdateControlPoints()
        {
            float t = Mathf.Clamp01(_elapsed / breathDuration * cp2PathSpeed);
            t = Mathf.SmoothStep(0f, 1f, t);
            // Path of Control Point 2 
            Vector3 cp2OnPath = PathCalculator.GetCP2OnPathXY(
                _ringCenter, _ringRadius, movementType, isLeftHand, t, _ringRight, chestInnerRatio);
            _cp2Position = cp2OnPath + _forwardDir * (_beamTravelDistance * 0.2f);

            // Smoothly follow CP2
            float tLead = Mathf.Clamp01(t * cp1FollowRatio);
            
            // Path of Control Point 1
            Vector3 cp1OnPath = PathCalculator.GetCP2OnPathXY(
                _ringCenter, _ringRadius, movementType, isLeftHand, tLead, _ringRight, chestInnerRatio);
            // Move Control Point 1 towards target smoothly
            Vector3 cp1Target = cp1OnPath + _forwardDir * _beamTravelDistance;
            _cp1Position = Vector3.SmoothDamp(
                _cp1Position,
                cp1Target,
                ref _cp1Velocity,
                cp1SmoothTime
            );
        }

        void UpdateLineRenderer()
        {
            if (lineRenderer == null) return;

            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.15f;

            int seg = Mathf.Max(2, lineSegments);
            if (lineRenderer.positionCount != seg)
                lineRenderer.positionCount = seg;

            Vector3 midControl = (_cp2Position + _cp1Position) * 0.5f;

            if (movementType == MovementType.HorizontalOpen ||
                movementType == MovementType.HorizontalClose)
            {
                midControl += Vector3.up * 0.1f;
            }

            for (int i = 0; i < seg; i++)
            {
                float t = (float)i / (seg - 1);
                Vector3 point = PathCalculator.QuadraticBezier(_cp2Position, midControl, _cp1Position, t);
                lineRenderer.SetPosition(i, point);
            }
        }

        #region Distance Detection

        void FindControllerTransforms()
        {
            // Try multiple common VR controller naming patterns
            string[] leftHandNames = new string[] 
            { 
                "LeftHand Controller", 
                "Left Controller", 
                "LeftHandAnchor", 
                "LeftControllerAnchor",
                "Left Hand",
                "XR_Left",
                "LeftHand"
            };

            string[] rightHandNames = new string[] 
            { 
                "RightHand Controller", 
                "Right Controller", 
                "RightHandAnchor", 
                "RightControllerAnchor",
                "Right Hand",
                "XR_Right",
                "RightHand"
            };

            // Search for left hand
            if (leftHandTransform == null)
            {
                foreach (string name in leftHandNames)
                {
                    GameObject found = GameObject.Find(name);
                    if (found != null)
                    {
                        leftHandTransform = found.transform;
                        break;
                    }
                }
            }

            // Search for right hand
            if (rightHandTransform == null)
            {
                foreach (string name in rightHandNames)
                {
                    GameObject found = GameObject.Find(name);
                    if (found != null)
                    {
                        rightHandTransform = found.transform;
                        break;
                    }
                }
            }
        }

        void UpdateDistanceDetection()
        {
            _distanceCheckTimer += Time.deltaTime;

            if (_distanceCheckTimer >= distanceCheckInterval)
            {
                _distanceCheckTimer = 0f;

                bool anyHandInRange = false;

                // check if in range
                if (leftHandTransform != null)
                {
                    if (IsControllerNearBeam(leftHandTransform.position))
                    {
                        anyHandInRange = true;
                    }
                }

                if (rightHandTransform != null)
                {
                    if (IsControllerNearBeam(rightHandTransform.position))
                    {
                        anyHandInRange = true;
                    }
                }

                // 处理状态变化
                if (anyHandInRange && !_isTouching)
                {
                    // Enter Range
                    HandleDistanceEnter();
                }
                else if (!anyHandInRange && _isTouching)
                {
                    // Exit Range
                    HandleDistanceExit();
                }

                _isTouching = anyHandInRange;
            }
        }

        bool IsControllerNearBeam(Vector3 controllerPosition)
        {
            // Check distance to multiple sample points along the beam
            Vector3 midControl = (_cp2Position + _cp1Position) * 0.5f;

            if (movementType == MovementType.HorizontalOpen ||
                movementType == MovementType.HorizontalClose)
            {
                midControl += Vector3.up * 0.1f;
            }

            float minDistance = float.MaxValue;

            for (int i = 0; i < detectionSamplePoints; i++)
            {
                float t = (float)i / (detectionSamplePoints - 1);
                Vector3 beamPoint = PathCalculator.QuadraticBezier(_cp2Position, midControl, _cp1Position, t);
                
                float distance = Vector3.Distance(controllerPosition, beamPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance <= detectionRadius;
        }

        void HandleDistanceEnter()
        {
            if (_isDestroyed) return;

            _isTouching = true;
            _hapticTimer = 0f;
            StartVSFeedback();
        }

        void HandleDistanceExit()
        {
            if (_isDestroyed) return;

            _isTouching = false;
            StopVSFeedbackIfNoTouching();
        }

        #endregion

        #region Touch Feedback

        void UpdateTouchFeedback()
        {
            if (_isTouching)
            {
                _hapticTimer += Time.deltaTime;
                if (_hapticTimer >= hapticInterval)
                {
                    HandTracker.instance?.TriggerHaptic(isLeftHand, hapticAmplitude, hapticInterval);
                    _hapticTimer = 0f;
                }
            }
        }

        void StartVSFeedback()
        {
            if (hitParticles != null && !hitParticles.isPlaying)
                hitParticles.Play();
            AudioManager.instance.Play("breath_hit");
        }
        void StopVSFeedbackIfNoTouching()
        {
            if (hitParticles == null) return;
            if (_isTouching) return;
            hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        
        #endregion

        #region Auto Completion

        void CheckAutoCompletion()
        {
            if (_hasCompleted) return;
            
            if (GetProgress() >= autoCompleteAtProgress)
            {
                _hasCompleted = true;

                // Stop touch feedback
                _isTouching = false;
                StopVSFeedbackIfNoTouching();

                // Notify listeners
                OnBeamCompleted?.Invoke(isLeftHand);

                // Start fly away
                if (!_flyAwayStarted)
                {
                    _flyAwayStarted = true;
                    _flyAwayStartTime = _elapsed;
                }
            }
        }

        private float GetProgress()
        {
            return Mathf.Clamp01(_elapsed / breathDuration);
        }
        
        #endregion

        #region LifeCycle Management

        void ManageLifecycle()
        {
            if (!_flyAwayStarted && _elapsed >= breathDuration)
            {
                _flyAwayStarted = true;
                _flyAwayStartTime = _elapsed;

            }

            if (_elapsed > breathDuration + flyAwayDuration + extraLifetime)
                DestroySegment();
        }

        void DestroySegment()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            Destroy(gameObject);
        }

        #endregion
        
        #region Getters

        public float GetBeamFlowSpeed() => beamFlowSpeed;
        
        #endregion
    }
}