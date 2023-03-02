using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    /// <summary>
    /// Configuration profile settings for Hand Tracking .
    /// </summary>
    [CreateAssetMenu(
        menuName =
            "Mixed Reality/Toolkit/Profiles/Magic Leap Hand Tracking Profile",
        fileName = "MagicLeapHandTrackingInputProfile", order = (int)CreateProfileMenuItemIndices.HandTracking)]
    [MixedRealityServiceProfile(typeof(MagicLeapHandTrackingInputProvider))]
    public class MagicLeapHandTrackingInputProfile : BaseMixedRealityProfile
    {
        [Header("Magic Leap Settings")]
        [Tooltip("Choose which hands to track.")]
        public MagicLeapHandTrackingInputProvider.HandSettings HandednessSettings = MagicLeapHandTrackingInputProvider.HandSettings.Both;

        public enum MLGestureType { KeyPoints, MLGestureClassification, Both }
        [Tooltip("Use either the Tracked Key Points, Magic Leap Gesture Classification , or Both to determine gestures.")]
        public MLGestureType GestureInteractionType = MLGestureType.Both;

        public enum SmoothingType { None, Simple, Robust}
        [Tooltip("Enable smoothing for Hand Tracking.")]
        public SmoothingType Smoothing = SmoothingType.Robust;

    
    }
}
