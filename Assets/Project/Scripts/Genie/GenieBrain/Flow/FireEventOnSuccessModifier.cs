using System;
using Unity.Behavior;
using UnityEngine;
using Modifier = Unity.Behavior.Modifier;
using Unity.Properties;
using GeniesIRL;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FireEventOnSuccessOrFail", story: "Fires [SuccessEvent] on the success of [Plan] and [FailEvent] on its failure.", category: "Flow/GeniesIRL", id: "84cc666e0287514a047082a11a811581")]
public partial class FireEventOnSuccessOrFailModifier : Modifier
{
    [SerializeReference] public BlackboardVariable<OnFinishedPlan> SuccessEvent;
    [SerializeReference] public BlackboardVariable<OnFailedPlan> FailEvent;
    [SerializeReference] public BlackboardVariable<GeniePlan> Plan;

    protected override Status OnStart()
    {
        base.OnStart();
        
        Status status = StartNode(Child);
        
        if (status == Status.Success)
            return Status.Success;
        if (status == Status.Failure)
            return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
         // Check the child status 
        Status status = Child.CurrentStatus;

        if (status == Status.Success) 
        {
            //Debug.Log("Success Detected for Plan " + Plan.Value);
            SuccessEvent.Value.SendEventMessage(Plan.Value);
            return Status.Success;
        }
        else if (status == Status.Failure)
        {
            //Debug.Log("Failure Detected for Plan " + Plan.Value);
            FailEvent.Value.SendEventMessage(Plan.Value);
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }
}

