using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ReachAndPlaceItem", story: "[Genie] reaches out hand and places [Item] at [position] on [ARPlane]", category: "Action/GeniesIRL", id: "548209425e72a128d8bba77b22c35e84")]
    public partial class ReachAndPlaceItemAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<Item> Item;
        [SerializeReference] public BlackboardVariable<Vector3> Position;

        private Genie _genie => Genie.Value;
        private Item _item => Item.Value;
        private Vector3 _position => Position.Value;

        private bool _finishedPlacingItem = false;

        protected override Status OnStart()
        {
            Debug.Log("ReachAndPlaceItemAction: OnStart");
            _finishedPlacingItem = false;
            PlaceItem();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return _finishedPlacingItem ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();

            if (!CurrentStatus.IsCompleted())
            {
                Debug.Log("ReachAndPlaceItemAction: Interrupted");
                _genie.genieGrabber.ExternallyCancelGrabOrRelease();
            }
        }

        private void PlaceItem()
        {
            _genie.genieGrabber.ReleaseObject(GenieHand.Right, _position, () => 
            {
                Debug.Log("Item successfully placed!");
                _finishedPlacingItem = true;
            });
        }
    }
}


