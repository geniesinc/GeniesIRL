using UnityEngine;

// Requires hands package and VisionOSHandExtensions which is only compiled for visionOS and Editor
#if (UNITY_VISIONOS || UNITY_EDITOR)
using System.Collections.Generic;
using UnityEngine.XR.Hands;
using UnityEngine.XR.VisionOS;
#endif

public class HandVisualizer : MonoBehaviour
{
    [SerializeField]
    GameObject m_JointPrefab;

#if (UNITY_VISIONOS || UNITY_EDITOR)
    XRHandSubsystem m_Subsystem;
    HandGameObjects m_LeftHandGameObjects;
    HandGameObjects m_RightHandGameObjects;

    static readonly List<XRHandSubsystem> k_SubsystemsReuse = new();

    protected void OnEnable()
    {
        if (m_Subsystem == null)
            return;

        UpdateRenderingVisibility(m_LeftHandGameObjects, m_Subsystem.leftHand.isTracked);
        UpdateRenderingVisibility(m_RightHandGameObjects, m_Subsystem.rightHand.isTracked);
    }

    protected void OnDisable()
    {
        if (m_Subsystem != null)
            UnsubscribeSubsystem();

        UpdateRenderingVisibility(m_LeftHandGameObjects, false);
        UpdateRenderingVisibility(m_RightHandGameObjects, false);
    }

    void UnsubscribeSubsystem()
    {
        m_Subsystem.trackingAcquired -= OnTrackingAcquired;
        m_Subsystem.trackingLost -= OnTrackingLost;
        m_Subsystem.updatedHands -= OnUpdatedHands;
        m_Subsystem = null;
    }

    protected void OnDestroy()
    {
        if (m_LeftHandGameObjects != null)
        {
            m_LeftHandGameObjects.OnDestroy();
            m_LeftHandGameObjects = null;
        }

        if (m_RightHandGameObjects != null)
        {
            m_RightHandGameObjects.OnDestroy();
            m_RightHandGameObjects = null;
        }
    }

    protected void Update()
    {
        if (m_Subsystem != null)
        {
            if (m_Subsystem.running)
                return;

            UnsubscribeSubsystem();
            UpdateRenderingVisibility(m_LeftHandGameObjects, false);
            UpdateRenderingVisibility(m_RightHandGameObjects, false);
            return;
        }

        SubsystemManager.GetSubsystems(k_SubsystemsReuse);
        for (var i = 0; i < k_SubsystemsReuse.Count; ++i)
        {
            var handSubsystem = k_SubsystemsReuse[i];
            if (handSubsystem.running)
            {
                UnsubscribeHandSubsystem();
                m_Subsystem = handSubsystem;
                break;
            }
        }

        if (m_Subsystem == null)
            return;

        if (m_LeftHandGameObjects == null)
        {
            m_LeftHandGameObjects = new HandGameObjects(
                Handedness.Left,
                transform,
                m_JointPrefab);
        }

        if (m_RightHandGameObjects == null)
        {
            m_RightHandGameObjects = new HandGameObjects(
                Handedness.Right,
                transform,
                m_JointPrefab);
        }

        UpdateRenderingVisibility(m_LeftHandGameObjects, m_Subsystem.leftHand.isTracked);
        UpdateRenderingVisibility(m_RightHandGameObjects, m_Subsystem.rightHand.isTracked);

        SubscribeHandSubsystem();
    }

    void SubscribeHandSubsystem()
    {
        if (m_Subsystem == null)
            return;

        m_Subsystem.trackingAcquired += OnTrackingAcquired;
        m_Subsystem.trackingLost += OnTrackingLost;
        m_Subsystem.updatedHands += OnUpdatedHands;
    }

    void UnsubscribeHandSubsystem()
    {
        if (m_Subsystem == null)
            return;

        m_Subsystem.trackingAcquired -= OnTrackingAcquired;
        m_Subsystem.trackingLost -= OnTrackingLost;
        m_Subsystem.updatedHands -= OnUpdatedHands;
    }

    static void UpdateRenderingVisibility(HandGameObjects handGameObjects, bool isTracked)
    {
        if (handGameObjects == null)
            return;

        handGameObjects.ToggleDebugDrawJoints(isTracked);
    }

    void OnTrackingAcquired(XRHand hand)
    {
        switch (hand.handedness)
        {
            case Handedness.Left:
                UpdateRenderingVisibility(m_LeftHandGameObjects, true);
                break;

            case Handedness.Right:
                UpdateRenderingVisibility(m_RightHandGameObjects, true);
                break;
        }
    }

    void OnTrackingLost(XRHand hand)
    {
        switch (hand.handedness)
        {
            case Handedness.Left:
                UpdateRenderingVisibility(m_LeftHandGameObjects, false);
                break;

            case Handedness.Right:
                UpdateRenderingVisibility(m_RightHandGameObjects, false);
                break;
        }
    }

    void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        // We have no game logic depending on the Transforms, so early out here
        // (add game logic before this return here, directly querying from
        // subsystem.leftHand and subsystem.rightHand using GetJoint on each hand)
        if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            return;

        m_LeftHandGameObjects.UpdateJoints(
            subsystem.leftHand,
            (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0);

        m_RightHandGameObjects.UpdateJoints(
            subsystem.rightHand,
            (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0);
    }

    class HandGameObjects
    {
        GameObject m_DrawJointsParent;
        const int k_NumVisionOSJoints = 2;

        readonly GameObject[] m_DrawJoints = new GameObject[XRHandJointID.EndMarker.ToIndex() + k_NumVisionOSJoints];
        readonly LineRenderer[] m_Lines = new LineRenderer[XRHandJointID.EndMarker.ToIndex() + k_NumVisionOSJoints];

        static Vector3[] s_LinePointsReuse = new Vector3[2];
        const float k_LineWidth = 0.005f;

        public HandGameObjects(
            Handedness handedness,
            Transform parent,
            GameObject debugDrawPrefab)
        {
            void AssignJoint(
                XRHandJointID jointID,
                Transform drawJointsParent)
            {
                var jointIndex = jointID.ToIndex();
                var joint = Instantiate(debugDrawPrefab);
                var jointTransform = joint.transform;
                jointTransform.parent = drawJointsParent;
                joint.name = jointID < XRHandJointID.EndMarker ? jointID.ToString() : ((VisionOSHandJointID)jointID).ToString();
                m_DrawJoints[jointIndex] = joint;

                m_Lines[jointIndex] = m_DrawJoints[jointIndex].GetComponent<LineRenderer>();
                m_Lines[jointIndex].startWidth = m_Lines[jointIndex].endWidth = k_LineWidth;
                s_LinePointsReuse[0] = s_LinePointsReuse[1] = jointTransform.position;
                m_Lines[jointIndex].SetPositions(s_LinePointsReuse);
            }

            m_DrawJointsParent = new GameObject();
            var parentTransform = m_DrawJointsParent.transform;
            parentTransform.parent = parent;
            parentTransform.localPosition = Vector3.zero;
            parentTransform.localRotation = Quaternion.identity;
            m_DrawJointsParent.name = handedness + "HandDebugDrawJoints";

            AssignJoint(XRHandJointID.Wrist, parentTransform);
            AssignJoint(XRHandJointID.Palm, parentTransform);

            for (var fingerIndex = (int)XRHandFingerID.Thumb;
                    fingerIndex <= (int)XRHandFingerID.Little;
                    ++fingerIndex)
            {
                var fingerId = (XRHandFingerID)fingerIndex;
                var jointIndexBack = fingerId.GetBackJointID().ToIndex();
                for (var jointIndex = fingerId.GetFrontJointID().ToIndex();
                        jointIndex <= jointIndexBack;
                        ++jointIndex)
                {
                    AssignJoint(XRHandJointIDUtility.FromIndex(jointIndex), parentTransform);
                }
            }

            AssignJoint((XRHandJointID)VisionOSHandJointID.ForearmWrist, parentTransform);
            AssignJoint((XRHandJointID)VisionOSHandJointID.ForearmArm, parentTransform);
        }

        public void OnDestroy()
        {
            for (var jointIndex = 0; jointIndex < m_DrawJoints.Length; ++jointIndex)
            {
                Destroy(m_DrawJoints[jointIndex]);
                m_DrawJoints[jointIndex] = null;
            }

            Destroy(m_DrawJointsParent);
            m_DrawJointsParent = null;
        }

        public void ToggleDebugDrawJoints(bool debugDrawJoints)
        {
            for (var jointIndex = 0; jointIndex < m_DrawJoints.Length; ++jointIndex)
            {
                ToggleRenderers<MeshRenderer>(debugDrawJoints, m_DrawJoints[jointIndex].transform);
                m_Lines[jointIndex].enabled = debugDrawJoints;
            }

            m_Lines[0].enabled = false;
        }

        public void UpdateJoints(
            XRHand hand,
            bool areJointsTracked)
        {
            if (!areJointsTracked)
                return;

            var wristPose = Pose.identity;
            var parentIndex = XRHandJointID.Wrist.ToIndex();
            UpdateJoint(hand.GetJoint(XRHandJointID.Wrist), ref wristPose, ref parentIndex);
            UpdateJoint(hand.GetJoint(XRHandJointID.Palm), ref wristPose, ref parentIndex, false);

            for (var fingerIndex = (int)XRHandFingerID.Thumb;
                fingerIndex <= (int)XRHandFingerID.Little;
                ++fingerIndex)
            {
                var parentPose = wristPose;
                var fingerId = (XRHandFingerID)fingerIndex;
                parentIndex = XRHandJointID.Wrist.ToIndex();

                var jointIndexBack = fingerId.GetBackJointID().ToIndex();
                for (var jointIndex = fingerId.GetFrontJointID().ToIndex();
                    jointIndex <= jointIndexBack;
                    ++jointIndex)
                {
                    UpdateJoint(hand.GetJoint(XRHandJointIDUtility.FromIndex(jointIndex)), ref parentPose, ref parentIndex);
                }
            }

            parentIndex = XRHandJointID.Wrist.ToIndex();
            UpdateJoint(hand.GetVisionOSJoint(VisionOSHandJointID.ForearmWrist), ref wristPose, ref parentIndex);
            UpdateJoint(hand.GetVisionOSJoint(VisionOSHandJointID.ForearmArm), ref wristPose, ref parentIndex);
        }

        void UpdateJoint(
            XRHandJoint joint,
            ref Pose parentPose,
            ref int parentIndex,
            bool cacheParentPose = true)
        {
            if (joint.id == XRHandJointID.Invalid)
                return;

            var jointIndex = joint.id.ToIndex();
            var drawJoint = m_DrawJoints[jointIndex];
            var line = m_Lines[jointIndex];
            if ((joint.trackingState & XRHandJointTrackingState.Pose) == 0 || !joint.TryGetPose(out var pose))
            {
                drawJoint.SetActive(false);
                line.gameObject.SetActive(false);
                return;
            }

            drawJoint.SetActive(true);
            line.gameObject.SetActive(true);
            var jointTransform = drawJoint.transform;
            jointTransform.localPosition = pose.position;
            jointTransform.localRotation = pose.rotation;

            if (joint.id != XRHandJointID.Wrist)
            {
                s_LinePointsReuse[0] = m_DrawJoints[parentIndex].transform.position;
                s_LinePointsReuse[1] = jointTransform.position;
                line.SetPositions(s_LinePointsReuse);
            }

            if (cacheParentPose)
            {
                parentPose = pose;
                parentIndex = jointIndex;
            }
        }

        static void ToggleRenderers<TRenderer>(bool toggle, Transform rendererTransform)
            where TRenderer : Renderer
        {
            if (rendererTransform.TryGetComponent<TRenderer>(out var renderer))
                renderer.enabled = toggle;

            for (var childIndex = 0; childIndex < rendererTransform.childCount; ++childIndex)
                ToggleRenderers<TRenderer>(toggle, rendererTransform.GetChild(childIndex));
        }
    }
#endif
}
