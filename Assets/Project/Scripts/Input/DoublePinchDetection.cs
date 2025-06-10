using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace GeniesIRL
{
    /// <summary>
    /// Based on some simple logic from the MixedReality PolySpatial sample, this class detects double pinches and dispatches the event.
    /// </summary>
    /// 
    [System.Serializable]
    public class DoublePinchDetection
    {
        public class DoublePinchEventArgs : EventArgs
        {
            public Vector3 indexFingerPosition;
            public bool rightHand;
        }

        public event EventHandler<DoublePinchEventArgs> OnDoublePinch;

        [SerializeField] private Transform m_PolySpatialCameraTransform;
        [SerializeField] float k_PinchThreshold = 0.02f; // The distance between the index and thumb joints to consider a pinch
        [SerializeField] float k_PinchResetThreshold = 0.03f; // The distance between the index and thumb joints to reset the pinch.
        [SerializeField] float k_doublePinchCooldownTime = 0.5f; // The time between double pinches to prevent accidental double pinches.
        private XRHandSubsystem m_HandSubsystem;
        private XRHandJoint m_RightIndexTipJoint;
        private XRHandJoint m_RightThumbTipJoint;
        private XRHandJoint m_LeftIndexTipJoint;
        private XRHandJoint m_LeftThumbTipJoint;
        private bool m_ActiveRightPinch;
        private bool m_ActiveLeftPinch;
        private float m_ScaledThreshold;
        private float m_lastPinchTime = -1f;
        private float m_lastDoublePinchTime = -1;
        private XRInputWrapper m_xrInputWrapper;
        private bool m_lastPinchedHand = false; // false = left, true = right
        private bool isCoolingDownFromDoublePinch = false;

        const float k_doublePinchTime = 0.25f;
        public void OnStart(XRInputWrapper xrInputWrapper)
        {
            m_xrInputWrapper = xrInputWrapper;
            GetHandSubsystem();
            m_ScaledThreshold = k_PinchThreshold / m_PolySpatialCameraTransform.localScale.x;
        }

        public void OnUpdate()
        {
            if (Application.isEditor) 
            {
                // Use mouse click to simulate pinching.
                if (Input.GetMouseButtonDown(0))
                {
                    // Pick a pinch pos in front of the head. At the time of writing, this position isn't being used for anything, but it might
                    // in the future.
                    Vector3 indexFingerPos = m_xrInputWrapper.Head.position + m_xrInputWrapper.Head.forward * 0.5f;
                    OnPinchDetected(indexFingerPos, true);
                }
            }

            if (!CheckHandSubsystem())
                return;

            var updateSuccessFlags = m_HandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != 0)
            {
                // assign joint values
                m_RightIndexTipJoint = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.IndexTip);
                m_RightThumbTipJoint = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.ThumbTip);

                DetectPinch(m_RightIndexTipJoint, m_RightThumbTipJoint, ref m_ActiveRightPinch, true);
            }

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != 0)
            {
                // assign joint values
                m_LeftIndexTipJoint = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.IndexTip);
                m_LeftThumbTipJoint = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.ThumbTip);

                DetectPinch(m_LeftIndexTipJoint, m_LeftThumbTipJoint, ref m_ActiveLeftPinch, false);
            }
        }

        void GetHandSubsystem()
        {
            var xrGeneralSettings = XRGeneralSettings.Instance;
            if (xrGeneralSettings == null)
            {
                Debug.LogError("XR general settings not set");
            }

            var manager = xrGeneralSettings.Manager;
            if (manager != null)
            {
                var loader = manager.activeLoader;
                if (loader != null)
                {
                    m_HandSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
                    if (!CheckHandSubsystem())
                        return;

                    m_HandSubsystem.Start();
                }
            }
        }

        private bool CheckHandSubsystem()
        {
            if (m_HandSubsystem == null)
            {
    #if !UNITY_EDITOR
                Debug.LogError("Could not find Hand Subsystem");
    #endif
                return false;
            }

            return true;
        }

        private void DetectPinch(XRHandJoint index, XRHandJoint thumb, ref bool pinchActiveFlag, bool right)
        {
            if (index.trackingState != XRHandJointTrackingState.None &&
                thumb.trackingState != XRHandJointTrackingState.None)
            {
                Vector3 indexPOS = Vector3.zero;
                Vector3 thumbPOS = Vector3.zero;

                if (index.TryGetPose(out Pose indexPose))
                {
                    // adjust transform relative to the PolySpatial Camera transform
                    indexPOS = m_PolySpatialCameraTransform.InverseTransformPoint(indexPose.position);
                }

                if (thumb.TryGetPose(out Pose thumbPose))
                {
                    // adjust transform relative to the PolySpatial Camera adjustments
                    thumbPOS = m_PolySpatialCameraTransform.InverseTransformPoint(thumbPose.position);
                }

                var pinchDistance = Vector3.Distance(indexPOS, thumbPOS);

                if (pinchDistance <= m_ScaledThreshold)
                {
                    if (!pinchActiveFlag)
                    {
                        OnPinchDetected(indexPOS, right);
                        pinchActiveFlag = true;
                    }
                }
                else if (pinchDistance >= k_PinchResetThreshold) // You have to pull apart your fingers at an even greater distance to reset the pinch.
                {
                    pinchActiveFlag = false;
                }
            }
        }

        private void OnPinchDetected(Vector3 indexFingerPos, bool right)
        {
            // In order to cool down from a double pinch, there must be no single pinches for a certain amount of time.
            float timeSinceLastSinglePinch = Time.unscaledTime - m_lastPinchTime;

            if (isCoolingDownFromDoublePinch && timeSinceLastSinglePinch > k_doublePinchCooldownTime)
            {
                isCoolingDownFromDoublePinch = false;
            }

            // If we're within the double pinch time, and if the hand matches, then we can trigger the double pinch.
            if (CheckForDoublePinch(right))
            {
                OnDoublePinchDetected(indexFingerPos, right);
            }

            m_lastPinchTime = Time.unscaledTime;
            m_lastPinchedHand = right;
        }

        private bool CheckForDoublePinch(bool right)
        {
            if (isCoolingDownFromDoublePinch) return false; // If we're cooling down from a double pinch, don't allow another one.

            bool hasPerformedSinglePinchAtLeastOnce = m_lastPinchTime > 0;
            float timeSinceLastSinglePinch = Time.unscaledTime - m_lastPinchTime;;

            bool isWithinDoublePinchWindow = hasPerformedSinglePinchAtLeastOnce && timeSinceLastSinglePinch < k_doublePinchTime;

            return isWithinDoublePinchWindow && m_lastPinchedHand == right; // Ensure it's within the double pinch window and with the same hand.
        }

        private void OnDoublePinchDetected(Vector3 indexFingerPos, bool right)
        {
            // Fire double pinch event here.
            Debug.Log("Double Pinch detected at " + indexFingerPos);
            m_lastDoublePinchTime = Time.unscaledTime;
            isCoolingDownFromDoublePinch = true;
            OnDoublePinch?.Invoke(this, new DoublePinchEventArgs { indexFingerPosition = indexFingerPos, rightHand = right });
        }
    }
}

