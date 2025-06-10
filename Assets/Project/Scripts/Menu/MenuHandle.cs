using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GeniesIRL.Utilities;
using UnityEngine.XR.ARFoundation;
using System.Collections;

namespace GeniesIRL
{
    public class MenuHandle : MonoBehaviour
    {

        [SerializeField] private bool initializeOnAwake = true;

        [SerializeField] private bool lookAtOnlyWhileGrabbed = true;

        [SerializeField, ConditionalField("lookAtOnlyWhileGrabbed")]
        private XRGrabInteractable xrGrabInteractable;

        [SerializeField, ConditionalField("lookAtOnlyWhileGrabbed")]
        private SmoothLookAt smoothLookAt;

        private bool _keepingOutOfTheWayOfTheUser = false;
        private Coroutine _keepOutOfTheWayOfTheUserCoroutine;
        private float _keepOutTheWayBuffer = 0.4f;

        /// <summary>
        /// Unparents the menu handle from its parent, then attaches a ParentConstrant to the previous parent such
        /// that it behaves as a child of the handle.
        /// </summary>
        public void Initialize()
        {
            Transform parent = transform.parent;

            if (parent == null)
            {
                Debug.LogError("MenuHandle must be a child of an object in order for it to be used as a handle");
                return;
            }


            // Unparent the handle, keeping its world position
            transform.SetParent(null, true);

            transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);

            // Add a parent constraint to this object
            ParentConstraint parentConstraint = parent.gameObject.AddComponent<ParentConstraint>();

            // Set up the parent constraint so that this follows around the menu handle like a parent.
            ConstraintSource source = new ConstraintSource
            {
                sourceTransform = transform,
                weight = 1f
            };

            // Add the source to the constraint
            int sourceIndex = parentConstraint.AddSource(source);

            // Compute the position and rotation offsets relative to the handle
            Vector3 positionOffset = parent.position - transform.position;
            Quaternion rotationOffset = Quaternion.Inverse(transform.rotation) * transform.rotation;
            Vector3 rotationOffsetEuler = rotationOffset.eulerAngles;

            // Set the translation and rotation offsets for the source
            parentConstraint.SetTranslationOffset(sourceIndex, positionOffset);
            parentConstraint.SetRotationOffset(sourceIndex, rotationOffsetEuler);

            parentConstraint.constraintActive = true;

            CreateAnchorAsync();
        }

        async void CreateAnchorAsync()
        {
            // This is inefficient. You should re-use a saved reference instead.
            var manager = GameObject.FindAnyObjectByType<ARAnchorManager>();

            // This is a "dummy" pose value. You should use a pose that is meaningful
            // to your app, such as from a raycast hit or another trackable.
            var pose = new Pose(Vector3.zero, Quaternion.identity);

            var result = await manager.TryAddAnchorAsync(pose);

            if (result.status.IsSuccess())
            {
                var anchor = result.value;
                transform.SetParent(anchor.transform, true);
            }
        }

        private void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (_keepOutOfTheWayOfTheUserCoroutine != null) return; // The coroutine is controlling the motion of the handle in this case.

            if (lookAtOnlyWhileGrabbed)
            {
                smoothLookAt.enabled = xrGrabInteractable.isSelected;
            }
        }

        /// <summary>
        /// Used by the UI Manager when the Prespawn UI is enabled, so that the user can see it and, eventually, the Genie when it it spawns.
        /// </summary>
        /// <returns></returns>
        public void KeepOutOfTheWayOfTheUser(bool enable, Transform userHead = null) 
        {
            if (_keepingOutOfTheWayOfTheUser == enable)
            {
                return;
            } 
            _keepingOutOfTheWayOfTheUser = enable;

            Debug.Log("Keep out of the way of the user: " + enable);

            if (enable)
            { 
                smoothLookAt.enabled = true;

                if (_keepOutOfTheWayOfTheUserCoroutine != null) 
                {
                    StopCoroutine(_keepOutOfTheWayOfTheUserCoroutine);
                }
                
                Debug.Log("Starting coroutine");
                _keepOutOfTheWayOfTheUserCoroutine = StartCoroutine(KeepOutOfTheWayOfTheUser_C(userHead));
            }
        }

        // Sorry the math here is so sketch but let's just get to beta.
        private IEnumerator KeepOutOfTheWayOfTheUser_C(Transform userHead) 
        {
            float minTimeBeforeStop = 1f;
            float duration = 0f;

            // Flattened direction to menu
            Vector3 toMenu = transform.position - userHead.position;
            toMenu.y = 0f;
            toMenu.Normalize();

            // Flattened user right vector
            Vector3 headRight = userHead.right;
            headRight.y = 0f;
            headRight.Normalize();

            // Dot product: +1 means fully right, -1 means fully left
            float sideDot = Vector3.Dot(toMenu, headRight);

            // Choose side: right if center or leaning right, left if leaning left
            Vector3 targetDirection = (sideDot >= 0f) ? headRight : -headRight;

            // Scale the buffer by how bad we need it
            // straight ahead? real bad! already to the side? don't need it.
            float needScalar = 1 - Mathf.Abs(sideDot);
            
            // Scale the buffer by how bad we need it
            // straight ahead? real bad! already to the side? don't need it.
            float bufferScaled = _keepOutTheWayBuffer * needScalar;
            Vector3 sidePos = transform.position + (targetDirection * bufferScaled);
            
            // Maintain the current distance tho, we are just moving it to the side
            // in like an arc...
            Vector3 targetDir = sidePos - userHead.position;
            targetDir.y = 0f;
            targetDir.Normalize();

            Vector3 headPos = userHead.position;
            headPos.y = transform.position.y;
            float currMenuDistance = Vector3.Distance(transform.position, headPos);

            Vector3 targetPos = headPos + (targetDir * currMenuDistance);

            float maxGetOutOfWayTime = 1.5f;

            while (_keepingOutOfTheWayOfTheUser || duration < minTimeBeforeStop)
            {
                yield return new WaitForEndOfFrame();

                // Smooth it
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

                duration += Time.deltaTime;

                if (duration > maxGetOutOfWayTime)
                {
                   _keepingOutOfTheWayOfTheUser = false;
                }
            }

            _keepOutOfTheWayOfTheUserCoroutine = null;
        }
    }
}

