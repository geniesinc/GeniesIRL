using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AdmireWindow", story: "[Genie] admires [window] at [windowPoint]", category: "Action/GeniesIRL", id: "66a92d4225b645010e80f510d69b653f")]
public partial class AdmireWindowAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<Window> Window;

    [Tooltip("The point on the glass that the Genie will touch with their hand.")]
    [SerializeReference] public BlackboardVariable<Vector3> WindowPoint;
    [SerializeReference] public BlackboardVariable<float> IdleDuration = new BlackboardVariable<float>(5.767f);
    [SerializeReference] public BlackboardVariable<float> HandHeightRelativeToGenie = new BlackboardVariable<float>(1f);

    private Genie _genie => Genie.Value;
    private Window _window => Window.Value;

    private string _startTriggerName = "AdmireWindow-Start";
    private string _endTriggerName = "AdmireWindow-End";
    private string _introStateName = "AdmireWindow-Intro";
    private string _idleStateName = "AdmireWindow-Idle";
    private string _outroStateName = "AdmireWindow-Outro";

    private bool _touchPointNeedsReset = true;
    private Vector3 _touchPoint;

    private GenieAnimation _genieAnimation;

    private Coroutine _coroutine;

    private bool _engageIKFlag = false;

    protected override Status OnStart()
    {
        _genieAnimation = _genie.genieAnimation;
        _touchPointNeedsReset = true;
        _coroutine = _genie.StartCoroutine(AdmireWindow_C());;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_coroutine == null)
        {
            return Status.Success;
        }

        UpdateIK();

        return Status.Running;
    }

    private void UpdateIK()
    {
        if (!_engageIKFlag) return;

        // Determine hand orientation.
        Vector3 genieForward = _genie.transform.forward;
        Quaternion rotation = Quaternion.LookRotation(genieForward, Vector3.up);
        //rotation *= Quaternion.Euler(180,0,90); // Alter the rotation to work with the left hand joint.

        // Calculate the touch point on the glass.
        if (_touchPointNeedsReset)
        {
            Vector3 shoulderPosition = _genieAnimation.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            float distToGlass = VectorUtils.GetDistanceXZ(shoulderPosition, WindowPoint.Value);
            _touchPoint = shoulderPosition + genieForward * distToGlass;
            _touchPoint.y = _genie.transform.position.y + HandHeightRelativeToGenie.Value;
            _touchPointNeedsReset = false;
        }

        // Reach hand to position.
        _genieAnimation.GeniesIKComponent.ReachTowardsPosition(_touchPoint, GenieHand.Left, rotation);  
    }

    protected override void OnEnd()
    {
        if (_coroutine != null)
        {
            _genie.StopCoroutine(_coroutine);
        }

        _coroutine = null;

        _engageIKFlag = false;
    }

    private IEnumerator AdmireWindow_C()
    {
        // Ready the IK system.
        _engageIKFlag = true;
        UpdateIK(); 

        // Kick things off with the start trigger. This will trigger the "intro" animator state.
       _genieAnimation.Animator.SetTrigger(_startTriggerName);

        // Wait 'till we reach the intro state.
        yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(_introStateName));

        // Wait 'til we reach idle, which is automatically played after the intro animator state.
        yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(_idleStateName));

        yield return new WaitForSeconds(IdleDuration.Value);

        // Trigger the outro state.
        _genieAnimation.Animator.SetTrigger(_endTriggerName);

        yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(_outroStateName));

        yield return new WaitUntil(() => _genieAnimation.Animator.IsStateClipComplete(_outroStateName));
            
        _engageIKFlag = false;

        _coroutine = null;
    }
}

