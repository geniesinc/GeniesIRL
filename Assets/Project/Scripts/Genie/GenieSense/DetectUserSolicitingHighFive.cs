using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Analyzes the player's body language to detect if they are soliciting a high five.
    /// </summary>
    [Serializable]
    public class DetectUserSolicitingHighFive
    {
        /// <summary>
        /// Invoked when the Genie notices the user soliciting a high five. Parameter is the user's hand transform.
        /// </summary>
        public event Action<Transform> OnGenieNoticesHighFiveSolicitation;
        public bool CanGenieSeeHighFiveSolicitation {get; private set;}

        public bool IsUserSolicitingHighFive {get; private set;}

        public Transform UserHandTransform {get; private set;}

        [SerializeField, Tooltip("When enabled, the Genie will use maxDistanceForGenieToAcknowledge and genieViewingAngle to determine if the Genie can see the user offering them an item. Otherwise, those will be ignored.")]
        private bool considerVisionCone = true;

        [SerializeField, Tooltip("The the max distance the Genie will 'see' the User's high five solicitation")] 
        private float maxDistanceForGenieToAcknowledge = 4f;

        [SerializeField, Tooltip("The angle around the genie on the XZ plane that the Genie can 'see' the user's high five solicitation")]
        private float genieViewingAngle = 120f;

        [Tooltip("The user must hold the pose for this duration for the Genie to consider it a valid offering.")]
        [SerializeField] private float minOfferingDuration = 2f;

        [NonSerialized]
        private GenieSense _genieSense;
        [NonSerialized]
        private XRInputWrapper _xrInputWrapper;

        [NonSerialized]
        private XRHandGestureManager _xrHandGestureManager;

        private bool _isHighFiveGesturePerformedInRightHand = false;
        private bool _isHighFiveGesturePerformedInLeftHand = false;

        private float _offerDuration = 0f;

        private Transform _debugUserHandTransform;

        public void OnStart(GenieSense genieSense) 
        {
            _genieSense = genieSense;
            _xrInputWrapper = _genieSense.Genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper;
            _xrHandGestureManager = _xrInputWrapper.hands.xrHandGestureManager;

            ListenForGestureEvents();
        }

        public void OnUpdate() 
        {
            bool canGenieSeeHighFiveSolicitation = ProcessGenieAcknowledgementOfHighFiveSolicitation(out Transform userHandTransform);

            UserHandTransform = userHandTransform;

            if (canGenieSeeHighFiveSolicitation == CanGenieSeeHighFiveSolicitation) return; // No change in state.

            CanGenieSeeHighFiveSolicitation = canGenieSeeHighFiveSolicitation;

            if (CanGenieSeeHighFiveSolicitation)
            {
                OnGenieNoticesHighFiveSolicitation?.Invoke(userHandTransform);
            }
        }

        private bool ProcessGenieAcknowledgementOfHighFiveSolicitation(out Transform userHandTransform)
        {
            IsUserSolicitingHighFive = CheckUserSolicitingHighFive(out userHandTransform);
            
            if (IsUserSolicitingHighFive)
            {
                _offerDuration += Time.deltaTime;
            }
            else
            {
                _offerDuration = 0f;
            }

            if (!IsUserSolicitingHighFive) return false;

            bool isPoseHeldLongEnough = _offerDuration >= minOfferingDuration;

            if (!isPoseHeldLongEnough) return false;

            bool canGenieSeeHighFiveSolicitation = CheckIfGenieSeeHighFiveSolicitation(userHandTransform);

            return canGenieSeeHighFiveSolicitation;
        }

        private bool CheckIfGenieSeeHighFiveSolicitation(Transform userHandTransform)
        {
            if (!considerVisionCone) // Ignore vision cone and just assume the Genie can see the user.
            {
                return true;
            }

            // Perform a proximity check
            if (!VectorUtils.IsWithinDistanceXZ(userHandTransform.position, _genieSense.Genie.transform.position, maxDistanceForGenieToAcknowledge))
            {
                return false;
            }

            // Perform an angle check
            Vector3 genieToItem = userHandTransform.position - _genieSense.Genie.transform.position;
            genieToItem.y = 0f;
            genieToItem.Normalize();

            Vector3 genieForward = _genieSense.Genie.transform.forward;
            genieForward.y = 0f;
            genieForward.Normalize();

            float angle = Vector3.Angle(genieForward, genieToItem);

            if (angle <= genieViewingAngle / 2f) 
            {
                return true;
            }

            return false;
        }

        private bool CheckUserSolicitingHighFive(out Transform userHandTransform)
        {
            if (!GeniesIRL.App.XR.IsPolySpatialEnabled) // Without Polyspatial, we don't have gestures to test with, so we're going to use the "H" key.
            {
                userHandTransform = null;

                // If debug is enabled, we can offer using the "H" key
                if (Input.GetKey(KeyCode.H))
                {
                     if (_debugUserHandTransform == null)
                     {
                         _debugUserHandTransform = CreateDebugUserHandTransform();
                     }

                     Transform userHead = _xrInputWrapper.Head;
                     _debugUserHandTransform.position = userHead.position + userHead.forward * 0.5f;
                     userHandTransform = _debugUserHandTransform;

                     return true;
                }

                return false;
            }

            userHandTransform = null;

            if (_isHighFiveGesturePerformedInLeftHand)
            {
                userHandTransform = _xrInputWrapper.hands.LeftHandPalm;

                // The user must be holding their hand high enough in relation to their head.
                if (userHandTransform.position.y > _xrInputWrapper.Head.position.y - 0.25f)
                {
                    return true;
                }
            }

            if (_isHighFiveGesturePerformedInRightHand)
            {
                userHandTransform = _xrInputWrapper.hands.RightHandPalm;
                
                // The user must be holding their hand high enough in relation to their head.
                if (userHandTransform.position.y > _xrInputWrapper.Head.position.y - 0.25f)
                {
                    return true;
                }
            }

            return false;
        }

        // When debugging in the Editor and without P2D, we need to create a 'fake' hand to simulate the user's hand.
        // Unlike in the GenieHighFiver which has its own debug hand transform, this is used only for testing high five gesture detection
        // and is not intended for the developer to use at runtime to test IK, etc. For that, you should look at the Proxy User Hand which is spawned
        // by the GenieHighFiver at runtime.
        private Transform CreateDebugUserHandTransform()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.1f;
            go.name = "DetectUserSolicitingHighFive: Debug User Hand";
            go.SetActive(false);

            return go.transform;
        }

        private void ListenForGestureEvents()
        {
            UserHandGesture[] gestures = _xrHandGestureManager.GetHandGesture(HandGestures.HighFive, InputHand.Both);
            
            foreach (UserHandGesture gesture in gestures)
            {
                gesture.OnGesturePerformed += OnHighFiveGesturePerformed;
                gesture.OnGestureEnded += OnHighFiveGestureEnded;
            }
        }

        private void OnHighFiveGesturePerformed(InputHand input)
        {
            if (input == InputHand.Left)
            {
                _isHighFiveGesturePerformedInLeftHand = true;
            }
            else if (input == InputHand.Right)
            {
                _isHighFiveGesturePerformedInRightHand = true;
            }
        }

        private void OnHighFiveGestureEnded(InputHand input)
        {
            if (input == InputHand.Left)
            {
                _isHighFiveGesturePerformedInLeftHand = false;
            }
            else if (input == InputHand.Right)
            {
                _isHighFiveGesturePerformedInRightHand = false;
            }
        }

    }
}

