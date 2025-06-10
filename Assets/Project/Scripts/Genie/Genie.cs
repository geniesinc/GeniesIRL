using GeneisIRL;
using Pathfinding;
using UnityEngine;

namespace GeniesIRL
{
    [RequireComponent(typeof(AIPath))]
    public class Genie : MonoBehaviour
    {
        /// <summary>
        /// Satisfies some legacy stuff inside GenieIKController, etc. We may eventually restore user controlled Genies.
        /// </summary>
        public bool IsUserControlled {get {return false;}}
        
        public float Height { get { return GetScaledColliderDimensions().y; } }

        /// <summary>
        /// If this genie was spawned by a GenieManager, this property will be set. Otherwise, it will be null (this will happen if the Genie exists as a scene
        /// object in a testing scene, for example.)
        /// </summary>
        public GenieManager GenieManager { get; private set; } 

        public Collider Collider {
            get {
                if (_collider == null)
                    _collider = GetComponentInChildren<Collider>();
                return _collider;
            }
        }
        public GenieLookAndYaw genieLookAndYaw;
        public GenieNavigation genieNavigation;
        public GenieAnimation genieAnimation;
        public GenieBrain genieBrain;
        public GenieGrabber genieGrabber;
        public GenieSitAndStand genieSitAndStand;
        public GenieHighFiver genieHighFiver;
        public GenieDraw genieDraw;
        public GenieOfferItem genieOfferItem;
        public GenieSense genieSense;
        public GenieAudio genieAudio;
        public GenieEphemeralProps genieEphemeralProps;
        private Collider _collider;

        /// <summary>
        /// Originally imported from GenieController, this method will return the scaled dimensions of the Genie's collider.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetScaledColliderDimensions()
        {
            Vector3 scaledDimensions = Vector3.zero;

            if (Collider is BoxCollider boxCollider)
            {
                // Apply lossy scale to BoxCollider's size
                scaledDimensions = Vector3.Scale(boxCollider.size, Collider.transform.lossyScale);
            }
            else if (Collider is SphereCollider sphereCollider)
            {
                // Sphere's radius is uniform, apply the largest scale factor to get the effective diameter
                float scaledRadius = sphereCollider.radius * Mathf.Max(Collider.transform.lossyScale.x, Collider.transform.lossyScale.y, Collider.transform.lossyScale.z);
                scaledDimensions = new Vector3(scaledRadius * 2, scaledRadius * 2, scaledRadius * 2);
            }
            else if (Collider is CapsuleCollider capsuleCollider)
            {
                // Apply lossy scale to CapsuleCollider's height and radius
                float scaledHeight = capsuleCollider.height * Collider.transform.lossyScale.y;
                float scaledRadius = capsuleCollider.radius * Mathf.Max(Collider.transform.lossyScale.x, Collider.transform.lossyScale.z);  // Max of x and z for the capsule's diameter
                scaledDimensions = new Vector3(scaledRadius * 2, scaledHeight, scaledRadius * 2);
            }

            return scaledDimensions;
        }
        
        public void OnSpawnedByGenieManager(GenieManager genieManager)
        {
            GenieManager = genieManager;
            genieLookAndYaw.OnSpawnedByGenieManager(this);
        }

        public void OnTeleported(Vector3 userHeadPosition)
        {
            genieLookAndYaw.InstantYawTowards(userHeadPosition);
            genieBrain.planDecider.OnGenieTeleported();
        }

        private void Start()
        {
            genieLookAndYaw.OnStart(this);
            genieNavigation.OnStart(this);
            genieAnimation.OnStart(this);
            genieAudio.OnStart(this);
            genieGrabber.OnStart(this);
            genieSitAndStand.OnStart(this);
            genieHighFiver.OnStart(this);
            genieDraw.OnStart(this);
            genieOfferItem.OnStart(this);
            genieSense.OnStart(this);
            genieEphemeralProps.OnInitialize(this);
            genieBrain.OnStart(this);
        }

        private void Update()
        {
            genieNavigation.OnUpdate();
            genieAnimation.OnUpdate();
            genieGrabber.OnUpdate();
            genieSense.OnUpdate();
            genieBrain.OnUpdate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            genieSense.OnCollisionEnter(collision);
        }

        private void OnDrawGizmos()
        {
            genieGrabber.OnDrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            genieSense.OnDrawGizmosSelected();
        }

    }

    public enum GenieHand {Left, Right} // <-- From the legacy code but still used throughout, so I'm keeping it here for now.

}

