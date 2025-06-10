using Unity.Behavior;
using UnityEngine;

namespace GeniesIRL 
{
    [BlackboardEnum]
    public enum GeniePlan 
    {
        None,
        WaveAtUser, // <-- This is only ever triggered when the app refocuses. Otherwise it's 'LookAtAndTrackUser', which includes the wave action.
        LookAtAndTrackUser,
        PlaceItemOnSurface,
        IdleZoneOut,
        TakeASeat,
        SolicitAndPerformHighFive,
        DrawOnWall,
        AdmireWindow,
        SpawnItemAndOfferToUser,
        FetchAndOfferItemToUser,
        ReactToProjectileImpact,
        AcceptItemBeingOffered,
        MaintainPersonalSpace,
        ThrowPencilAtCeiling,
        RespondToUserHighFiveSolicitation,
        DEBUG_TestGrab, // <-- For debugging purposes only. Genie will grab a target object when the user presses a key.
    }
}