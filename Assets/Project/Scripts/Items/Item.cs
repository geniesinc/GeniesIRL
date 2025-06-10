using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GeniesIRL.GlobalEvents;
using GeneisIRL;
using Unity.PolySpatial;
using System.Collections;

namespace GeniesIRL
{
    /// <summary>
    /// The replacement for the legacy ItemController component.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Item : MonoBehaviour
    {
        public enum ItemState
        {
            WaitingForInitialHold, // Kinda deprecated since we're not really using the Item spawner anymore.
            PlacedByAutoSpawner,
            HeldByUser,
            HeldByGenie,
            DroppedByUserAndInMotion,
            DroppedByUserAndAtRest,
            DroppedByGenie,
        }
        
        /// <summary>
        /// Fires when the user "steals" an Item that the Genie was holding. At the time of writing, this should only happen
        /// while the Genie is offering the item to the user.
        /// </summary>
        public event Action<Item> OnItemStolenFromGenie;

        /// <summary>
        /// Fires when the item is grabbed by either the user or the genie.
        /// </summary>
        public event Action<Item> OnGrabbed;

        /// <summary>
        /// Fires when the item is released by either the user or the genie.
        /// </summary>

        public event Action<Item> OnReleased;

        /// <summary>
        /// Fires when the item impacts another collider.
        /// </summary>
        public event Action<Item, float> OnPhysicalImpact;

        public event Action<Item> ReleasedByUser;

        public bool IsDebugMode {get; private set;} = false; 

        public string humanReadableName;

        [Tooltip("If this is set to a positive value, the object will be destroyed after this many seconds of inactivity.")]
        public float expireTime = -1f;

        [SerializeField, Tooltip("If the object falls below this world space Y value, it will be destroyed.")] 
        private float autoDestroyHeight = -1f;

        [SerializeField, Tooltip("If the object should be grabbable by the player, you'll need an xrGrabInteractable component")]
        private XRGrabInteractable xrGrabInteractable;

        [SerializeField]
        private GrabTutorial grabTutorialPrefab;

        [ReadOnly]
        public ItemState state = ItemState.WaitingForInitialHold;

        public GenieGrabbable GenieGrabbable {get; private set;}

        public float TimeSinceLastReleased {get; private set;}
        
        private Vector3 _cachedBoundsSizeWhenUnrotated;

        private GrabTutorial _grabTutorial;

        private bool _isGazeHovering = false;

        private static bool _hasUserHasPickedUpAnyItemInScene = false;


        public Collider Collider
        {
            get
            {
                if (_collider == null) _collider = GetComponentInChildren<Collider>();
                return _collider;
            }
        }

        /// <summary>
        /// The bounds of the collider when it isn't rotated isn't important for the Genie knowing how to place an item. Unfortunately, Collider.bounds doesn't 
        /// work when the Collider is disabled, if needed, this property will lean on the cached size value and transform position.
        /// </summary>        
        public Bounds BoundsWhenNotRotated
        {
            get
            {
                if (Collider.enabled)
                {
                    return Collider.GetBoundsWhenNotRotated();
                }

                return new Bounds(transform.position, _cachedBoundsSizeWhenUnrotated);
            }
        }

        public bool IsGrabbableByGenie => GenieGrabbable != null;

        private Rigidbody _rigidbody;
        private Collider _collider;

        /// <summary>
        /// Create a new item and set it up so that it just lies on the ground ready for someone to pick it up.
        /// </summary>
        /// <param name="itemPrefab"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public static Item CreateFromItemSpawner(Item itemPrefab, Vector3 position, Quaternion rotation)
        {
            Item item = Instantiate(itemPrefab, position, rotation);
            item.state = Item.ItemState.PlacedByAutoSpawner;
            item.GetComponent<Rigidbody>().useGravity = true;
            item.transform.SetPositionAndRotation(position, rotation);
            item.TimeSinceLastReleased = Time.time;

            return item;
        }

        /// <summary>
        /// Generally speaking, the user should not be able to grab an object that is held by the Genie. However, when the Genie is offering them the object,
        /// we need to temporarily allow the user to grab it. Just make sure to disable it when the offer is withdrawn. 
        /// If the item is grabbed by the User or released by the Genie in this state, you do not need to call this function again to reset its state.
        /// </summary>
        /// <param name="enable"></param>
        public void TemporarilyAllowUserToGrabItemHeldByGenie(bool enable) 
        {
            if (state != ItemState.HeldByGenie) 
            {
                Debug.LogError("Cannot set temporary grabbability by user when the item is not held by the Genie.");
                return;
            }

            if (xrGrabInteractable != null) 
            {
                xrGrabInteractable.enabled = enable;
            }

            EnableCollider(enable);
        }

        private void Awake () 
        {
            GenieGrabbable = GetComponent<GenieGrabbable>();

             if (xrGrabInteractable != null) 
            {
                xrGrabInteractable.selectEntered.AddListener(OnGrabbedByUser);
                xrGrabInteractable.selectExited.AddListener(OnReleasedByUser);
            }

            if (IsGrabbableByGenie) 
            {
                GenieGrabbable.OnGenieGrabbed += OnGrabbedByGenie;
                GenieGrabbable.OnGenieReleased += OnReleasedByGenie;
            }

            _rigidbody = GetComponent<Rigidbody>();

            // In debug mode, pretend we were placed by the Auto Spawner.
            IsDebugMode = Time.time < 0.5f; // Assume object was placed into the scene by a developer and not spawned at runtime.

            if (IsDebugMode) 
            {
                state = ItemState.PlacedByAutoSpawner;
                _rigidbody.useGravity = true;
            }
            else {
                StartCoroutine(ScaleUp_C());
            }
        }

        private IEnumerator ScaleUp_C()
        {
            Vector3 startScale = transform.localScale * 0.01f;
            Vector3 endScale = transform.localScale;
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = endScale;
        }

        private void Start()
        {
            GlobalEventManager.Trigger(new ItemSpawned(this));

            if (!_hasUserHasPickedUpAnyItemInScene)
            {
                GlobalEventManager.Subscribe<UserPicksUpItem>(OnUserPicksUpAnyItemInTheScene);
            }
        }

        private void OnGrabbedByUser(SelectEnterEventArgs args)
        {
            ItemState prevState = state;

            state = ItemState.HeldByUser;

            if (prevState == ItemState.HeldByGenie) 
            {
                OnItemStolenFromGenie?.Invoke(this);
            }

            GlobalEventManager.Trigger(new UserPicksUpItem(this));

            OnGrabbed?.Invoke(this);
        }

        private void OnReleasedByUser(SelectExitEventArgs args)
        {
            if (state == ItemState.HeldByGenie) return; // Ignore in this case -- it means we are transferring the item directly from the user to the Genie.

            state = ItemState.DroppedByUserAndInMotion;

            TimeSinceLastReleased = Time.time;

            ReleasedByUser?.Invoke(this);

            OnReleased?.Invoke(this);
        }

        private void OnGrabbedByGenie()
        {
            if (state == ItemState.HeldByUser) // If currently held by user, force them to drop it. This happens when we are transferring the item directly from the user to the Genie.
            {
                OnReleasedByUser(null);
            }

            state = ItemState.HeldByGenie;

            // Prevent the item from being grabbed by the player, as well as any other weirdness that could result from 
            // xrGrabInteractable being enabled.
            if (xrGrabInteractable != null)
            {
                xrGrabInteractable.enabled = false;
            }

            // Disable colliders that might prevent the genie from holding the object.
            EnableCollider(false);

            _rigidbody.useGravity = false;

            OnGrabbed?.Invoke(this);
        }

        private void OnReleasedByGenie(bool stolenByUser)
        {
            state = ItemState.DroppedByGenie;

            // At the time of writing, it's only possible to have an item stolen by the user if the genie was offering it. In most cases, this value will
            // be false. If it *was* stolen by the user, we want to skip some physics-stuff and just allow the xrGrabInteractable to do its thing.
            if (!stolenByUser) 
            {
                EnableCollider(true);

                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;

                _rigidbody.useGravity = true; // Enable gravity in case it was disabled on spawn (which traditionally was how things spawned from the Item Spawner.)
            }

            // Reenable xrGrabInteractable so the player can pick it up again.
            if (xrGrabInteractable != null)
            {
                xrGrabInteractable.enabled = true;
            }

            TimeSinceLastReleased = Time.time;

            OnReleased?.Invoke(this);
        }

        private void Update()
        {
            if (state == ItemState.WaitingForInitialHold) 
            {
                // turning the collider to trigger seems to interfere with XRGrabInteractable, so we're just manually locking it.
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            else if (state == ItemState.DroppedByUserAndInMotion)
            {
                 if (_rigidbody.IsSleeping())
                 {
                     state = ItemState.DroppedByUserAndAtRest;
                     GlobalEventManager.Trigger(new UserPlacesItem(this));
                 }
            }

            CheckAndDestroyIfBelowHeight();

            CheckExpireTime();

            UpdateGrabTutorial();
        }

        private void CheckExpireTime()
        {
            // Don't expire if we're in debug mode.
            if (IsDebugMode) return;

            // Don't expire if expire time is set to zero or less.
            if (expireTime <= 0) return;

            // We cannot expire if we're in any of these states:
            if (state == ItemState.WaitingForInitialHold || state == ItemState.HeldByGenie || state == ItemState.HeldByUser) return;

            // We cannot expire if a genie intends to grab us.
            if (IsGrabbableByGenie && GenieGrabbable.IsTargetedByGenie) return;

            if (Time.time - TimeSinceLastReleased > expireTime)
            {
                Destroy(gameObject);
            }
        }

        private void CheckAndDestroyIfBelowHeight()
        {
             // We cannot auto-destroy if we're in any of these states:
            if (state == ItemState.WaitingForInitialHold || state == ItemState.HeldByGenie || state == ItemState.HeldByUser) return;

            // We cannot expire if a genie intends to grab us.
            if (IsGrabbableByGenie && GenieGrabbable.IsTargetedByGenie) return;

            if (transform.position.y < autoDestroyHeight)
            {
                Debug.Log("Destroying item " + name + " because it fell below the auto-destroy height.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            GlobalEventManager.Trigger(new ItemDestroyed(this));
        }

        private void EnableCollider(bool enable)
        {
            if (Collider.enabled && !enable)
            {
                _cachedBoundsSizeWhenUnrotated = Collider.GetBoundsWhenNotRotated().size;
            }

            Collider.enabled = enable;
        }

        private void OnCollisionEnter(Collision collision)
        {
            float force = collision.impulse.magnitude;

            OnPhysicalImpact?.Invoke(this, force);
        }

        private void OnUserPicksUpAnyItemInTheScene(UserPicksUpItem item)
        {
            _hasUserHasPickedUpAnyItemInScene = true;

            if (_grabTutorial != null) 
            {
                _grabTutorial.CollapseAndSelfDestruct();
                _grabTutorial = null;
            }
        }

        // Grab tutorial is supposed to be based on gaze, but unfortunately you can't actually get an "ongazed" event on vision pro,
        // so we have to fake it by checking the alignment of the camera and the item.

        private void UpdateGrabTutorial() 
        {
            if (xrGrabInteractable == null) return; // Don't do the grab tutorial if there's no xrGrabInteractable.
            if (_hasUserHasPickedUpAnyItemInScene) return;
            
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraToItem = transform.position - Camera.main.transform.position;
            cameraToItem.Normalize();

            float alignment = Vector3.Dot(cameraForward, cameraToItem);

            Debug.DrawLine(Camera.main.transform.position, transform.position, Color.green);
            //Debug.DrawLine(Camera.main.transform.position, Camera.main.transform.position + cameraForward, Color.red);

            if (alignment > 0.8f && xrGrabInteractable != null && xrGrabInteractable.enabled)
            {
                if (!_isGazeHovering)
                {
                    _isGazeHovering = true;
                    OnGazeHoverEnter();
                }
            }
            else
            {
                if (_isGazeHovering)
                {
                    _isGazeHovering = false;
                    OnGazeHoverExit();
                }
            }
        }

        private void OnGazeHoverEnter()
        {
            if (grabTutorialPrefab == null) return; // Don't do the grab tutorial at all, if there's no prefab reference.

            if (_grabTutorial == null)
            {
                _grabTutorial = Instantiate(grabTutorialPrefab);
                _grabTutorial.AttachTo(transform);
                _grabTutorial.transform.localPosition = Vector3.zero;
            }
            
            _grabTutorial.Expand();
        }

        private void OnGazeHoverExit()
        {
            if (_grabTutorial != null) _grabTutorial.Collapse();
        }
    }
}
