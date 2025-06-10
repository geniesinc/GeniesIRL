using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace GeniesIRL 
{

    /// <summary>
    /// Facilitates the Genie's ability to grab and release items on either hand. Leverages animation and IK.
    /// TODO: As of writing, a lot of this code should go into the GenieAnimation class.
    [System.Serializable]
    public class GenieGrabber
    {
        public Transform RightHandTransform {get; private set;}
        public Transform LeftHandTransform {get; private set;}
        public Item LeftHandItem {get; private set;}
        public Item RightHandItem {get; private set;}

        /// <summary>
        /// Returns the right-hand item if there is one, otherwise returns the left-hand item. Returns null if neither hand
        /// is holding an item.
        /// </summary>
        public Item HeldItem => RightHandItem != null ? RightHandItem : LeftHandItem;

        /// <summary>
        /// Returns true if the Genie is holding an item in either hand.
        /// </summary>
        public bool IsHoldingItem => HeldItem != null;

        [FormerlySerializedAs("nearGrabReach")]
        public float grabReach = 0.5f;
        [FormerlySerializedAs("nearGrabLerpCurve")]
        public AnimationCurve grabLerpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("The adjustment to the hand rotation when grabbing an item. NOTE that this is optimized for the right hand -- the left hand may need its own opposite adjustment.")]
        public Vector3 grabHandRotationAdjustment = new Vector3(0, -90, -90);
        private Genie _genie;
        private Animator _animator;
        private GeniesIKComponent _geniesIKComponent;
        private Transform _transform;
        private Transform _leftHandAttachPoint;
        private Transform _rightHandAttachPoint;

        private float _rightHandGrabStartTime = -1f;
        private float _leftHandGrabStartTime = -1f;

        private Vector3 _rightGrabItemStartPos;
        private Quaternion _rightGrabItemStartRot;
        private Vector3 _leftGrabItemStartPos;
        private Quaternion _leftGrabItemStartRot;

        private Coroutine _grabCoroutine;
        private Coroutine _releaseCoroutine;

        private Coroutine _yawCoroutine;

        public void OnStart(Genie genie)
        {
            _genie = genie;

            _transform = genie.transform;
            _animator = _genie.genieAnimation.Animator;
            _geniesIKComponent = _genie.genieAnimation.GeniesIKComponent;

            RightHandTransform = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            LeftHandTransform = _animator.GetBoneTransform(HumanBodyBones.LeftHand);

            CreateHandAttachPoints();
        }

        public void OnUpdate()
        {
            UpdateHandItemPosition();
        }

        public Vector3 GetHandAttachmentOffset(GenieHand handSide)
        {
            if (handSide == GenieHand.Right)
            {
                return _rightHandAttachPoint.transform.localPosition;
            }
            else
            {
                return _leftHandAttachPoint.transform.localPosition;
            }
        }

        private void UpdateHandItemPosition()
        {
            UpdateGrabbedItemPosition(false);
            UpdateGrabbedItemPosition(true);
        }

        private void UpdateGrabbedItemPosition(bool rightHand) 
        {
            Item item = rightHand ? RightHandItem : LeftHandItem;

            if (item == null) return;

            Transform attachPoint = rightHand ? _rightHandAttachPoint : _leftHandAttachPoint;
            Vector3 itemStartPos = rightHand ? _rightGrabItemStartPos : _leftGrabItemStartPos;
            Quaternion itemStartRot = rightHand ? _rightGrabItemStartRot : _leftGrabItemStartRot;

            // Based on the time since the grab started, determine how much to lerp the item towards the hand.
            float grabStartTime = rightHand ? _rightHandGrabStartTime : _leftHandGrabStartTime;
            float grabEndTime = grabStartTime + grabLerpCurve.keys[grabLerpCurve.length - 1].time;
            float t = Mathf.InverseLerp(grabStartTime, grabEndTime, Time.time);
            float lerpValue = grabLerpCurve.Evaluate(t);

            Vector3 targetPos = attachPoint.TransformPoint(item.GenieGrabbable.inGenieHandOffset);

            Quaternion targetRot = attachPoint.rotation;
            targetRot *= Quaternion.Euler(item.GenieGrabbable.inGenieHandRotationOffset);

            item.transform.position = Vector3.Lerp(itemStartPos, targetPos, lerpValue);
            item.transform.rotation = Quaternion.Slerp(itemStartRot, targetRot, lerpValue);
        }

        private void CreateHandAttachPoints()
        {
            // Create Hands attach point.
            // Since hands axis can't be assumed between avatars, it looks for joints as reference. Right now is name dependant on the joints.

            //Right Side
            Transform ringJoint = _transform.FindDeepChild("RightHandRing1");
            Transform middleJoint = _transform.FindDeepChild("RightHandMiddle1");
            Transform thumbJoint = _transform.FindDeepChild("RightHandThumb3");

            Vector3 rightPalmNormal = GetPalmPlaneNormal(RightHandTransform.position, ringJoint.position, middleJoint.position, thumbJoint.position);
            Vector3 rightMidPosition = Vector3.Lerp(RightHandTransform.position, Vector3.Lerp(middleJoint.position, ringJoint.position, 0.5f), 0.75f);

            GameObject rightHandPoint = new GameObject("RightHandAttachPoint");
            _rightHandAttachPoint = rightHandPoint.transform;
            _rightHandAttachPoint.SetParent(RightHandTransform);
            _rightHandAttachPoint.localRotation = Quaternion.Inverse(Quaternion.Euler(_geniesIKComponent.rightHandRotationOffset));
            _rightHandAttachPoint.localScale = Vector3.one;
            _rightHandAttachPoint.position = rightMidPosition + rightPalmNormal * Vector3.Distance(ringJoint.position, middleJoint.position) * 1f;

            //Right Side
            ringJoint = _transform.FindDeepChild("LeftHandRing1");
            middleJoint = _transform.FindDeepChild("LeftHandMiddle1");
            thumbJoint = _transform.FindDeepChild("LeftHandThumb3");

            Vector3 leftPalmNormal = GetPalmPlaneNormal(LeftHandTransform.position, ringJoint.position, middleJoint.position, thumbJoint.position);
            Vector3 leftMidPosition = Vector3.Lerp(LeftHandTransform.position, Vector3.Lerp(middleJoint.position, ringJoint.position, 0.5f), 0.75f);

            GameObject leftHandPoint = new GameObject("LeftHandAttachPoint");
            _leftHandAttachPoint = leftHandPoint.transform;
            _leftHandAttachPoint.SetParent(LeftHandTransform);
            _leftHandAttachPoint.localRotation = Quaternion.Inverse(Quaternion.Euler(_geniesIKComponent.leftHandRotationOffset));
            _leftHandAttachPoint.localScale = Vector3.one;
            _leftHandAttachPoint.position = leftMidPosition + leftPalmNormal * Vector3.Distance(ringJoint.position, middleJoint.position) * 1f;
        }

         private Vector3 GetPalmPlaneNormal(Vector3 wrist, Vector3 middle, Vector3 ring, Vector3 thumb)
        {
            // Calculate vectors lying on the plane
            Vector3 vector1 = middle - wrist;
            Vector3 vector2 = ring - wrist;

            // Calculate the normal using the cross product
            Vector3 normal = Vector3.Cross(vector1, vector2).normalized;

            // Calculate the vector from the plane to the fourth point
            Vector3 pointToFourth = thumb - wrist;

            // Check if the normal is pointing in the correct direction
            if (Vector3.Dot(normal, pointToFourth) < 0)
            {
                // If not, reverse the direction of the normal
                normal = -normal;
            }
            return normal;
        }
        
        /// <summary>
        /// Intended to be used when the Genie spawns and item, so that it can instantly appear in her hand. 
        /// </summary>
        /// <param name="handSide"></param>
        /// <param name="item"></param>
        public void InstantGrabAndTeleportToHand(GenieHand handSide, Item item)
        {
            if (handSide == GenieHand.Right)
            {
                RightHandItem = item;
                item.transform.position = _rightHandAttachPoint.position;
                item.transform.rotation = _rightHandAttachPoint.rotation;
            }
            else
            {
                LeftHandItem = item;
                item.transform.position = _leftHandAttachPoint.position;
                item.transform.rotation = _leftHandAttachPoint.rotation;
            }

            item.GenieGrabbable.PerformGrab(OnUserStoleItem);
        }

        public void GrabObject(GenieHand handSide, Item item, bool andExamine, Action finishedCallback = null)
        {
            if (!item.IsGrabbableByGenie) 
            {
                Debug.LogWarning("Genie can't grab this item.");
                return;
            }

            _grabCoroutine = _genie.StartCoroutine(GrabObject_C(handSide, item, andExamine, finishedCallback));
        }

        private IEnumerator GrabObject_C(GenieHand handSide, Item item, bool andExamine, Action finishedCallback = null)
        {
            // Turn to face the object.
            _yawCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(item.transform.position, 4f));

            yield return _yawCoroutine;

            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(item.transform);

            Vector3 itemPosXY = new Vector3(item.transform.position.x, 0, item.transform.position.z);
            Vector3 geniePosXY = new Vector3(_transform.position.x, 0, _transform.position.z);
           
            // Grab Item from its inHand offset position
            // Convert the local offset to world space
            Vector3 worldOffset = item.transform.TransformPoint(item.GenieGrabbable.inGenieHandOffset) - item.transform.position;
            // Subtract the world space offset from the GameObject's world position
            Vector3 targetPosition = item.transform.position - worldOffset;

            // Rotate the hand to sort of a "handshake" position
            Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
            Quaternion grabRotation = Quaternion.LookRotation(targetPosition - head.position, _genie.transform.up); 
            grabRotation *= Quaternion.Euler(grabHandRotationAdjustment); // Rotate the hand to "handshake" position 

            // Extend the arm (and spine if necessary) to reach the item.
            _geniesIKComponent.ReachTowardsPosition(targetPosition, handSide, grabRotation);
            
            if (handSide == GenieHand.Right)
            {
                _animator.SetTrigger(GenieAnimation.Triggers.RStartGrabbing);
            }
            else
            {
                _animator.SetTrigger(GenieAnimation.Triggers.LStartGrabbing);
            }

            // 0.9569004 is the grab time clip
            yield return new WaitForSeconds(0.9569004f);

            // Place Item on hand
            if (handSide == GenieHand.Right)
            {
                RightHandItem = item;
                _rightHandGrabStartTime = Time.time;
                _rightGrabItemStartPos = item.transform.position;
                _rightGrabItemStartRot = item.transform.rotation;
            }
            else
            {
                LeftHandItem = item;
                _leftHandGrabStartTime = Time.time;
                _leftGrabItemStartPos = item.transform.position;
                _leftGrabItemStartRot = item.transform.rotation;
            }

            item.GenieGrabbable.PerformGrab(OnUserStoleItem);

            // Wait for the grab to finish flying to the hand.
            yield return new WaitForSeconds(GetGrabCurveDuration());

            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
            
            if (handSide == GenieHand.Right)
            {
                if (andExamine)
                {
                    _animator.SetTrigger("rLookAtHeldObject"); // HACK -- show the Genie examining the held item
                }
                else
                {
                    _animator.SetTrigger(GenieAnimation.Triggers.RItemGrabbed);
                }
            }
            else{
                _animator.SetTrigger(GenieAnimation.Triggers.LItemGrabbed);
            }

            float endWaitTime = andExamine ? 3f : 1.0f;

            yield return new WaitForSeconds(endWaitTime);

            finishedCallback?.Invoke();
        }

        private float GetGrabCurveDuration()
        {
            return grabLerpCurve.keys[grabLerpCurve.length - 1].time;
        }

        /// <summary>
        /// Releases an object from the Genie's hand and places it at the specified destination.
        /// </summary>
        /// <param name="handSide"></param>
        /// <param name="itemDestination"></param>
        /// <param name="finishedCallback"></param>
        public void ReleaseObject(GenieHand handSide, Vector3 itemDestination, Action finishedCallback = null)
        {
            _releaseCoroutine = _genie.StartCoroutine(ReleaseObject_C(handSide, itemDestination, finishedCallback));
        }

        private IEnumerator ReleaseObject_C(GenieHand handSide, Vector3 itemDestination, Action finishedCallback)
        {
             // Turn to face the item destination
            _yawCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(itemDestination, 4f));

            _genie.genieLookAndYaw.eyeballAimer.TrackLocation(itemDestination);

            yield return _yawCoroutine;

            // Rotate the hand to sort of a "handshake" position
            Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
            Quaternion grabRotation = Quaternion.LookRotation(itemDestination - head.position, _genie.transform.up); 
            grabRotation *= Quaternion.Euler(grabHandRotationAdjustment);

            // Extend the arm (and spine if necessary) to reach the item destination.
            _geniesIKComponent.ReachTowardsPosition(itemDestination, handSide, grabRotation);

            // TODO Work with animation -> Provide animations!
            if (handSide == GenieHand.Right)
            {
                _animator.SetTrigger(GenieAnimation.Triggers.RStartPlacingHand);
            }
            else
            {
                _animator.SetTrigger(GenieAnimation.Triggers.LStartPlacingHand);
            }

            // 0.6052634 is the length of the clip rn
            yield return new WaitForSeconds(0.6052634f);

            Item item;

            if (handSide == GenieHand.Right)
            {
                item = RightHandItem;
                RightHandItem = null;
            }
            else
            {
                item = LeftHandItem;
                LeftHandItem = null;
            }

            float lerpDuration = 0.25f;

            // Lerp into position
            float startTime = Time.time;
            float endTime = startTime + lerpDuration;

            Vector3 startPos = item.transform.position;

            float t = 0;
            while (t < 1f)
            {
                t = Mathf.InverseLerp(startTime, endTime, Time.time);
                item.transform.position = Vector3.Lerp(startPos, itemDestination, t);
                yield return null;
            }
            
            item.transform.position = itemDestination;
            item.GenieGrabbable.PerformRelease();

            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();

             if (handSide == GenieHand.Right)
            {
                _animator.SetTrigger(GenieAnimation.Triggers.RRestHand);
            }
            else
            {
                _animator.SetTrigger(GenieAnimation.Triggers.LRestHand);
            }
            yield return new WaitForSeconds(1f);

            finishedCallback?.Invoke();
        }

        public void InstantReleaseHeldItem() 
        {
            if (RightHandItem != null) 
            {
                RightHandItem.GenieGrabbable.PerformRelease();
                RightHandItem = null;
            }
            if (LeftHandItem != null) 
            {
                LeftHandItem.GenieGrabbable.PerformRelease();
                LeftHandItem = null;
            }
        }

        /// <summary>
        /// If the PickUpItemAction is interrupted, or if the ReachAndPlaceItemAction is interrupted, we need to quickly end the behavior and clean up.
        /// </summary>
        public void ExternallyCancelGrabOrRelease()
        {
            if (_grabCoroutine != null)
            {
                _genie.StopCoroutine(_grabCoroutine);
                _grabCoroutine = null;
            }

            if (_releaseCoroutine != null)
            {
                _genie.StopCoroutine(_releaseCoroutine);
                _releaseCoroutine = null;
            }

            if (_yawCoroutine != null)
            {
                _genie.StopCoroutine(_yawCoroutine);
                _yawCoroutine = null;
            }

            InstantReleaseHeldItem();

            _animator.SetTrigger(GenieAnimation.Triggers.RRestHand);
            _animator.SetTrigger(GenieAnimation.Triggers.LRestHand);
        }

        public void OnDrawGizmos()
        {
            // if (_genie == null) return;
            // Gizmos.color = Color.red;
            // Gizmos.DrawSphere(_genie.transform.position, nearGrabReach);
        }

        /// <summary>
        /// Special grab action for debugging. Target object does not need to be an Item.
        /// </summary>
        /// <param name="target"></param>
        public void DebugReachTarget(Transform target)
        {
            Debug.Log("DebugReachTarget: " + target.name);

            _genie.genieLookAndYaw.InstantYawTowards(target.position);

            Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
            Quaternion grabRotation = Quaternion.LookRotation(target.position - head.position, _genie.transform.up);
            grabRotation *= Quaternion.Euler(grabHandRotationAdjustment); // Rotate the hand to "handshake" position 

            _geniesIKComponent.ReachTowardsPosition(target.position, GenieHand.Right, grabRotation);
            _animator.SetTrigger(GenieAnimation.Triggers.RStartGrabbing);

            //_genie.StartCoroutine(DebugKeepReaching_C(target));
        }

        // private IEnumerator DebugKeepReaching_C(Transform target)
        // {
        //     while (true)
        //     {
        //         yield return null;
        //          Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
        //         Quaternion grabRotation = Quaternion.LookRotation(target.position - head.position, _genie.transform.up);
        //         grabRotation *= Quaternion.Euler(grabHandRotationAdjustment); // Rotate the hand to "handshake" position 
        //         _geniesIKComponent.ReachTowardsPosition(target.position, GenieHand.Right, grabRotation);

        //     }
        // }

        // At the time of writing, this should only happen when the Genie is offering the item to the user.
        // We'll just need to quickly wrap things up here if that happens.
        private void OnUserStoleItem(Item item)
        {
            if (item == RightHandItem)
            {
                RightHandItem = null;
            }
            else if (item == LeftHandItem)
            {
                LeftHandItem = null;
            }
        }

    }
}
