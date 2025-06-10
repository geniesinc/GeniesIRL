using System;
using UnityEngine;

namespace GeniesIRL.Utilities
{
    public class SmoothLookAt : MonoBehaviour
    {
        public enum ForwardDirection
        {
            Forward,
            Backward,
            Up,
            Down,
            Left,
            Right
        }

        public ForwardDirection localForwardDirection = ForwardDirection.Forward;

        [Tooltip("If you leave this empty, it will try to use the current camera transform.")]
        public Transform target; // The target to look at

        public bool enableSmoothing = true;

        [ConditionalField("enableSmoothing")]
        public float smoothingFactor = 5f;

        public bool worldYRotationOnly = false;

        /// <summary>
        /// Instantly look at a position without applying any interpolation.
        /// </summary>
        /// <param name="position"></param>
        public void SnapToLookAt(Vector3 position)
        {
            LookAt(position, false);
        }

        private void LateUpdate()
        {
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            if (target == null) return;

            LookAt(target.position, enableSmoothing);
            
        }

        private void LookAt(Vector3 point, bool useSmoothing)
        {
            // Calculate the desired look rotation
            Vector3 targetDirection = point - transform.position;

            // Limit to world Y rotation only if enabled
            if (worldYRotationOnly)
            {
                targetDirection.y = 0; // Ignore vertical component for horizontal rotation
                if (targetDirection == Vector3.zero)
                    return; // Avoid zero direction
            }

            // Get the target rotation as though we were aiming using transform.forward.
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            // Now process the rotation to use our selected "forward"
            targetRotation *= GetForwardRotationalOffset(localForwardDirection);

            if (useSmoothing)
            {
                // Apply smoothing and the custom forward direction
                Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothingFactor);
                transform.rotation = smoothedRotation;
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        // Returns a rotation that reorients the transform such that it faces using the eselected local direction;
        private Quaternion GetForwardRotationalOffset(ForwardDirection direction)
        {
            Quaternion rotation = Quaternion.identity;
            switch (direction)
            {
                case ForwardDirection.Forward:
                    return Quaternion.identity;
                case ForwardDirection.Backward:
                    return Quaternion.Euler(0, 180f, 0);
                case ForwardDirection.Up:
                    return Quaternion.Euler(90f, 0, 0f);
                case ForwardDirection.Down:
                    return Quaternion.Euler(-90f, 0, 0f);
                case ForwardDirection.Left:
                    return Quaternion.Euler(0, -90, 0);
                case ForwardDirection.Right:
                    return Quaternion.Euler(0, 90, 0);
            }

            return rotation;
        }

        
    }

}
