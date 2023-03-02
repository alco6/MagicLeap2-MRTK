using MagicLeap.MRTK.SpatialAwareness;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.XRSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.MagicLeap.Meshing;

namespace MagicLeap.MRTK.SpatialAwareness
{
    public enum GeneralMeshRenderMode
    {
        None,
        Colored,
        PointCloud,
        Occlusion
    }

    [MixedRealityDataProvider(
        typeof(IMixedRealitySpatialAwarenessSystem),
        SupportedPlatforms.Android,
        "MagicLeap Spatial Mesh Observer")]
    [HelpURL("https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/spatial-awareness/spatial-awareness-getting-started")]
    public class MagicLeapSpatialMeshObserver :
        BaseSpatialMeshObserver
    {
        /// <summary>
        /// An event which is invoked whenever a new mesh is added
        /// </summary>
        public event Action<GameObject> MeshAdded;

        /// <summary>
        /// An event which is invoked whenever an existing mesh is updated (regenerated).
        /// </summary>
        public event Action<GameObject> MeshUpdated;

#if UNITY_MAGICLEAP || UNITY_ANDROID
        /// <summary>
        /// Altering the mesh profile data at runtime may require calling ForceUpdateMeshData() to clear visuals;
        /// </summary>
        public MagicLeapSpatialMeshObserverProfile Profile;

        private MeshingSubsystemComponent subsystemComponent;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        private GameObject mainCamera = null;

        private GameObject meshParent = null;
        private GameObject meshingSubsystemParent = null;

#endif
        private XRInputSubsystem inputSubsystem;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the service.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public MagicLeapSpatialMeshObserver(
            IMixedRealitySpatialAwarenessSystem spatialAwarenessSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(spatialAwarenessSystem, name, priority, profile)
        {
        }

        public override void Enable()
        {
            base.Enable();
#if UNITY_MAGICLEAP || UNITY_ANDROID

            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            MLPermissions.RequestPermission(MLPermission.SpatialMapping, permissionCallbacks);

            Profile = ConfigurationProfile as MagicLeapSpatialMeshObserverProfile;
            if (Profile == null)
            {
                Debug.LogWarning($"Use the `MagicLeapSpatialMeshObserverProfile` configuration to set Magic Leap specific meshing settings. Default settings will be used.");

                Profile = new MagicLeapSpatialMeshObserverProfile();
            }

            if (meshParent == null)
            {
                meshParent = new GameObject("MeshParent");
            }

            if (meshingSubsystemParent == null)
            {
                meshingSubsystemParent = new GameObject("MeshingSubsystem");
            }


            subsystemComponent = meshingSubsystemParent.gameObject.AddComponent<MeshingSubsystemComponent>();

            subsystemComponent.meshPrefab = Profile.MeshPrefab;
            subsystemComponent.computeNormals = Profile.ComputeNormals;
            subsystemComponent.density = Profile.Density;
            subsystemComponent.meshParent = meshParent.transform;
            subsystemComponent.requestedMeshType = Profile.MeshType;
            subsystemComponent.fillHoleLength = Profile.FillHoleLength;
            subsystemComponent.planarize = Profile.Planarize;
            subsystemComponent.disconnectedComponentArea = Profile.DisconnectedComponentArea;
            subsystemComponent.meshQueueSize = Profile.MeshQueueSize;
            subsystemComponent.pollingRate = Profile.PollingRate;
            subsystemComponent.batchSize = Profile.BatchSize;
            subsystemComponent.requestVertexConfidence = Profile.RequestVertexConfidence;
            subsystemComponent.removeMeshSkirt = Profile.RemoveMeshSkirt;

            inputSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();
            inputSubsystem.trackingOriginUpdated += OnTrackingOriginChanged;

            mainCamera = Camera.main.gameObject;

            meshingSubsystemParent.transform.position = mainCamera.transform.position;

            subsystemComponent.meshAdded += HandleOnMeshAdded;
            subsystemComponent.meshUpdated += HandleOnMeshUpdated;

            UpdateBounds();
#endif
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            inputSubsystem.trackingOriginUpdated -= OnTrackingOriginChanged;

#if UNITY_MAGICLEAP || UNITY_ANDROID
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
            subsystemComponent.meshAdded -= HandleOnMeshAdded;
            subsystemComponent.meshUpdated -= HandleOnMeshUpdated;
#endif
        }

        public override void Update()
        {
            base.Update();

            if (Profile.Follow)
            {
                meshingSubsystemParent.transform.position = mainCamera.transform.position;
            }

            if(Profile.IsBounded && meshingSubsystemParent.transform.localScale != Profile.BoundedExtentsSize ||
                !Profile.IsBounded && meshingSubsystemParent.transform.localScale != Profile.BoundlessExtentsSize)
            {
                UpdateBounds();
            }
        }

        public void ForceUpdateMeshData()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            subsystemComponent.meshPrefab = Profile.MeshPrefab;
            subsystemComponent.computeNormals = Profile.ComputeNormals;
            subsystemComponent.density = Profile.Density;
            subsystemComponent.meshParent = meshParent.transform;
            subsystemComponent.requestedMeshType = Profile.MeshType;
            subsystemComponent.fillHoleLength = Profile.FillHoleLength;
            subsystemComponent.planarize = Profile.Planarize;
            subsystemComponent.disconnectedComponentArea = Profile.DisconnectedComponentArea;
            subsystemComponent.meshQueueSize = Profile.MeshQueueSize;
            subsystemComponent.pollingRate = Profile.PollingRate;
            subsystemComponent.batchSize = Profile.BatchSize;
            subsystemComponent.requestVertexConfidence = Profile.RequestVertexConfidence;
            subsystemComponent.removeMeshSkirt = Profile.RemoveMeshSkirt;

            subsystemComponent.DestroyAllMeshes();
            subsystemComponent.RefreshAllMeshes();
            UpdateBounds();
#endif
        }

        private void GeneralRendering(MeshRenderer meshRenderer)
        {
            // Toggle the GameObject(s) and set the correct materia based on the current RenderMode.
            if (Profile.GeneralRenderMode == GeneralMeshRenderMode.None)
            {
                meshRenderer.enabled = false;
            }
            else if (Profile.GeneralRenderMode == GeneralMeshRenderMode.PointCloud)
            {
                meshRenderer.enabled = true;
                meshRenderer.material = Profile.GeneralPointCloudMaterial;
            }
            else if (Profile.GeneralRenderMode == GeneralMeshRenderMode.Colored)
            {
                meshRenderer.enabled = true;
                meshRenderer.material = Profile.GeneralColoredMaterial;
            }
            else if (Profile.GeneralRenderMode == GeneralMeshRenderMode.Occlusion)
            {
                meshRenderer.enabled = true;
                meshRenderer.material = Profile.OcclusionMaterial;
            }
        }

        private void UpdateBounds()
        {
            meshingSubsystemParent.transform.localScale = Profile.IsBounded ? Profile.BoundedExtentsSize : Profile.BoundlessExtentsSize;
        }

        private void OnPermissionDenied(string permission)
        {
            Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
            subsystemComponent.enabled = false;
        }

        private void OnTrackingOriginChanged(XRInputSubsystem inputSubsystem)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            subsystemComponent.DestroyAllMeshes();
            subsystemComponent.RefreshAllMeshes();
#endif
        }

        private void HandleOnMeshAdded(UnityEngine.XR.MeshId meshId)
        {
            if (subsystemComponent.meshIdToGameObjectMap.ContainsKey(meshId))
            {
                if (MeshAdded != null)
                {
                    MeshAdded(subsystemComponent.meshIdToGameObjectMap[meshId]);
                }

                if (Profile.UseGeneralRendering)
                {
                    GeneralRendering(subsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshRenderer>());
                }
            }
        }

        private void HandleOnMeshUpdated(UnityEngine.XR.MeshId meshId)
        {
            if (subsystemComponent.meshIdToGameObjectMap.ContainsKey(meshId))
            {
                if (MeshUpdated != null)
                {
                    MeshUpdated(subsystemComponent.meshIdToGameObjectMap[meshId]);
                }

                if (Profile.UseGeneralRendering)
                {
                    GeneralRendering(subsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshRenderer>());
                }
            }
        }
    }
}