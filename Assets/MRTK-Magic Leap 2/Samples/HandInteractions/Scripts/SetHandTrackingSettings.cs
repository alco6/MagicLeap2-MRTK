using MagicLeap.MRTK.DeviceManagement.Input;
using UnityEngine;
namespace MagicLeap.MRTK.Samples
{
    /// <summary>
    /// Demo script to show how to toggle hand tracking settings.
    /// The settings can be changed at runtime
    /// </summary>
    [System.Obsolete("Hand Tracking Settings are now located in the MagicLeapHandTrackingInputProfile attached to the Magic Leap Hand Tracking Input Data Provider. Settings can still be changed at runtime the same way.")]
    public class SetHandTrackingSettings : MonoBehaviour
    {
        public MagicLeapHandTrackingInputProvider.HandSettings _settings;

        // Start is called before the first frame update
        void Start()
        {
            MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings = _settings;
        }
    }
}
