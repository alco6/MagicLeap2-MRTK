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

//Prevents Code From compiling if MRTK is not installed.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    // Added inside of the scope to prevent conflicts with Unity's 2020.3 Version Control package.
    using Microsoft.MixedReality.Toolkit;

    [MixedRealityController(SupportedControllerType.ArticulatedHand,
        new[] { Handedness.Left, Handedness.Right },
        flags: MixedRealityControllerConfigurationFlags.UseCustomInteractionMappings)]
    public class MagicLeapHand : BaseHand
    {

        public MagicLeapHand(
            TrackingState trackingState,
            Handedness controllerHandedness,
            IMixedRealityInputSource inputSource = null,
            MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions,
                new ArticulatedHandDefinition(inputSource, controllerHandedness))
        {
            handDefinition = Definition as ArticulatedHandDefinition;
        }

        #region IMixedRealityHand Implementation

        /// <inheritdoc/>
        public override bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose) => _magicLeapHandJointProvider.JointPoses.TryGetValue(joint, out pose);

        #endregion IMixedRealityHand Implementation
        private ArticulatedHandDefinition handDefinition;


        /// <summary>
        /// If true, the current joint pose supports far interaction via the default controller ray.  
        /// </summary>
        public override bool IsInPointingPose
        {
            get
            {
                if (!IsPositionAvailable)
                    return false;
                // We check if the palm forward is roughly in line with the camera lookAt
                if (!TryGetJoint(TrackedHandJoint.Palm, out var palmPose) || CameraCache.Main == null) return false;
                
                Transform cameraTransform = CameraCache.Main.transform;
                Vector3 projectedPalmUp = Vector3.ProjectOnPlane(-palmPose.Up, cameraTransform.up);
                
                // We check if the palm forward is roughly in line with the camera lookAt
                return Vector3.Dot(cameraTransform.forward, projectedPalmUp) > .3f;
            }
        }

        /// <summary>
        /// If true, the hand is in air tap gesture, also called the pinch gesture.
        /// </summary>
        public bool IsPinching { set; get; }

        public bool IsGrabbing { set; get; }

        private InputDevice  _hand;
        private InputDevice _gestureHand;
        private static readonly ProfilerMarker UpdateStatePerfMarker = new ProfilerMarker("[MRTK] MagicLeapArticulatedHand.UpdateState");

        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose gripPose = MixedRealityPose.ZeroIdentity;

        //For hand ray
        public const float shoulderWidth = 0.37465f;
        public const float shoulderDistanceBelowHead = 0.2159f;

        private MagicLeapHandJointProvider _magicLeapHandJointProvider;

        private MagicLeapHandTrackingInputProfile.SmoothingType smoothingType;
        private bool useMLGestureClassification = true;
        private MagicLeapHandTrackingInputProfile.MLGestureType gestureType;
        private bool gesturesStarted = false;
        private bool gesturesIgnoreFirstUpdate = true;

        public void Initalize(InputDevice hand)
        {
            _hand = hand;
            _magicLeapHandJointProvider = new MagicLeapHandJointProvider(ControllerHandedness);
        }

        /// <summary>
        /// Set whether smoothing logic is applied to HandTracking.
        /// </summary>
        public void SetSmoothing(MagicLeapHandTrackingInputProfile.SmoothingType smoothing)
        {
            smoothingType = smoothing;
        }

        /// <summary>
        /// Set whether to use Magic Leap's Gesture Classification API for HandTracking Gestures.
        /// </summary>
        public void SetUseMLGestures(MagicLeapHandTrackingInputProfile.MLGestureType useMLGestures)
        {
            gestureType = useMLGestures;
            useMLGestureClassification = useMLGestures == MagicLeapHandTrackingInputProfile.MLGestureType.Both 
                                         || useMLGestures == MagicLeapHandTrackingInputProfile.MLGestureType.MLGestureClassification;
            if (!gesturesStarted)
            {
                gesturesStarted = SetupMagicLeapGestures();
            }
        }

        /// <summary>
        /// Updates the joint poses and interactions for the articulated hand.
        /// </summary>
        public void DoUpdate(bool isTracked)
        {
            using (UpdateStatePerfMarker.Auto())
            {

                if (isTracked)
                {

                    // We are not using the gesture device for hand center due to rotations
                    // being reported incorrectly
                    _magicLeapHandJointProvider.UpdateHandJoints(_hand, _gestureHand, smoothingType);

                    IsPositionAvailable = _magicLeapHandJointProvider.IsPositionAvailable;
                    IsRotationAvailable = _magicLeapHandJointProvider.IsRotationAvailable;
                    if (!IsPositionAvailable)
                        return;

                    // Update hand joints and raise event via handDefinition
                    handDefinition?.UpdateHandJoints(_magicLeapHandJointProvider.JointPoses);

                    if (useMLGestureClassification && !gesturesStarted)
                    {
                        gesturesStarted = SetupMagicLeapGestures();
                        if (!gesturesStarted && !gesturesIgnoreFirstUpdate)
                        {
                            Debug.Log("MagicLeapHand failed to find Gesture InputDevice. Falling back to MRTK for Pinch and Grip.");
                        }
                        gesturesIgnoreFirstUpdate = false;
                    }
                    
                    CalculatePinch();
                    CalculateGrab();
                    UpdateHandRay();
                    UpdateVelocity();
                }
                else
                {
                    IsPositionAvailable = IsRotationAvailable = false;
                    IsGrabbing = false;
                    IsPinching = false;
                    _magicLeapHandJointProvider.Reset();
                }

                UpdateInteractions();
            }
        }

        /// <summary>
        /// Determines if the user is grabbing using gesture recognition and key points
        /// </summary>
        private void CalculateGrab()
        {
            var isGrabbingGesture = IsGrabbing;
            var isGrabbingInterpreted = IsGrabbing;

            // If the gestures are not was not detected, we try to check if the user is grabbing using the key points
            gripPose = _magicLeapHandJointProvider.JointPoses[TrackedHandJoint.Palm];

            //Oculus and Leap motion providers do this "out of sync" call as well
            CoreServices.InputSystem?.RaiseSourcePoseChanged(InputSource, this, _magicLeapHandJointProvider.JointPoses[TrackedHandJoint.Palm]);

            bool isIndexGrabbing = IsIndexGrabbing(isGrabbingInterpreted);
            bool isMiddleGrabbing = IsMiddleGrabbing(isGrabbingInterpreted);
            isGrabbingInterpreted = isIndexGrabbing && isMiddleGrabbing;

            //Set the gesture grab in-case gestures have not started.
            isGrabbingGesture = isGrabbingInterpreted;
            //If the gesture device is valid, use the posture type.
            if (useMLGestureClassification && gesturesStarted && _gestureHand.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.HandGesture.GesturePosture,
                    out uint postureInt))
            {
                InputSubsystem.Extensions.MLGestureClassification.PostureType postureType =
                    ((InputSubsystem.Extensions.MLGestureClassification.PostureType)postureInt);

                if (postureType == InputSubsystem.Extensions.MLGestureClassification.PostureType.Grasp)
                {
                    isGrabbingGesture = true;
                }
                else
                {
                    isGrabbingGesture = false;
                }
            }

            switch (gestureType)
            {
                case MagicLeapHandTrackingInputProfile.MLGestureType.KeyPoints:
                    IsGrabbing = isGrabbingInterpreted;
                    break;
                case MagicLeapHandTrackingInputProfile.MLGestureType.MLGestureClassification:
                    IsGrabbing = isGrabbingGesture;
                    break;
                case MagicLeapHandTrackingInputProfile.MLGestureType.Both:
                    IsGrabbing = isGrabbingInterpreted || isGrabbingGesture;
                    break;
            }
        }

        /// <summary>
        /// Determines if the user is pinching using gesture recognition and key points
        /// </summary>
        private void CalculatePinch()
        {
            var isPinchingGesture = IsPinching;
            var isPinchingInterpreted = IsPinching;

            //If the gesture device is valid, use the posture type.
          

            //Check if the hand is pinching using the standard hand definition
            isPinchingInterpreted = handDefinition.IsPinching;

            //If we still did not detect the pinch we change the required pinch strength
            if (!isPinchingInterpreted)
            {
                float pinchStrength = HandPoseUtils.CalculateIndexPinch(ControllerHandedness);
                if (IsPinching)
                {
                    // If we are already pinching, we make the pinch a bit sticky
                    isPinchingInterpreted = pinchStrength > 0.1f;
                }
                else
                {
                    // If not yet pinching, only consider pinching if finger confidence is high
                    isPinchingInterpreted = pinchStrength > 0.5f;
                }
            }

            //Set the gesture grab in-case gestures have not started.
            isPinchingGesture = isPinchingInterpreted;
            if (useMLGestureClassification && gesturesStarted && InputSubsystem.Extensions.MLGestureClassification.TryGetHandPosture(_gestureHand, out InputSubsystem.Extensions.MLGestureClassification.PostureType postureType))
            {

                if (postureType == InputSubsystem.Extensions.MLGestureClassification.PostureType.Pinch)
                {
                    isPinchingGesture = true;

                    //Use Specific Pose instead of posture if able to get pose.
                    if(InputSubsystem.Extensions.MLGestureClassification.TryGetHandKeyPose(_gestureHand, out InputSubsystem.Extensions.MLGestureClassification.KeyPoseType keyPoseType))
                    {
                        if(keyPoseType != InputSubsystem.Extensions.MLGestureClassification.KeyPoseType.Pinch && keyPoseType != InputSubsystem.Extensions.MLGestureClassification.KeyPoseType.OK)
                        {
                            isPinchingGesture = false;
                        }
                    }

                }
                else
                {
                    isPinchingGesture = false;
                }
            }

            switch (gestureType)
            {
                case MagicLeapHandTrackingInputProfile.MLGestureType.KeyPoints:
                    IsPinching = isPinchingInterpreted;
                    break;
                case MagicLeapHandTrackingInputProfile.MLGestureType.MLGestureClassification:
                    IsPinching = isPinchingGesture;

                    break;
                case MagicLeapHandTrackingInputProfile.MLGestureType.Both:
                    IsPinching = isPinchingGesture || isPinchingInterpreted;
                    break;
            }
        }

        private bool IsIndexGrabbing(bool activelyGrabbing)
        {
            if (TryGetJoint(TrackedHandJoint.Wrist, out var wristPose) &&
                TryGetJoint(TrackedHandJoint.IndexTip, out var indexTipPose) &&
                TryGetJoint(TrackedHandJoint.IndexMiddleJoint, out var indexMiddlePose))
            {
                // compare wrist-middle to wrist-tip
                Vector3 wristToIndexTip = indexTipPose.Position - wristPose.Position;
                Vector3 wristToIndexMiddle = indexMiddlePose.Position - wristPose.Position;
                // Make grabbing a little sticky if activelyGrabbing
                return wristToIndexMiddle.sqrMagnitude >= wristToIndexTip.sqrMagnitude * (activelyGrabbing ? .8f : 1.0f);
            }
            return false;
        }

        private bool IsMiddleGrabbing(bool activelyGrabbing)
        {
            if (TryGetJoint(TrackedHandJoint.Wrist, out var wristPose) &&
                TryGetJoint(TrackedHandJoint.MiddleTip, out var middleTipPose) &&
                TryGetJoint(TrackedHandJoint.MiddleMiddleJoint, out var middleMiddlePose))
            {
                // compare wrist-middle to wrist-tip
                Vector3 wristToMiddleTip = middleTipPose.Position - wristPose.Position;
                Vector3 wristToMiddleMiddle = middleMiddlePose.Position - wristPose.Position;
                // Make grabbing a little sticky if activelyGrabbing
                return wristToMiddleMiddle.sqrMagnitude >= wristToMiddleTip.sqrMagnitude * (activelyGrabbing ? .8f : 1.0f);
            }
            return false;
        }

        /// <summary>
        /// Raises MRTK input system events based on joint pose data.
        /// </summary>
        protected void UpdateInteractions()
        {
            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = currentPointerPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentPointerPose);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = gripPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, gripPose);
                        }
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.TriggerPress:
                    case DeviceInputType.GripPress:
                        Interactions[i].BoolData = IsGrabbing || IsPinching;
                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        handDefinition?.UpdateCurrentIndexPose(Interactions[i]);
                        break;
                    case DeviceInputType.ThumbStick:
                    //  Not supported
                        break;
                }
            }
        }

        /// <summary>
        /// Calculate the hand ray using the key points. The hand ray's origin is located
        /// between the thumb and index knuckles. The direction of the ray is based on an
        /// interpreted shoulder and the position of the pointer origin.
        /// </summary>
        protected void UpdateHandRay()
        {
            // Pointer Origin Position
            TryGetJoint(TrackedHandJoint.IndexKnuckle, out MixedRealityPose indexKnucklePose);
            currentPointerPose.Position = indexKnucklePose.Position;

            //Pointer Rotation
            Camera mainCam = Camera.main;
            float extraRayRotationX = -20.0f;
            float extraRayRotationY = 25.0f * ((ControllerHandedness == Handedness.Left) ? 1.0f : -1.0f);

            Vector3 screenSpacePosition = CameraCache.Main.WorldToScreenPoint(currentPointerPose.Position);
            //Vector3 farClipWorldPosition = CameraCache.Main.ScreenToWorldPoint(new Vector3(screenSpacePosition.x, screenSpacePosition.y, mainCam.nearClipPlane + mainCam.farClipPlane));
            Quaternion targetRotation = Quaternion.LookRotation(currentPointerPose.Position - mainCam.transform.position, Vector3.up);
            Vector3 euler = targetRotation.eulerAngles + new Vector3(extraRayRotationX, extraRayRotationY, 0.0f);
            currentPointerPose.Rotation = Quaternion.Euler(euler);
        }

        private bool SetupMagicLeapGestures()
        {
            InputSubsystem.Extensions.MLGestureClassification.StartTracking();
            string deviceName = (ControllerHandedness.IsLeft() ? InputSubsystem.Extensions.MLGestureClassification.LeftGestureInputDeviceName : InputSubsystem.Extensions.MLGestureClassification.RightGestureInputDeviceName);

            if (!_gestureHand.isValid)
            {
                List<InputDevice> foundDevices = new List<InputDevice>();
                InputDevices.GetDevices(foundDevices);

                foreach (InputDevice device in foundDevices)
                {
                    if (device.name == deviceName)
                    {
                        _gestureHand = device;
                        break;
                    }
                }

                if (!_gestureHand.isValid)
                {
                    // Potentially will be invalid when StartTracking is called the first time. Setup will check again in the update.
                    return false;
                }
            }

            return true;
        }

    }
}

