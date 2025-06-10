using System;
using System.Collections.Generic;
using GeneisIRL;
using TMPro;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Uses a simple heuristic to detect if the user is offering an item to the genie.
    /// </summary>
    [Serializable]
    public class DetectUserOfferingItem
    {
        /// <summary>
        /// Fires the moment a new item is offered AND in the Genie's view.
        /// </summary>
        public event Action<Item> OnGenieNoticesItemOffered;

        /// <summary>
        /// Returns a list of items offered by the user to the Genie. In most cases this list will be of size 0 or 1. Rarely, it might be 2 if the user
        /// is offering two items at once, one in each hand.
        /// </summary>
        public List<Item> ItemsOfferedThatGenieCanSee {get; private set;}

        [Header("Elbow Extension")]
        [Tooltip("To detect elbow extension, we measure the angle between the head and elbow. If it falls between a certain min " + 
         " and max we can detect that the user is extending their elbow in a way that suggests they are offering an item.")]
        [SerializeField] private float minElbowExtensionAngle = 75f;

        [Header("Hand Pointing")]
        [Tooltip("On the XZ plane only, the minimum dot product between head-to-hand and head-to-genie to be considered pointing toward the Genie.")]
        [SerializeField] private float minHandPointingToGenieDotProductXZ = 0.9f;

        [Tooltip("By comparing the head-to-hand pointing to direction to world up, we can filter out cases where the user" + 
            " is pointing too far up or down. The higher this value, the higher the tolerance for pointing up.")]
        [SerializeField] private float maxVerticalHandPointingDotProduct = 0.3f;

        [Tooltip("By comparing the head-to-hand pointing to direction to world up, we can filter out cases where the user" + 
            " is pointing too far own. The lower this value, the higher the tolerance for pointing down.")]
        [SerializeField] private float minVerticalHandPointingDotProduct = -0.8f;

        [Header("General Detection")]

        [Tooltip("The user must hold the pose for this duration for the Genie to consider it a valid offering.")]
        [SerializeField] private float minOfferingDuration = 3f;

        [SerializeField, Tooltip("When enabled, the Genie will use maxDistanceForGenieToAcknowledge and genieViewingAngle to determine if the Genie can see the user offering them an item. Otherwise, those will be ignored.")]
        private bool considerVisionCone = true;

        [SerializeField, Tooltip("The the max distance the Genie will 'see' the User offering them an item.")] 
        private float maxDistanceForGenieToAcknowledge = 4f;

        [SerializeField, Tooltip("The angle around the genie on the XZ plane that the Genie can 'see' the user offering them an item.")]
        private float genieViewingAngle = 120f;

        [Header("Debug")]
        [SerializeField, Tooltip("When enabled, in PolySpatial, it will display debug text over each hand. Without PolySpatial in the Editor, you can offer an Item by holding the 'O' key.")] 
        private bool enableDebug = false;
        [SerializeField] private TextMeshPro debugTextPrefab;

        [NonSerialized] private GenieSense _genieSense;
        
        private XRInputWrapper _xrInputWrapper;

        private TextMeshPro _debugTextLeft;
        private TextMeshPro _debugTextRight;

        private float _leftHandOfferDuration = 0f;
        private float _rightHandOfferDuration = 0f;

        public void OnStart(GenieSense genieSense) 
        {
            _genieSense = genieSense;

            _xrInputWrapper = _genieSense.Genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper;
        }

        public void OnUpdate() 
        {
            List<Item> itemsOffered = ProcessItemsOffered();

            List<Item> itemsOfferedGenieCanSee = FilterItemsGenieCanSee(itemsOffered);

            foreach (Item item in itemsOfferedGenieCanSee) 
            {
                // If there's a new item being offered, call an event.
                if (!ItemsOfferedThatGenieCanSee.Contains(item)) 
                {
                    OnGenieNoticesItemOffered?.Invoke(item);
                    break;
                }
            }

            ItemsOfferedThatGenieCanSee = itemsOfferedGenieCanSee;
        }

        private List<Item> FilterItemsGenieCanSee(List<Item> itemsOffered)
        {
            List<Item> itemsGenieCanSee = new List<Item>();

            // The next test is to identify which of these items can the Genie actually see. We'll be using proximity and angle to determine this.
            foreach (Item item in itemsOffered) 
            {
                if (CanGenieSeeItem(item)) 
                {
                    itemsGenieCanSee.Add(item);
                }
            }

            return itemsGenieCanSee;
        }

        private bool CanGenieSeeItem(Item item)
        {
            if (!considerVisionCone) // Ignore vision cone and just assume the Genie can see the user.
            {
                return true;
            }

            // Perform a proximity check
            if (!VectorUtils.IsWithinDistanceXZ(item.transform.position, _genieSense.Genie.transform.position, maxDistanceForGenieToAcknowledge))
            {
                return false;
            }

            // Perform an angle check
            Vector3 genieToItem = item.transform.position - _genieSense.Genie.transform.position;
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

        private List<Item> ProcessItemsOffered()
        {
            if (!GeniesIRL.App.XR.IsPolySpatialEnabled) // Without Polyspatial, we don't have arms or elbows to really test with, so we can't detect offering the same way. 
            {
                // If debug is enabled, we can offer using the "O" key
               if (enableDebug && Input.GetKey(KeyCode.O)) 
               {
                   Item heldItem = _xrInputWrapper.hands.GetAnyHeldItem(true);

                    if (heldItem != null) 
                    {
                        return new List<Item> { heldItem };
                    }
               }

               return new List<Item>();
            }

            // With PolySpatial, we can detect offering by the user holding an item in their hand and extending their elbow while pointing at the Genie.

            List<Item> itemsOffered = new List<Item>();

            Item item1 = GetItemOfferedByHand(InputHand.Right, ref _rightHandOfferDuration, ref _debugTextRight);
            Item item2 = GetItemOfferedByHand(InputHand.Left, ref _leftHandOfferDuration, ref _debugTextLeft);

            if (item1 != null) 
            {
                itemsOffered.Add(item1);
            }

            if (item2 != null) 
            {
                itemsOffered.Add(item2);
            }

            return itemsOffered;
        }

        private Item GetItemOfferedByHand(InputHand hand, ref float offerDuration, ref TextMeshPro debugText)
        {
            // Is hand holding an object?
            Item item = _xrInputWrapper.hands.GetItemHeldInHand(hand);
            bool isHandHoldingItem = item != null && item.IsGrabbableByGenie; // There MUST be an item and it MUST be grabbable by a Genie.

            // Right elbow extension
            float elbowAngle = GetAngleBetweenUserHeadAndElbow(hand);
            bool isElbowExtended = elbowAngle >= minElbowExtensionAngle;

            // Right hand pointing to Genie on XZ plane
            float handPointingDotProductXZ = GetHandPointingToGenieDotProductYaw(hand);
            bool isHandPointingAtGenieXZ = handPointingDotProductXZ >= minHandPointingToGenieDotProductXZ;

            // Check Right hand pointing too far up or down.
            float handPointingUpDotProduct = GetHandPointingUpDotProduct(hand);
            bool isHandPointingWithinVerticalRange = handPointingUpDotProduct <= maxVerticalHandPointingDotProduct && handPointingUpDotProduct >= minVerticalHandPointingDotProduct;
            
            // Calculate the duration the right hand has been in the offering pose.
            bool isValidOfferPose = isHandHoldingItem && isElbowExtended && isHandPointingAtGenieXZ && isHandPointingWithinVerticalRange;

            if (isValidOfferPose) 
            {
                offerDuration += Time.deltaTime;
            }
            else 
            {
                offerDuration = 0f;
            }

            bool isPoseHeldLongEnough = offerDuration >= minOfferingDuration;

            if (enableDebug) 
            {
                if (debugText == null) 
                {
                    debugText = GameObject.Instantiate(debugTextPrefab);
                }

                string elbowAngleStr = NumberFormatter.FormatNumber(elbowAngle, 0);
                string handPointingToGenieDotProductXZStr = NumberFormatter.FormatNumber(handPointingDotProductXZ, 2);
                string handPointingUpDotProductStr = NumberFormatter.FormatNumber(handPointingUpDotProduct, 2);

                string handStr = hand == InputHand.Right ? "R" : "L";
                debugText.text = handStr + " Elbow Angle: " + elbowAngleStr + 
                    " \n +  Point to Genie Dot: " + handPointingToGenieDotProductXZStr +
                    " \n +  Vertical Pointing Dot: " + handPointingUpDotProductStr + 
                    " \n + IsHolding Item: " + isHandHoldingItem +
                    " \n + Duration: " + NumberFormatter.FormatNumber(offerDuration, 1);

                Vector3 handPos = hand == InputHand.Right ? _xrInputWrapper.hands.RightHandPalm.position : _xrInputWrapper.hands.LeftHandPalm.position;
                debugText.transform.position = handPos + Vector3.up * 0.2f;
                debugText.color = isValidOfferPose ? Color.green : Color.red;

                if (isPoseHeldLongEnough) 
                {
                    debugText.color = Color.blue;
                }
            }

            if (isPoseHeldLongEnough) 
            {
                return item;
            }

            return null;
        }

        // Compares head-to-hand to head-to-genie on XZ plane and returns the dot product.
        private float GetHandPointingToGenieDotProductYaw(InputHand hand)
        {
            Transform userHead = _xrInputWrapper.Head;
            Transform handPalm = hand == InputHand.Right ? _xrInputWrapper.hands.RightHandPalm : _xrInputWrapper.hands.LeftHandPalm;
            Vector3 headToHand = handPalm.position - userHead.position;
            headToHand.y = 0f;
            headToHand.Normalize();

            Vector3 headToGenie = _genieSense.Genie.Collider.bounds.center - userHead.position;
            headToGenie.y = 0f;
            headToGenie.Normalize();

            float dot = Vector3.Dot(headToHand, headToGenie);

            return dot;
        }

        // Compares head-to-hand to world up vector and returns the dot product
        private float GetHandPointingUpDotProduct(InputHand hand) 
        {
            Transform userHead = _xrInputWrapper.Head;
            Transform handPalm = hand == InputHand.Right ? _xrInputWrapper.hands.RightHandPalm : _xrInputWrapper.hands.LeftHandPalm;
            Vector3 headToHand = handPalm.position - userHead.position;
            headToHand.Normalize();

            float dotUp = Vector3.Dot(headToHand, Vector3.up);

            return dotUp;
        }

        private float GetAngleBetweenUserHeadAndElbow(InputHand hand) 
        {
            if (hand != InputHand.Right && hand != InputHand.Left) 
            {
                Debug.LogError("Invalid hand value. Must be either InputHand.Right or InputHand.Left.");
                return 0f;
            }

            Transform userHead = _xrInputWrapper.Head;

            Ray elbowRay = hand == InputHand.Right ? _xrInputWrapper.hands.RightElbowRay : _xrInputWrapper.hands.LeftElbowRay;

            Vector3 dirFromElbowToHead = userHead.position - elbowRay.origin;

            // Get the direction to the right of the user's head (disregarding roll)
            Vector3 rightOfHead = userHead.right;
            rightOfHead.y = 0f;
            rightOfHead.Normalize();

            // Collapse the direction vector onto the same plane as the user's right direction.
            dirFromElbowToHead = Vector3.ProjectOnPlane(dirFromElbowToHead, rightOfHead);
            
            // Do the same collapse for the elbow direction.
            Vector3 elbowDirection = elbowRay.direction;
            elbowDirection = Vector3.ProjectOnPlane(elbowDirection, rightOfHead);

            // Now get the angle between thee two vectors.
            float angle = Vector3.Angle(dirFromElbowToHead, elbowDirection);

            if (enableDebug)
            {
                Debug.DrawRay(elbowRay.origin, dirFromElbowToHead, Color.blue, 0.1f);
                Debug.DrawRay(elbowRay.origin, elbowDirection, Color.green);
            }
            
            return angle;
        }
    }
}

