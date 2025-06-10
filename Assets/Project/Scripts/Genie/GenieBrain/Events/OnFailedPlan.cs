using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

namespace GeniesIRL 
{
    #if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/OnFailedPlan")]
    #endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "OnFailedPlan", message: "Fires when a GeniePlan fails", category: "Events", id: "6700800a68872c0d5e97efa1d7c0e6b0")]
    public partial class OnFailedPlan : EventChannelBase
    {
        public delegate void OnFailedPlanEventHandler(GeniePlan Plan);
        public event OnFailedPlanEventHandler Event; 

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
            OnFailedPlanEventHandler del = (Plan) =>
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
            Event += del as OnFailedPlanEventHandler;
        }

        public override void UnregisterListener(Delegate del)
        {
            Event -= del as OnFailedPlanEventHandler;
        }
    }
}


