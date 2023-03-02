/// -------------------------------------------------------------------------------
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

using MagicLeap.MRTK.DeviceManagement.Input;
using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI;

namespace MagicLeap.MRTK.Samples
{
    // Added inside of the scope to prevent conflicts with Unity's 2020.3 Version Control package.
    using Microsoft.MixedReality.Toolkit.Input;
    
    [RequireComponent(typeof(Interactable))]
    public class ToggleReflectHandSetting : MonoBehaviour
    {
       public MagicLeapHandTrackingInputProvider.HandSettings SettingToReflect;
        Interactable interactable;

        private void Awake()
        {
            interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            UpdateToggle();
        }

        private void UpdateToggle()
        {
            if (MagicLeapHandTrackingInputProvider.Instance == null)
            {
                Debug.Log("Device Manager Not here");
                return;
            }
            
            switch(SettingToReflect)
            {
                case MagicLeapHandTrackingInputProvider.HandSettings.Both:
                    interactable.IsToggled =
                        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings == MagicLeapHandTrackingInputProvider.HandSettings.Both;
                    break;
            
                case MagicLeapHandTrackingInputProvider.HandSettings.Left:
                    interactable.IsToggled =
                        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings == MagicLeapHandTrackingInputProvider.HandSettings.Both ||
                        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings == MagicLeapHandTrackingInputProvider.HandSettings.Left;
                    break;
            
                case MagicLeapHandTrackingInputProvider.HandSettings.Right:
                    interactable.IsToggled =
                        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings == MagicLeapHandTrackingInputProvider.HandSettings.Both ||
                        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings == MagicLeapHandTrackingInputProvider.HandSettings.Right;
                    break;
            }
        }

        public void ReflectToggleButton(Interactable interactable)
        {
            MagicLeapHandTrackingInputProvider.HandSettings CurrentHandSettings = MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings;
            MagicLeapHandTrackingInputProvider.HandSettings NewHandSettings = MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings;
            
            switch (SettingToReflect)
            {
                case MagicLeapHandTrackingInputProvider.HandSettings.Left:
                    switch (CurrentHandSettings)
                    {
                        case MagicLeapHandTrackingInputProvider.HandSettings.Left:
                            if (!interactable.IsToggled) // Turn off Left
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.None;
                            }
                            break;
            
                        case MagicLeapHandTrackingInputProvider.HandSettings.Right:
                            if (interactable.IsToggled)
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Both;
                            }
                            break;
            
                        case MagicLeapHandTrackingInputProvider.HandSettings.None:
                            if (interactable.IsToggled)
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Left;
                            }
                            break;
            
                        case MagicLeapHandTrackingInputProvider.HandSettings.Both:
                            if (!interactable.IsToggled)
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Right;
                            }
                            break;
                    }
                    break;
            
                case MagicLeapHandTrackingInputProvider.HandSettings.Right:
                    switch (MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings)
                    {
                        case MagicLeapHandTrackingInputProvider.HandSettings.Right:
                            if (!interactable.IsToggled) // Turn off Right
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.None;
                            }
                            break;
            
                        case MagicLeapHandTrackingInputProvider.HandSettings.Left:
                            if (interactable.IsToggled)
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Both;
                            }
                            break;
            
                        case MagicLeapHandTrackingInputProvider.HandSettings.None:
                            if (interactable.IsToggled)
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Right;
                            }
                            break;
            
                        case MagicLeapHandTrackingInputProvider.HandSettings.Both:
                            if (!interactable.IsToggled)
                            {
                                NewHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Left;
                            }
                            break;
                    }
                    break;
            
                case MagicLeapHandTrackingInputProvider.HandSettings.Both:
                    NewHandSettings = interactable.IsToggled ?
                        MagicLeapHandTrackingInputProvider.HandSettings.Both : MagicLeapHandTrackingInputProvider.HandSettings.None;
                    break;
            }
            MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings = NewHandSettings;
            //Debug.Log("New Hand Settings: " + MagicLeapDeviceManager.Instance.CurrentHandSettings);
        }
    }
}
