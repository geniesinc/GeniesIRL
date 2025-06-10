using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "TurnToFaceTarget", 
    story: "[Genie] turns to face [Target] / [Location]", 
    category: "Action/GeniesIRL", 
    description: "Turns a Genie to face a Target transform or Location. If the Target transform is null, it will use the Location value.",
    id: "29c8f7e0e555b09eb125964e8dfd1e78")]
    public partial class TurnToFaceTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<Transform> Target;
        [SerializeReference] public BlackboardVariable<Vector3> Location;
        [SerializeReference] public BlackboardVariable<float> Speed = new BlackboardVariable<float>(2f);
        private Genie _genie => Genie.Value;
        private Transform _target => Target.Value;
        private Vector3 _location => Location.Value;
        private Coroutine _coroutine;
        private Coroutine _nestedCoroutine;
        private bool _useTarget => Target.Value != null;

        protected override Status OnStart()
        {
            Debug.Log("TurnToFaceTargetAction.OnStart");
            _genie.genieAnimation.Animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun); // Reset the Genie's animation state and allow her legs to do the "yawing" animation.

            if (_coroutine != null)
            {
                _genie.StopCoroutine(_coroutine);
            }

            if (_useTarget)
            {
                _genie.genieLookAndYaw.eyeballAimer.TrackTarget(_target);
            }
            else 
            {
                _genie.genieLookAndYaw.eyeballAimer.TrackLocation(_location);
            }
            
            _coroutine = _genie.StartCoroutine(LookAtAndTrackTarget_C());

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_coroutine == null)
            {
                return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            // This is important because it allows us to tie up any loose ends if the Action is externally interrupted.

            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();

            if (_coroutine != null)
            {
                _genie.StopCoroutine(_coroutine);
                _coroutine = null;
            }

            if (_nestedCoroutine != null)
            {
                _genie.StopCoroutine(_nestedCoroutine);
                _nestedCoroutine = null;
            }
        }

        private IEnumerator LookAtAndTrackTarget_C() 
        {
            if (_useTarget) 
            {
                _nestedCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(_target, 3f, Speed.Value));
            }
            else 
            {
                _nestedCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(_location, 3f, Speed.Value));
            }

            yield return _nestedCoroutine;
            
            _coroutine = null;
            _nestedCoroutine = null;
        }
    }

}