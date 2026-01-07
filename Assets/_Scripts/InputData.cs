using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace _Scripts
{
    /// <summary>
    /// 增强版 InputData - 带诊断和自动重试
    /// </summary>
    public class InputData : MonoBehaviour
    {
        [Header("XR Devices")]
        public InputDevice RightController;
        public InputDevice LeftController;
        public InputDevice Hmd;

        [Header("Button States")]
        private bool leftTriggerPressed { get; set; }
        private bool rightTriggerPressed { get; set; }
        public bool anyTriggerDown { get; private set; }

        [Header("Auto Retry")]
        [SerializeField] private bool enableAutoRetry = true;
        [SerializeField] private float retryInterval = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private bool _lastAnyTriggerPressed = false;
        private float _retryTimer = 0f;
        private bool _hasLoggedSuccess = false;

        void Start()
        {
            InitializeInputDevices();
            
            if (showDebug)
            {
                LogDeviceStatus();
            }
        }

        void Update()
        {
            if (enableAutoRetry && (!RightController.isValid || !LeftController.isValid || !Hmd.isValid))
            {
                _retryTimer += Time.deltaTime;
                if (_retryTimer >= retryInterval)
                {
                    _retryTimer = 0f;
                    
                    InitializeInputDevices();
                }
            }

            UpdateButtonStates();
            
            if (!_hasLoggedSuccess && RightController.isValid && LeftController.isValid)
            {
                _hasLoggedSuccess = true;
            }
        }

        private void InitializeInputDevices()
        {
            if (!RightController.isValid)
                InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, ref RightController, "Right");
            if (!LeftController.isValid)
                InitializeInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, ref LeftController, "Left");
            if (!Hmd.isValid)
                InitializeInputDevice(InputDeviceCharacteristics.HeadMounted, ref Hmd, "HMD");
        }

        private void InitializeInputDevice(InputDeviceCharacteristics inputCharacteristics, ref InputDevice inputDevice, string deviceName)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(inputCharacteristics, devices);

            if (devices.Count > 0)
            {
                inputDevice = devices[0];
                
                if (showDebug)
                    Debug.Log($"[InputData] {deviceName} controller found: {inputDevice.name}");
            }
            else
            {
                if (showDebug)
                    Debug.LogWarning($"[InputData] {deviceName} controller not found!");
            }
        }

        private void UpdateButtonStates()
        {
            bool leftPressedNow = false;
            bool rightPressedNow = false;

            if (LeftController.isValid &&
                LeftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftPressed))
            {
                leftPressedNow = leftPressed;
            }

            if (RightController.isValid &&
                RightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightPressed))
            {
                rightPressedNow = rightPressed;
            }

            leftTriggerPressed = leftPressedNow;
            rightTriggerPressed = rightPressedNow;

            bool anyNow = leftPressedNow || rightPressedNow;
            anyTriggerDown = anyNow && !_lastAnyTriggerPressed;
            _lastAnyTriggerPressed = anyNow;
        }

        private void LogDeviceStatus()
        {
            Debug.Log($"[InputData] Device Status:\n" +
                      $"Right Controller: {(RightController.isValid ? "Valid" : "NotValid")} {RightController.name}\n" +
                      $"Left Controller: {(LeftController.isValid ? "Valid" : "NotValid")} {LeftController.name}\n" +
                      $"HMD: {(Hmd.isValid ? "Valid" : "NotValid")} {Hmd.name}");
        }

        [ContextMenu("Force Reinitialize")]
        public void ForceReinitialize()
        {
            RightController = new InputDevice();
            LeftController = new InputDevice();
            Hmd = new InputDevice();
            _hasLoggedSuccess = false;
            InitializeInputDevices();
            LogDeviceStatus();
        }

        [ContextMenu("Log Device Status")]
        public void LogStatus()
        {
            LogDeviceStatus();
        }
    }
}