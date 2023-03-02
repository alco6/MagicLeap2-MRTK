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


using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.XRSDK.Input;
using Microsoft.MixedReality.Toolkit.XRSDK;
using UnityEngine.XR.MagicLeap;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    // Added inside of the scope to prevent conflicts with Unity's 2020.3 Version Control package.
    using Microsoft.MixedReality.Toolkit;

    /// <summary>
    /// Manages Magic Leap Device
    /// </summary>
    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem), SupportedPlatforms.Android, "Magic Leap Device Manager")]
    public class MagicLeapDeviceManager : XRSDKDeviceManager
    {
        List<IMixedRealityController> trackedControls = new List<IMixedRealityController>();

        private bool? IsActiveLoader =>
            LoaderHelpers.IsLoaderActive<MagicLeapLoader>();

        public bool MLControllerCallbacksActive = false;

        public static MagicLeapDeviceManager Instance = null;

        private MagicLeapMRTKController mrtkController;
        private InputDevice controllerDevice;
        private MagicLeapInputs mlInputs;
        private MagicLeapInputs.ControllerActions controllerActions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public MagicLeapDeviceManager(
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(inputSystem, name, priority, profile)
        {

        }
        public override void Initialize()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            EnablePointerCache = false;

            base.Initialize();
        }

        private async void EnableIfLoaderBecomesActive()
        {
            await new WaitUntil(() => IsActiveLoader.HasValue);
            if (IsActiveLoader.Value)
            {
                Enable();
            }
        }

        public override void Enable()
        {
            if (!IsActiveLoader.HasValue)
            {
                IsEnabled = false;
                EnableIfLoaderBecomesActive();
                return;
            }
            else if (!IsActiveLoader.Value)
            {
                IsEnabled = false;
                return;
            }

            SetupInput();

            base.Enable();

        }

        private void SetupInput()
        {

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            if (!MLControllerCallbacksActive)
            {
                if (mlInputs != null)
                {
                    mlInputs.Enable();
                    controllerActions.Enable();
                }
                else
                {
                    mlInputs = new MagicLeapInputs();
                    mlInputs.Enable();
                    controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
                    controllerActions.IsTracked.performed += MLControllerConnected;
                    controllerActions.IsTracked.canceled += MLControllerDisconnected;
                }

                if (controllerActions.IsTracked.IsPressed())
                {
                    MLControllerConnected(new InputAction.CallbackContext());
                    Debug.Log("Controller events added");
                }

                MLControllerCallbacksActive = true;
            }
#endif
        }

        public override void Update()
        {

            if (!IsEnabled)
            {
                return;
            }

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            // Ensure input is active
            if (MLDevice.IsReady())
            {              
                if(mrtkController != null) 
                    mrtkController.UpdatePoses();

            }
#endif
        }

        public override void Disable()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            if (MLControllerCallbacksActive)
            {
                mlInputs.Disable();
                controllerActions.Disable();

                RemoveAllControllerDevices();

                MLControllerCallbacksActive = false;
            }

            if (Instance == this)
            {
                Instance = null;
            }
#endif
        }

        public override IMixedRealityController[] GetActiveControllers()
        {
            return trackedControls.ToArray<IMixedRealityController>();
        }

        /// <inheritdoc />
        public override bool CheckCapability(MixedRealityCapability capability)
        {
            return (capability == MixedRealityCapability.MotionController);
        }

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
        #region Controller Management

        void MLControllerConnected(UnityEngine.InputSystem.InputAction.CallbackContext callbackContext)
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            controllerDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand);

            if (controllerDevice.isValid)
            {
                Debug.Log("Controller Connected and found and valid");

                if (mrtkController == null)
                {
                    Handedness handedness = Handedness.Left;
                  
                    var pointers = RequestPointers(SupportedControllerType.GenericUnity, handedness);
                    var inputSourceType = InputSourceType.Controller;

                    var inputSource = Service?.RequestNewGenericInputSource($"Magic Leap {handedness} Controller", pointers, inputSourceType);

                    MagicLeapMRTKController controller = new MagicLeapMRTKController(controllerDevice, TrackingState.Tracked, handedness, inputSource);
                    for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
                    {
                        controller.InputSource.Pointers[i].Controller = controller;
                    }

                    Service?.RaiseSourceDetected(controller.InputSource, controller);
                    mrtkController = controller;
                    Debug.Log("Controller Connected and found and valid and registered");
                    trackedControls.Add(controller);
                }
            }
#endif
        }

        void MLControllerDisconnected(UnityEngine.InputSystem.InputAction.CallbackContext callbackContext)
        {
            if (mrtkController != null)
            {
                IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;
                inputSystem?.RaiseSourceLost(mrtkController.InputSource, mrtkController);
                trackedControls.Remove(mrtkController);
                RecyclePointers(mrtkController.InputSource);
                mrtkController.CleanupController();
                mrtkController = null;
            }
        }

        private void RemoveAllControllerDevices()
        {
            MLControllerDisconnected(new UnityEngine.InputSystem.InputAction.CallbackContext());
        }

        #endregion
#endif // PLATFORM_RELISH
    }
}

