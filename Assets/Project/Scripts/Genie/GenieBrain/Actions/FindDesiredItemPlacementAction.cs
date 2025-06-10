using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;
using GeneisIRL;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindDesiredItemPlacement", story: "[Genie] searches for somewhere to place the [Item]", category: "Action/GeniesIRL", id: "9228c571e4f0e2da165cb47165726b3e")]
public partial class FindDesiredItemPlacementAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<Item> Item;

    private Genie _genie => Genie.Value;
    private Item _item => Item.Value;
    private ItemPlacementOnHorizontalSurfaces _itemPlacementOnHorizontalSurfaces;

    private GenieBrain _genieBrain => _genie.genieBrain;

    protected override Status OnStart()
    {
        // Get the bounds of the item when it is not rotated. This is what we'll use to test placement.
        Bounds itemBounds = _item.BoundsWhenNotRotated;

        // Get some placement options
        _itemPlacementOnHorizontalSurfaces = _genie.GenieManager.Bootstrapper.ARSurfaceUnderstanding.itemPlacementOnHorizontalSurfaces;
        List<Vector3> potentialPlacements = _itemPlacementOnHorizontalSurfaces.FindPotentialItemPlacements(itemBounds.size);

        while (potentialPlacements.Count > 0)
        {
            // Pick a random placement
            int idx = UnityEngine.Random.Range(0, potentialPlacements.Count);
            Vector3 placement = potentialPlacements[idx];

            // Is it too close to the user?
            if (VectorUtils.IsWithinDistanceXZ(placement, _genieBrain.genieBeliefs.UserHead.position, _genie.genieSense.personalSpace.Radius))
            {
                potentialPlacements.RemoveAt(idx); // Too close. Remove from list and try again.
                continue;
            }

            // Is it on the same floor/level as the Genie?
            if (!_genieBrain.genieBeliefs.IsPointOnSameLevelAsMe(placement))
            {
                potentialPlacements.RemoveAt(idx); // Not on the same level. Remove from list and try again.
                continue;
            }

            // Can the Genie reach it?
            //bool isReachable = _genieBrain.Genie.genieNavigation.IsPathReachable(placement, _genieBrain.Genie.genieGrabber.nearGrabReach);
            bool isReachable = NavArrivalEvaluation.EvaluateIfPossible(NavArrivalEvaluationType.ReachableWithArms, _genieBrain.Genie, placement);
            
            if (!isReachable)
            {
                Debug.Log("FindDesiredItemPlacementAction: Placement not reachable");
                potentialPlacements.RemoveAt(idx); // Can't reach. Remove from list and try again.
                continue;
            }

            // Evaluate the placement to see if it can hold the object.
            Bounds bounds = new Bounds(placement, itemBounds.size);
            bool canHold = _itemPlacementOnHorizontalSurfaces.EvaluateVolume(ref bounds);

            if (!canHold)
            {
                Debug.Log("FindDesiredItemPlacementAction: Placement found to be invalid");
                potentialPlacements.RemoveAt(idx); // Can't hold. Remove from list and try again.
                continue;
            }

            // Found a valid placement!
            Debug.Log("FindDesiredItemPlacementAction: Placement found to be valid.");
            _genieBrain.planDecider.DesiredItemPlacement = placement;  // Set the desired item placement. 
            _genieBrain.UpdateBlackboard(); // Force it to propagate to the behavior graph immediately.
            return Status.Success;
        }

        // No valid item placements found.
        return Status.Failure;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

