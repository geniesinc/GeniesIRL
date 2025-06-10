using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReactToProjectileImpact", story: "[Genie] reacts to impact from projectile", category: "Action/GeniesIRL", 
id: "f35b6eae2c02f35acd71670b2530d437", description: "At the time of writing, the this just plays a simple animation, however, we could later expand the behavior to be more interesting.")]
public partial class ReactToProjectileImpactAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<string> ImpactFromFrontPart1AnimationTrigger = new BlackboardVariable<string>("ReactToProjectileImpact-FrontPart1");
    [SerializeReference] public BlackboardVariable<float> ImpactFromFrontPart1AnimationDuration = new BlackboardVariable<float>(1.5f);
    [SerializeReference] public BlackboardVariable<string> ImpactFromFrontPart2AnimationTrigger = new BlackboardVariable<string>("ReactToProjectileImpact-FrontPart2");
    [SerializeReference] public BlackboardVariable<float> ImpactFromFrontPart2AnimationDuration = new BlackboardVariable<float>(1.5f);
    [SerializeReference] public BlackboardVariable<string> ImpactFromBehindAnimationTrigger = new BlackboardVariable<string>("ReactToProjectileImpact-Behind");
    [SerializeReference] public BlackboardVariable<float> ImpactFromBehindAnimationDuration = new BlackboardVariable<float>(1.5f);

    private Genie _genie => Genie.Value;

    private float _endTime;
    private Coroutine _coroutine;

    private Coroutine _nestedCoroutine;

    private bool _isImpactFromFront;

    protected override Status OnStart()
    {
        _coroutine = null;
        _nestedCoroutine = null;

        _isImpactFromFront = _genie.genieBrain.genieBeliefs.IsLatestUserProjectileImpactFromFront;

        Debug.Log("ReactToProjectileImpactAction: Impact from " + (_isImpactFromFront ? "front" : "back"));

        if (!_isImpactFromFront) 
        {
            // The impact from the back, so things are pretty simple -- we just play an animation.
            _genie.genieAnimation.Animator.SetTrigger(ImpactFromBehindAnimationTrigger.Value);
            _endTime = Time.time + ImpactFromBehindAnimationDuration.Value;
        }
        else 
        {
            // The impact is from the front. First yaw towards the user, then play the animation.
            _coroutine = _genie.StartCoroutine(FrontImpactReaction_C());
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_isImpactFromFront) 
        {
             if (Time.time >= _endTime) // Simply wait for the "hit from back" animation to finish.
            {
                return Status.Success;
            }

            return Status.Running;
        }

        // For front-impact reactions, we must wait for the coroutine to finish.

        if (_coroutine == null)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_coroutine != null)
        {
            _genie.StopCoroutine(_coroutine);
        }

        if (_nestedCoroutine != null)
        {
            _genie.StopCoroutine(_nestedCoroutine);
        }

        _coroutine = null;
        _nestedCoroutine = null;

        _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
    }

    private IEnumerator FrontImpactReaction_C() 
    {
        Transform userHead = _genie.genieBrain.genieBeliefs.UserHead;

        // Aim eyballs at the user.
        _genie.genieLookAndYaw.eyeballAimer.TrackTarget(userHead);

        // Play the first part of the animation
        _genie.genieAnimation.Animator.SetTrigger(ImpactFromFrontPart1AnimationTrigger.Value);
        yield return new WaitForSeconds(ImpactFromFrontPart1AnimationDuration.Value);
        
        // Determine whether we need to yaw towards the user.
        Vector3 genieToUserDir = userHead.position - _genie.transform.position;
        genieToUserDir.y = 0f;
        genieToUserDir.Normalize();

        Vector3 genieForward = _genie.transform.forward;
        genieForward.y = 0f;
        genieForward.Normalize();

        float angleToYaw = Vector3.Angle(_genie.transform.forward, genieToUserDir);

        float minAngleToYaw = 30f; // If the angle is greater than this, interrupt the animation to yaw towards the user.

        // Only yaw if the angle is large enough.
        if (angleToYaw >= minAngleToYaw) 
        {
            // Orient towards the user.
            _genie.genieAnimation.Animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun); // Allow her legs to do the "yawing" animation.
            _nestedCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(userHead.position, 10f, 2.5f));
            yield return _nestedCoroutine;
        }
        
        // Play the second part of the animation
        _genie.genieAnimation.Animator.SetTrigger(ImpactFromFrontPart2AnimationTrigger.Value);
        yield return new WaitForSeconds(ImpactFromFrontPart2AnimationDuration.Value);

        _coroutine = null;
        _nestedCoroutine = null;
    }
}

