using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaveAtUser", story: "[Genie] waves at [Target]", category: "Action/GeniesIRL", id: "56dc6ff3d60bec99f3d69cd69f6f33fe")]
    public partial class WaveAtUserAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        private Coroutine _coroutine;

        private Coroutine _nestedCoroutine;

        private Genie _genie => Genie.Value;

        protected override Status OnStart()
        {
            _genie.genieBrain.brainInspector.WaveAtUser.IsInProgress = true;

            if (_coroutine != null)
            {
                _genie.StopCoroutine(_coroutine);
            }

            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(Target.Value.transform);
            _coroutine = _genie.StartCoroutine(WaveAtUserCoroutine_C());
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_coroutine != null)
            {
                return Status.Running;
            }

            return Status.Success;
        }

        protected override void OnEnd()
        {
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

            if (CurrentStatus.IsCompleted())
            {
                _genie.genieBrain.brainInspector.WaveAtUser.IsAccomplished = true;
            }

            _genie.genieBrain.brainInspector.WaveAtUser.IsInProgress = false;
        }

        private IEnumerator WaveAtUserCoroutine_C()
        {
            //Transform userHead = _genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper.Head;

            //yield return Genie.StartCoroutine(Genie.YawTowards_C(userHead, 0)); //<-- this is going to be happening in a separate action.
            // We may add Yawing as part of this Action as well, but for now, we're just going to wave and not track the user if they move.

            _nestedCoroutine = _genie.StartCoroutine(_genie.genieAnimation.Wave_C());
            yield return _nestedCoroutine;

            yield return new WaitForSeconds(0.5f);

            _coroutine = null;
            _nestedCoroutine = null;
        }
    }   
}

