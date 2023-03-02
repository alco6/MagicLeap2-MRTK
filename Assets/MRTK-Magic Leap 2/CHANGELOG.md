**Unity MRTK Package 1.1.0-Dev1 Version 1.0**
===================================

* Magic Leap SDK Version 1.1.0-Dev1
* Unity Version: 2022.2 (custom)
* MRTK Foundations v2.8.2
* MRTK Examples v2.8.2

**Release Focus**
-----------------

Initial Update to 1.1.0-Dev1

**Features**
------------

* Voice Intents - Refer to SpeechCommandsDemoMagicLeap scene for usage.
* Control - Refer to ControlMagicLeapDemo scene for usage.
* HandTracking - Refer to HandInterationExamplesMagicleap for usage. Only Partially implemented on platform currently.
* EyeTracking - Refer to EyeTrackingDemoMagicLeap for usage.
* Meshing - Refer to MeshingDemoMagicLeap for usage.
* AllInteractions - Refer to InteractionsDemoMagicLeap for Handtracking, Control, EyeTracking, and Voice usage together.

**1.1.0-Dev1 Version 1 Updates**
----------------------------

* Updated to MagicLeap SDK 1.1.0-Dev1
* **Important** Updated Controller Touchpad Interactions to be the proper type to get data from.
* Added a MagicLeapTouchInputHandler to be able to get data from Input Action Events for the touchpad. An Example has been added to the Control Sample.
* Modified Robust smoothing option in HandTracking. This is still experimental and being worked on for improvements to the HandTracking experience.
* Modified HandTracking Pointer to improve selection behaviour.
* HandTracking Sample cleanup to remove unusable components.

**Known Issues**
----------------

* Handtracking Performance issues when interacting with other objects. Continuous Improvements made each Sprint.
* To use the simulator when running MRTK, you must set the **Script Changes While Playing** setting to **Stop Playing and Recompile** or **Recompile and Conintue Playing** in the Unity **Preferences**.

**Important Notes**
-------------------

* Instead of copying a configuration file, clone the DefaultMixedReality version and make adjustments. We have found copying an MRTK configuration file can cause issues such as Input Data Providers not loading or visualizers not attaching properly.
* Controller Visualizer sometimes stops positioning and the logs say: Left_ControllerModel(Clone) is missing a IMixedRealityControllerVisualizer component! This happens sporatically, we have found adding the MixedRealityControllerVisualizer component to the model itself resolves this.
* If your application builds and results in a blank/empty scene, you must adjust your projects quality settings. (Known issue in editor. This will be resolved in future 2022.2 editor releases) To resolve this, remove all but one of the quality presets in your projects quality settings (**Edit>Player Settings>Quality**)
* Users may need to add the tracked pose driver to the camera themselves.
