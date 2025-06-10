using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "OfferItemToUser", story: "[Genie] offers item to [User]", category: "Action/GeniesIRL", id: "0bb5392d7f7f8061fed15be7d47c55a6")]
public partial class OfferItemToUserAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<XRInputWrapper> User;

    [Tooltip("The duration of the offer. If the user doesn't take the item within this time, the offer will be considered rejected.")]
    [SerializeReference] public BlackboardVariable<float> WaitTimeBeforeGivingUp = new BlackboardVariable<float>(5f);

    private Genie _genie => Genie.Value;

    private float _waitTimeBeforeGivingUp => WaitTimeBeforeGivingUp.Value;

    private bool _isOfferAccepted;

    private float _startTime;

    private Coroutine _coroutine;

    private Coroutine _nestedCoroutine; // We must keep track of the nested coroutines as well, so we can stop them if the main coroutine is interrupted.

    protected override Status OnStart()
    {
        Debug.Log("OfferItemToUserAction.OnStart()");
        _startTime = Time.time;
        _isOfferAccepted = false;
        _genie.genieSense.personalSpace.SetRadiusOverride(PersonalSpace.kRadiusDuringIntentionalPhysicalContact);
        _genie.genieOfferItem.OnOfferAccepted += OnOfferAccepted;
        _coroutine = _genie.StartCoroutine(MainCoroutine_C());
        return Status.Running;
    }

    private void OnOfferAccepted(Item item)
    {
        _genie.genieOfferItem.OnOfferAccepted -= OnOfferAccepted;
        
        _isOfferAccepted = true;

        if (_coroutine != null)
        {
            _genie.StopCoroutine(_coroutine);
        }

        _coroutine = _genie.StartCoroutine(Celebrate_C());
    }

    protected override Status OnUpdate()
    {
        if (_coroutine != null)
        {
            return Status.Running;
        }

         return _isOfferAccepted ? Status.Success : Status.Failure;
    }

    // OnEnd() is important because it gets called even when the Action is interrupted externally, which allows us to do extra cleanup if needed.
    protected override void OnEnd()
    {
        _genie.genieSense.personalSpace.ResetRadius();
        _genie.genieOfferItem.OnOfferAccepted -= OnOfferAccepted;

        if (_coroutine != null)
        {
            _genie.StopCoroutine(_coroutine);
        }

        if (_nestedCoroutine != null)
        {
            _genie.StopCoroutine(_nestedCoroutine);
        }

        if (!CurrentStatus.IsCompleted())
        {
            _genie.genieOfferItem.ExternallyCancelOffer(); // The offer was interrupted externally, so we need to clean up.
        }
    }

    private IEnumerator MainCoroutine_C()
    {
        _nestedCoroutine = _genie.StartCoroutine(_genie.genieOfferItem.OfferItem_C());
        yield return _nestedCoroutine;
        
        // Wait for either the offer to be accepted, or the time limit to expire
        while (!_isOfferAccepted && Time.time - _startTime < _waitTimeBeforeGivingUp)
        {
            yield return null;
        }

        Debug.Log("waiting time expired or offer accepted");

        // Waiting time is over, so just give up :(
        if (!_isOfferAccepted) 
        {
            _genie.genieSense.personalSpace.ResetRadius();
            _nestedCoroutine = _genie.StartCoroutine(_genie.genieOfferItem.OfferRejected_C());
            yield return _nestedCoroutine;
        }

        _coroutine = null;
        _nestedCoroutine = null;
    }
    
    private IEnumerator Celebrate_C()
    {
        _nestedCoroutine = _genie.StartCoroutine(_genie.genieOfferItem.Celebrate_C());
        yield return _nestedCoroutine;

        _coroutine = null;
        _nestedCoroutine = null;
    }
}

