// -------------------------------------------------------------------------------
// MRTK - MagicLeap
// https://github.com/magicleap/MRTK-MagicLeap
// -------------------------------------------------------------------------------
//
// MIT License
//
// Copyright(c) 2021 Magic Leap, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// -------------------------------------------------------------------------------
//
// Note code inspired from https://github.com/provencher/MRTK-MagicLeap
//


using System;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
using UnityEngine.XR.MagicLeap;
#endif

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    // Added inside of the scope to prevent conflicts with Unity's 2020.3 Version Control package.
    using Microsoft.MixedReality.Toolkit;

    
    [MixedRealityController(SupportedControllerType.GenericUnity,
        new[] { Handedness.Left, Handedness.Right })]
    public class MagicLeapMRTKController : BaseController, IMixedRealityHapticFeedback
    {
        InputDevice mlController;

        public MagicLeapMRTKController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null) : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

#endif

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
        public MagicLeapMRTKController(InputDevice controller, TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null) : base(trackingState, controllerHandedness, inputSource, interactions)
        {
            mlController = controller;
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();
            controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

            controllerActions.TouchpadPosition.performed += HandleOnTouchpadDownPerformed;
            controllerActions.TouchpadPosition.canceled += HandleOnTouchpadDownCanceled;

            controllerActions.Bumper.started += MLControllerBumperDown;
            controllerActions.Bumper.canceled += MLControllerBumperUp;

            controllerActions.Trigger.started += MLControllerTriggerDown;
            controllerActions.Trigger.canceled += MLControllerTriggerUp;

            controllerActions.Menu.started += MLControllerMenuDown;
            controllerActions.Menu.canceled += MLControllerMenuUp;
         
            // Get the position and rotation
            IsPositionAvailable = true;
            IsPositionApproximate = true;
            IsRotationAvailable = true;

        }
#endif

        public void CleanupController()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            controllerActions.TouchpadPosition.performed -= HandleOnTouchpadDownPerformed;
            controllerActions.TouchpadPosition.canceled -= HandleOnTouchpadDownCanceled;

            controllerActions.Bumper.started -= MLControllerBumperDown;
            controllerActions.Bumper.canceled -= MLControllerBumperUp;

            controllerActions.Trigger.started -= MLControllerTriggerDown;
            controllerActions.Trigger.canceled -= MLControllerTriggerUp;

            controllerActions.Menu.started -= MLControllerMenuDown;
            controllerActions.Menu.canceled -= MLControllerMenuUp;
            mlInputs.Dispose();
#endif
        }

        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer),
            new MixedRealityInteractionMapping(1, "Select", AxisType.Digital, DeviceInputType.ButtonPress),
            new MixedRealityInteractionMapping(2, "Bumper Press", AxisType.Digital, DeviceInputType.ButtonPress),
            new MixedRealityInteractionMapping(3, "Menu", AxisType.Digital, DeviceInputType.ButtonPress),
            new MixedRealityInteractionMapping(4, "Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch),
            new MixedRealityInteractionMapping(5, "Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad),
            new MixedRealityInteractionMapping(6, "Touchpad Press", AxisType.SingleAxis, DeviceInputType.TouchpadPress),
        };

        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer),
            new MixedRealityInteractionMapping(1, "Select", AxisType.Digital, DeviceInputType.ButtonPress),
            new MixedRealityInteractionMapping(2, "Bumper Press", AxisType.Digital, DeviceInputType.ButtonPress),
            new MixedRealityInteractionMapping(3, "Menu", AxisType.Digital, DeviceInputType.ButtonPress),
            new MixedRealityInteractionMapping(4, "Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch),
            new MixedRealityInteractionMapping(5, "Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad),
            new MixedRealityInteractionMapping(6, "Touchpad Press", AxisType.SingleAxis, DeviceInputType.TouchpadPress),
        };

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
        public override bool IsInPointingPose
        {
            get
            {
                return true;
            }
        }

        private Vector3 _lastTouchVector;

        private float _lastPressure;

        public void UpdatePoses()
        {
            bool isPositionAvailable = mlController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 position);
            bool isRotationAvailable = mlController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

            MixedRealityPose pointerPose = new MixedRealityPose(MixedRealityPlayspace.TransformPoint(position),
                MixedRealityPlayspace.Rotation * rotation);

            Interactions[0].PoseData = pointerPose;
            CoreServices.InputSystem?.RaiseSourcePoseChanged(InputSource, this, pointerPose);
            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[0].MixedRealityInputAction, pointerPose);
         
           
        }

        private void HandleOnTouchpadDownPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            HandleOnTouchpadDown(obj, true);
        }

        private void HandleOnTouchpadDownCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            HandleOnTouchpadDown(obj, false);
        }

        private void HandleOnTouchpadDown(UnityEngine.InputSystem.InputAction.CallbackContext obj, bool touchActive)
        {

            //This is also a good time to implement the Touchpad if you want to update that source type
            if (Interactions.Length > 4)
            {
                IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;

                // Test out touch
                Interactions[4].BoolData = touchActive;

                if (Interactions[4].Changed)
                {
                    if (touchActive)
                    {
                        inputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[4].MixedRealityInputAction);
                    }
                    else
                    {
                        inputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[4].MixedRealityInputAction);
                    }
                }

                if (touchActive)
                {
                    _lastTouchVector = controllerActions.TouchpadPosition.ReadValue<Vector2>();
                    _lastPressure = controllerActions.TouchpadForce.ReadValue<float>();

                    Interactions[5].Vector2Data = _lastTouchVector;
                    Interactions[6].FloatData = _lastPressure;

                    if (Interactions[5].Changed)
                    {
                        inputSystem?.RaisePositionInputChanged(InputSource, ControllerHandedness, Interactions[5].MixedRealityInputAction, Interactions[5].Vector2Data);
                        // There is no press without a position, therefore, they're nested. Opposite not true (press without a position)
                        if (Interactions[6].Changed) // Pressure was last down
                        {
                            inputSystem?.RaiseFloatInputChanged(InputSource, ControllerHandedness, Interactions[6].MixedRealityInputAction, Interactions[6].FloatData);
                        }
                    }

                }
                else if (Interactions[6].FloatData > 0)
                {
                    _lastPressure = 0;
                    Interactions[6].FloatData = _lastPressure;
                }
            }
        }

        void MLControllerBumperDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;
            Interactions[2].BoolData = true;
            inputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[2].MixedRealityInputAction);
        }
        void MLControllerBumperUp(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;
            Interactions[2].BoolData = false;
            inputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[2].MixedRealityInputAction);
        }

        void MLControllerMenuDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;
            Interactions[3].BoolData = true;
            inputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[3].MixedRealityInputAction);
        }

        void MLControllerMenuUp(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;
            Interactions[3].BoolData = false;
            inputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[3].MixedRealityInputAction);
        }

        void MLControllerTriggerDown(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;
            Interactions[1].BoolData = true;
            inputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[1].MixedRealityInputAction);
        }

        void MLControllerTriggerUp(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            IMixedRealityInputSystem inputSystem = CoreServices.InputSystem;
            Interactions[1].BoolData = false;
            inputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[1].MixedRealityInputAction);
        }

#endif // PLATFORM_RELISH

        /// <summary>
        /// Default Haptics. No customization, intensity and duration are not taken into consideration.
        /// </summary>
        public bool StartHapticImpulse(float intensity, float durationInSeconds = Single.MaxValue)
        {

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            Handheld.Vibrate();
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Starts a buzz haptics pattern.
        /// </summary>
        /// <param name="startHz">Start frequency of the buzz command (0 - 1250).</param>
        /// <param name="endHz">End frequency of the buzz command (0 - 1250).</param>
        /// <param name="durationMs">Duration of the buzz command in milliseconds (ms).</param>
        /// <param name="amplitude">Amplitude of the buzz command, as a percentage (0 - 100).</param>
        public bool StartHapticImpulse(ushort startHz, ushort endHz, ushort durationMs, byte amplitude)
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            MLResult result = InputSubsystem.Extensions.Haptics.StartBuzz(startHz, endHz, durationMs, amplitude);

            return result.IsOk;
#else
            return false;
#endif
        }

        /// <summary>
        /// Starts a pre-defined haptics pattern.
        /// </summary>
        /// <param name="preDefinedType">Pre-defined pattern to be played.</param>
        public bool StartHapticImpulse(InputSubsystem.Extensions.Haptics.PreDefined.Type preDefinedType)
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            MLResult result = InputSubsystem.Extensions.Haptics.StartPreDefined(preDefinedType);

            return result.IsOk;
#else
            return false;
#endif
        }

        /// <summary>
        /// Starts a custom haptic pattern.
        /// </summary>
        /// <param name="customPattern">A custom haptics pattern can be played by combining Buzz haptic commands and/or pre-defined patterns. PreDefined.Create and Buzz.Create can be used and then added to the customPattern.</param>
        public bool StartHapticImpulse(InputSubsystem.Extensions.Haptics.CustomPattern customPattern)
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            MLResult result = customPattern.StartHaptics();

            return result.IsOk;
#else
            return false;
#endif
        }

        public void StopHapticFeedback()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            MLResult result = InputSubsystem.Extensions.Haptics.Stop();

            if(!result.IsOk)
            {
                Debug.LogWarning("MagicLeapMRTKController failed to Stop Haptics with result: " + result.ToString());
            }
#endif
        }
    }
}

