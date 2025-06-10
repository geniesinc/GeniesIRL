using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

namespace GeniesIRL 
{
    [RequireComponent(typeof(Item))]
    public class Pencil : MonoBehaviour
    {
        [SerializeField, Tooltip("The curve that indicates how the pencil initially approaches cruzing velocity.")]
        private AnimationCurve launchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Tooltip("The duration of the launch curve before reaching cruzing velocity.")]
        private float launchDuration = 0.25f;
        [SerializeField, Tooltip("The max speed at which the pencil cruizes to its destination.")]
        private float terminalSpeed = 10f;

        [SerializeField, Tooltip("The height of the pencil tip from the center. Everything above this point gets embedded into the ceiling.")]
        private float _pencilTipHeight = 0.05f;

        [Header("Audio")]
        [SerializeField] private AudioSource pencilImpactAudioSource;

        public Item Item 
        {
            get
            {
                if (_item == null) _item = GetComponent<Item>();
                return _item;
            }
        }

        private Item _item;

        public bool IsLaunched { get; private set; } = false;

        public void Launch()
        {
            IsLaunched = true;

            LayerMask spatialMeshLayerMask = LayerMask.GetMask("SpatialMesh");

            Ray ray = new Ray(transform.position, Vector3.up);

            float castDist = 20f;

            Vector3 destinationPoint;

            bool doesHitCeiling;
                
            if (Physics.Raycast(ray, out RaycastHit hitInfo, castDist, spatialMeshLayerMask))
            {
               destinationPoint = hitInfo.point;
               doesHitCeiling = true;
            }
            else 
            {
                destinationPoint = ray.GetPoint(castDist);
                doesHitCeiling = false;
            }

            // We don't want this bumping into anything during its flight, or falling due to gravity.
            _item.Collider.enabled = false;
            _item.GetComponent<Rigidbody>().isKinematic = true;

            // We don't want the Genie trying to grab this later.
            GameObject.Destroy(_item.GetComponent<GenieGrabbable>());

            StartCoroutine(FlyToDestination_C(destinationPoint, doesHitCeiling));
        }

        private IEnumerator FlyToDestination_C(Vector3 destinationPoint, bool playCeilingHitSound)
        {
            float startTime = Time.time;

            Vector3 startPos = transform.position;
            Vector3 endPos = destinationPoint - Vector3.up * _pencilTipHeight;

            Quaternion startRot = transform.rotation;

            float dist = Vector3.Distance(startPos, endPos);
            float duration = dist / terminalSpeed;

            float launchEndTime = startTime + launchDuration;

            // Launch the pencil.
            while (Time.time < launchEndTime)
            {
                float elapsed = Time.time - startTime;
                float t = elapsed / duration;
                
                if (elapsed < launchDuration)
                {
                    // Accelerate to cruzing speed.
                    t = launchCurve.Evaluate(t);
                    // Rotate during the launch speed.
                    transform.rotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
                    t *= (launchDuration/duration); // Scale t to match the duration.
                }
                else 
                {
                    t = (elapsed - launchDuration) / (duration - launchDuration); // Fly at cruzing speed.
                }
                
                transform.position = Vector3.Lerp(startPos, endPos, t);
                
                yield return null;
            }

            transform.position = endPos;

            if (playCeilingHitSound)
            {
                pencilImpactAudioSource.Play();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.up * _pencilTipHeight);
        }
    }
}

