using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SolicitHighFive", story: "[Genie] solicits high five from [UserInput]", category: "Action/GeniesIRL", id: "be457cca649d6af26a290346e7c08dd3")]
public partial class SolicitHighFiveAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<XRInputWrapper> UserInput;
    [SerializeReference] public BlackboardVariable<float> WaitTimeBeforeGivingUp = new BlackboardVariable<float>(5f);

    private Genie _genie => Genie.Value;

    private XRInputWrapper _userInput => UserInput.Value;

    private float _waitTimeBeforeGivingUp => WaitTimeBeforeGivingUp.Value;

    private float _startTime;

    private bool _isHighFiveSuccessful;

    private Coroutine _coroutine;
    private Coroutine _nestedCoroutine;

    protected override Status OnStart()
    {
        _startTime = Time.time;
        _isHighFiveSuccessful = false;
        _coroutine = _genie.StartCoroutine(MainCoroutine_C());
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_isHighFiveSuccessful) 
        {
            return Status.Success;
        }

        if (_coroutine != null)
        {
            return Status.Running;
        }

        return Status.Failure;
    }

    protected override void OnEnd()
    {
        // Check if high five was externally cancelled
        if (!CurrentStatus.IsCompleted()) 
        {
            _genie.genieHighFiver.ExternallyCancelHighFive(); // It was externally cancelled, so we need to call a method to clean it up.
        }
        
        _genie.genieHighFiver.OnSuccess -= OnHighFiveSuccess;

         if (_coroutine != null)
        {
            _genie.StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private void OnHighFiveSuccess()
    {
        _isHighFiveSuccessful = true;

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

    private IEnumerator MainCoroutine_C()
    {
        _genie.genieHighFiver.OnSuccess += OnHighFiveSuccess;
        _genie.genieHighFiver.SolicitHighFive(_userInput);

        // Wait for either a successful high five, or the time limit to expire
        while (!_isHighFiveSuccessful && Time.time - _startTime < _waitTimeBeforeGivingUp)
        {
            yield return null;
        }

        // Waiting time is over, no high five from player :(
        if (!_isHighFiveSuccessful) 
        {
            _nestedCoroutine = _genie.StartCoroutine(_genie.genieHighFiver.FailHighFive_C());
            yield return _nestedCoroutine;
        }

        _coroutine = null;
        _nestedCoroutine = null;
    }
}

