using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Pathfinding;
using UnityEngine;

namespace GeniesIRL
{
    [System.Serializable]
    public class GenieAnimation
    {

        public GeniesIKComponent GeniesIKComponent { get; private set; }
        public Animator Animator {get; private set;}

        /// <summary>
        /// Flag set to true by GenieLookAndYaw each cycle the couroutine is running, but resets to false in Update.
        /// </summary>
        public bool IsPlayingYawCoroutine {get; set;}= false;

        /// <summary>
        /// Use this class to listen for animation events from the Genie Animator.
        /// </summary>
        public GenieAnimEventDispatcher animEventDispatcher;
        
        [Header("Hand Height Adjustment")]
        public float maxRelativeHandHeight = 1f;
        public float minRelativeHandHeight = 0f;
        public AnimationCurve handHeightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Yawing")]
        [SerializeField, Tooltip("The higher the speed, the faster the yaw will move toward the target yaw.")]
        private float yawTransitionSpeed = 5f;

        private Genie _genie;

        private Transform _transform;

        private float _yaw;
        private float _targetYaw;
        private float _yawSpeed;
        private float _targetYawSpeed;
        private float _targetIdleWalkRunSpeed;
        private float _idleWalkRunSpeed;

        public void OnStart(Genie genie)
        {
            _genie = genie;
            _transform = genie.transform;
            Animator = _genie.GetComponentInChildren<Animator>();
            GeniesIKComponent = _genie.GetComponentInChildren<GeniesIKComponent>();

            if (Animator == null)
            {
                Debug.LogError("Animator component not found inside Genie. Please make sure there is a rig and compatible" +
                    " Animator inside the Genie prefab.");
            }
        }

        public void OnUpdate()
        {
            IKDrivenByAnimator();
            UpdateIdleWalkRunYaw();

            // Use Debug.DrawLine to visualize the hand height.
            Debug.DrawLine(_transform.position + Vector3.up * minRelativeHandHeight, _transform.position + Vector3.up * maxRelativeHandHeight, Color.red);

            IsPlayingYawCoroutine = false;// Reset the flag.
        }

        private void IKDrivenByAnimator()
        {
            // The IK weight is actually keyed in the character's animations. Grab it from there.
            float leftHandIKWeight = Animator.GetFloat(Floats.LHandWorldWeight);
            float rightHandIKWeight = Animator.GetFloat(Floats.RHandWorldWeight);
            
            GeniesIKComponent.SetHandsIKComponentWeight(GenieHand.Right, rightHandIKWeight);
            GeniesIKComponent.SetHandsIKComponentWeight(GenieHand.Left, leftHandIKWeight);
            
            // Spine IK is used to assist the hands in reaching for items, so we'll use hand weights for the spine as well.
            GeniesIKComponent.UpdateSpineIKComponentWeight(Mathf.Max(leftHandIKWeight, rightHandIKWeight));
        }

        public void SetHandAnimHeight(GenieHand handSide, float IKHeight)
        {
            float relativeIKHeight = IKHeight - _transform.position.y; // Get the height relative to the genie's position.
            //float animHeight = relativeIKHeight / _genie.GetScaledColliderDimensions().y; // Now get the height as a percentage of the genie's height.
            float animHeight = Mathf.InverseLerp(minRelativeHandHeight, maxRelativeHandHeight, relativeIKHeight);

            string animKey = handSide == GenieHand.Right ? Floats.RHandHeight : Floats.LHandHeight;

            Animator.SetFloat(animKey, animHeight);
        }

        /// <summary>
        /// -1 for left yaw, 0 for no yaw, 1 for right yaw.
        /// </summary>
        /// <param name="yaw"></param>
        public void SetYawing(float yaw, float yawSpeed = 1f)
        {
            _targetYaw = yaw;

            _targetYawSpeed = yawSpeed;
        }

        private void UpdateIdleWalkRunYaw()
        {
            // IdleWalkeRun is not compatible with Yaw (this would require an updated set of animations and configurations).
            // This means we need to be careful about when we set the Yawing and IdleWalkRunSpeed values.
            // If the Genie is in the middle of a Yaw coroutine, that takes precedence.
            if (IsPlayingYawCoroutine) 
            {
                _targetIdleWalkRunSpeed = 0; // Force idle/walk/run speed to zero.
                // Don't do anything with yaw -- leave it be.
            }
            else 
            {
                // We're not in the middle of an explicit Yaw coroutine, but that doesn't mean we can't make use of it if the Genie is turning due to pathfinding motion.
                // Let's start out by getting an estimate of current pathing velocity. Actual velocity may vary, but this should give us enough info to determine what the 
                // Animator should be doing.
                AIPath aiPath = _genie.genieNavigation.AIPath;
                Vector3 vel = aiPath.velocity;
                vel.y = 0;
                float speed = vel.magnitude;

                float minSpeedThresh = 0.1f; // Minimum speed threshold for walk/run. Otherwise, we'll assume the Genie is either idling or yawing.
                float minDotThresh = 0.5f;

                float dot = Vector3.Dot(_transform.forward, vel.normalized);

                if (speed <= minSpeedThresh) 
                {
                    // Speed is less than the threshold. This means the Genie is either idling or yawing.
                    _targetIdleWalkRunSpeed = 0; // Force idle/walk/run speed to zero.

                    // If the Genie has a path and appears to be turning, perform the yaw animation.
                    if (aiPath.hasPath && dot >= minDotThresh)
                    {
                        Vector3 dir = vel.normalized;
                        float yaw = Vector3.Dot(dir, _transform.right) * 5f;
                        yaw = Mathf.Clamp(yaw, -1f, 1f);
                        
                        _targetYaw = yaw;
                        _targetYawSpeed = 2.5f;
                    }
                }
                else 
                {
                    // Speed is greater than the threshold. This means the Genie is either walking or running.
                    _targetIdleWalkRunSpeed = Mathf.Clamp(speed - minSpeedThresh, 0, Mathf.Infinity) * 2f;
                    _targetYaw = 0;
                }
            }

            // Process Yaw and apply to animation.
             _yaw = Mathf.Lerp(_yaw, _targetYaw, Time.deltaTime * yawTransitionSpeed);

            if (Mathf.Abs(_yaw) < 0.0001f)
            {
                _yaw = 0;
            }

            // Set the yawing value in the animator. This indicates which direction the Genie is "yawing" towards.
            Animator.SetFloat(Floats.Yawing, _yaw);

            // Update the yaw speed, which controls the speed of the yawing animation. Note that this value is technically hooked up to the entire idle/walk/run/yaw 
            // locomotion blend tree, but we're really only using it to control the speed of the yaw animation.

            if (Mathf.Abs(_targetYaw) < 0.0001f)
            {
                _targetYawSpeed = 1f;
            }

            float yawSpeedTransitionSpeed = 20f;
            _yawSpeed = Mathf.Lerp(_yawSpeed, _targetYawSpeed, Time.deltaTime * yawSpeedTransitionSpeed);
            Animator.SetFloat(Floats.YawAnimSpeed, _yawSpeed);

            // Process IdleWalkRun speed and apply to animation.
            float idleWalRunTransitionSpeed = 5f;
            _idleWalkRunSpeed = Mathf.Lerp(_idleWalkRunSpeed, _targetIdleWalkRunSpeed, Time.deltaTime * idleWalRunTransitionSpeed);

            if (Mathf.Abs(_idleWalkRunSpeed) < 0.0001f)
            {
                _idleWalkRunSpeed = 0;
            }

            Animator.SetFloat(Floats.IdleWalkRunSpeed, _idleWalkRunSpeed);
        }

        /// <summary>
        /// Calls the MaintainPersonalSpace trigger and resets the MaintainPersonalSpaceLeftToRight float to 0.5 (the center).
        /// </summary>
        public void StartMaintainPersonalSpace()
        {
            Animator.SetTrigger(Triggers.MaintainPersonalSpace);
            _justStartedMaintainingPersonalSpace = true;
        }

        private bool _justStartedMaintainingPersonalSpace = false;

        /// <summary>
        /// Used during the MaintainPersonalSpace Action, which uses a separate locomotion State from IdleWalkRun. 
        /// In MaintainPersonalSpace, the Genie walks backwards or side-to-side, or some combination of the two, to avoid the user.
        /// This function uses the Genie's velocity to determine how it should blend between side-to-side and backwards movement.
        /// </summary>
        public void UpdateMaintainPersonalSpace()
        {
            Vector3 direction = _genie.genieNavigation.AIPath.velocity;
            direction.y = 0;
            direction.Normalize();

            float leftToRightValue = Vector3.Dot(_transform.right, direction);
            // At the moment, all the way left is -1, and all the way right is 1. We want to convert this to 0 to 1 instead.
            leftToRightValue = (leftToRightValue + 1f) / 2f;
            leftToRightValue = Mathf.Clamp(leftToRightValue, 0f, 1f);

            if (_justStartedMaintainingPersonalSpace) 
            {
                Animator.SetFloat(Floats.MaintainPersonalSpaceLeftToRight, leftToRightValue); // Instantly set.
                _justStartedMaintainingPersonalSpace = false;
            }
            else 
            {
                Animator.SetFloat(Floats.MaintainPersonalSpaceLeftToRight, leftToRightValue, 0.1f, Time.deltaTime); // Smoothly set.
            }
            

            Debug.DrawLine(_transform.position, _transform.position + direction, Color.green);
            Debug.DrawLine(_transform.position, _transform.position + _transform.right, Color.red);
        }

        /// <summary>
        /// Performes a wave animation, then waits until it's done before ending the coroutine.
        /// </summary>
        /// <returns></returns>
        public IEnumerator Wave_C()
        {
            Debug.Log("Firing wave animation");
            Animator.SetTrigger(Triggers.Wave);
            yield return new WaitForSeconds(3.7f); 
        }

        public static class Floats
        {
            public const string RHandHeight = "rHandHeight";
            public const string LHandHeight = "lHandHeight";
            public const string LHandWorldWeight = "lHand_world_weight";
            public const string RHandWorldWeight = "rHand_world_weight";
            /// <summary>
            /// The speed of idling to walking. 0 means idle, 1 means walking. In most cases you don't want to set this
            /// value directly, and instead you can rely on IdleWalkRun.
            /// </summary>
            public const string IdleWalkRunSpeed = "idleWalkRunSpeed";
            /// <summary>
            /// -1 means yaw left, 0 means no yaw, 1 means yaw right. In most cases you don't want to set this
            /// value directly. Instead, use GenieAnimation.SetYaw. Note that when run speed is greater than 0,
            /// yawing is essentially disabled as it's only meant to work while idling.
            /// </summary>
            public const string Yawing = "yawing";

            /// <summary>
            /// Controls the speed of the yawing animation.
            /// </summary>
            public const string YawAnimSpeed = "yawAnimSpeed";

            /// <summary>
            /// Controls the directionality of the MaintainPersonalSpace animation. 0 means left, 0.5 means center, 1 means right.
            /// </summary>
            public const string MaintainPersonalSpaceLeftToRight = "MaintainPersonalSpaceLeftToRight";
        }

        public static class Bools
        {
            
        }

        public static class Triggers
        {
            public const string Sit = "Sit";
            public const string Sit2 = "Sit2";
            public const string Nope = "Nope";
            public const string Wave = "Wave";
            public const string StandUp = "StandUp";
            public const string StandUp2 = "StandUp2";
            public const string RStartGrabbing = "rStartGrabbing";
            public const string LStartGrabbing = "lStartGrabbing";
            public const string LItemGrabbed = "lItemGrabbed";
            public const string RItemGrabbed = "rItemGrabbed";
            public const string RStartPlacingHand = "rStartPlacingHand";
            public const string LStartPlacingHand = "lStartPlacingHand";
            public const string RRestHand = "rRestHand";
            public const string LRestHand = "lRestHand";
            /// <summary>
            /// Triggers the animator to enter the IdleWalkRun state.
            /// </summary>
            public const string IdleWalkRun = "IdleWalkRun";
            public const string MaintainPersonalSpace = "MaintainPersonalSpace";
        }
    }
}

