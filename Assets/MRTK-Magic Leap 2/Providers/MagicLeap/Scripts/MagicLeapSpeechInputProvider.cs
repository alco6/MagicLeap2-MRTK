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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UInput = UnityEngine.Input;

namespace MagicLeap.MRTK.DeviceManagement.Input
{

    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem), SupportedPlatforms.Android, "Magic Leap Speech Input")]
    public class MagicLeapSpeechInputProvider : BaseInputDeviceManager, IMixedRealitySpeechSystem,
    IMixedRealityCapabilityCheck
    {
        private MLVoiceIntentsConfiguration voiceConfiguration;

        private Dictionary<uint, string> voiceIds;

        private int randomRangeMin = 0;

        private int randomRangeMax = 1000;

        private bool isRunning = false;

#pragma warning disable 414
        // Was Voice Intents permission granted by user
        private bool permissionGranted = false;
#pragma warning restore 414
        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        public MagicLeapSpeechInputProvider(IMixedRealityServiceRegistrar registrar, IMixedRealityInputSystem inputSystem,
#pragma warning disable 618
        string name, uint priority, BaseMixedRealityProfile profile) : base(registrar, inputSystem, name, priority,
#pragma warning restore 618
        profile)
        {
        }

        public MagicLeapSpeechInputProvider(IMixedRealityInputSystem inputSystem, string name, uint priority,
            BaseMixedRealityProfile profile) : base(inputSystem, name, priority, profile)
        {
        }
        /// <summary>
        /// The keywords to be recognized and optional keyboard shortcuts.
        /// </summary>
        private SpeechCommands[] Commands => InputSystemProfile.SpeechCommandsProfile.SpeechCommands;

        /// <summary>
        /// The Input Source for Windows Speech Input.
        /// </summary>
        public IMixedRealityInputSource InputSource => globalInputSource;

        /// <summary>
        /// The global input source used by the the speech input provider to raise events.
        /// </summary>
        private BaseGlobalInputSource globalInputSource = null;

        public bool IsRecognitionActive
        {
            get
            {
#if (UNITY_MAGICLEAP || UNITY_ANDROID) && !UNITY_EDITOR
                return MLVoice.IsStarted;
#else
                return isRunning;
#endif

            }
        }

        #region IMixedRealityCapabilityCheck Implementation

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID) && !UNITY_EDITOR
            return MLVoice.VoiceEnabled;
#else
            return capability == MixedRealityCapability.VoiceCommand;
#endif
        }

        #endregion IMixedRealityCapabilityCheck Implementation

        /// <inheritdoc />
        public void StartRecognition()
        {
            if (!voiceConfiguration)
            {
                voiceConfiguration = ScriptableObject.CreateInstance<MLVoiceIntentsConfiguration>();
            }

            if (voiceIds != null)
            {
                voiceIds.Clear();
            }

            voiceIds = new Dictionary<uint, string>();
            isRunning = true;
            InitializeKeywordRecognizer();
        }

        /// <inheritdoc />
        public void StopRecognition()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID) && !UNITY_EDITOR
            MLVoice.OnVoiceEvent -= VoiceEvent;
            MLVoice.Stop();

            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
#endif
            isRunning = false;
        }

        /// <inheritdoc />
        public override void Enable()
        {
#if UNITY_EDITOR
            // Done in Permission Callback on Magic Leap Device
            StartRecognition();
#endif
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            MLPermissions.RequestPermission(MLPermission.VoiceInput, permissionCallbacks);

            // Call the base here to ensure any early exits do not
            // artificially declare the service as enabled.
            base.Enable();
        }

        private void InitializeKeywordRecognizer()
        {
            if (!Application.isPlaying ||
                (Commands == null) ||
                (Commands.Length == 0) ||
                InputSystemProfile == null
            )
            {
                return;
            }

            globalInputSource =
                Service?.RequestNewGlobalInputSource("Magic Leap Speech Input Source", sourceType: InputSourceType.Voice);

            if (voiceConfiguration.VoiceCommandsToAdd == null)
            {
                voiceConfiguration.VoiceCommandsToAdd = new List<MLVoiceIntentsConfiguration.CustomVoiceIntents>();
            }

            if (voiceConfiguration.AllVoiceIntents == null)
            {
                voiceConfiguration.AllVoiceIntents = new List<MLVoiceIntentsConfiguration.JSONData>();
            }

            // feed speech commands into config
            foreach (SpeechCommands command in Commands)
            {
                MLVoiceIntentsConfiguration.CustomVoiceIntents newIntent;
                newIntent.Value = command.Keyword;

                uint val = (uint)UnityEngine.Random.Range(randomRangeMin, randomRangeMax);
                while (voiceIds.ContainsKey(val))
                {
                    val = (uint)UnityEngine.Random.Range(randomRangeMin, randomRangeMax);
                }

                newIntent.Id = val;

                voiceConfiguration.VoiceCommandsToAdd.Add(newIntent);
                voiceIds.Add(val, command.Keyword);
            }
#if (UNITY_MAGICLEAP || UNITY_ANDROID) && !UNITY_EDITOR
            MLResult result = MLVoice.SetupVoiceIntents(voiceConfiguration);

            if (result.IsOk)
            {
                MLVoice.OnVoiceEvent += VoiceEvent;
            }
            else
            {
                Debug.LogError("Failed to Setup Voice Intents with result: " + result);
            }
#endif

        }

        private static readonly ProfilerMarker UpdatePerfMarker =
            new ProfilerMarker("[MRTK] MagicLeapSpeechInputProvider.Update");

        /// <inheritdoc />
        public override void Update()
        {
            using (UpdatePerfMarker.Auto())
            {
                base.Update();
#if (UNITY_MAGICLEAP || UNITY_ANDROID) && !UNITY_EDITOR
                if (!permissionGranted)
                {
                    return;
                }
#endif
#if UNITY_EDITOR
                for (int i = 0; i < Commands.Length; i++)
                {
                    if (UInput.GetKeyDown(Commands[i].KeyCode))
                    {
                        MLVoice.IntentEvent newEvent = new MLVoice.IntentEvent();
                        newEvent.EventName = Commands[i].LocalizedKeyword;
                        newEvent.EventID = voiceIds.FirstOrDefault(X => X.Value == Commands[i].LocalizedKeyword).Key;

                        VoiceEvent(true, newEvent);
                    }
                }
#endif
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
#if (UNITY_MAGICLEAP || UNITY_ANDROID) && !UNITY_EDITOR
            StopRecognition(); 
#endif
            isRunning = false;
            base.Disable();
        }

        private static readonly ProfilerMarker OnPhraseRecognizedPerfMarker =
            new ProfilerMarker("[MRTK] MagicLeapInputProvider.OnPhraseRecognized");

        void VoiceEvent(in bool wasSuccessful, in MLVoice.IntentEvent voiceEvent)
        {
            using (OnPhraseRecognizedPerfMarker.Auto())
            {
                if (wasSuccessful)
                {
                    globalInputSource.UpdateActivePointers();

                    int index = 0;

                    for (int i = 0; i < Commands.Length; i++)
                    {
                        if (Commands[i].Keyword == voiceEvent.EventName)
                        {
                            index = i;
                        }
                    }

                    Service?.RaiseSpeechCommandRecognized(InputSource, RecognitionConfidenceLevel.High,
                        TimeSpan.Zero, DateTime.UtcNow, Commands[index]);
                }
            }
        }

        private void OnPermissionDenied(string permission)
        {
            MLPluginLog.Error($"MagicLeapSpeechInputProvider {permission} permission denied.");
        }

        private void OnPermissionGranted(string permission)
        {
            permissionGranted = true;
            StartRecognition();
        }
    }
}