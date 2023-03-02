using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace MagicLeap.MRTK.Samples
{
    /// <summary>
    /// Demo script to show how to change the behavior of MRTK pointers.
    /// The settings can be changed at runtime
    /// </summary>
    public class MRTKPointerSettings : MonoBehaviour
    {
        public PointerBehavior MotionControllerPointerBehavior = PointerBehavior.AlwaysOn;
        public PointerBehavior GazePointerBehavior = PointerBehavior.AlwaysOn;

        // Keeps the pointer active on the Motion Controller
        void Start()
        {
            PointerUtils.SetMotionControllerRayPointerBehavior(MotionControllerPointerBehavior);
            PointerUtils.SetGazePointerBehavior(GazePointerBehavior);
        }
    }
}
