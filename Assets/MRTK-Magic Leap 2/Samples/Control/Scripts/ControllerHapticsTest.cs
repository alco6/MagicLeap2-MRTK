using MagicLeap.MRTK.DeviceManagement.Input;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;


namespace MagicLeap.MRTK.Samples
{
    public class ControllerHapticsTest: MonoBehaviour
    {
        public void StartHapticBuzz()
        {
            IMixedRealityController[] trackedControls = MagicLeapDeviceManager.Instance.GetActiveControllers();

            if(trackedControls.Length == 0)
            {
                Debug.Log("ControllerHapticsTest failed to locate an IMixedRealityController");
                return;
            }

            ((MagicLeapMRTKController)trackedControls[0]).StartHapticImpulse(700, 500, 3000, 50);
        }
    }
}
