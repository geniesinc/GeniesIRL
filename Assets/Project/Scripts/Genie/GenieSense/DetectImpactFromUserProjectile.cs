using UnityEngine;
using System; 

namespace GeniesIRL 
{
    [Serializable]
    public class DetectImpactFromUserProjectile 
    {
        /// <summary>
        /// Fires when the Genie detects an impact from a user-thrown item. The bool parameter indicates whether the impact was from the 'front' of the Genie (true means in front).
        /// </summary>
        public event Action<Item, bool> OnItemImpact;

        [SerializeField, Tooltip("The minimum speed at which the Genie will react to an impact.")] 
        private float minSpeedToReactToItemImpact = 1f;

        [SerializeField, Tooltip("The field of view angle that the Genie will consider as 'in front' of them.")]
        private float fovToCountAsInFront = 120f;

        [System.NonSerialized]
        private Genie _genie;

        public void OnStart(GenieSense genieSense)
        {
            _genie = genieSense.Genie;
        }

        public void OnCollisionEnter(Collision collision)
        {
            Item item = collision.collider.GetComponentInParent<Item>();

            if (item == null) return; // Ignore collisions with non-Item objects

            if (item.state != Item.ItemState.DroppedByUserAndInMotion) return; // Ignore collisions with Items that are not in motion from a user toss.

            // Output the collision speed
            float speed = collision.relativeVelocity.magnitude;

            bool isFront = IsCollisionInFront(collision);

            Debug.Log("Impact Speed: " + speed + (isFront ? " from front" : " from back"));

            if (speed >= minSpeedToReactToItemImpact)
            {
                OnItemImpact?.Invoke(item, isFront);
            }
        }

        private bool IsCollisionInFront(Collision collision)
        {
            Vector3 relativeVelocity = collision.relativeVelocity;
            relativeVelocity.y = 0;
            relativeVelocity.Normalize();

            Vector3 genieForward = _genie.transform.forward;
            genieForward.y = 0;
            genieForward.Normalize();

            float angle = Vector3.Angle(genieForward, -relativeVelocity);
            return angle <= fovToCountAsInFront / 2f;
        }
    }
}

