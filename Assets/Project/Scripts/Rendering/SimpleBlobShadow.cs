using System;
using System.Security.Cryptography;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Add this component to any GameObject to display a simple blob shadow on the floor.
    /// </summary>
    public class SimpleBlobShadow : MonoBehaviour
    {
        
        [SerializeField]
        private float distanceAboveFloor = 0.01f;

        [SerializeField]
        private bool fadeOutWhenGenieIsSeated = true;

        [SerializeField]
        private Renderer shadowRenderer;

        private FloorManager _floorManager;
        private Genie _genie;

        private Color _defaultShadowColor;

        private void Awake()
        {
            if (fadeOutWhenGenieIsSeated) 
            {
                _genie = transform.GetComponentInParent<Genie>();
            }

            _defaultShadowColor = shadowRenderer.material.color;
        }

        private void LateUpdate() 
        {
            UpdatePosition();
            UpdateOpacity();
        }

        private void UpdatePosition()
        {
            if (_floorManager == null) 
            {
                _floorManager = FindFirstObjectByType<FloorManager>();
            }

            if (_floorManager == null) return;

            transform.position = new Vector3(transform.position.x, _floorManager.FloorY + distanceAboveFloor, transform.position.z);
        }

         private void UpdateOpacity()
        {
            if (_genie == null) return; 

            bool isGenieSitting = _genie.genieSitAndStand.IsSittingOrInTransition;

            float fadeDuration = 0.25f;

            Color currentColor = shadowRenderer.material.color;
            Color targetColor = isGenieSitting ? new Color(_defaultShadowColor.r, _defaultShadowColor.g, _defaultShadowColor.b, 0f) : _defaultShadowColor;
            float fadeSpeed = Time.deltaTime / fadeDuration;
            shadowRenderer.material.color = Color.Lerp(currentColor, targetColor, fadeSpeed);
        }
    }
}
