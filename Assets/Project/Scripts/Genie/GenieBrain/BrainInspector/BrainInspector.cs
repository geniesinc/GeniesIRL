using UnityEngine;
using System;

namespace GeniesIRL 
{
    /// <summary>
    /// Used by other classes to peer into the "mind" of the Genie at runtime.
    /// </summary>
    public class BrainInspector
    {
        /// <summary>
        ///  A 'Task' is a generic term that could be a Goal, and Action, etc. Basically it's something the Genie can do, that we would like to track.
        /// </summary>
        public class BrainTaskStatus
        {
            public event Action<BrainTaskStatus> onTaskAccomplished;
            public string taskName;
            public string taskDescr;
            public bool IsAccomplished 
            {
                get => _isAccomplished;
                set {
                    if (value != _isAccomplished)
                    {
                        _isAccomplished = value;

                        if (_isAccomplished)
                        {
                            onTaskAccomplished?.Invoke(this);
                        }
                    }
                }
            }

            public bool IsInProgress {get; set;} = false;

            private bool _isAccomplished = false;
        }

        public BrainTaskStatus WaveAtUser {get;} = new BrainTaskStatus() {
            taskName = "Wave At Me",
            taskDescr = "Just a nice, friendly wave!"};
        public BrainTaskStatus GiveHighFive {get;} = new BrainTaskStatus() {
            taskName = "Give High Five",
            taskDescr = "The Genie will put her hand in the air for a high five. Don't leave her hanging! (Or maybe do, I'm not the boss of you.)"};
        public BrainTaskStatus RecieveHighFive {get;} = new BrainTaskStatus() {
            taskName = "Recieve High Five",
            taskDescr = "Hold up your hand for a high-five, and she'll come over and slap it!"};
        public BrainTaskStatus OfferItem {get;} = new BrainTaskStatus() {
            taskName = "Give Item",
            taskDescr = "The Genie can offer you a sheep plushie! Use Grab and Pinch to accept it. Or you can ignore and watch her get mad!"};
        public BrainTaskStatus RecieveItem {get;} = new BrainTaskStatus() {
            taskName = "Recieve Item",
            taskDescr = "Grab a plushie and extend your arm for a few seconds in the Genie's direction, and she'll come and take it from you. Make sure you really reach!"};
        public BrainTaskStatus ZoneOut {get;} = new BrainTaskStatus() {
            taskName = "Zone Out",
            taskDescr = "Sometimes the Genie will take a 'lil break."};
        public BrainTaskStatus PutItemOnTable {get;} = new BrainTaskStatus() {
            taskName = "Put Item On Table",
            taskDescr = "Scan a table or desk, and watch the Genie place objects on it!"};
        public BrainTaskStatus ReactToProjectile {get;} = new BrainTaskStatus() {
            taskName = "React To Projectile",
            taskDescr = "If you throw a sheep plushie at the Genie, she'll react! She'll also react differently depending on whether it was in front or behind."};
        public BrainTaskStatus DrawOnWall {get;} = new BrainTaskStatus() {
            taskName = "Draw On Wall",
            taskDescr = "The Genie will draw pictures on walls that you scan! Make sure the wall is clear, and nothing's on the floor in front of it."};
        public BrainTaskStatus SitOnSeat {get;} = new BrainTaskStatus() {
            taskName = "Sit On Seat",
            taskDescr = "The Genie can sit down on scanned chairs, sofas, and stools."};
        public BrainTaskStatus AdmireWindow {get;} = new BrainTaskStatus() {
            taskName = "Admire Window",
            taskDescr = "Scan a window, and the Genie will walk up to it and look outside! Make sure the floor in front is unobstructed."};
        public BrainTaskStatus ThrowPencilAtCeiling {get;} = new BrainTaskStatus() {
            taskName = "Throw Pencil At Ceiling",
            taskDescr = "The Genie will throw pencils at ceilings you scan! Just remember to look up, and make sure the ceiling is low enough."};
        public BrainTaskStatus MaintainPersonalSpace {get;} = new BrainTaskStatus() {
            taskName = "Maintain Personal Space",
            taskDescr = "If you step too close to the Genie, she'll back up until she's at a comfortable distance."};

        public bool IsInitialized {get; private set;} = false;

        public BrainTaskStatus[] BrainTaskStatuses {get; private set; }

        public void Initialize(GenieBrain genieBrain)
        {
            BrainTaskStatuses = new BrainTaskStatus[] {
                WaveAtUser,
                GiveHighFive,
                RecieveHighFive,
                OfferItem,
                RecieveItem,
                ZoneOut,
                PutItemOnTable,
                ReactToProjectile,
                DrawOnWall,
                SitOnSeat,
                AdmireWindow,
                ThrowPencilAtCeiling,
                MaintainPersonalSpace
            };

            genieBrain.planDecider.OnPlanChanged += OnCurrentGoalChanged;
            genieBrain.planDecider.OnFinishedPlanEventChannel.Event += OnPlanFinished;

            IsInitialized = true;
        }

        private void OnCurrentGoalChanged(GeniePlan plan)
        {
            Debug.Log("Current plan changed to: " + plan);
            // Most tasks can simply be updated with a related plan. One exception is WaveAtUser, which is updated in the WaveAtUserAction.
            RecieveHighFive.IsInProgress = plan == GeniePlan.SolicitAndPerformHighFive;
            GiveHighFive.IsInProgress = plan == GeniePlan.RespondToUserHighFiveSolicitation;
            OfferItem.IsInProgress = plan == GeniePlan.SpawnItemAndOfferToUser || plan == GeniePlan.FetchAndOfferItemToUser;
            RecieveItem.IsInProgress = plan == GeniePlan.AcceptItemBeingOffered;
            ZoneOut.IsInProgress = plan == GeniePlan.IdleZoneOut;
            PutItemOnTable.IsInProgress = plan == GeniePlan.PlaceItemOnSurface;
            ReactToProjectile.IsInProgress = plan == GeniePlan.ReactToProjectileImpact;
            DrawOnWall.IsInProgress = plan == GeniePlan.DrawOnWall;
            AdmireWindow.IsInProgress = plan == GeniePlan.AdmireWindow;
            ThrowPencilAtCeiling.IsInProgress = plan == GeniePlan.ThrowPencilAtCeiling;
            MaintainPersonalSpace.IsInProgress = plan == GeniePlan.MaintainPersonalSpace;
            SitOnSeat.IsInProgress = plan == GeniePlan.TakeASeat;
            // WaveAtUser is handled in the WaveAtUserAction.
        }

        private void OnPlanFinished(GeniePlan plan)
        {
            // Most tasks can simply be updated with a successful goal. One exception is WaveAtUser, which is accomplished in the WaveAtUserAction.

            switch (plan) 
            {
                case GeniePlan.SolicitAndPerformHighFive:
                    RecieveHighFive.IsAccomplished = true;
                    break;
                case GeniePlan.RespondToUserHighFiveSolicitation:
                    GiveHighFive.IsAccomplished = true;
                    break;
                case GeniePlan.SpawnItemAndOfferToUser:
                    OfferItem.IsAccomplished = true;
                    break;
                case GeniePlan.FetchAndOfferItemToUser:
                    OfferItem.IsAccomplished = true;
                    break;
                case GeniePlan.AcceptItemBeingOffered:
                    RecieveItem.IsAccomplished = true;
                    break;
                case GeniePlan.IdleZoneOut:
                    ZoneOut.IsAccomplished = true;
                    break;
                case GeniePlan.PlaceItemOnSurface:
                    PutItemOnTable.IsAccomplished = true;
                    break;
                case GeniePlan.ReactToProjectileImpact:
                    ReactToProjectile.IsAccomplished = true;
                    break;
                case GeniePlan.DrawOnWall:
                    DrawOnWall.IsAccomplished = true;
                    break;
                case GeniePlan.AdmireWindow:
                    AdmireWindow.IsAccomplished = true;
                    break;
                case GeniePlan.ThrowPencilAtCeiling:
                    ThrowPencilAtCeiling.IsAccomplished = true;
                    break;
                case GeniePlan.MaintainPersonalSpace:
                    MaintainPersonalSpace.IsAccomplished = true;
                    break;
                case GeniePlan.TakeASeat:
                    SitOnSeat.IsAccomplished = true;
                    break;
            }
        } 
    }
}

