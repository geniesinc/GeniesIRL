using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PickUpItem", story: "[Genie] picks up [Item]", category: "Action/GeniesIRL", id: "52e939c24dd3195980d334aa87f4fcd7")]
    public partial class PickUpItemAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<Item> Item;
        [SerializeReference] public BlackboardVariable<bool> IsAcceptingItemFromUserOffering = new BlackboardVariable<bool>(false);
        
        private Genie _genie => Genie.Value;
        private Item _item => Item.Value;
        private bool _isItemPickedUp = false;
        private float _lastReleasedByUserTime = -1f;
        private GeniesIRL.Item.ItemState _lastItemState;
        
        protected override Status OnStart()
        {
            _isItemPickedUp = false;
            _lastReleasedByUserTime = -1f;
            _lastItemState = _item.state;
            
            if (IsAcceptingItemFromUserOffering)
            {
                _genie.genieSense.personalSpace.SetRadiusOverride(PersonalSpace.kRadiusDuringIntentionalPhysicalContact); // Tighten personal space to allow grabbing from user.
            }

            _genie.genieGrabber.GrabObject(GenieHand.Right, Item, true, () =>
            {
                _isItemPickedUp = true;
            });

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            // Track state transitions to detect when the user releases the item
            if (_lastItemState == GeniesIRL.Item.ItemState.HeldByUser && _item.state != GeniesIRL.Item.ItemState.HeldByUser)
            {
                _lastReleasedByUserTime = Time.time;
            }
            _lastItemState = _item.state;

            if (IsAcceptingItemFromUserOffering.Value)
            {
                 bool heldByUserOrGenie = _item.state == GeniesIRL.Item.ItemState.HeldByUser || _item.state == GeniesIRL.Item.ItemState.HeldByGenie;

                 // Allow a grace period after user releases the item early
                bool withinGracePeriod = _lastReleasedByUserTime > 0 && (Time.time - _lastReleasedByUserTime) <= 0.5f;

                if (!heldByUserOrGenie && !withinGracePeriod)
                {
                    // The user has dropped the item they were offering before the Genie could take it. Fail the action.
                    Debug.Log("User unexpectedly dropped item they were offering. PickUpItemAction failed. " + _item.state);
                    return Status.Failure;
                }
            }
            else
            {
                if (_item.state == GeniesIRL.Item.ItemState.HeldByUser)
                {
                    // The user is unexpectedly holding the item now. Fail the action.
                    Debug.Log("The user unexpectedly grabbed the item I wanted to grab. PickUpItemAction failed.");
                    return Status.Failure;
                }
            }

            return _isItemPickedUp ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
             if (CurrentStatus != Status.Success)
            {
                Debug.Log("PickUpItemAction: Interrupted");
                _genie.genieGrabber.ExternallyCancelGrabOrRelease();
            }

            _genie.genieSense.personalSpace.ResetRadius();
            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
        }
    }
}