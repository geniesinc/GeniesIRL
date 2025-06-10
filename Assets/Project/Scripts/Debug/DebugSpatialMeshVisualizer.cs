using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using GeniesIRL.GlobalEvents;

namespace GeniesIRL 
{
    [RequireComponent(typeof(ARMeshManager))]
    public class DebugSpatialMeshVisualizer : MonoBehaviour
    {
        public bool showSpatialMeshGrid = false;
        public bool enableOcclusion = true;

        [SerializeField] private Material occlusionMaterial;
        [SerializeField] private Material nonOcclusionMaterial;
        [SerializeField] private Material debugGridMaterial;

        private ARMeshManager arMeshManager;

        private bool _showSpatialMeshGridLastFrame = false;
        private bool _enableOcclusionLastFrame = true;

        private void Awake()
        {
            arMeshManager = GetComponent<ARMeshManager>();
            GlobalEventManager.Subscribe<GlobalEvents.DebugShowSpatialMesh>(OnShowSpatialMesh);
            GlobalEventManager.Subscribe<GlobalEvents.DebugEnableSpatialMeshOcclusion>(OnEnableOcclusion);
        }

        private void OnEnableOcclusion(DebugEnableSpatialMeshOcclusion args)
        {
            enableOcclusion = args.Enable;
        }

        private void OnShowSpatialMesh(GlobalEvents.DebugShowSpatialMesh args)
        {
            showSpatialMeshGrid = args.Show;
        }

        private void Update()
        {
            if (_showSpatialMeshGridLastFrame != showSpatialMeshGrid || _enableOcclusionLastFrame != enableOcclusion)
            {
                _showSpatialMeshGridLastFrame = showSpatialMeshGrid;
                _enableOcclusionLastFrame = enableOcclusion;

                UpdateSpatialMeshVisibility();
            }
        }

        private void UpdateSpatialMeshVisibility()
        {
            // Updates the material of the existing meshes. 
            IList<MeshFilter> meshes = arMeshManager.meshes;
            Renderer renderer = null;
            for (int i = 0; i < meshes.Count; i++)
            {
                renderer = meshes[i].GetComponent<MeshRenderer>();
                renderer.material = GetMaterialToUse();
            }

            // Update the prefab's shared material so that if new stuff is spawned, it
            // gets the memo.
            arMeshManager.meshPrefab.gameObject.GetComponent<Renderer>().sharedMaterial = GetMaterialToUse();
        }

        private Material GetMaterialToUse()
        {
            // If spatial mesh grid is enabled, use the debug grid material.
            if (showSpatialMeshGrid)
                return debugGridMaterial;

            // Otherwise, use the occlusion or non-occlusion material based on the enableOcclusion setting.
            return enableOcclusion ? occlusionMaterial : nonOcclusionMaterial;
            
        }

        private void OnDestroy()
        {
            // Restore prefab values
            arMeshManager.meshPrefab.gameObject.GetComponent<Renderer>().sharedMaterial = occlusionMaterial;
        }
    }
}


