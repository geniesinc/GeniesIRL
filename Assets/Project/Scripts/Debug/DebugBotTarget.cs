using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class DebugBotTarget : MonoBehaviour
{
    public bool EnableFollow = false;

    [SerializeField]
    Transform m_PolySpatialCameraTransform;

    XRHandSubsystem m_HandSubsystem;
    XRHandJoint m_RightIndexTipJoint;
    XRHandJoint m_RightThumbTipJoint;
    XRHandJoint m_LeftIndexTipJoint;
    XRHandJoint m_LeftThumbTipJoint;
    bool m_ActiveRightPinch;
    bool m_ActiveLeftPinch;
    float m_ScaledThreshold;
    float m_lastPinchTime = -1f;
    const float k_PinchThreshold = 0.02f;
    const float k_doublePinchTime = 0.25f;

    void Start()
    {
        GetHandSubsystem();
        m_ScaledThreshold = k_PinchThreshold / m_PolySpatialCameraTransform.localScale.x;
    }

    void Update()
    {
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

    bool CheckHandSubsystem()
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

    void DetectPinch(XRHandJoint index, XRHandJoint thumb, ref bool activeFlag, bool right)
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
                if (!activeFlag)
                {
                    OnPinch(indexPOS);
                    activeFlag = true;
                }
            }
            else
            {
                activeFlag = false;
            }
        }
    }

    private void OnPinch(Vector3 pos)
    {
        if (m_lastPinchTime > 0 && Time.unscaledTime - m_lastPinchTime < k_doublePinchTime)
        {
            OnDoublePinch(pos);
        }

        m_lastPinchTime = Time.unscaledTime;
    }

    private void OnDoublePinch(Vector3 pos)
    {
        transform.position = pos;
        EnableFollow = true;
    }
}
