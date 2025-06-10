using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace GeniesIRL
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "DEBUGTestGrab", story: "DEBUG: [Genie] reaching and grabbing is controlled by developer", category: "Action/GeniesIRL/Debug", id: "d844d57a83176634a7f0ce6bc1c244d1")]
    public partial class DebugTestGrabAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;

        [SerializeReference] public BlackboardVariable<string> TestReachTargetName = new BlackboardVariable<string>("TestReachTarget");

        private Genie _genie => Genie.Value;

        private GameObject _testReachTarget => GameObject.Find(TestReachTargetName.Value);

        protected override Status OnStart()
        {
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                TryReachTarget();
            }
            
            return Status.Running; // This is a debug action that just keeps running forever.
        }

        private void TryReachTarget()
        {
            if (_testReachTarget == null) 
            {
                Debug.LogWarning("There must be a game object named " + TestReachTargetName.Value + " in the scene for this action to work.");
                return;
            }
            
            _genie.genieGrabber.DebugReachTarget(_testReachTarget.transform);
        }

        protected override void OnEnd()
        {
        }
    }
}

