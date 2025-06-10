using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.PolySpatial.InputDevices;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.XR.Hands;

namespace GeniesIRL
{
    /// <summary>
    /// Wraps Input and Interactions systems in a convenient place so that they can be easily accessible anywhere.
    /// It's based upon the Polyspatial Manipulation_XRI example which had Inputs and Interactions pretty tightly coupled, at least
    /// from our initial analysis. To clarify, Interactions deals with Interactables, which is a related but separate
    /// concept, so ultimately this might not be the best organization for the future.
    /// </summary>
    public class XRInputWrapper : MonoBehaviour
    {
        /// <summary>
        /// Detects double pinches and dispatches events. These types of pinches don't need to accompany gaze/hover/selection/touches and can
        /// be used in isolation of those systems. In other words, the user doesn't need to be pinching an object to trigger a dobule pinch.
        /// </summary>
        public DoublePinchDetection doublePinchDetection;

        /// <summary>
        /// Returns the transform of the user's Head. Note that, as of writing, head tracking does not work with Play-to-Device, in which
        /// case the head will stay at the origin.
        /// </summary>
        public Transform Head { get; private set; }

        public XRHands hands;

        public void OnInitialize(XRNode xrNode)
        {
            Head = xrNode.xrOrigin.Camera.transform;
            hands.OnInitialize(xrNode);
        }

        private void Start()
        {
            doublePinchDetection.OnStart(this);
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable(); // Originally from PolySpatial.Samples.HubInputManager
        }

        private void Update()
        {
            UpdateTouches();
            doublePinchDetection.OnUpdate();
        }

        private void LateUpdate()
        {
            hands.OnLateUpdate();
        }

        private void UpdateTouches()
        {
            var activeTouches = Touch.activeTouches;

            if (activeTouches.Count == 0) return;

            var primaryTouchData = EnhancedSpatialPointerSupport.GetPointerState(activeTouches[0]);

            if (activeTouches[0].phase == TouchPhase.Began && primaryTouchData.targetObject != null && primaryTouchData.targetObject.scene == gameObject.scene)
            {
                if (primaryTouchData.targetObject.TryGetComponent(out SpatialButton button))
                {
                    button.Press();
                }
            }
        }
    }
}

