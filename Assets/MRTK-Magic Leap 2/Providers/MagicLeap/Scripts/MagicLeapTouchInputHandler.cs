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
// Note this code is inspired by MRTK provided example: https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/input-events?view=mrtkunity-2022-05#register-for-global-input-events
// And: https://github.com/microsoft/MixedRealityToolkit-Unity/blob/main/Assets/MRTK/SDK/Features/Input/Handlers/InputActionHandler.cs

using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;

namespace MagicLeap.MRTK.DeviceManagement
{
    public class MagicLeapTouchInputHandler : BaseInputHandler, IMixedRealityInputHandler<Vector2>, IMixedRealityInputHandler<float>, IMixedRealityInputHandler
    {
        [SerializeField]
        [Tooltip("Input Action to handle")]
        private MixedRealityInputAction InputAction = MixedRealityInputAction.None;

        public InputActionUnityEvent EvenActions;

        #region InputSystemGlobalHandlerListener Implementation

        protected override void RegisterHandlers()
        {
            // Register for Input Events (Listen for MR w/ Vector2, float & bool).
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler<float>>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);
        }

        protected override void UnregisterHandlers()
        {
            // Unregister for Input Events.
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler<float>>(this); 
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler>(this);
        }

        #endregion InputSystemGlobalHandlerListener Implementation

        #region IMixedRealityInputHandler Implementation
        void IMixedRealityInputHandler<Vector2>.OnInputChanged(InputEventData<Vector2> eventData)
        {
            // Handle Touchpad Touch events & eventData
            if (eventData.MixedRealityInputAction.Id == InputAction.Id)
            {
                EvenActions.Invoke(eventData);
            }
        }

        void IMixedRealityInputHandler<float>.OnInputChanged(InputEventData<float> eventData)
        {
            // Handle Touchpad Press events & eventData
            if (eventData.MixedRealityInputAction.Id == InputAction.Id)
            {
                EvenActions.Invoke(eventData);
            }
        }
        void IMixedRealityInputHandler.OnInputDown(InputEventData eventData)
        {
            // Handle Touchpad Touch events & eventData (true)
            if (eventData.MixedRealityInputAction.Id == InputAction.Id)
            {
                EvenActions.Invoke(eventData);
            }
        }
        void IMixedRealityInputHandler.OnInputUp(InputEventData eventData)
        {
            // Handle Touchpad touch events & eventData (on ended) (false)
            if (eventData.MixedRealityInputAction.Id == InputAction.Id)
            {
                EvenActions.Invoke(eventData);
            }
        }
        #endregion IMixedRealityInputHandler Implementation
    }
}