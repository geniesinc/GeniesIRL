using System;
using UnityEngine;

namespace GeniesIRL 
{

    /// <summary>
    /// Manages the Genie's personal space from the user, informing decision-making about when to avoid the user when they step too close.
    /// </summary>
    [System.Serializable]
    public class PersonalSpace
    {
        public enum PersonalSpaceType
        {
            None,
            /// <summary>
            /// The default personal space radius.
            /// </summary>
            Base,
            /// <summary>
            /// The personal space radius when the Genie is navigating to a target in the real world. This is tighter than the base radius to allow the Genie to more easily path around the user.
            /// </summary>
            NavigatingIrl,
            /// <summary>
            /// The personal space radius when the Genie is accepting/offering an item, or high-fiving the user. This is tighter than the base radius to allow the user to get closer.
            /// </summary>
            IntentionalPhysicalContact
        }

        /// <summary>
        /// The current radius of the Genie's personal space, on the world XZ plane. This changes at runtime based on the Genie's current state.
        /// </summary>
        public float Radius {
            get 
            {
                if (_radiusOverride >= 0) 
                {
                    return _radiusOverride;
                }

                return baseRadius;
            }
        }

        /// <summary>
        /// The base radius of the Genie's personal space, on the world XZ plane.
        /// </summary>
        public float BaseRadius => baseRadius;

        /// <summary>
        // During the NavigateToTargetIRL or NavigatetoLocationIRL Actions, we want to keep the personal space tighter to allow the Genie to path around the user.
        // Otherwise, the Genie would cancel their current Goal and enter the "Maintain Personal Space" Goal.
        /// </summary>
        public const float kRadiusDuringNavigatingIrlActions = 0.05f;

        /// <summary>
        /// When the Genie is accepting/offering an item, or high-fiving the user, she is implicitly inviting the user into her personal space. 
        /// So, we tighten the radius to allow the user to get closer.
        /// </summary>
        public const float kRadiusDuringIntentionalPhysicalContact = 0.2f;

        [SerializeField, Tooltip("The base radius of the Genie's personal space, on the world XZ plane." 
            + " If the user's head is within this range, the Genie will avoid the user.")]
        private float baseRadius = .75f;

        [NonSerialized]
        private Genie _genie;

        [NonSerialized]
        private GenieSense _genieSense;

        private XRInputWrapper _user;

        private float _radiusOverride = -1f; // -1 means there's no override, and that we're using the base radius.

        public void OnStart(GenieSense genieSense)
        {
            _genieSense = genieSense;
            _genie = genieSense.Genie;
            _user = _genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper;
        }
        
        /// <summary>
        /// Checks if the user is within the Genie's personal space.
        /// </summary>
        /// <returns></returns>
        public bool IsUserTooClose() 
        {
            // Note: Personal space doesn't apply to every GenieGoal and situation, but for now we'll assume it does.

            return VectorUtils.IsWithinDistanceXZ(_genie.transform.position, _user.Head.position, Radius);
        }

        public void OnUpdate()
        {
            
        }

        public void OnDrawGizmosSelected()
        {
            if (_genie == null) return;
            Gizmos.color = Color.yellow;
            GizmoUtilities.DrawCircle(_genie.transform.position, baseRadius, Vector3.up);
        }

        /// <summary>
        /// Overrides the base radius with whatever value is passed in. This allows the Genie to have different personal space radii based on the current state.
        /// To reset the radius to the base radius, call ResetRadius().
        /// </summary>
        /// <param name="radius"></param>
        public void SetRadiusOverride(float radius) 
        {
            _radiusOverride = radius;
        }

        /// <summary>
        /// Resets the personal space radius to the base radius.
        /// </summary>
        public void ResetRadius() 
        {
            _radiusOverride = -1f;
        }
    }
}

