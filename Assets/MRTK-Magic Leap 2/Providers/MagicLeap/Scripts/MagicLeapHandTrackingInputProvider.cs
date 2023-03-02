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
    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem), SupportedPlatforms.Android, "Magic Leap HandTracking Input")]
    public class MagicLeapHandTrackingInputProvider : XRSDKDeviceManager
    {
        Dictionary<Handedness, MagicLeapHand> trackedHands = new Dictionary<Handedness, MagicLeapHand>();
        Dictionary<Handedness, MLHandContainer> allHands = new Dictionary<Handedness, MLHandContainer>();

        private bool? IsActiveLoader =>
            LoaderHelpers.IsLoaderActive<MagicLeapLoader>();

        public bool MLHandTrackingActive = false;

        public static MagicLeapHandTrackingInputProvider Instance = null;

        public enum HandSettings
        {
            None,
            Left,
            Right,
            Both
        }

        public HandSettings CurrentHandSettings
        {
            get
            {
                return _CurrentHandSettings;
            }

            set
            {
                _CurrentHandSettings = value;
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
                // TODO: Update real-time hand settings
                switch (value)
                {
                    case HandSettings.None:
                        RemoveAllHandDevices();
                        return;

                    case HandSettings.Left:
                        if (trackedHands.ContainsKey(Handedness.Right))
                        {
                            MagicLeapHand hand = trackedHands[Handedness.Right];
                            if (hand != null)
                            {
                                RemoveHandDevice(hand);
                            }
                        }
                        break;

                    case HandSettings.Right:
                        if (trackedHands.ContainsKey(Handedness.Left))
                        {
                            MagicLeapHand hand = trackedHands[Handedness.Left];
                            if (hand != null)
                            {
                                RemoveHandDevice(hand);
                            }
                        }
                        break;
                }
#endif
            }
        }

        public MagicLeapHandTrackingInputProfile.SmoothingType HandTrackingSmoothing
        {
            get
            {
                return handTrackingSmoothing;
            }

            set
            {
                handTrackingSmoothing = value;
            }
        }

        public MagicLeapHandTrackingInputProfile.MLGestureType GestureInteractionType
        {
            get
            {
                return gestureInteractionType;
            }

            set
            {
                gestureInteractionType = value;
            }
        }

        private MagicLeapHandTrackingInputProfile profile;

        private HandSettings _CurrentHandSettings = HandSettings.Both;
        private MagicLeapHandTrackingInputProfile.SmoothingType handTrackingSmoothing = MagicLeapHandTrackingInputProfile.SmoothingType.Robust;
        private MagicLeapHandTrackingInputProfile.MLGestureType gestureInteractionType = MagicLeapHandTrackingInputProfile.MLGestureType.Both;


        private InputDevice leftHandDevice;
        private InputDevice rightHandDevice;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public MagicLeapHandTrackingInputProvider(
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile baseProfile = null) : base(inputSystem, name, priority, baseProfile)
        {
        }
        public override void Initialize()
        {
            if (Instance == null)
            {
                Instance = this;
            }
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
            profile = ConfigurationProfile as MagicLeapHandTrackingInputProfile;

            CurrentHandSettings = profile.HandednessSettings;
            HandTrackingSmoothing = profile.Smoothing;
            GestureInteractionType = profile.GestureInteractionType;

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
            if (!MLHandTrackingActive)
            {
                if (!MLPermissions.CheckPermission(MLPermission.HandTracking).IsOk)
                {
                    Debug.LogError($"You must include the {MLPermission.HandTracking} permission in the AndroidManifest.xml to use Hand Tracking in this scene.");
                    return;
                }
                else
                {
                    InputSubsystem.Extensions.MLHandTracking.StartTracking();

                    MLHandTrackingActive = true;
                }

            }
#endif
        }

        private void FindDevices()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left, devices);
            foreach (var device in devices)
            {
                if (device.isValid && device.name.Contains("MagicLeap"))
                {
                    leftHandDevice = device;
                    break;
                }
            }

            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right, devices);
            foreach (var device in devices)
            {
                if (device.isValid && device.name.Contains("MagicLeap"))
                {
                    rightHandDevice = device;
                    break;
                }
            }
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
                UpdateHands();

            }
#endif
        }

        protected void UpdateHands()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            if (!leftHandDevice.isValid || !rightHandDevice.isValid)
                FindDevices();

            UpdateHand(rightHandDevice, Handedness.Right);
            UpdateHand(leftHandDevice, Handedness.Left);
#endif

        }

        public override void Disable()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID)
            if (MLHandTrackingActive)
            {
                RemoveAllHandDevices();

                MLHandTrackingActive = false;
            }

            if (Instance == this)
            {
                Instance = null;
            }
#endif
        }
        public override bool CheckCapability(MixedRealityCapability capability)
        {
            return (capability == MixedRealityCapability.ArticulatedHand);
        }

#if (UNITY_MAGICLEAP || UNITY_ANDROID)
        #region Hand Management

        protected void UpdateHand(InputDevice mlHand, Handedness handedness)
        {
            if (!IsHandednessValid(handedness, CurrentHandSettings))
                return;

            if (mlHand.isValid && TryGetOrAddHand(mlHand, handedness, out MagicLeapHand hand))
            {
                hand.SetSmoothing(HandTrackingSmoothing);
                hand.SetUseMLGestures(GestureInteractionType);
                hand.DoUpdate(allHands[handedness].IsPoseValid());
            }
            else
            {
                RemoveHandDevice(handedness);
            }
        }

        private void RemoveHandDevice(Handedness handedness)
        {
            if (trackedHands.TryGetValue(handedness, out MagicLeapHand hand))
            {
                RemoveHandDevice(hand);
            }
        }

        private bool IsHandednessValid(Handedness handedness, HandSettings settings)
        {
            switch (settings)
            {
                case HandSettings.None:
                    return false;

                case HandSettings.Left:
                    if (handedness != Handedness.Left)
                    {
                        return false;
                    }
                    break;

                case HandSettings.Right:
                    if (handedness != Handedness.Right)
                    {
                        return false;
                    }
                    break;

                case HandSettings.Both:
                    if (handedness != Handedness.Left && handedness != Handedness.Right)
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Used to determine if a hand is tracked. Stores the amount of time the hand has been considered tracked.
        /// </summary>
        public class MLHandContainer
        {
            //The hand device
            public InputDevice Device;
            //The time the hand was last tracking
            public float TrackTime;
            // The amount of time the hand has been detected
            public float LifeTime;
            //The MRTK Magic Leap Hand
            public MagicLeapHand MagicLeapHand;

            public MLHandContainer(InputDevice device)
            {
                Device = device;
            }
            public bool IsTrackingValid()
            {
                Device.TryGetFeatureValue(InputSubsystem.Extensions.DeviceFeatureUsages.Hand.Confidence, out float confidence);
                if (confidence > 0)
                {
                    TrackTime = Time.time;
                }

                bool isDoingAction = MagicLeapHand !=null && 
                                     (MagicLeapHand.IsPinching || MagicLeapHand.IsGrabbing);

                float latency = isDoingAction ? 1.25f : .75f;
                bool isTracking = Time.time - TrackTime < latency;

                return isTracking;
            }

            public bool IsPoseValid()
            {
                if (IsTrackingValid())
                {
                    LifeTime += Time.deltaTime;
                    if (LifeTime > .5f)
                    {
                        return true;
                    }
                }
                else
                {
                    LifeTime = 0;
                }
                return false;
            }

        }

        private bool TryGetOrAddHand(InputDevice mlHand, Handedness handedness, out MagicLeapHand magicLeapHand)
        {
            magicLeapHand = null;

            //Checks if the hand was previously tracked
            if (allHands.ContainsKey(handedness))
            {
                // If the hand is tracked but not considered valid, do not track it.
                // Used to filter when Left and Right hand are incorrectly identified
                if (!allHands[handedness].IsPoseValid() 
                    && (allHands[handedness].MagicLeapHand ==null || ( !allHands[handedness].MagicLeapHand.IsPositionAvailable 
                    && !allHands[handedness].MagicLeapHand.IsRotationAvailable)))
                {
                    return false;
                }
            }
            else
            {
                // If the hand has not been tracked before add it to the list
                // and start calculating the how long the hand has been considered tracked
                allHands.Add(handedness, new MLHandContainer(mlHand));
                return false;
            }

            // If the hand is valid and has been considered tracked, return it
            if (trackedHands.ContainsKey(handedness))
            {
                allHands[handedness].Device = mlHand;
                allHands[handedness].MagicLeapHand = trackedHands[handedness];
                magicLeapHand = trackedHands[handedness];
                return true;
            }

            // If the hand is valid and considered tracked, but has not been reported as track before.
            // Create a new MRTK input source provider.

            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSourceType = InputSourceType.Hand;

            var inputSource = Service?.RequestNewGenericInputSource($"Magic Leap {handedness} Hand", pointers, inputSourceType);

            var controller = new MagicLeapHand(TrackingState.Tracked, handedness, inputSource);
            controller.Initalize(mlHand);

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            Service?.RaiseSourceDetected(controller.InputSource, controller);
            trackedHands.Add(handedness, controller);

            magicLeapHand = controller;
            allHands[handedness].MagicLeapHand = controller;
            allHands[handedness].Device = mlHand;

            return true;
        }

        private void RemoveAllHandDevices()
        {
            if (trackedHands.Count == 0) return;

            // Create a new list to avoid causing an error removing items from a list currently being iterated on.
            foreach (MagicLeapHand hand in new List<MagicLeapHand>(trackedHands.Values))
            {
                RemoveHandDevice(hand);
            }
            trackedHands.Clear();
        }

        private void RemoveHandDevice(MagicLeapHand hand)
        {
            //if (hand == null) return;
            allHands[hand.ControllerHandedness].MagicLeapHand = null;
            CoreServices.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
            trackedHands.Remove(hand.ControllerHandedness);
            RecyclePointers(hand.InputSource);
        }

        #endregion
#endif // PLATFORM_RELISH
    }
}
