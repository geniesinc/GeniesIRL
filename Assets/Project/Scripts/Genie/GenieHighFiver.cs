using System;
using System.Collections;
using GeneisIRL;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Facilitates the Genie's ability to high five the user.
    /// </summary>
    [System.Serializable]
    public class GenieHighFiver
    {
        /// <summary>
        /// Fires the moment the User's hand makes contact with the Genie's hand.
        /// </summary>
        public event Action OnSuccess;

        public float delayAfterSolcitationToStartCheckingIntersection = 1f;
        public Vector3 hitboxDimensions = new Vector3(0.2f, 0.2f, 0.3f);

        [Tooltip("The offset from the Genie's right hand to place the high five box. The components are represented as follows:"
            + "\nX: Left/Right with respect to the Genie."
            + "\nY: World Up/Down"
            + "\nZ: Forward/Back with respect to the Genie.")]
        public Vector3 hitboxOffset;

        [SerializeField, Tooltip("The FX to spawn on high-fiving.")]
        private GameObject highFiveFXPrefab;
        [SerializeField, Tooltip("The trigger and state name for a Genie-solicited high five animation.")]
        private string performGenieSolicitedHighFiveTriggerAndStateName = "PerformHighFive-GenieSolicited";
        [SerializeField, Tooltip("The trigger and state name for a User-solicited high five animation.")]
        private string performUserSolicitedHighFiveTriggerAndStateName = "PerformHighFive-UserSolicited";
        [SerializeField, Tooltip("The state name for the high five success animation.")]
        private string highFiveSuccessStateName = "HighFiveSuccess";

        [SerializeField, Tooltip("The trigger and state name for the rejected from high five animation.")]
        private string rejectedFromHighFiveTriggerAndStateName = "RejectedFromHighFive";

        [SerializeField, Tooltip("The threshold for the user's hand to be considered in contact with the Genie's hand. If the user's hand gets within this threshold, the FX will play early.")]
        private Vector3 handContactDetectionSize = new Vector3(0.1f, 0.1f, 0.05f);

        [Header("Debugging")]
        [SerializeField, Tooltip("Enables debug visualization for the high five box, and allows the user to hit the return key to perform the high five, so long as they're in the Editor and P2D is disabled.")]
        private bool enableDebugMode;
        [SerializeField, Tooltip("The material to use for the debug box that appears when the Genie solicits a high five.")]
        private Material debugBoxMaterial;
        [SerializeField, Tooltip("The debug proxy hand will spawn here, with respect to the user.")]
        private Vector3 debugProxyHandOffset = new Vector3(-0.25f, 0.25f, 0.25f);
        [NonSerialized] private Genie _genie;
        [NonSerialized] private GenieAnimation _genieAnimation;
        private Collider _highFiveBox;
        private Coroutine _highFiveCheckCoroutine;
        private GameObject _debugProxyUserHand;
        private Transform _userHandTarget;

        private bool _didPlayFX = false;

        public void OnStart(Genie genie)
        {
            _genie = genie;
            _genieAnimation = _genie.genieAnimation;

            _genieAnimation.animEventDispatcher.OnHighFiveHit += PlayHighFiveHitFX;

            if (enableDebugMode && !GeniesIRL.App.XR.IsPolySpatialEnabled)  // The proxy hand is used to test the Genie's IK response in the Editor without P2D.
            {
                _debugProxyUserHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _debugProxyUserHand.transform.localScale = new Vector3(0.1f, 0.1f, 0.025f);
                GameObject.Destroy(_debugProxyUserHand.GetComponent<Collider>());
                _debugProxyUserHand.name = "GenieHighFiver: Debug Proxy User Hand for testing IK";
                _debugProxyUserHand.SetActive(false);
            }
        }

        public void SolicitHighFive(XRInputWrapper userInput)
        {
            _genieAnimation.Animator.SetTrigger("SolicitHighFive");
            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(userInput.Head);
            _genie.genieSense.personalSpace.SetRadiusOverride(PersonalSpace.kRadiusDuringIntentionalPhysicalContact);

            CreateHighFiveBox(userInput);
        }

        /// <summary>
        /// Fails the high five and plays and "rejected" animation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator FailHighFive_C()
        {
            DestroyHighFiveBox();

            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
            _genie.genieSense.personalSpace.ResetRadius();

            _genieAnimation.Animator.SetTrigger(rejectedFromHighFiveTriggerAndStateName);
            
            yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(rejectedFromHighFiveTriggerAndStateName)); // wait for the rejected from high five animation to start

            yield return new WaitUntil(() => _genieAnimation.Animator.IsStateClipComplete(rejectedFromHighFiveTriggerAndStateName)); // wait for the rejected from high five animation to finish
        }

        /// <summary>
        /// Plays the high five and success success animation. _userHandTarget must not be null -- this gets set automatically when the Genie
        /// solicits the high-five, but it is set manually if the user is the one soliciting.
        /// </summary>
        /// <param name="genieSolicited">If true, the Genie is the one soliciting. Otherwise, it's the user. Depending on this,
        // a different animation will play.</param>
        /// <returns></returns>
        public IEnumerator PerformHighFiveAndSuccessAnimations_C(bool genieSolicited, XRInputWrapper userInput) 
        {
            _didPlayFX = false;

            Debug.Assert(_userHandTarget != null, "User hand target is null. Cannot perform high five.");

            // FOR DEBUG ONLY: In the case of a user-solicited high five, if we're testing in the Editor, we'll want to update the Proxy User Hand
            // position to match the User Hand Target for testing purposes.
            if (_debugProxyUserHand != null) 
            {
                 _debugProxyUserHand.transform.position = _userHandTarget.position;
                 _debugProxyUserHand.transform.rotation = _userHandTarget.rotation;
                 _debugProxyUserHand.SetActive(true);
            }

            // Temporarily shrink the Genie's personal space.
            _genie.genieSense.personalSpace.SetRadiusOverride(PersonalSpace.kRadiusDuringIntentionalPhysicalContact);

            // Make sure we're gazing at the user
            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(userInput.Head);

            // Select the high-five animation to play.
            string performHighFiveTriggerAndStateName = genieSolicited ? performGenieSolicitedHighFiveTriggerAndStateName : performUserSolicitedHighFiveTriggerAndStateName;

            // Play the high-five animation.
            _genieAnimation.Animator.SetTrigger(performHighFiveTriggerAndStateName);

            // Wait for the 'perform' state to start.
            yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(performHighFiveTriggerAndStateName)); 

            // While performing the high-five, update the Genie's IK to aim their hand at the user's hand.
            // Note for devs: if you slow down Time.timeScale at this point, you can more closely see the IK in action. If testing in the Edtior
            // without P2D, you can move around the proxy hand to see how the Genie's IK responds.
            while (_genieAnimation.Animator.IsInState(performHighFiveTriggerAndStateName)) 
            {
                // Determine orientation:
                Vector3 genieForward = _genie.transform.forward;
                Quaternion rotation = Quaternion.LookRotation(genieForward, Vector3.up);

                // Reach hand to position.
                _genieAnimation.GeniesIKComponent.ReachTowardsPosition(_userHandTarget.position, GenieHand.Right, rotation);

                // Check to see if the user's hand is within a box around the Genie's hand If so, play the 'slap' FX early.
                if (!_didPlayFX) 
                {
                    Vector3 genieHandPosition = GetGenieRightHand().position;
                    Vector3 userHandPosition = _userHandTarget.position;
                    Quaternion boxRot = Quaternion.LookRotation(genieForward, Vector3.up);

                    if (VectorUtils.IsPointInsideBox(userHandPosition, genieHandPosition, boxRot, handContactDetectionSize)) 
                    {
                        PlayHighFiveHitFX();
                    }
                }

                // FOR DEBUG ONLY: In the case of a user-solicited high five, if we're testing in the Editor, we want the _proxyUserHand to drive
                // the user hand target in real-time so we can test the IK more easily in the Editor. This will allow devs to modify the
                // _proxyUserHand position to see how the Genie's IK responds. (Although you'll probably want to slow down Time.timeScale for this.
                if (_debugProxyUserHand != null) 
                {
                   _userHandTarget.position = _debugProxyUserHand.transform.position;
                   _userHandTarget.rotation = _debugProxyUserHand.transform.rotation;
                }

                yield return null;
            }

            // High five success animation is set up to happen immediately after the perform high five animation. Wait for it to start. 
            yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(highFiveSuccessStateName)); // wait for the high five success animation to start

            yield return new WaitUntil(() => _genieAnimation.Animator.IsStateClipComplete(highFiveSuccessStateName)); // wait for the high five success animation to finish
            
            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
            _genie.genieSense.personalSpace.ResetRadius();
        }

        /// <summary>
        /// In case the High Five needs to be externally canceled, we need to gracefully clean up.
        /// </summary>
        public void ExternallyCancelHighFive()
        {
            DestroyHighFiveBox();
            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
            _genie.genieSense.personalSpace.ResetRadius();
        }

        /// <summary>
        /// Sets the target for the Genie to aim their hand while performing the high-five.
        /// </summary>
        /// <param name="userHand"></param>
        public void SetUserHandTarget(Transform userHand)
        {
            _userHandTarget = userHand;
        }

        private void SucceedHighFive()
        {
            Debug.Log("Succeed high five");
            DestroyHighFiveBox();
            OnSuccess?.Invoke();
        }
        private void PlayHighFiveHitFX()
        {
            if (_didPlayFX) return; // We already played the FX early.

            Debug.Assert(_userHandTarget != null, "User hand target is null. Cannot spawn high five FX.");

            GameObject.Instantiate(highFiveFXPrefab, _userHandTarget.position, Quaternion.identity);
            _didPlayFX = true;
        }

        private void CreateHighFiveBox(XRInputWrapper userInput) 
        {
            DestroyHighFiveBox(); // Destroy any existing high five box, just in case.

            _highFiveBox = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Collider>();
            _highFiveBox.gameObject.name = "HighFiveBox";
            _highFiveBox.transform.localScale = hitboxDimensions;
            _highFiveBox.isTrigger = true;
            _highFiveBox.GetComponent<MeshRenderer>().enabled = false;

            // Place it at the Genie's right hand.
            PositionAndOrientHighFiveBox(userInput.Head);

            // Kick off checking for the user's high five.
            _highFiveCheckCoroutine = _genie.StartCoroutine(CheckForHighFiveIntersection_C(userInput));
        }

        private IEnumerator CheckForHighFiveIntersection_C(XRInputWrapper userInput)
        {
            yield return new WaitForSeconds(delayAfterSolcitationToStartCheckingIntersection);
            yield return null; // Skip a frame for good measure.
            
            // FOR DEBUG ONLY: Set up the proxy hand.
            if (_debugProxyUserHand != null) 
            {
                _debugProxyUserHand.transform.position = userInput.Head.position + userInput.Head.forward * debugProxyHandOffset.z;
                _debugProxyUserHand.transform.position += userInput.Head.right * debugProxyHandOffset.x;
                _debugProxyUserHand.transform.position += Vector3.up * debugProxyHandOffset.y;
                Vector3 forward = Camera.main.transform.forward;
                forward.y = 0;
                forward.Normalize();
                _debugProxyUserHand.transform.forward = forward;
                _debugProxyUserHand.SetActive(true);
            }

            while (true) 
            {
                // Keep the box at the Genie's right hand
                PositionAndOrientHighFiveBox(userInput.Head);

                // If enabled, show the box
                if (enableDebugMode) 
                {
                    MeshRenderer meshRenderer = _highFiveBox.GetComponent<MeshRenderer>();
                    meshRenderer.material = debugBoxMaterial;
                    meshRenderer.enabled = true;
                }

                // Check the user's hand positions. If they intersect with the high five box, we consider it a success.
                Transform leftHand = userInput.hands.LeftHandPalm;

                // If we're testing in the Editor without P2D, use the debug proxy hand for the right hand.
                Transform rightHand = _debugProxyUserHand != null ? _debugProxyUserHand.transform : userInput.hands.RightHandPalm;

                bool rightHandIntersected = _highFiveBox.Contains(rightHand.position);
                bool leftHandIntersected = _highFiveBox.Contains(leftHand.position);

                bool highFiveDetected = false;

                if (leftHandIntersected) 
                {
                    _userHandTarget = leftHand;
                    highFiveDetected = true;
                }
                else if (rightHandIntersected) 
                {
                    _userHandTarget = rightHand;
                    highFiveDetected = true;
                }
                else if (_debugProxyUserHand != null && Input.GetKeyDown(KeyCode.Return)) // Pressing return places the debug proxy hand where it needs to be to intersect.
                {
                    _debugProxyUserHand.transform.position = _highFiveBox.transform.position;
                    _userHandTarget = _debugProxyUserHand.transform;
                    highFiveDetected = true;
                }

                if (highFiveDetected)
                {
                    SucceedHighFive();
                    yield break;
                }

                yield return null;
            }
        }

        private void DestroyHighFiveBox() 
        {
            if (_highFiveCheckCoroutine != null) 
            {
                _genie.StopCoroutine(_highFiveCheckCoroutine);
                _highFiveCheckCoroutine = null;
            }

            if (_highFiveBox != null) 
            {
                GameObject.Destroy(_highFiveBox.gameObject);
                _highFiveBox = null;
            }
        }
        
        private void PositionAndOrientHighFiveBox(Transform userHead) 
        {
            Transform genieRightHand = GetGenieRightHand();
            _highFiveBox.transform.position = genieRightHand.position;

            // Aim the box 
            //_highFiveBox.transform.forward = _genie.transform.forward;
            Vector3 userHeadPosition = userHead.position;
            userHeadPosition.y = _highFiveBox.transform.position.y;
            _highFiveBox.transform.forward = (userHeadPosition-genieRightHand.position).normalized;
            
            // Offset the box to be in the front of the Genie's hand.
            _highFiveBox.transform.position += _highFiveBox.transform.forward * _highFiveBox.transform.localScale.z/2f;

            // Apply the offset
            _highFiveBox.transform.position += _highFiveBox.transform.right * hitboxOffset.x;
            _highFiveBox.transform.position += _highFiveBox.transform.up * hitboxOffset.y;
            _highFiveBox.transform.position += _highFiveBox.transform.forward * hitboxOffset.z;
        }

        private Transform GetGenieRightHand() {
            return _genieAnimation.Animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
        }
    }
}

