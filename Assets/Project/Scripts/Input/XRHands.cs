using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Management;
using UnityEngine.XR.VisionOS;


namespace GeniesIRL 
{

    public enum InputHand
    {
        Left,
        Right,
        Undefined,
        Both
    }
    
    /// <summary>
    /// Used as a wrapper to represent the hands of the user in XR, including positions, poses, etc. At the time of writing, this logic is scattered,
    /// so we'll be using this class to consolidate that over time. At the start, much of the code will be sourced from the old "InputManager" class
    /// created for this project in its early days.
    /// </summary>
    [Serializable]
    public class XRHands
    {
        public XRHandGestureManager xrHandGestureManager;
        public Transform LeftHandPalm {get; private set;}
        public Transform RightHandPalm {get; private set;}
        /// <summary>
        /// A ray that originates at the left elbow and points towards the hand.
        /// </summary>
        public Ray LeftElbowRay {get{return _leftElbowRay;}}
        /// <summary>
        /// A ray that originates at the right elbow and points towards the hand.
        /// </summary>
        public Ray RightElbowRay {get{return _rightElbowRay;}}

        public bool debugVisualizeHands = false;

        private Pose _leftHandPalmPose = new Pose();
        private Pose _rightHandPalmPose = new Pose();
        private XRHandSubsystem xrHandSubsystem;
        private const float palmOffsetRatio = 0.4f;
        private bool _debugVisualizeHandsLastFrame = false;
        private Transform _debugLeftHandPalmVisualizer;
        private Transform _debugRightHandPalmVisualizer;
        private Transform _debugLeftElbowVisualizer;
        private Transform _debugRightElbowVisualizer;

        private Ray _leftElbowRay = new Ray();
        private Ray _rightElbowRay = new Ray();

        private List<Item> _itemsHeld = new List<Item>();

        public void OnInitialize(XRNode xrNode) 
        {
            LeftHandPalm = new GameObject("LeftHandPalm").transform;
            LeftHandPalm.SetParent(xrNode.xrInputWrapper.transform);
            RightHandPalm = new GameObject("RightHandPalm").transform;
            RightHandPalm.SetParent(xrNode.xrInputWrapper.transform);

            InitializeInteractors(xrNode);

            if (!GeniesIRL.App.XR.IsPolySpatialEnabled)
            {
                Debug.Log("<color=yellow>XRHands requires Polyspatial to be enabled. User will not have hand tracking.</color>");
                return;
            }

            InitializeXrHandSubsystem();
        }
        
        /// <summary>
        /// Compares the distances beteween the hands and the items being held to determine which hand is holding the item. (Warning: this is not fool-proof and
        /// fails when the hands are close together. Read more in the comment in the method.)
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public Item GetItemHeldInHand(InputHand hand)
        {
            // NOTE: There currently doesn't seem to be a straightforward way of getting the handedness of the interactor. Yes,
            // there is a "handedness" property, but it's set to "Right" in both the Primary and Secondary interactors in accordance
            // with the XR Interaction Toolkit 3.0.7 sample for visionOS. The way it works is that the Primary interactor gets used first
            // if the user pinches or touches, and then the Secondary interactor gets used if the user makes another pinch or touch with the 
            // second hand. So the "handeness" property is meaningless since either hand can be used for either interactor.
            
            // There doesn't seem to be any other obvious way of getting the handedness of a touch or pinch, so we're instead going to just
            // keep track of which items are being held and then compare distances to each hand to determine which hand is holding the item.
            // It should be noted that this might fail at times, but it should work for what we need it for at the time of writing.

            Vector3 leftHandPos = LeftHandPalm.position;
            Vector3 rightHandPos = RightHandPalm.position;

            Vector3 thisHandPos = (hand == InputHand.Left) ? leftHandPos : rightHandPos;
            Vector3 otherHandPos = (hand == InputHand.Left) ? rightHandPos : leftHandPos;

            Debug.Assert(_itemsHeld.Count <= 2, "XRHands._itemsHeld is tracking " + _itemsHeld.Count + " held Items. Does the user have more than two hands??");

            // Iterate through each Item and compare the distance to each hand. The hand that is closest to the Item is the hand that is holding it.
            for (int i=_itemsHeld.Count-1; i>=0; i--)
            {
                Item item = _itemsHeld[i];

                if (item == null) // This shouldn't happen, but just in case...
                {
                    _itemsHeld.RemoveAt(i);
                    continue;
                }

                Vector3 itemPos = item.transform.position;

                float sqrDistanceToThisHand = (thisHandPos - itemPos).sqrMagnitude;
                float sqrDistanceToOtherHand = (otherHandPos - itemPos).sqrMagnitude;

                if (sqrDistanceToThisHand < sqrDistanceToOtherHand)
                {
                    return item;
                }
            }

            return null;
        }

        private void InitializeInteractors(XRNode xrNode)
        {
            NearFarInteractor[] nearFarInteractors = xrNode.xrInputWrapper.GetComponentsInChildren<NearFarInteractor>();
            
            foreach (NearFarInteractor nearFarInteractor in nearFarInteractors)
            {
                nearFarInteractor.selectEntered.AddListener(OnInteractorGrab);
                nearFarInteractor.selectExited.AddListener(OnInteractorRelease);
            }
        }

        private void OnInteractorGrab(SelectEnterEventArgs args)
        {
            Item item = args.interactableObject.transform.GetComponent<Item>();

            if (item == null) return;

            Debug.Assert(_itemsHeld.Count < 2, "XRHands._itemsHeld is already tracking " + _itemsHeld.Count + " held Items, and you're trying to track a third. Does the user have more than two hands??");
            
            if (_itemsHeld.Contains(item))
            {
                Debug.LogError("Item is already being tracked in XRHands._itemsHeld, but it's being grabbed again. This should not happen.");
                return;
            }

            _itemsHeld.Add(item);
        }

        private void OnInteractorRelease(SelectExitEventArgs args)
        {
            Item item = args.interactableObject.transform.GetComponent<Item>();
            
            if (item == null) return;

            if (!_itemsHeld.Contains(item))
            {
                Debug.LogError("Item is not being tracked in XRHands._itemsHeld, but it's being released. This should not happen.");
                return;
            }

            _itemsHeld.Remove(item);
        }

        public void OnLateUpdate() 
        {
            if (xrHandSubsystem == null) return;

            UpdateHandTracking();

            UpdateDebugVisualizer();
        }

        /// <summary>
        /// Returns a ray with the origin at the elbow elbow and aims in the direction of the hand.
        /// </summary>
        /// <param name="whichHand"></param>
        /// <returns></returns>
        public Ray GetElbowRay(InputHand whichHand)
        {
            if (xrHandSubsystem == null) 
            {
                Debug.LogError("XRHandSubsystem not initialized.");
                return new Ray();
            }

            return new Ray();
        }

        /// <summary>
        /// Returns any item being held, or null if no item is being held.
        /// </summary>
        /// <param name="mustBeGrabbableByGenie">If true, it will ignore items not grabbable by the Genie.</param>
        /// <returns></returns>
        public Item GetAnyHeldItem(bool mustBeGrabbableByGenie)
        {
            // Iterate backwards through Items held
            for (int i=_itemsHeld.Count-1; i>=0; i--)
            {
                Item item = _itemsHeld[i];

                if (mustBeGrabbableByGenie && !item.IsGrabbableByGenie)
                {
                    continue;
                }

                if (item == null) // This shouldn't happen, but just in case...
                {
                    _itemsHeld.RemoveAt(i);
                    continue;
                }

                return item;
            }

            return null;
        }

        private void InitializeXrHandSubsystem()
        {
            XRGeneralSettings xrGeneralSettings = XRGeneralSettings.Instance;
            if (xrGeneralSettings == null)
            {
                Debug.LogError("XRGeneralSettings.Instance not set.");
                return;
            }

            XRManagerSettings manager = xrGeneralSettings.Manager;
            if (manager == null)
            {
                Debug.LogError("XRGeneralSettings.Instance.Manager not set.");
                return;
            }

            XRLoader loader = manager.activeLoader;
            if (loader == null)
            {
                Debug.LogError("XRGeneralSettings.Instance.Manager.activeLoader not set.");
                return;
            }

            // Initialize Subsystem
            xrHandSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
            if (xrHandSubsystem == null)
            {
                Debug.LogError("XRGeneralSettings.Instance.Manager.activeLoader has no XRHandSubsystem loaded.");
                return;
            }

            // Yay!
            xrHandSubsystem.Start();
        }

        private void UpdateHandTracking() 
        {
            var updateSuccessFlags = xrHandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);

            // Update palm poses.
            TryUpdatePalmPose(InputHand.Left, updateSuccessFlags, ref _leftHandPalmPose);
            TryUpdatePalmPose(InputHand.Right, updateSuccessFlags, ref _rightHandPalmPose);

            LeftHandPalm.position = _leftHandPalmPose.position;
            LeftHandPalm.rotation = _leftHandPalmPose.rotation;

            RightHandPalm.position = _rightHandPalmPose.position;
            RightHandPalm.rotation = _rightHandPalmPose.rotation;

            // Update elbow rays.
            TryUpdateElbowRay(InputHand.Left, updateSuccessFlags, ref _leftElbowRay);
            TryUpdateElbowRay(InputHand.Right, updateSuccessFlags, ref _rightElbowRay);
        }
    
        private void TryUpdatePalmPose(InputHand whichHand, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, ref Pose handPalmPose)
        {
            var handRootBit = (whichHand == InputHand.Right) ? XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose :
                                                                XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose;

            var xrHand = (whichHand == InputHand.Right) ? xrHandSubsystem.rightHand : xrHandSubsystem.leftHand;

            // If the data has been updated successfully for this hand,
            if ((updateSuccessFlags & handRootBit) != 0)
            {
                // Calculate middle point to get palm.
                XRHandJoint middleMetacarpalJoint = xrHand.GetJoint(XRHandJointID.MiddleMetacarpal);
                XRHandJoint middleProximalJoint = xrHand.GetJoint(XRHandJointID.MiddleProximal);

                if (middleMetacarpalJoint.TryGetPose(out Pose middleMetacarpalJointPose) &&
                    middleProximalJoint.TryGetPose(out Pose middleProximalJointPose))
                {
                    Vector3 palmPosition = Vector3.Lerp(middleMetacarpalJointPose.position,
                                                        middleProximalJointPose.position,
                                                        palmOffsetRatio);

                    handPalmPose.position = palmPosition;
                    handPalmPose.rotation = middleMetacarpalJointPose.rotation;
                }
            }
        }

        private void TryUpdateElbowRay(InputHand whichHand, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, ref Ray elbowRay)
        {
            var handRootBit = (whichHand == InputHand.Right) ? XRHandSubsystem.UpdateSuccessFlags.RightHandJoints :
                                                                XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;

            var xrHand = (whichHand == InputHand.Right) ? xrHandSubsystem.rightHand : xrHandSubsystem.leftHand;

            // If the data has been updated successfully for this hand,
            if ((updateSuccessFlags & handRootBit) == 0) return;
            
            // Calculate middle point to get palm.
            XRHandJoint elbowJoint = xrHand.GetVisionOSJoint(VisionOSHandJointID.ForearmArm);

            if (elbowJoint.TryGetPose(out Pose elbowJointPose))
            {
                elbowRay.origin = elbowJointPose.position;
                elbowRay.direction = -elbowJointPose.forward;
            }
        }

        private void UpdateDebugVisualizer()
        {
            if (debugVisualizeHands != _debugVisualizeHandsLastFrame)
            {
                if (debugVisualizeHands) 
                {
                    _debugLeftHandPalmVisualizer = CreatePalmVisualizer("DebugLeftHandPalmVisualizer", LeftHandPalm);
                    _debugRightHandPalmVisualizer = CreatePalmVisualizer("DebugRightHandPalmVisualizer", RightHandPalm);
                    _debugLeftElbowVisualizer = CreateElbowVisualizer("DebgugLeftElbowVisualizer", LeftElbowRay);
                    _debugRightElbowVisualizer = CreateElbowVisualizer("DebgugRightElbowVisualizer", RightElbowRay);
                }
                else
                {
                    GameObject.Destroy(_debugLeftHandPalmVisualizer.gameObject);
                    _debugLeftHandPalmVisualizer = null;
                    GameObject.Destroy(_debugRightHandPalmVisualizer.gameObject);
                    _debugRightHandPalmVisualizer = null;

                    GameObject.Destroy(_debugLeftElbowVisualizer.gameObject);
                    _debugLeftElbowVisualizer = null;
                    GameObject.Destroy(_debugRightElbowVisualizer.gameObject);
                    _debugRightElbowVisualizer = null;
                }

                _debugVisualizeHandsLastFrame = debugVisualizeHands;
            }

            UpdateElbowVisualizer(_debugLeftElbowVisualizer, LeftElbowRay);
            UpdateElbowVisualizer(_debugRightElbowVisualizer, RightElbowRay);
        }

        private Transform CreatePalmVisualizer(string v, Transform parentPalm)
        {
            Transform visualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            GameObject.Destroy(visualizer.GetComponent<Collider>());
            visualizer.name = v;
            visualizer.localScale = Vector3.one * 0.15f;
            visualizer.SetParent(parentPalm);
            visualizer.localPosition = Vector3.zero;
            return visualizer;  
        }

        private Transform CreateElbowVisualizer(string gameObjectName, Ray elbowRay)
        {
            Transform visualizer = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            GameObject.Destroy(visualizer.GetComponent<Collider>());
            visualizer.name = gameObjectName;

            UpdateElbowVisualizer(visualizer, elbowRay);
            
            return visualizer;
        }

        private void UpdateElbowVisualizer(Transform visualizer, Ray elbowRay)
        {
            if (visualizer == null) return;

            float visualizerLength = 0.25f;
            visualizer.localScale = new Vector3(0.05f, 0.05f, visualizerLength);
            visualizer.position = elbowRay.origin + elbowRay.direction * visualizerLength / 2;
            visualizer.rotation = Quaternion.LookRotation(elbowRay.direction);
        }
    }
}

