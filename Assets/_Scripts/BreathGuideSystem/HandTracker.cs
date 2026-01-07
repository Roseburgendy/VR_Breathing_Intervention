using UnityEngine;
using UnityEngine.XR;

namespace _Scripts.BreathGuideSystem
{
    /// <summary>
    /// Singleton class for tracking VR controller/hand positions
    /// Provides easy access to hand positions throughout the system
    /// </summary>
    [RequireComponent(typeof(InputData))]
    public class HandTracker : MonoBehaviour
    {
        public static HandTracker instance { get; private set; }
    
        [Header("Hand Positions")]
        private Vector3 leftHandPosition { get; set; }
        private Vector3 rightHandPosition { get; set; }
    
        [Header("Controllers")]
    
        private bool leftControllerValid { get; set; }
        private bool rightControllerValid { get; set; }
    
        private InputData _inputData;
        // Controller devices
    
        void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
    
        void Start()
        {
            _inputData = GetComponent<InputData>();
        }
    
        void Update()
        {
            UpdateHandPositions();
        }
        
        /// <summary>
        /// Update hand positions from controllers
        /// </summary>
        void UpdateHandPositions()
        {
            // Try to get left hand position
            if (_inputData.LeftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos))
            {
                leftHandPosition = leftPos;
                leftControllerValid = true;
            }
            else
            {
                leftControllerValid = false;
            }
        
            // Try to get right hand position
            if (_inputData.RightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos))
            {
                rightHandPosition = rightPos;
                rightControllerValid = true;
            }
            else
            {
                rightControllerValid = false;
            }
        }
    
        /// <summary>
        /// Trigger haptic feedback on a controller
        /// </summary>
        /// <param name="isLeftHand">True for left hand, false for right</param>
        /// <param name="intensity">Vibration intensity (0-1)</param>
        /// <param name="duration">Duration in seconds</param>
        public void TriggerHaptic(bool isLeftHand, float intensity, float duration)
        {
            InputDevice controller = isLeftHand ? _inputData.LeftController : _inputData.RightController;
        
            if (controller.isValid)
            {
                controller.SendHapticImpulse(0, intensity, duration);
            }
        }
    
        /// <summary>
        /// Get distance from either hand to a point
        /// </summary>
        /// <returns>Tuple of (leftDistance, rightDistance)</returns>
        public (float left, float right) GetDistanceToPoint(Vector3 point)
        {
            float leftDist = Vector3.Distance(leftHandPosition, point);
            float rightDist = Vector3.Distance(rightHandPosition, point);
            return (leftDist, rightDist);
        }
        
    }
}