using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "LookAtAndTrackTarget", story: "[Genie] continuously looks at and tracks [Target]", category: "Action/GeniesIRL", id: "175dd710245baa81235bb1b0ca950c79")]
    public partial class LookAtAndTrackTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        [Tooltip("If true, the Genie will continue to look at the target until the Action is externally ended.")]
        [SerializeReference] public BlackboardVariable<bool> Indefinitely = new BlackboardVariable<bool>(false);

        [Tooltip("If not indefinite, this will be the minimum duration the Genie will track the target.")]
        [SerializeReference] public BlackboardVariable<float> MinDuration = new BlackboardVariable<float>(2);
        [Tooltip("If not indefinite, this will be the maximum duration the Genie will track the target.")]
        [SerializeReference] public BlackboardVariable<float> MaxDuration = new BlackboardVariable<float>(5);
        [SerializeReference] public BlackboardVariable<float> YawSpeed = new BlackboardVariable<float>(4f);

        private Genie _genie => Genie.Value;
        private GameObject _target => Target.Value;
        private Coroutine _coroutine;

        private Coroutine _nestedCoroutine;

        private float endTime = -1;

        protected override Status OnStart()
        {
            Debug.Log("LookAtAndTrackTargetAction.OnStart");
            // NOTE: The logic here is coroutine-based to get the legacy code working. In the future, we'll probably want to
            // refactor it with a simple call from OnUpdate().

            if (_coroutine != null)
            {
                _genie.StopCoroutine(_coroutine);
            }

            _genie.genieAnimation.Animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun); // Reset the Genie's animation state and allow her legs to do the "yawing" animation.
            _coroutine = _genie.StartCoroutine(ContinouslyLookAtTargetCoroutine_C());
            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(_target.transform);

            // Set the end time if we're not indefinitely looking at the target.
            if (!Indefinitely.Value)
            {
                endTime = Time.time + UnityEngine.Random.Range(MinDuration.Value, MaxDuration.Value);
            }

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (!Indefinitely.Value && Time.time > endTime)
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

            _genie.genieAnimation.SetYawing(0);
        }

        private IEnumerator ContinouslyLookAtTargetCoroutine_C() 
        {
            while (true)
            {
                _nestedCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(_target.transform, 30f, YawSpeed.Value));
                yield return _nestedCoroutine;
            }
        }
    }
}


