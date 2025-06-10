using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GeniesIRL;
using Pathfinding;
using UnityEngine;

namespace GeneisIRL 
{
    /// <summary>
    /// Gives the Genie the ability to sit and stand. At present, this logic is pulled straight from the
    /// legacy script GenieController.cs, and it is subject to heavy revision.
    /// </summary>
    [Serializable]
    public class GenieSitAndStand
    {
        /// <summary>
        /// Returns true only if the Genie is starting to sit, sitting, or in the middle of standing up from sitting.
        /// </summary>
        public bool IsSittingOrInTransition {get; private set;}
        [SerializeField] private float _yOffsetWhileSittingFromJumpAndTwirl = -0.75f;
        [SerializeField] private float _yOffsetWhileSittingFromNonJumpAndTwirl =-0.45f;
        [System.NonSerialized] private Genie _genie;
        [System.NonSerialized] private Animator _animator;
        [System.NonSerialized] private GeniesIKComponent _geniesIKComponent;

        private Coroutine _nestedCoroutine1;
        private Coroutine _nestedCoroutine2;

        public void OnStart(Genie genie)
        {
            _genie = genie;
            _animator = genie.genieAnimation.Animator;
            _geniesIKComponent = genie.genieAnimation.GeniesIKComponent;
        }

        /// <summary>
        /// Sit down on a seat.
        /// </summary>
        /// <param name="seatPosition">Center point of seat. Genie will be positioned here.</param>
        /// <param name="seatRotation">Rotation of seat - Genie will be rotated to match.</param>
        /// <param name="posInterpolationTime">-1 means it will match the animation clip length. Otherwise it will be clamped to the animation clip length.</param>
        /// <param name="rotInterpolationTime">-1 means it will match the animation clip length. Otherwise it will be clamped to the animation clip length.</param>
        /// <returns></returns>
        public IEnumerator Sit_C(Vector3 seatPosition, Quaternion seatRotation, bool useJumpAndTwirl)
        {
            IsSittingOrInTransition= true;

            _genie.genieNavigation.PositionCharacterOnFloor = false; // Disable the Y positioning while we're sitting.

            if (useJumpAndTwirl)
            {
                _animator.SetTrigger(GenieAnimation.Triggers.Sit2);
            }
            else
            {
                _animator.SetTrigger(GenieAnimation.Triggers.Sit);
            }

            // Determine interpolation time.
            if (useJumpAndTwirl) 
            {
                // New sit type.
                Vector3 offset = new Vector3(0, _yOffsetWhileSittingFromJumpAndTwirl, 0);
                _nestedCoroutine1 = _genie.StartCoroutine(JumpAndTwirl_Lerp_C(seatPosition + offset, seatRotation, _jumpAndTwirlIntroCurve, _jumpAndTwirlExtraYElevationCurveForIntro));
                yield return _nestedCoroutine1;
            } 
            else
            {
                // Old sit type. (Subject for deletion)
                string standToSitStateName = "StandToSit";

                yield return new WaitUntil(() => _animator.IsInState(standToSitStateName)); // wait for the animation to start.

                AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
                float clipLength = clipInfo[0].clip.length;

                float minInterpolationTime = 1f;
                float posInterpolationTime = Mathf.Min(minInterpolationTime, clipLength);
                float rotInterpolationTime = Mathf.Min(minInterpolationTime, clipLength);

                // Interpolate to seat position and rotation. This will happen in parallel with the animation.
                _nestedCoroutine1 = _genie.StartCoroutine(LerpToSeatPosition_C(seatPosition, posInterpolationTime));
                _nestedCoroutine2 = _genie.StartCoroutine(LerpToSeatRotation_C(seatRotation, rotInterpolationTime));

                // Wait until Sitting animation completed
                yield return new WaitForSeconds(clipLength);
            }
        }

        public IEnumerator StandUp_C(bool useJumpAndTwirl)
        {
            if (useJumpAndTwirl)
            {
                _animator.SetTrigger(GenieAnimation.Triggers.StandUp2);
            }
            else
            {
                _animator.SetTrigger(GenieAnimation.Triggers.StandUp);
            }
            
            if (useJumpAndTwirl) 
            {
                // New method of standing up.
                Vector3 startPos = _genie.transform.position;

                // Locate the nearest walkable node. That's where we want our genie to end up.
                GridNode node = GenieNavigation.GetNearestWalkableNode(startPos);
                Vector3 targetPos = node == null ? startPos : (Vector3)node.position;

                Vector3 lookDir = targetPos - startPos;
                lookDir.y = 0;
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

                targetPos.y = _genie.GenieManager.Bootstrapper.XRNode.xrFloorManager.FloorY;

                _nestedCoroutine1 = _genie.StartCoroutine(JumpAndTwirl_Lerp_C(targetPos, targetRot, _jumpAndTwirlOutroCurve, _jumpAndTwirlExtraYElevationCurveForOutro));

                yield return _nestedCoroutine1;
            }
            else 
            {
                // Old method of standing up. Subject to deletion.
                string sitToStandStateName = "SitToStand";

                yield return new WaitUntil(() => _animator.IsInState(sitToStandStateName)); // wait for the animation to start.

                AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
                float clipLength = clipInfo[0].clip.length;

                float positionInterpolationTime = 1f;
                _nestedCoroutine1 = _genie.StartCoroutine(LerpToStandingPositionAndRotation_C(positionInterpolationTime, false));

                // Wait until standing up animation completed
                yield return new WaitForSeconds(clipLength);
            }

            _genie.genieNavigation.PositionCharacterOnFloor = true;
            IsSittingOrInTransition = false;
        }

        /// <summary>
        /// Call when the SitOnSeatAction is externally canceled, so we can stop any nested coroutines and quickly get to the standing position.
        /// The point is not to look pretty -- the point is to stop sitting as quickly as possible. 
        /// </summary>
        public void CancelSittingAndStandUpQuickly()
        {
            // Stop any nested coroutines.
            if (_nestedCoroutine1 != null)
            {
                _genie.StopCoroutine(_nestedCoroutine1);
            }
            if (_nestedCoroutine2 != null)
            {
                _genie.StopCoroutine(_nestedCoroutine2);
            }

            _animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun);
            _genie.StartCoroutine(LerpToStandingPositionAndRotation_C(0.25f, false)); // Use a very quick lerp time and ignore rotation.
             // NOTE: This coroutine above isn't going to be a solid solution going forward, because it's going to be playing over whatever the next Action will be.
            // For example, if the next Action is for the Genie to avoid the user, we'll need to either cancel this coroutine or wait until it's finished.
            // Rather than just having a dangling coroutine here, I think we'll want to move this logic into the GenieNavigation class so we can cancel it when we need to.
    
            _genie.genieNavigation.PositionCharacterOnFloor = true;
            IsSittingOrInTransition = false;
        }

        [SerializeField, Tooltip("General lerp curve. Should be the same length as the 'intro' animation clip.")]
        private AnimationCurve _jumpAndTwirlIntroCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Tooltip("Adds extra 'y' positioning during the 'intro'. Should be the same length as the 'intro' animation clip.")]
        private AnimationCurve _jumpAndTwirlExtraYElevationCurveForIntro = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Tooltip("General lerp curve. Should be the same length as the 'outro' animation clip.")]
        private AnimationCurve _jumpAndTwirlOutroCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Tooltip("Adds extra 'y' positioning during the 'outro'. Should be the same length as the 'outro' animation clip.")]
        private AnimationCurve _jumpAndTwirlExtraYElevationCurveForOutro = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private IEnumerator JumpAndTwirl_Lerp_C(Vector3 targetPosition, Quaternion targetRotation, AnimationCurve curve, AnimationCurve extraYCurve)
        {
            Debug.Log("JumpAndTwirl_Lerp_C() called.");

            float curveDuration = curve.keys[curve.length - 1].time;
            
            float startTime = Time.time;
            float endTime = startTime + curveDuration;

            Vector3 startPos = _genie.transform.position;
            Vector3 targetPos = targetPosition;

            Quaternion targetRot = targetRotation;
            Quaternion startRot = _genie.transform.rotation;

            while (Time.time < endTime)
            {
                yield return new WaitForEndOfFrame();
                //float t = Mathf.InverseLerp(startTime, endTime, Time.time);
                float elapsed = Time.time - startTime;
                float val = curve.Evaluate(elapsed); // Apply easing
                
                Vector3 newPos = Vector3.Lerp(startPos, targetPos, val);
                newPos.y += extraYCurve.Evaluate(elapsed); // Apply extra Y elevation

                _genie.transform.position = newPos;
                _genie.transform.rotation = Quaternion.Slerp(startRot, targetRot, val);
                //yield return null;
            }

            _genie.transform.position = targetPos;
            _genie.transform.rotation = targetRot;
        }

        private IEnumerator LerpToSeatPosition_C(Vector3 seatPosition, float interpolationTime)
        {
            float startTime = Time.time;
            float endTime = startTime + interpolationTime;

            Vector3 startPos = _genie.transform.position;
            Vector3 targetPos = seatPosition + Vector3.up * _yOffsetWhileSittingFromNonJumpAndTwirl;

            while (Time.time < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.time);
                t = Mathf.SmoothStep(0, 1f, t); // Apply easing
                _genie.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            _genie.transform.position = targetPos;
        }

        private IEnumerator LerpToSeatRotation_C(Quaternion seatRotation, float interpolationTime)
        {
            float startTime = Time.time;
            float endTime = startTime + interpolationTime;

            Quaternion startRot = _genie.transform.rotation;

            while (Time.time < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.time);
                t = Mathf.SmoothStep(0, 1f, t); // Apply easing
                _genie.transform.rotation = Quaternion.Slerp(startRot, seatRotation, t);
                yield return null;
            }

            _genie.transform.rotation = seatRotation;
        }

        private IEnumerator LerpToStandingPositionAndRotation_C(float interpolationTime, bool enableRotation = true)
        {
            float startTime = Time.time;
            float endTime = startTime + interpolationTime;

            Vector3 startPos = _genie.transform.position;

            // Locate the nearest walkable node. That's where we want our genie to end up.
            GridNode node = GenieNavigation.GetNearestWalkableNode(startPos);
            Vector3 targetPos = node == null ? startPos : (Vector3)node.position;

            Quaternion startRot = _genie.transform.rotation;
            Vector3 lookDir = targetPos - startPos;
            lookDir.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

            FloorManager xrFloorManager = _genie.GenieManager.Bootstrapper.XRNode.xrFloorManager;

            while (Time.time < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.time);
                t = Mathf.SmoothStep(0, 1f, t); // Apply easing

                targetPos.y = xrFloorManager.FloorY; // Make sure we're hitting the floor.

                _genie.transform.position = Vector3.Lerp(startPos, targetPos, t);

                if (enableRotation) 
                {
                    _genie.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                }
                
                yield return null;
            }

            _genie.transform.position = targetPos;
            
            if (enableRotation) 
            {
                _genie.transform.rotation = targetRot;
            }
        }
    }
}
