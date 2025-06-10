using System.Collections;
using Genies.Avatars;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Used to make the character's eyeballs look at a target.
    /// </summary>
    [DefaultExecutionOrder( 9000 )]  // To allow this to run after the Blend Shape Animator Behaviour
    public class EyeballAimer : MonoBehaviour
    {
        public Transform leftEye;
        public Transform rightEye;
        public Transform head;
        public SkinnedMeshRenderer leftEyeMeshRenderer;
        public SkinnedMeshRenderer rightEyeMeshRenderer;
        [Tooltip("The EyeballAimer should be processed after the Blend Shape Animator Behavior")]
        public bool useLegacyGeniesBlendshapes = false;
        [Tooltip("If true, the blendshapes will be set to the default values when not aiming. If false, the blendshapes will be set to whatever the animator is telling them to be.")]

        /// <summary>
        /// The transform we're currently locked onto. If we're locked onto a Target, then we're not using TargetLocation.
        /// </summary>
        public Transform Target { get; private set; }

        /// <summary>
        /// The world space point we're currently locked onto. If we're locked onto a Location, then we're not using Target.
        /// </summary>
        public Vector3? TargetLocation { get; private set; }

        [SerializeField, Tooltip("Minimum allowed distance from the head to converge onto a target. Make this bigger to prevent the eyes from looking too cross-eyed.")]
        private float minConvergenceDistance = 0.25f; // minimum allowed distance from the head to converge onto a target. Make this bigger to prevent the eyes from looking too cross-eyed.
        [SerializeField, Tooltip("The duration of the crossfade between 'aiming' and 'not aiming' states.")]
        private float blendShapeCrossfadeDuration = 0.2f;
        [SerializeField]
        private bool enableSmoothTransition = false;
        [SerializeField]
        [Tooltip("The base, horizontal center angle when looking straight ahead that will get applied to each eye.")]
        private float baseHorizontalCenter = 90f;
        [SerializeField]
        [Tooltip("The amount that the eye deviates horizontally from the base center when looking straight ahead. Gets applied to each eye in opposite signs.")]
        private float centerHorizontalDeviation = 0f;
        [SerializeField]
        [Tooltip("The span that the eye can deviate from the center when looking at a target.")]
        private float horizontalSpan = 30f;
        [SerializeField]
        [Tooltip("The vertical center angle when looking straight ahead that will get applied to each eye.")]
        private float verticalCenter = 80f; 
        [SerializeField]
        [Tooltip("The span that the eye can deviate from the center when looking at a target.")]
        private float verticalSpan = 40f;
        [SerializeField]
        [Range(0f, 100f)]
        [Tooltip("Cap the blendshape weight as needed to prevent the eyes from rolling behind the head.")]
        private float blendshapeLimitIn = 100f;
        [SerializeField]
        [Tooltip("Cap the blendshape weight as needed to prevent the eyes from rolling behind the head.")]
        [Range(0f, 100f)]
        private float blendshapeLimitOut = 100f;
        [SerializeField]
        [Tooltip("Cap the blendshape weight as needed to prevent the eyes from rolling behind the head.")]
        [Range(0f, 100f)]
        private float blendshapeLimitDown = 100f;
        [SerializeField]
        [Tooltip("Cap the blendshape weight as needed to prevent the eyes from rolling behind the head.")]
        [Range(0f, 100f)]
        private float blendshapeLimitUp = 100f;
        [SerializeField]
        private bool autoTrackMainCameraOnStart = false;
        private float _blendShapeTransitionStartTime = 0f;
        private bool _eyeAimEnabledLastFrame = false;
        private int _leftInIndex, _leftOutIndex, _leftUpIndex, _leftDownIndex, _rightInIndex, _rightOutIndex, _rightUpIndex, _rightDownIndex = 0;
        private float _leftInAtTransitionStart, _leftOutAtTransitionStart, _leftUpAtTransitionStart, _leftDownAtTransitionStart, _rightInAtTransitionStart, _rightOutAtTransitionStart, _rightUpAtTransitionStart, _rightDownAtTransitionStart = 0f;
        private float _leftInLastFrame, _leftOutLastFrame, _leftUpLastFrame, _leftDownLastFrame, _rightInLastFrame, _rightOutLastFrame, _rightUpLastFrame, _rightDownLastFrame = 0f;
        private bool _eyeAimEnabled;
        private Coroutine _waitAndStopTrackingTargetCoroutine;
        public void TrackTarget(Transform target)
        {
            Debug.Log("EyeballAimer: Tracking target: " + target.name);
            if (_waitAndStopTrackingTargetCoroutine != null) StopCoroutine(_waitAndStopTrackingTargetCoroutine);

            Target = target;
            TargetLocation = null;
            _eyeAimEnabled = target != null;
        }

        public void TrackLocation(Vector3 location)
        {
            Debug.Log("EyeballAimer: Tracking location.");
            if (_waitAndStopTrackingTargetCoroutine != null) StopCoroutine(_waitAndStopTrackingTargetCoroutine);

            Target = null;
            TargetLocation = location;
            _eyeAimEnabled = true;
        }

        public void StopTrackingTarget()
        {
            Debug.Log("EyeballAimer: Stopped tracking target.");

            if (_waitAndStopTrackingTargetCoroutine != null) StopCoroutine(_waitAndStopTrackingTargetCoroutine);
            
            if (gameObject.activeInHierarchy) // This normally basically never happen but it's throwing an error on Destroy() otherwise
            {
                _waitAndStopTrackingTargetCoroutine = StartCoroutine(WaitAndStopTrackingTarget_C());
            }
        }

        private IEnumerator WaitAndStopTrackingTarget_C()
        {
            // Hold for a few frames before resetting the blendshapes. This prevents issues with transitions, which sometimes
            // occur with a frame delay.

            int framesToWait = 20;
            for (int i = 0; i < framesToWait; i++)
            {
                yield return null;
            }

            Target = null;
            TargetLocation = null;
            _eyeAimEnabled = false;
        }

        private void Start()
        {
            _blendShapeTransitionStartTime = Time.time;

            // Initialize blend shape processing
            _leftInIndex = GetIdxEyesLookIn(true);
            _leftOutIndex = GetIdxEyesLookOut(true);
            _leftUpIndex = GetIdxEyesLookUp(true);
            _leftDownIndex = GetIdxEyesLookDown(true);

            _rightInIndex = GetIdxEyesLookIn(false);
            _rightOutIndex = GetIdxEyesLookOut(false);
            _rightUpIndex = GetIdxEyesLookUp(false);
            _rightDownIndex = GetIdxEyesLookDown(false);

            _leftInAtTransitionStart = leftEyeMeshRenderer.GetBlendShapeWeight(_leftInIndex);
            _leftOutAtTransitionStart = leftEyeMeshRenderer.GetBlendShapeWeight(_leftOutIndex);
            _leftUpAtTransitionStart = leftEyeMeshRenderer.GetBlendShapeWeight(_leftUpIndex);
            _leftDownAtTransitionStart = leftEyeMeshRenderer.GetBlendShapeWeight(_leftDownIndex);
            _rightInAtTransitionStart = rightEyeMeshRenderer.GetBlendShapeWeight(_rightInIndex);
            _rightOutAtTransitionStart = rightEyeMeshRenderer.GetBlendShapeWeight(_rightOutIndex);
            _rightUpAtTransitionStart = rightEyeMeshRenderer.GetBlendShapeWeight(_rightUpIndex);
            _rightDownAtTransitionStart = rightEyeMeshRenderer.GetBlendShapeWeight(_rightDownIndex);

            if (autoTrackMainCameraOnStart)
            {
                TrackTarget(Camera.main.transform);
            }    
        }

        // NOTE: LateUpdate() here MUST be called after the BlendShapeAnimatorBehaviour's LateUpdate(). 
        // This is why we set the execution order to a high value in our class definition above.
        private void LateUpdate()
        {
            // Start by declaring base blendshape weights. 
            float leftIn = 0, leftOut = 0, leftUp = 0, leftDown = 0, rightIn = 0, rightOut = 0, rightUp = 0, rightDown = 0;

            if (_eyeAimEnabled)
            {
                Vector3 targetPos;

                if (Target == null && TargetLocation == null)
                {
                    targetPos = _lastKnownTargetPos; // Fallback codepath.
                }
                else
                {
                    targetPos = Target != null ? Target.position : TargetLocation.Value; // Normal codepath
                    _lastKnownTargetPos = targetPos;
                }

                targetPos = ConstrainTargetPosition(targetPos);

                // Calculate the direction vectors
                Vector3 leftDirection = targetPos - leftEye.position;
                Vector3 rightDirection = targetPos - rightEye.position;

                // Calculate the local space directions of the eyes
                Vector3 leftEyeLeft = -leftEye.right;
                Vector3 leftEyeUp = leftEye.up;
                Vector3 rightEyeLeft = -rightEye.right;
                Vector3 rightEyeUp = rightEye.up;

                float leftHorizontalAngle = Vector3.Angle(leftDirection, leftEyeLeft);
                float leftVerticalAngle = Vector3.Angle(leftDirection, leftEyeUp);
                float rightHorizontalAngle = Vector3.Angle(rightDirection, rightEyeLeft);
                float rightVerticalAngle = Vector3.Angle(rightDirection, rightEyeUp);

                float leftEyeHorizontalCenter = baseHorizontalCenter - centerHorizontalDeviation; // Note: One should be + and the other should be -, but I don't know which is which yet.
                float rightEyeHorizontalCenter = baseHorizontalCenter + centerHorizontalDeviation;

                if (leftHorizontalAngle < leftEyeHorizontalCenter)
                {
                    leftOut = Mathf.InverseLerp(leftEyeHorizontalCenter, leftEyeHorizontalCenter - horizontalSpan, leftHorizontalAngle) * 100f;
                    leftIn = 0f;
                }
                else if (leftHorizontalAngle >= leftEyeHorizontalCenter)
                {
                    leftIn = Mathf.InverseLerp(leftEyeHorizontalCenter, leftEyeHorizontalCenter + horizontalSpan, leftHorizontalAngle) * 100f;
                    leftOut = 0f;
                }

                if (rightHorizontalAngle < rightEyeHorizontalCenter)
                {
                    rightIn = Mathf.InverseLerp(rightEyeHorizontalCenter, rightEyeHorizontalCenter - horizontalSpan, rightHorizontalAngle) * 100f;
                    rightOut = 0;
                }
                else if (rightHorizontalAngle >= rightEyeHorizontalCenter)
                {
                    rightOut = Mathf.InverseLerp(rightEyeHorizontalCenter, rightEyeHorizontalCenter + horizontalSpan, rightHorizontalAngle) * 100f;
                    rightIn = 0;
                }

                if (leftVerticalAngle > verticalCenter)
                {
                    leftDown = Mathf.InverseLerp(verticalCenter, verticalCenter + verticalSpan, leftVerticalAngle) * 100f;
                    leftUp = 0f;
                }
                else if (leftVerticalAngle <= verticalCenter)
                {
                    leftDown = 0f;
                    leftUp = Mathf.InverseLerp(verticalCenter, verticalCenter - verticalSpan, leftVerticalAngle) * 100f;
                }

                if (rightVerticalAngle > verticalCenter)
                {
                    rightDown = Mathf.InverseLerp(verticalCenter, verticalCenter + verticalSpan, rightVerticalAngle) * 100f;
                    rightUp = 0f;
                }
                else if (rightVerticalAngle <= verticalCenter)
                {
                    rightDown = 0f;
                    rightUp = Mathf.InverseLerp(verticalCenter, verticalCenter - verticalSpan, rightVerticalAngle) * 100f;
                }

                // Clamp the blendshape weights to prevent the eyes from rolling behind the head
                leftIn = Mathf.Clamp(leftIn, 0f, blendshapeLimitIn);
                leftOut = Mathf.Clamp(leftOut, 0f, blendshapeLimitOut);
                leftUp = Mathf.Clamp(leftUp, 0f, blendshapeLimitUp);
                leftDown = Mathf.Clamp(leftDown, 0f, blendshapeLimitDown);
                rightIn = Mathf.Clamp(rightIn, 0f, blendshapeLimitIn);
                rightOut = Mathf.Clamp(rightOut, 0f, blendshapeLimitOut);
                rightUp = Mathf.Clamp(rightUp, 0f, blendshapeLimitUp);
                rightDown = Mathf.Clamp(rightDown, 0f, blendshapeLimitDown);
            }
            else
            {
                // If we're not aiming, set the target blendshape weights to be whatever the animator is telling them to be.
                leftIn = leftEyeMeshRenderer.GetBlendShapeWeight(_leftInIndex);
                leftOut = leftEyeMeshRenderer.GetBlendShapeWeight(_leftOutIndex);
                leftUp = leftEyeMeshRenderer.GetBlendShapeWeight(_leftUpIndex);
                leftDown = leftEyeMeshRenderer.GetBlendShapeWeight(_leftDownIndex);
                rightIn = rightEyeMeshRenderer.GetBlendShapeWeight(_rightInIndex);
                rightOut = rightEyeMeshRenderer.GetBlendShapeWeight(_rightOutIndex);
                rightUp = rightEyeMeshRenderer.GetBlendShapeWeight(_rightUpIndex);
                rightDown = rightEyeMeshRenderer.GetBlendShapeWeight(_rightDownIndex);
            }

            // If the eye aim state is changed, record the previous blendshape weights to use as the starting point for the transition.
            if (_eyeAimEnabled != _eyeAimEnabledLastFrame)
            {
                _blendShapeTransitionStartTime = Time.time;

                _leftInAtTransitionStart = _leftInLastFrame;
                _leftOutAtTransitionStart = _leftOutLastFrame;
                _leftUpAtTransitionStart = _leftUpLastFrame;
                _leftDownAtTransitionStart = _leftDownLastFrame;
                _rightInAtTransitionStart = _rightInLastFrame;
                _rightOutAtTransitionStart = _rightOutLastFrame;
                _rightUpAtTransitionStart = _rightUpLastFrame;
                _rightDownAtTransitionStart = _rightDownLastFrame;
            }

            // Set the blendshapes with respect to crossfading
            ApplyBlendShapeWeight(leftEyeMeshRenderer, _leftInIndex, _leftInAtTransitionStart, leftIn);
            ApplyBlendShapeWeight(leftEyeMeshRenderer, _leftOutIndex, _leftOutAtTransitionStart, leftOut);
            ApplyBlendShapeWeight(leftEyeMeshRenderer, _leftUpIndex, _leftUpAtTransitionStart, leftUp);
            ApplyBlendShapeWeight(leftEyeMeshRenderer, _leftDownIndex, _leftDownAtTransitionStart, leftDown);

            ApplyBlendShapeWeight(rightEyeMeshRenderer, _rightInIndex, _rightInAtTransitionStart, rightIn);
            ApplyBlendShapeWeight(rightEyeMeshRenderer, _rightOutIndex, _rightOutAtTransitionStart, rightOut);
            ApplyBlendShapeWeight(rightEyeMeshRenderer, _rightUpIndex, _rightUpAtTransitionStart, rightUp);
            ApplyBlendShapeWeight(rightEyeMeshRenderer, _rightDownIndex, _rightDownAtTransitionStart, rightDown);

            // Record previous frame values
            _leftInLastFrame = leftIn;
            _leftOutLastFrame = leftOut;
            _leftUpLastFrame = leftUp;
            _leftDownLastFrame = leftDown;
            _rightInLastFrame = rightIn;
            _rightOutLastFrame = rightOut;
            _rightUpLastFrame = rightUp;
            _rightDownLastFrame = rightDown;

            _eyeAimEnabledLastFrame = _eyeAimEnabled;
        }
        
        private void ApplyBlendShapeWeight(SkinnedMeshRenderer meshRenderer, int blendShapeIndex, float weightAtLatestTransitionStart, float targetWeight)
        {
            if (enableSmoothTransition)
            {
                float elapsedTime = Time.time - _blendShapeTransitionStartTime;
                float t = Mathf.Clamp01(elapsedTime / blendShapeCrossfadeDuration);
                float newWeight = Mathf.Lerp(weightAtLatestTransitionStart, targetWeight, t);
                meshRenderer.SetBlendShapeWeight(blendShapeIndex, newWeight);
            }
            else
            {
                meshRenderer.SetBlendShapeWeight(blendShapeIndex, targetWeight);
            }
        }

        private Vector3 ConstrainTargetPosition(Vector3 targetPos)
        {
            // Prevent the target position from being too close to the head, causing the eyes to cross unnaturally
            float minDistance = minConvergenceDistance; // minimum allowed distance from the head
            Vector3 dirFromHead = targetPos - head.position;
            if (dirFromHead.sqrMagnitude < minDistance * minDistance)
            {
                dirFromHead = dirFromHead.sqrMagnitude < 0.0001f ? head.forward : dirFromHead.normalized;
                targetPos = head.position + dirFromHead * minDistance;
            }

            // If the target is significantly behind the head, move it to the right side relative to the head. 
            // This prevents chaotic eye movement when the target is behind the head.
            if (Vector3.Dot(head.forward, targetPos - head.position) < -0.5f)
            {
                float distance = (targetPos - head.position).magnitude;
                targetPos = head.position + head.right * distance;
            }

            return targetPos;
        }

        private Vector3 _lastKnownTargetPos;

        private int GetIdxEyesLookDown(bool leftEye)
        {
            string blendShapeName = leftEye ? "eye_L_geo_blendShape." : "eye_R_geo_blendShape.";

            if (useLegacyGeniesBlendshapes)
            {
                blendShapeName += "eyeLookDown" + (leftEye ? "Left" : "Right");
            }
            else
            {
                blendShapeName += "EYES_LOOK_DOWN_" + (leftEye ? "L" : "R");
            }

            SkinnedMeshRenderer smr = leftEye ? leftEyeMeshRenderer : rightEyeMeshRenderer;

            int idx = smr.sharedMesh.GetBlendShapeIndex(blendShapeName);
            return idx;
        }

        private int GetIdxEyesLookUp(bool leftEye)
        {
            string blendShapeName = leftEye ? "eye_L_geo_blendShape." : "eye_R_geo_blendShape.";

            if (useLegacyGeniesBlendshapes)
            {
                blendShapeName += "eyeLookUp" + (leftEye ? "Left" : "Right");
            }
            else
            {
                blendShapeName += "EYES_LOOK_UP_" + (leftEye ? "L" : "R");
            }

            SkinnedMeshRenderer smr = leftEye ? leftEyeMeshRenderer : rightEyeMeshRenderer;
            
            int idx = smr.sharedMesh.GetBlendShapeIndex(blendShapeName);
            return idx;
        }

        private int GetIdxEyesLookIn(bool leftEye)
        {
            string blendShapeName = leftEye ? "eye_L_geo_blendShape." : "eye_R_geo_blendShape.";

            if (useLegacyGeniesBlendshapes)
            {
                blendShapeName += leftEye ? "eyeLookInLeft" : "eyeLookInRight";
            }
            else
            {
                // "In" maps to "right" for the left eye and "left" for the right eye
                blendShapeName += leftEye ? "EYES_LOOK_RIGHT_L" : "EYES_LOOK_LEFT_R";
            }

            SkinnedMeshRenderer smr = leftEye ? leftEyeMeshRenderer : rightEyeMeshRenderer;

            int idx = smr.sharedMesh.GetBlendShapeIndex(blendShapeName);
            return idx;
        }

        private int GetIdxEyesLookOut(bool leftEye)
        {
            string blendShapeName = leftEye ? "eye_L_geo_blendShape." : "eye_R_geo_blendShape.";

            if (useLegacyGeniesBlendshapes)
            {
                blendShapeName += leftEye ? "eyeLookOutLeft" : "eyeLookOutRight";
            }
            else
            {
                // "Out" maps to "left" for the left eye and "right" for the right eye
                blendShapeName += leftEye ? "EYES_LOOK_LEFT_L" : "EYES_LOOK_RIGHT_R";
            }

            SkinnedMeshRenderer smr = leftEye ? leftEyeMeshRenderer : rightEyeMeshRenderer;

            int idx = smr.sharedMesh.GetBlendShapeIndex(blendShapeName);
            return idx;
        }
    }
}
