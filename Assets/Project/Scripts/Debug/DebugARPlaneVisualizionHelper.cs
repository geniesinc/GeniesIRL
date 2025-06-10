using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GeniesIRL 
{
    /// <summary>
    /// Works with the DebugARPlaneVisualizer to allow us to see ARPlanes at runtime. Enable or Disable component to control debug visibility.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    public class DebugARPlaneVisualizationHelper : MonoBehaviour
    {
        [SerializeField] private Material _debugMaterial;
        private Material _originalMaterial;
        private ARPlane _arPlane;
        private Renderer _renderer;

        private float _alphaValue;

        private void Awake()
        {
            _arPlane = GetComponent<ARPlane>();
            _renderer = GetComponent<Renderer>();
            _originalMaterial = _renderer.material;
            _alphaValue = _debugMaterial.color.a;
        }
        
        private void OnEnable()
        {
            _arPlane.boundaryChanged += OnBoundaryChanged;
            _renderer.material = _debugMaterial;

            UpdateDebugVisualization(_arPlane.classifications);
        }

        private void OnDisable()
        {
            _arPlane.boundaryChanged -= OnBoundaryChanged;
            _renderer.material = _originalMaterial;
        }

        private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs eventArgs)
        {
            UpdateDebugVisualization(_arPlane.classifications);
        }

        private void UpdateDebugVisualization(PlaneClassifications classification)
        {
            if (_renderer.material == null) return;
            
            Color color;
            
            // Display color based on classification.
            switch (classification)
            {
                case PlaneClassifications.WallFace:
                    color = Color.white;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.Floor:
                    color = Color.gray;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.Ceiling:
                    color = Color.cyan;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.WindowFrame:
                    color = Color.green;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.Table:
                    color = Color.yellow;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.DoorFrame:
                    color = Color.blue;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.Seat:
                    color = Color.black;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.None:
                    color = Color.magenta;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;

                case PlaneClassifications.Other:
                    color = Color.magenta;
                    color.a = _alphaValue;
                    _renderer.material.color = color;
                    break;
            }
        }
    }
}
