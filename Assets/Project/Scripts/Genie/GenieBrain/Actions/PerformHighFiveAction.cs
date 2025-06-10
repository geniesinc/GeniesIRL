using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PerformHighFive", story: "[Genie] performs high five with [User]", category: "Action/GeniesIRL", id: "3f0423442eca95b463493d221e10ea47")]
public partial class PerformHighFiveAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<XRInputWrapper> UserInput;
    
    [Tooltip("Only enable this if the reason the Genie is performing a high five is because the user is soliciting it.")]
    [SerializeReference] public BlackboardVariable<bool> IsRespondingToUserSolicitation = new BlackboardVariable<bool>(false);

    private Genie _genie => Genie.Value;

    private Coroutine _coroutine;

    private Coroutine _nestedCoroutine;

    protected override Status OnStart()
    {
        if (IsRespondingToUserSolicitation.Value)
        {
            // The genie is responding to a high-five solicitation from the user, so the code path is slightly different.
            if (!_genie.genieSense.detectUserSolicitingHighFive.IsUserSolicitingHighFive)
            {
                // Fail if the user is no longer soliciting (too slow...).
                Debug.Log("User is no longer soliciting a high five. Cancelling high five.");
                return Status.Failure;
            }
            // Get a reference to the user's hand transform.
            Transform userHand = _genie.genieSense.detectUserSolicitingHighFive.UserHandTransform;

            // Manually set the user's hand target for the high five, so the Genie knows where to aim.
            _genie.genieHighFiver.SetUserHandTarget(userHand);
        }

       _coroutine = _genie.StartCoroutine(MainCoroutine_C());
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

    // This is important because we can perform cleanup in case the Action is interrupted externally.
    protected override void OnEnd()
    {
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

        if (!CurrentStatus.IsCompleted())
        {
            // This means we were cancelled externally, so we should completely cancel the high five.
            _genie.genieHighFiver.ExternallyCancelHighFive();
        }
    }

    private IEnumerator MainCoroutine_C() 
    {
        _nestedCoroutine = _genie.StartCoroutine(_genie.genieHighFiver.PerformHighFiveAndSuccessAnimations_C(!IsRespondingToUserSolicitation.Value, UserInput.Value));
        yield return _nestedCoroutine;

        _coroutine = null;
        _nestedCoroutine = null;
    }
}

