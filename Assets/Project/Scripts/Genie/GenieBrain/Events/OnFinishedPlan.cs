using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

namespace GeniesIRL 
{
    #if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/OnFinishedPlan")]
    #endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "OnFinishedPlan", message: "Completed [Plan]", category: "Action/GeniesIRL", id: "aeb6a30f0b3a2b5dca564f14fbd07762")]
    public partial class OnFinishedPlan : EventChannelBase
    {
        public delegate void OnFinishedPlanEventHandler(GeniePlan Plan);
        public event OnFinishedPlanEventHandler Event; 

        public void SendEventMessage(GeniePlan Plan)
        {
            Event?.Invoke(Plan);
        }

        public override void SendEventMessage(BlackboardVariable[] messageData)
        {
            BlackboardVariable<GeniePlan> AgentBlackboardVariable = messageData[0] as BlackboardVariable<GeniePlan>;
            var Plan = AgentBlackboardVariable != null ? AgentBlackboardVariable.Value : default(GeniePlan);
            
            Event?.Invoke(Plan);
        }

        public override Delegate CreateEventHandler(BlackboardVariable[] vars, System.Action callback)
        {
            OnFinishedPlanEventHandler del = (Plan) =>
            {
                BlackboardVariable<GeniePlan> var0 = vars[0] as BlackboardVariable<GeniePlan>;
                if(var0 != null)
                    var0.Value = Plan;

                callback();
            };
            return del;
        }

        public override void RegisterListener(Delegate del)
        {
            Event += del as OnFinishedPlanEventHandler;
        }

        public override void UnregisterListener(Delegate del)
        {
            Event -= del as OnFinishedPlanEventHandler;
        }
    }
}


