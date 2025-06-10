using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.XR.Hands;
using GeniesIRL;

public class GeniesIKComponent : MonoBehaviour
{
    public Vector3 headRotationOffset;
    public Vector3 rightHandRotationOffset;
    public Vector3 leftHandRotationOffset;
    public bool engageShouldersWhenReaching = false;
    public GeniesHandJointMapping handJointMapping;
    public GameObject leftLegTarget {get; private set;}
    public GameObject rightLegTarget {get; private set;}
    public GameObject hipTarget {get; private set;}

    private Genie genie;
    private Animator animator;
    private GameObject ikRig, ikhead, ikrightArm, ikleftArm, ikrightLeg, ikleftLeg, hipJoint, spineTarget, headToBodyOffset;
    private GameObject rightArmTarget, rightHandJoint, rightForearmJoint, rightShoulderJoint, rightArmJoint;
    private TwoBoneIKConstraint rightArmConstraint, leftArmConstraint;
    private GameObject rightShoulderTarget, rightShoulderContraint;
    private GameObject leftArmTarget, leftHandJoint, leftForearmJoint, leftShoulderJoint, leftArmJoint;
    private GameObject leftShoulderTarget, leftShoulderContraint;
    private GameObject rightFootJoint, leftFootJoint;
    private TwoBoneIKConstraint rightLegConstraint, leftLegConstraint;
    private MultiParentConstraint hipConstraint;
    private ChainIKConstraint spineConstraint;

    private Dictionary<Transform, Quaternion> BindPoseDic = new Dictionary<Transform, Quaternion>();

    private Dictionary<XRHandJointID, Transform[]> jointsTransformMap;

    private void Start()
    {
        genie = GetComponentInParent<Genie>();
        animator = GetComponent<Animator>();

        GetBindPoseTransforms();

        CreateGenieIKRig();
        FingerMapping();

        // Turn off constraints when needed
        if (!genie.IsUserControlled)
        {
            rightArmConstraint.weight = 0;
            leftArmConstraint.weight = 0;
        }
        rightLegConstraint.weight = 0;
        leftLegConstraint.weight = 0;

    }

    void Update()
    {
        ShoulderIKMobility();

        // Output lean-related data
        // if (spineConstraint.weight > 0)
        // {
            // string str = "";
            // str += "Spine Target XZ Dist: " + VectorUtils.GetDistanceXZ(transform.position, spineTarget.transform.position);
            // str += "Spine Target Y Dist: " + Mathf.Abs(transform.position.y - spineTarget.transform.position.y);
            // Debug.Log(str);
        //}
    }

    private void LateUpdate()
    {
        if (genie.IsUserControlled)
        {
            UpdateHeadToUsers();

            UpdateGeniePositionToUser();
        }

        // This function completely overrides animation rotation on hands as the IK should
        // For some reason IK wasn't doing it at 100%.
        ForceHandRotationToIK();
        ForceFeetRotationToIK();
    }

    void CreateGenieIKRig()
    {
        //RigBuilder
        gameObject.AddComponent<RigBuilder>();
        //gameObject.AddComponent<Animator>();

#if UNITY_EDITOR
        // Adding boneRenderer for easier visualization
        var boneRenderer = gameObject.AddComponent<BoneRenderer>();
        var skeletalRoot = transform.FindDeepChild("Root");
        boneRenderer.transforms = skeletalRoot.GetComponentsInChildren<Transform>();  
#endif
        // IKs RIG
        ikRig = new GameObject("IKRigs");
        ikRig.transform.parent = this.gameObject.transform;
        ikRig.transform.position = this.transform.position;
        ikRig.transform.rotation = this.transform.rotation;
        ikRig.AddComponent<Rig>();

        rightHandJoint = transform.FindDeepChild("RightHand").gameObject;
        leftHandJoint = transform.FindDeepChild("LeftHand").gameObject;
        rightFootJoint = animator.GetBoneTransform(HumanBodyBones.RightFoot).gameObject;
        leftFootJoint = animator.GetBoneTransform(HumanBodyBones.LeftFoot).gameObject;

        rightShoulderJoint = animator.GetBoneTransform(HumanBodyBones.RightShoulder).gameObject;
        leftShoulderJoint = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).gameObject;

        gameObject.GetComponent<RigBuilder>().layers.Add(new RigLayer(ikRig.GetComponent<Rig>(), true));

        if (genie.IsUserControlled)
        {
            // HeadToBodyOffset
            headToBodyOffset = new GameObject("HeadToBodyOffset");
            Transform head = transform.FindDeepChild("Head");
            Transform LeftEyeT = transform.FindDeepChild("LeftEye");
            Transform RightEyeT = transform.FindDeepChild("RightEye");
            Vector3 middleEyes = Vector3.Lerp(LeftEyeT.position, RightEyeT.position, 0.5f);

            headToBodyOffset.transform.parent = this.gameObject.transform;
            headToBodyOffset.transform.position = middleEyes;
            headToBodyOffset.transform.rotation = head.rotation;
            
            // Head
            ikhead = new GameObject("IKHead");
            ikhead.transform.parent = ikRig.gameObject.transform;
            ikhead.transform.position = head.position;
            ikhead.transform.rotation = head.rotation;

            ikhead.AddComponent<MultiParentConstraint>();
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedObject = head;
            ikhead.GetComponent<MultiParentConstraint>().data.sourceObjects = new WeightedTransformArray { new WeightedTransform(ikhead.transform, 1) };
            ikhead.GetComponent<MultiParentConstraint>().data.maintainPositionOffset = true;
            ikhead.GetComponent<MultiParentConstraint>().data.maintainRotationOffset = true;
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedPositionXAxis = true;
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedPositionYAxis = true;
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedPositionZAxis = true;
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedRotationXAxis = true;
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedRotationYAxis = true;
            ikhead.GetComponent<MultiParentConstraint>().data.constrainedRotationZAxis = true;
        }

        //Right Arm
        ikrightArm = new GameObject("IKRightArm");
        ikrightArm.transform.parent = ikRig.gameObject.transform;
        ikrightArm.transform.position = ikRig.gameObject.transform.position;
        ikrightArm.transform.rotation = ikRig.gameObject.transform.rotation;
        rightArmConstraint = ikrightArm.AddComponent<TwoBoneIKConstraint>();

        rightArmTarget = new GameObject("RightArmTarget");
        rightArmTarget.transform.position = rightHandJoint.transform.position;
        rightArmTarget.transform.rotation = rightHandJoint.transform.rotation;

        GameObject rightArmHint = new GameObject("Hint");
        rightArmHint.transform.parent = ikrightArm.gameObject.transform;
        rightForearmJoint = transform.FindDeepChild("RightForeArm").gameObject;
        rightArmHint.transform.position = rightForearmJoint.transform.position;
        //Hint on Genie Space Rotation
        rightArmHint.transform.rotation = ikrightArm.transform.rotation;
        rightArmHint.transform.Translate(new Vector3(0.15f, -0.1f, 0), Space.Self);
        //Debug.Log("Right Arm Hint Local Position: " + rightArmHint.transform.localPosition);

        rightArmJoint = transform.FindDeepChild("RightArm").gameObject;

        rightArmConstraint.data.root = rightArmJoint.transform;
        rightArmConstraint.data.mid = rightForearmJoint.transform;
        rightArmConstraint.data.tip = rightHandJoint.transform;
        rightArmConstraint.data.target = rightArmTarget.transform;
        rightArmConstraint.data.hint = rightArmHint.transform;
        rightArmConstraint.data.targetPositionWeight = 1;
        rightArmConstraint.data.targetRotationWeight = 1;
        rightArmConstraint.data.hintWeight = 1;


        // Left Arm
        ikleftArm = new GameObject("IKLeftArm");
        ikleftArm.transform.parent = ikRig.gameObject.transform;
        ikleftArm.transform.position = ikRig.gameObject.transform.position;
        ikleftArm.transform.rotation = ikRig.gameObject.transform.rotation;
        leftArmConstraint = ikleftArm.AddComponent<TwoBoneIKConstraint>();

        leftArmTarget = new GameObject("LeftArmTarget");
        leftArmTarget.transform.position = leftHandJoint.transform.position;
        leftArmTarget.transform.rotation = leftHandJoint.transform.rotation;

        GameObject leftArmHint = new GameObject("Hint");
        leftArmHint.transform.parent = ikleftArm.gameObject.transform;
        leftForearmJoint = transform.FindDeepChild("LeftForeArm").gameObject;
        leftArmHint.transform.position = leftForearmJoint.transform.position;
        //Hint on Genie Space Rotation
        leftArmHint.transform.rotation = ikleftArm.transform.rotation;
        leftArmHint.transform.Translate(new Vector3(-0.15f, -0.1f, 0), Space.Self);

        leftArmJoint = transform.FindDeepChild("LeftArm").gameObject;

        leftArmConstraint.data.root = leftArmJoint.transform;
        leftArmConstraint.data.mid = leftForearmJoint.transform;
        leftArmConstraint.data.tip = leftHandJoint.transform;
        leftArmConstraint.data.target = leftArmTarget.transform;
        leftArmConstraint.data.hint = leftArmHint.transform;
        leftArmConstraint.data.targetPositionWeight = 1;
        leftArmConstraint.data.targetRotationWeight = 1;
        leftArmConstraint.data.hintWeight = 1;


        // Left Leg
        ikleftLeg = new GameObject("IKLeftLeg");
        ikleftLeg.transform.parent = ikRig.gameObject.transform;
        ikleftLeg.transform.position = ikRig.gameObject.transform.position;
        ikleftLeg.transform.rotation = ikRig.gameObject.transform.rotation;
        leftLegConstraint = ikleftLeg.AddComponent<TwoBoneIKConstraint>();

        leftLegTarget = new GameObject("Target");
        leftLegTarget.transform.parent = ikleftLeg.gameObject.transform;
        leftLegTarget.transform.position = leftFootJoint.transform.position;
        leftLegTarget.transform.rotation = leftFootJoint.transform.rotation;

        GameObject leftLegHint = new GameObject("Hint");
        leftLegHint.transform.parent = ikleftLeg.gameObject.transform;
        leftLegHint.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
        leftLegHint.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).rotation;
        leftLegHint.transform.Translate(new Vector3(0.4f, 0.4f, 0), Space.Self);

        leftLegConstraint.data.root = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        leftLegConstraint.data.mid = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        leftLegConstraint.data.tip = leftFootJoint.transform;
        leftLegConstraint.data.target = leftLegTarget.transform;
        leftLegConstraint.data.hint = leftLegHint.transform;
        leftLegConstraint.data.targetPositionWeight = 1;
        leftLegConstraint.data.targetRotationWeight = 1;
        leftLegConstraint.data.hintWeight = 1;

        // Right Leg
        ikrightLeg = new GameObject("IKRightLeg");
        ikrightLeg.transform.parent = ikRig.gameObject.transform;
        ikrightLeg.transform.position = ikRig.gameObject.transform.position;
        ikrightLeg.transform.rotation = ikRig.gameObject.transform.rotation;
        rightLegConstraint = ikrightLeg.AddComponent<TwoBoneIKConstraint>();

        rightLegTarget = new GameObject("Target");
        rightLegTarget.transform.parent = ikrightLeg.gameObject.transform;
        rightLegTarget.transform.position = rightFootJoint.transform.position;
        rightLegTarget.transform.rotation = rightFootJoint.transform.rotation;

        GameObject rightLegHint = new GameObject("Hint");
        rightLegHint.transform.parent = ikrightLeg.gameObject.transform;
        rightLegHint.transform.position = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position;
        rightLegHint.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).rotation;
        rightLegHint.transform.Translate(new Vector3(-0.4f, -0.4f, 0), Space.Self);

        rightLegConstraint.data.root = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        rightLegConstraint.data.mid = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        rightLegConstraint.data.tip = rightFootJoint.transform;
        rightLegConstraint.data.target = rightLegTarget.transform;
        rightLegConstraint.data.hint = rightLegHint.transform;
        rightLegConstraint.data.targetPositionWeight = 1;
        rightLegConstraint.data.targetRotationWeight = 1;
        rightLegConstraint.data.hintWeight = 1;

        var LeftFootIK = leftLegTarget.transform;
        var RightFootIK = rightLegTarget.transform;

        ShoulderContraintSetup();
        HipConstraintSetup();
        SpineConstraintSetup();

        // BUILD RIG
        gameObject.GetComponent<RigBuilder>().Build();

    }

    private void ShoulderContraintSetup()
    {
        // RIGHT SHOULDER
        // Shoulder Rotation with constraints
        rightShoulderTarget = new GameObject("rightShoulderTarget");
        rightShoulderContraint = new GameObject("rightShoulderConstraint");
        rightShoulderContraint.transform.parent = ikRig.gameObject.transform;
        rightShoulderContraint.transform.SetSiblingIndex(0);
        rightShoulderContraint.transform.position = ikRig.gameObject.transform.position;
        rightShoulderContraint.transform.rotation = ikRig.gameObject.transform.rotation;
        rightShoulderTarget.transform.parent = animator.GetBoneTransform(HumanBodyBones.Chest);
        rightShoulderTarget.transform.position = rightShoulderJoint.transform.position;
        rightShoulderTarget.transform.rotation = ikRig.gameObject.transform.rotation;

        // Right shoulder Rotation constraint
        rightShoulderContraint.AddComponent<MultiRotationConstraint>();
        rightShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedObject = rightShoulderJoint.transform;
        rightShoulderContraint.GetComponent<MultiRotationConstraint>().data.sourceObjects = new WeightedTransformArray { new WeightedTransform(rightShoulderTarget.transform, 1) };
        rightShoulderContraint.GetComponent<MultiRotationConstraint>().data.maintainOffset = true;
        rightShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedXAxis = true;
        rightShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedYAxis = true;
        rightShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedZAxis = true;

        // LEFT SHOULDER
        // Shoulder Rotation with constraints
        leftShoulderTarget = new GameObject("leftShoulderTarget");
        leftShoulderContraint = new GameObject("leftShoulderConstraint");
        leftShoulderContraint.transform.parent = ikRig.gameObject.transform;
        leftShoulderContraint.transform.SetSiblingIndex(0);
        leftShoulderContraint.transform.position = ikRig.gameObject.transform.position;
        leftShoulderContraint.transform.rotation = ikRig.gameObject.transform.rotation;
        leftShoulderTarget.transform.parent = animator.GetBoneTransform(HumanBodyBones.Chest);
        leftShoulderTarget.transform.position = leftShoulderJoint.transform.position;
        leftShoulderTarget.transform.rotation = ikRig.gameObject.transform.rotation;

        // Right shoulder Rotation constraint
        leftShoulderContraint.AddComponent<MultiRotationConstraint>();
        leftShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedObject = leftShoulderJoint.transform;
        leftShoulderContraint.GetComponent<MultiRotationConstraint>().data.sourceObjects = new WeightedTransformArray { new WeightedTransform(leftShoulderTarget.transform, 1) };
        leftShoulderContraint.GetComponent<MultiRotationConstraint>().data.maintainOffset = true;
        leftShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedXAxis = true;
        leftShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedYAxis = true;
        leftShoulderContraint.GetComponent<MultiRotationConstraint>().data.constrainedZAxis = true;
    }

    private void HipConstraintSetup()
    {
        hipJoint = animator.GetBoneTransform(HumanBodyBones.Hips).gameObject;
        hipTarget = new GameObject("HipTarget");
        hipTarget.transform.position = hipJoint.transform.position;
        hipTarget.transform.rotation = hipJoint.transform.rotation;

        GameObject hipConstraintGO = new GameObject("HipOverride");
        hipConstraintGO.transform.parent = ikRig.gameObject.transform;
        hipConstraintGO.transform.position = ikRig.gameObject.transform.position;
        hipConstraintGO.transform.rotation = ikRig.gameObject.transform.rotation;
        hipConstraintGO.transform.SetAsFirstSibling();
        hipTarget.transform.parent = hipConstraintGO.transform;

        hipConstraint = hipConstraintGO.AddComponent<MultiParentConstraint>();
        hipConstraint.data.constrainedObject = hipJoint.transform;
        hipConstraint.data.sourceObjects = new WeightedTransformArray { new WeightedTransform(hipTarget.transform, 1) };
        hipConstraint.data.maintainPositionOffset = true;
        hipConstraint.data.maintainRotationOffset = true;
        hipConstraint.data.constrainedPositionXAxis = true;
        hipConstraint.data.constrainedPositionYAxis = true;
        hipConstraint.data.constrainedPositionZAxis = true;
        hipConstraint.data.constrainedRotationXAxis = true;
        hipConstraint.data.constrainedRotationYAxis = true;
        hipConstraint.data.constrainedRotationZAxis = true;
        hipConstraint.weight = 0;

    }
    private void SpineConstraintSetup()
    {
        var chestJoint = animator.GetBoneTransform(HumanBodyBones.Chest).gameObject;
        var headJoint = animator.GetBoneTransform(HumanBodyBones.Head).gameObject;

        spineTarget = new GameObject("SpineTarget");
        spineTarget.transform.position = chestJoint.transform.position;
        spineTarget.transform.rotation = chestJoint.transform.rotation;

        GameObject spineConstraintGO = new GameObject("SpineOverride");
        spineConstraintGO.transform.parent = ikRig.gameObject.transform;
        spineConstraintGO.transform.position = ikRig.gameObject.transform.position;
        spineConstraintGO.transform.rotation = ikRig.gameObject.transform.rotation;
        spineConstraintGO.transform.SetAsFirstSibling();
        spineTarget.transform.parent = spineConstraintGO.transform;

        spineConstraint = spineConstraintGO.AddComponent<ChainIKConstraint>();
        spineConstraint.data.root = chestJoint.transform;
        spineConstraint.data.tip = headJoint.transform;
        spineConstraint.data.target = spineTarget.transform;
        spineConstraint.data.maintainTargetPositionOffset = true;
        spineConstraint.data.maintainTargetRotationOffset = true;
        spineConstraint.data.chainRotationWeight = 1;
        spineConstraint.data.tipRotationWeight = 1;
        spineConstraint.data.maxIterations = 15;
        spineConstraint.weight = 0;
    }


    private void FingerMapping()
    {
        if (!genie.IsUserControlled)
        {
            return;
        }
        if (handJointMapping == null)
        {
            Debug.LogWarning("Genies Finger Mapping ScriptableObject is not assigned.");
            return;
        }

        jointsTransformMap = new Dictionary<XRHandJointID, Transform[]>();

        foreach (var mapping in handJointMapping.jointMappings)
        {
            int i = 0;
            Transform[] jointsTransforms = new Transform[2];
            foreach (string side in handJointMapping.SidePrefix)
            {
                Transform jointTransform = transform.FindDeepChild(side + mapping.jointRigName);
                if (jointTransform != null)
                {
                    jointsTransforms[i] = jointTransform;
                    //Debug.Log($"Mapped {mapping.jointID} to {jointTransform.name}");
                }
                else
                {
                    Debug.LogWarning($"Bone named {mapping.jointRigName} not found in rig.");
                }
                i++;
            }
            jointsTransformMap[mapping.jointID] = jointsTransforms;
        }
    }

    private void GetBindPoseTransforms()
    {
        // Get the SkinnedMeshRenderer attached to the body
        Transform body = transform.FindDeepChild("bodyOnly_geo");
        if (body == null)
        {
            body = transform.FindDeepChild("body_geo");
        }
        if (body == null)
        {
            body = transform.FindDeepChild("torso_geo");
        }
        if (body == null)
        {
            return;
        }

        SkinnedMeshRenderer skinnedMeshRenderer = body.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("No SkinnedMeshRenderer found!");
            return;
        }

        // Get the Mesh from the SkinnedMeshRenderer
        Mesh mesh = skinnedMeshRenderer.sharedMesh;

        // Get the bind poses from the mesh
        Matrix4x4[] bindPoses = mesh.bindposes;

        // Get the bones from the SkinnedMeshRenderer
        Transform[] bones = skinnedMeshRenderer.bones;

        // Iterate through each bone and bind pose
        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            Matrix4x4 bindPose = bindPoses[i];

            // Store bone with bindpose rotation on a dictionary
            //Debug.Log($"Bone: {bone.name}, Bind Pose: {bindPose}");
            DecomposeBindPoseMatrix(bindPose, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            
            BindPoseDic[bone] = rot;
        }
    }

    private void DecomposeBindPoseMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetColumn(3);  // Extract position (4th column)

        // Extract rotation (based on lossless scale assumption)
        rotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));

        // Extract scale
        scale = new Vector3(
            matrix.GetColumn(0).magnitude,
            matrix.GetColumn(1).magnitude,
            matrix.GetColumn(2).magnitude
        );
    }

    private void UpdateGeniePositionToUser()
    {
        Vector3 offset = -(headToBodyOffset.transform.localPosition);

        Transform userHeadTransform = GeniesIrlBootstrapper.Instance.XRNode.xrOrigin.Camera.transform;
        genie.transform.position = userHeadTransform.position + offset;
        genie.transform.forward = Vector3.Lerp(genie.transform.forward,
                                            Vector3.ProjectOnPlane(userHeadTransform.forward,
                                                                Vector3.up).normalized, Time.deltaTime * 2);
    }

    private void UpdateHeadToUsers()
    {
        // Updates head target position with XR device data on head position
        Quaternion offset = Quaternion.Euler(headRotationOffset);
        ikhead.transform.rotation = GeniesIrlBootstrapper.Instance.XRNode.xrOrigin.Camera.transform.rotation * offset;
    }

    /// <summary>
    /// Places a Hand IK target on the target position. Note that this does not necessarily mean the hand will reach the target -- the weight is controlled in 
    /// GenieAnimation.
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <param name="handSide"></param>
    /// <param name="handRotationLogic"></param>
    /// <param name="customHandRotation"></param>
    /// <returns></returns>
    public TwoBoneIKConstraint ReachTowardsPosition(Vector3 targetPosition, GenieHand handSide, Quaternion customHandRotation)
    {
        TwoBoneIKConstraint twoBoneIKConstraint;
        GameObject ikTarget;
        // Get right or left hand IKs
        if (handSide == GenieHand.Right)
        {
            twoBoneIKConstraint = rightArmConstraint;
            ikTarget = rightArmTarget;
        }
        else
        {
            twoBoneIKConstraint = leftArmConstraint;
            ikTarget = leftArmTarget;
        }
        // Place IK hand target on the item
        Vector3 handOffset = genie.genieGrabber.GetHandAttachmentOffset(handSide);
        ikTarget.transform.position = targetPosition;
        ikTarget.transform.rotation = customHandRotation;
        
        // Offsetting it to match the hand attachment offset.
        ikTarget.transform.Translate(-handOffset, Space.Self);

        // Set hand height for animation. (Determines the crouching/standing height of the character.)
        genie.genieAnimation.SetHandAnimHeight(handSide, ikTarget.transform.position.y);

        // Consider leaning the spine towards the target
        if (ShouldLean(targetPosition)) 
        {
            spineTarget.transform.position = GetSpineTargetPosition(targetPosition);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            spineTarget.transform.rotation = Quaternion.LookRotation((targetPosition - head.position).normalized, Vector3.up);
            shouldBeLeaning = true;
        }
        else
        {
            shouldBeLeaning = false;
            //Debug.Log("Lean not needed");
        }

        // Activate IK component externally to get a transition.
        return twoBoneIKConstraint;
    }

    public const float dontLeanBelowHeight = 0.6f;
    public const float dontLeanAboveHeight = 1.5f;
    public const float dontLeanInsideXZDistance = 0.5f;
    public const float maxReachWithLeaning = 0.7f;

    private bool shouldBeLeaning = false;

    private Vector3 GetSpineTargetPosition(Vector3 targetPosition)
    {
        Vector3 spineTargetPosition = transform.position;
        spineTargetPosition.y = targetPosition.y;

        spineTargetPosition = Vector3.Lerp(spineTargetPosition, targetPosition, 0.5f);
        
        // Clamp the spine target position to the max reach with leaning.
        if (VectorUtils.IsGreaterThanDistanceXZ(spineTargetPosition, targetPosition, maxReachWithLeaning))
        {
            Vector3 directionToTarget = (targetPosition - spineTargetPosition).normalized;
            spineTargetPosition += directionToTarget * maxReachWithLeaning;
        }

        return spineTargetPosition;
    }

    // Determines whether the spine should lean to extend our reach.
    private bool ShouldLean(Vector3 targetPosition) 
    {
        float yDiff = targetPosition.y - transform.position.y;

        if (yDiff < dontLeanBelowHeight) 
        {
            Debug.Log("Target too low for leaning");
            return false; // No need to lean forward when the target is below a certain height.
        }

        if (yDiff > dontLeanAboveHeight) 
        {
            Debug.Log("Target too high for leaning");
            return false; // Lean forward at max when the target is above a certain height.
        }
        
        float xzDist = VectorUtils.GetDistanceXZ(transform.position, targetPosition);

        if (xzDist < dontLeanInsideXZDistance) {
            //Debug.Log("Target too close on the XZ plane for leaning");
            return false;
        }

        //Debug.Log("Performing lean for extra reach. YDiff: " + yDiff + ", XZDist: " + xzDist);

        return true;
    }

    public void SetHandsIKComponentWeight(GenieHand handSide, float ikWeight)
    {
        TwoBoneIKConstraint twoBoneIKConstraint;
        if (handSide == GenieHand.Right)
        {
            twoBoneIKConstraint = rightArmConstraint;
        }
        else
        {
            twoBoneIKConstraint = leftArmConstraint;
        }
        twoBoneIKConstraint.weight = ikWeight;
    }

    public void UpdateSpineIKComponentWeight(float ikWeight)
    {
        if (!shouldBeLeaning) 
        {
            spineConstraint.weight = 0f;
        }
        else
        {
            spineConstraint.weight = ikWeight;
        }
    }

    public void ActivateIKComponent(TwoBoneIKConstraint twoBoneIKConstraint, float duration = 1)
    {
        StartCoroutine(FadeINConstraintWeight(twoBoneIKConstraint, duration));
    }
    public void ActivateRightArmIKComponent(float duration = 1)
    {
        StartCoroutine(FadeINConstraintWeight(rightArmConstraint, duration));
    }
    public void ActivateLeftArmIKComponent(float duration = 1)
    {
        StartCoroutine(FadeINConstraintWeight(leftArmConstraint, duration));
    }
    public void ActivateRightLegIKComponent(float duration = 1)
    {
        if (duration == 0)
        {
            rightLegConstraint.weight = 1;
        }
        else
        {
            StartCoroutine(FadeINConstraintWeight(rightLegConstraint, duration));
        }
        
    }
    public void ActivateLeftLegIKComponent(float duration = 1)
    {
        if (duration == 0)
        {
            leftLegConstraint.weight = 1;
        }
        else
        {
            StartCoroutine(FadeINConstraintWeight(leftLegConstraint, duration));
        }
    }

    private IEnumerator FadeINConstraintWeight(TwoBoneIKConstraint twoBoneIKConstraint, float duration)
    {
        float elapsedTime = 0f;
        float startValue = 0;
        float endValue = 1;

        yield return new WaitForSeconds(1);
        while (elapsedTime < duration)
        {
            // Calculate the interpolation factor (0 to 1)
            float t = elapsedTime / duration;

            // Perform linear interpolation (lerp) between startValue and endValue
            float lerpedValue = Mathf.Lerp(startValue, endValue, t);

            // Apply lerp value to constraint
            twoBoneIKConstraint.weight = lerpedValue;

            // Increment the elapsed time
            elapsedTime += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }
        twoBoneIKConstraint.weight = endValue;
    }

    public void DeactivateIKComponent(TwoBoneIKConstraint twoBoneIKConstraint, float duration = 1)
    {
        StartCoroutine(FadeOutConstraintWeight(twoBoneIKConstraint, duration));
    }
    public void DeactivateRightArmIKComponent(float duration = 1)
    {
        StartCoroutine(FadeOutConstraintWeight(rightArmConstraint, duration));
    }
    public void DeactivateLeftArmIKComponent(float duration = 1)
    {
        StartCoroutine(FadeOutConstraintWeight(leftArmConstraint, duration));
    }
    public void DeactivateRightLegIKComponent(float duration = 1)
    {
        StartCoroutine(FadeOutConstraintWeight(rightLegConstraint, duration));
    }
    public void DeactivateLeftLegIKComponent(float duration = 1)
    {
        StartCoroutine(FadeOutConstraintWeight(leftLegConstraint, duration));
    }

    private IEnumerator FadeOutConstraintWeight(TwoBoneIKConstraint twoBoneIKConstraint, float duration)
    {
        float elapsedTime = 0f;
        //float duration = 1;
        float startValue = 1;
        float endValue = 0;

        yield return new WaitForSeconds(1);
        while (elapsedTime < duration)
        {
            // Calculate the interpolation factor (1 to 0)
            float t = elapsedTime / duration;

            // Perform linear interpolation (lerp) between startValue and endValue
            float lerpedValue = Mathf.Lerp(startValue, endValue, t);

            // Apply lerp value to constraint
            twoBoneIKConstraint.weight = lerpedValue;

            // Increment the elapsed time
            elapsedTime += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }
        twoBoneIKConstraint.weight = endValue;
    }

    public void ActivateHipConstraint(float duration = 1)
    {
        StartCoroutine(FadeInParentConstraint(hipConstraint, duration));
    }

    private IEnumerator FadeInParentConstraint(MultiParentConstraint parentConstraint, float duration)
    {
        float elapsedTime = 0f;
        //float duration = 1;
        float startValue = 0;
        float endValue = 1;

        while (elapsedTime < duration)
        {
            // Calculate the interpolation factor (1 to 0)
            float t = elapsedTime / duration;
            // Perform linear interpolation (lerp) between startValue and endValue
            float lerpedValue = Mathf.Lerp(startValue, endValue, t);
            // Apply lerp value to constraint
            parentConstraint.weight = lerpedValue;
            // Increment the elapsed time
            elapsedTime += Time.deltaTime;
            // Wait for the next frame
            yield return null;
        }
    }

    public void DeactivateHipConstraint(float duration = 1)
    {
        StartCoroutine(FadeOutParentConstraint(hipConstraint, duration));
    }

    private IEnumerator FadeOutParentConstraint(MultiParentConstraint parentConstraint, float duration)
    {
        float elapsedTime = 0f;
        //float duration = 1;
        float startValue = 1;
        float endValue = 0;

        while (elapsedTime < duration)
        {
            // Calculate the interpolation factor (1 to 0)
            float t = elapsedTime / duration;
            // Perform linear interpolation (lerp) between startValue and endValue
            float lerpedValue = Mathf.Lerp(startValue, endValue, t);
            // Apply lerp value to constraint
            parentConstraint.weight = lerpedValue;
            // Increment the elapsed time
            elapsedTime += Time.deltaTime;
            // Wait for the next frame
            yield return null;
        }
    }

    public void MatchFeetIKTargetToFeetPosition()
    {
        leftLegTarget.transform.SetPositionAndRotation(leftFootJoint.transform.position, leftFootJoint.transform.rotation);
        rightLegTarget.transform.SetPositionAndRotation(rightFootJoint.transform.position, rightFootJoint.transform.rotation);
    }

    private void ForceHandRotationToIK()
    {
        if (rightShoulderContraint == null || leftShoulderContraint == null)
        {
            return;
        }

        // This function completely overrides animation rotation on hands as the IK should
        // For some reason IK wasn't doing it at 100%.
        if (rightArmConstraint.weight > 0)
        {
            rightHandJoint.transform.rotation = Quaternion.Lerp(rightHandJoint.transform.rotation, rightArmTarget.transform.rotation, rightArmConstraint.weight);
            rightHandJoint.transform.GetChild(0).transform.rotation = Quaternion.Lerp(rightHandJoint.transform.GetChild(0).transform.rotation, rightArmTarget.transform.rotation, rightArmConstraint.weight);
        }
        if (leftArmConstraint.weight > 0)
        {
            leftHandJoint.transform.rotation = Quaternion.Lerp(leftHandJoint.transform.rotation, leftArmTarget.transform.rotation, leftArmConstraint.weight);
            leftHandJoint.transform.GetChild(0).transform.rotation = Quaternion.Lerp(leftHandJoint.transform.GetChild(0).transform.rotation, leftArmTarget.transform.rotation, leftArmConstraint.weight);
        }
    }
    private void ForceFeetRotationToIK()
    {
        if(rightLegConstraint == null || leftLegConstraint == null)
        {
            return; //  Genie is throwing errors
        }

        // This function completely overrides animation rotation on feet as the IK should
        // For some reason IK wasn't doing it at 100%.
        if (rightLegConstraint.weight > 0)
        {
            rightFootJoint.transform.rotation = Quaternion.Lerp(rightFootJoint.transform.rotation, rightLegTarget.transform.rotation, rightLegConstraint.weight);
            //rightLegJoint.transform.GetChild(0).transform.rotation = Quaternion.Lerp(rightLegJoint.transform.GetChild(0).transform.rotation, rightLegTarget.transform.rotation, rightLegConstraint.weight);
        }
        if (leftLegConstraint.weight > 0)
        {
            leftFootJoint.transform.rotation = Quaternion.Lerp(leftFootJoint.transform.rotation, leftLegTarget.transform.rotation, leftLegConstraint.weight);
        }
    }

    private void ShoulderIKMobility()
    {
        if (rightShoulderContraint == null || leftShoulderContraint == null)
        {
            return;
        }

        if (!engageShouldersWhenReaching)
        {
            leftShoulderContraint.GetComponent<MultiRotationConstraint>().weight = 0;
            rightShoulderContraint.GetComponent<MultiRotationConstraint>().weight = 0;
            return;
        }

        // Shoulder should move as well when IK is ON.
        // If hand goes above shoulder or hand is stretched. Shoulder should follow.
        MultiRotationConstraint rightRotConstraint = rightShoulderContraint.GetComponent<MultiRotationConstraint>();
        rightRotConstraint.weight = 0;
        Quaternion rightShoulderRotation = Quaternion.identity;

        MultiRotationConstraint leftRotConstraint = leftShoulderContraint.GetComponent<MultiRotationConstraint>();
        leftRotConstraint.weight = 0;
        Quaternion leftShoulderRotation = Quaternion.identity;

        // Right Side
        if (rightArmConstraint.weight > 0)
        {
            float baseLength = Vector3.Distance(rightShoulderJoint.transform.position, rightArmJoint.transform.position);
            float height = (rightArmTarget.transform.position.y - rightShoulderJoint.transform.position.y) - 0.1f;
            if (height > 0)
            {
                rightShoulderContraint.GetComponent<MultiRotationConstraint>().weight = rightArmConstraint.weight;
                // Calculate the angle in radians
                float angleRadians = Mathf.Atan2(height, baseLength);

                // Convert to degrees
                float angleDegrees = angleRadians * Mathf.Rad2Deg;
                angleDegrees = Mathf.Clamp(angleDegrees, 0, 40);

                rightShoulderRotation = rightShoulderRotation * Quaternion.Euler(0, 0, angleDegrees);
                rightShoulderTarget.transform.localRotation = Quaternion.Lerp(rightShoulderJoint.transform.rotation, rightShoulderRotation, rightArmConstraint.weight);
            }

            float armslenght = Vector3.Distance(rightArmJoint.transform.position, rightForearmJoint.transform.position) + Vector3.Distance(rightForearmJoint.transform.position, rightHandJoint.transform.position);
            float handTargetFromShoulder = Vector3.Distance(rightArmJoint.transform.position, rightArmTarget.transform.position);
            if (handTargetFromShoulder > armslenght)
            {
                rightShoulderContraint.GetComponent<MultiRotationConstraint>().weight = rightArmConstraint.weight;

                float delta = handTargetFromShoulder - armslenght;
                // Calculate the angle in radians
                float angleRadians = Mathf.Atan2(delta, baseLength);

                // Convert to degrees
                float angleDegrees = angleRadians * Mathf.Rad2Deg;
                angleDegrees = Mathf.Clamp(angleDegrees, 0, 40);

                rightShoulderRotation = rightShoulderRotation * Quaternion.Euler(0, -angleDegrees, 0);
                rightShoulderTarget.transform.localRotation = Quaternion.Lerp(rightShoulderJoint.transform.rotation, rightShoulderRotation, rightArmConstraint.weight);
            }

            rightShoulderTarget.transform.localRotation = Quaternion.Lerp(Quaternion.identity, rightShoulderRotation, rightRotConstraint.weight);
        }

        // Left Side
        if (leftArmConstraint.weight > 0)
        {
            float baseLength = Vector3.Distance(leftShoulderJoint.transform.position, leftArmJoint.transform.position);
            float height = (leftArmTarget.transform.position.y - leftShoulderJoint.transform.position.y) - 0.1f;
            if (height > 0)
            {
                leftShoulderContraint.GetComponent<MultiRotationConstraint>().weight = leftArmConstraint.weight;
                // Calculate the angle in radians
                float angleRadians = Mathf.Atan2(height, baseLength);

                // Convert to degrees
                float angleDegrees = angleRadians * Mathf.Rad2Deg;
                angleDegrees = Mathf.Clamp(angleDegrees, 0, 40);

                leftShoulderRotation = leftShoulderRotation * Quaternion.Euler(0, 0, -angleDegrees);
                leftShoulderTarget.transform.localRotation = Quaternion.Lerp(leftShoulderJoint.transform.rotation, leftShoulderRotation, leftArmConstraint.weight);
            }

            float armslenght = Vector3.Distance(leftArmJoint.transform.position, leftForearmJoint.transform.position) + Vector3.Distance(leftForearmJoint.transform.position, leftHandJoint.transform.position);
            float handTargetFromShoulder = Vector3.Distance(leftArmJoint.transform.position, leftArmTarget.transform.position);
            if (handTargetFromShoulder > armslenght)
            {
                leftShoulderContraint.GetComponent<MultiRotationConstraint>().weight = leftArmConstraint.weight;

                float delta = handTargetFromShoulder - armslenght;
                // Calculate the angle in radians
                float angleRadians = Mathf.Atan2(delta, baseLength);

                // Convert to degrees
                float angleDegrees = angleRadians * Mathf.Rad2Deg;
                angleDegrees = Mathf.Clamp(angleDegrees, 0, 40);

                leftShoulderRotation = leftShoulderRotation * Quaternion.Euler(0, angleDegrees, 0);
                leftShoulderTarget.transform.localRotation = Quaternion.Lerp(leftShoulderJoint.transform.rotation, leftShoulderRotation, leftArmConstraint.weight);
            }

            leftShoulderTarget.transform.localRotation = Quaternion.Lerp(Quaternion.identity, leftShoulderRotation, leftRotConstraint.weight);
        }
    }

    void OnDestroy()
    {
        if (rightArmTarget != null)
        {
            Destroy(rightArmTarget);
        }
        if (leftArmTarget != null)
        {
            Destroy(leftArmTarget);
        }
        if (hipTarget != null)
        {
            Destroy(hipTarget);
        }
    }
}
