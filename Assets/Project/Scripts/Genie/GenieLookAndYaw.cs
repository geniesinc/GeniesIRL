using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Encapsulates logic that allows the Genie to turn to look at stuff, using its body and eyeballs.
    /// </summary>
    [System.Serializable]
    public class GenieLookAndYaw
    {
        public EyeballAimer eyeballAimer;

        public AnimationCurve yawCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [NonSerialized] // Use this to prevent Unity from secretly serializing the private field, causing circular serialized reference issues.
        private Genie _genie;

        private Transform _transform => _genie.transform;

        /// <summary>
        /// Called 
        /// </summary>
        /// <param name="genie"></param>
        public void OnSpawnedByGenieManager(Genie genie)
        {
            _genie = genie;
        }   

        public void OnStart(Genie genie)
        {
            _genie = genie;
        } 

        /// <summary>
        /// Stops the "IsYawing" animation from playing. (At time of writing, this will not stop any yawing coroutines that are running.)
        /// </summary>
        public void StopYawAnimation()
        {
            _genie.genieAnimation.SetYawing(0);
        }

        /// <summary>
        /// Instantly rotate the Genie towards the target. Based on code from GenieController.
        /// </summary>
        /// <param name="lookAtTarget"></param>
        /// <param name="reverse"></param>
        public void InstantYawTowards(Vector3 lookAtTarget)
        {
            // Get the direction from this object to the target
            Vector3 direction = Vector3.ProjectOnPlane(lookAtTarget - _transform.position, Vector3.up);

            // Create a rotation that looks in the direction of the target
            Quaternion lookAtRotation = Quaternion.LookRotation(direction, Vector3.up);

            _transform.rotation = lookAtRotation;
        }

        /// <summary>
        /// A Coroutine that will gradually rotate the Genie towards the target with animation. Originally imported from GenieController.
        /// </summary>
        /// <param name="LookAtTarget">Position in world space to target.</param>
        /// <param name="cancelIfAngleDifferenceLessThan"> If the angle difference is less than this value in degrees, cancel beginning the Yaw. (Only applies at the beginning of the operation)</param>
        /// <param name="reverse"></param>
        /// <param name="onCompleteCallback"></param>
        /// <returns></returns>
        public IEnumerator YawTowards_C(Vector3 lookAtPosition, float cancelIfAngleDifferenceLessThan = 30f, float speed = 2f, Action onCompleteCallback = null) 
        {
            yield return YawTowards_C(null, lookAtPosition, cancelIfAngleDifferenceLessThan, speed, onCompleteCallback);
        }

        /// <summary>
        /// A Coroutine that will gradually rotate the Genie towards the target with animation. Originally imported from GenieController.
        /// </summary>
        /// <param name="lookAtTarget">Transform that we'll target throughout the course of the yaw duration</param>
        /// <param name="cancelIfAngleDifferenceLessThan"></param>
        /// <param name="reverse"></param>
        /// <param name="onCompleteCallback"></param>
        /// <returns></returns>
        public IEnumerator YawTowards_C(Transform lookAtTarget, float cancelIfAngleDifferenceLessThan = 30f, float speed = 2f, Action onCompleteCallback = null) 
        {
            yield return YawTowards_C(lookAtTarget, (Vector3)default, cancelIfAngleDifferenceLessThan, speed, onCompleteCallback);
        }

        private IEnumerator YawTowards_C(Transform lookAtTarget, Vector3 lookAtPosition, float cancelIfAngleDifferenceLessThan = 30f, float speed = 2f, Action onCompleteCallback = null)
        {
            float timer = 0;
            float duration;
            Quaternion startingRot = _transform.rotation;

            // Get the direction from this object to the target
            Vector3 direction;
            
            if (lookAtTarget != null ) 
            {
                // Use the transform.
                direction = Vector3.ProjectOnPlane(lookAtTarget.position - _transform.position, Vector3.up); 
            }
            else 
            {
                // Use the position.
                direction = Vector3.ProjectOnPlane(lookAtPosition - _transform.position, Vector3.up);
            }

            // Calculate the angle between the two vectors
            float angleDif = Vector3.Angle(direction, _transform.forward);
            if (angleDif < cancelIfAngleDifferenceLessThan)
            {
                onCompleteCallback?.Invoke();
                yield break; // End the coroutine
            }
            
            // Create a rotation that looks in the direction of the target
            Quaternion lookAtRotation = Quaternion.LookRotation(direction, Vector3.up);

            //Calculate duration depending on how far needs to rotate and animation speed
            float totalRotation = Mathf.DeltaAngle(lookAtRotation.eulerAngles.y, startingRot.eulerAngles.y);
            float dir = Mathf.Sign(totalRotation);

            float yawAnimTotalAngle = 90f;
            float yawAnimDuration = 1.567f;
            duration = Math.Abs(totalRotation) * (yawAnimDuration / yawAnimTotalAngle);
            // Duration changes if speed is not 1
            duration = duration / Math.Abs(speed);
            //Sets Animation
            
            _genie.genieAnimation.SetYawing(dir, speed); // Note: in the controller, yawing is connected to Idle, so you won't see her legs doing the yaw thing if she's doing some other animation.

            while (timer < duration)
            {
                if (lookAtTarget != null)
                {
                     // Keep updating the direction as the target may move during this period. (Note that this might cause weird-looking results with the animation)
                    direction = Vector3.ProjectOnPlane(lookAtTarget.position - _transform.position, Vector3.up);
                    lookAtRotation = Quaternion.LookRotation(direction, Vector3.up);
                }

                float t = yawCurve.Evaluate(timer/duration);
                _transform.rotation = Quaternion.Slerp(startingRot, lookAtRotation, t);
                timer += Time.deltaTime;
                _genie.genieAnimation.IsPlayingYawCoroutine = true; // Let the animator know we're performing the yaw coroutine.
                yield return null;
            }

            if (lookAtTarget != null)
            {
                 // Keep updating the direction as the target may have moved (Note that this might cause weird-looking results with the animation)
                direction = Vector3.ProjectOnPlane(lookAtTarget.position - _transform.position, Vector3.up);
                lookAtRotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            _transform.rotation = lookAtRotation;
            _genie.genieAnimation.SetYawing(0);
            onCompleteCallback?.Invoke();
        }
    }
}