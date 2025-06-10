using System;
using System.Collections.Generic;
using Unity.PolySpatial;
using UnityEngine;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace GeniesIRL
{
    /// <summary>
    /// Based off of ImageTrackingObjectManger.cs from the PolySpatial samples, this script spawns and maintains objects based on what the XRNode's
    /// ARTrackedImageManager is tracking. The other script was geared towards spawning number objects, but this script is supposed to be more general.
    /// </summary>
    public class XRImageTrackingObjectManager : GeniesIrlSubManager
    {
        

        [SerializeField]
        [Tooltip("Reference Image Library")]
        private XRReferenceImageLibrary m_ImageLibrary;

        [SerializeField]
        [Tooltip("Prefabs to spawn")]
        private GameObject[] m_PrefabsToSpawn;

        [SerializeField]
        [Tooltip("If true, remove the spawned prefab when the tracked image is lost")]
        private bool removeOnTrackingLost = true;

        readonly Dictionary<Guid, GameObject> m_PrefabsToSpawnDictionary = new();
        readonly Dictionary<Guid, GameObject> m_SpawnedPrefabs = new();
        public Dictionary<Guid, GameObject> spawnedPrefabs => m_SpawnedPrefabs;

        private ARTrackedImageManager _arTrackedImageManager;

        public override void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            base.OnSceneBootstrapped(bootstrapper);

            _arTrackedImageManager = bootstrapper.XRNode.arTrackedImageManager;

            Initialize();
        }

        private void Initialize()
        {
            var count = m_PrefabsToSpawn.Length;
            var imageCount = m_ImageLibrary.count;
            if (count > imageCount)
                Debug.LogWarning($"Number of prefabs ({count}) exceeds the number of images in the reference library ({imageCount})");

            count = Math.Min(count, imageCount);
            for (var i = 0; i < count; i++)
            {
                var guid = m_ImageLibrary[i].guid;
                m_PrefabsToSpawnDictionary[guid] = m_PrefabsToSpawn[i];
            }

            _arTrackedImageManager.trackablesChanged.AddListener(ImageManagerOnTrackedImagesChanged);

            Bootstrapper.XRNode.volumeCamera.WindowStateChanged.AddListener(OnWindowEvent);
        }

        void ImageManagerOnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> obj)
        {
            // added, spawn prefab
            foreach (var image in obj.added)
            {
                var guid = image.referenceImage.guid;
                if (m_PrefabsToSpawnDictionary.TryGetValue(guid, out var prefab))
                {
                    var imageTransform = image.transform;
                    var spawnedPrefab = Instantiate(prefab, imageTransform.position, imageTransform.rotation);
                    m_SpawnedPrefabs[guid] = spawnedPrefab;
                }
            }

            // updated, set prefab position and rotation
            foreach (var image in obj.updated)
            {
                var guid = image.referenceImage.guid;

                // If the image is tracking, update its position and show its visuals
                var isTracking = image.trackingState == TrackingState.Tracking;
                if (isTracking && m_SpawnedPrefabs.TryGetValue(guid, out var spawnedPrefab))
                {
                    var spawnedPrefabTransform = spawnedPrefab.transform;
                    var imageTransform = image.transform;
                    spawnedPrefabTransform.SetPositionAndRotation(imageTransform.position, imageTransform.rotation);
                }
            }

            if (removeOnTrackingLost) 
            {
                // removed, destroy spawned instance
                foreach (var image in obj.removed)
                {
                    var guid = image.Value.referenceImage.guid;
                    if (m_SpawnedPrefabs.TryGetValue(guid, out var spawnedPrefab))
                    {
                        Destroy(spawnedPrefab);
                        m_SpawnedPrefabs.Remove(guid);
                    }
                }
            }
        }

        private void OnWindowEvent(VolumeCamera volumeCamera, VolumeCamera.WindowState s)
        {
            if (s.WindowEvent == VolumeCamera.WindowEvent.Focused && s.IsFocused)
            {
                // The app has regained focus. We should destroy all spawned prefabs and reset everything
                foreach (var prefab in m_SpawnedPrefabs.Values)
                {
                    Destroy(prefab);
                }
                m_SpawnedPrefabs.Clear();
            }
        }
    }
}
