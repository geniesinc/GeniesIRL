using System;
using System.Collections.Generic;
using GeneisIRL;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL 
{
    /// <summary>
    /// Looks for AR Planes marked as Seats, validates them, and processes them so the Genie knows how to sit on it.
    /// </summary>
    [Serializable]
    public class SeatProcessor
    {
        public Seat seatPrefab;

        public float seatHeightOffset = 0.05f;

        [System.NonSerialized]
        private ARPlaneManager _arPlaneManager;
        
        // This is a list of seats that have been assigned to planes in this script. It is not necessarily all of the
        // seat groups in the scene, as some seats may have been manually placed by a developer for testing.
        private List<Seat> _seatGroupsAssignedToPlanes = new List<Seat>();

        public List<Seat> FindSeats()
        {
            Seat[] seatsAsArray = GameObject.FindObjectsByType<Seat>(FindObjectsSortMode.None);

            List<Seat> seats = new List<Seat>(seatsAsArray);

            return seats;
        }

        public void OnSceneBootstrapped(ARPlaneManager aRPlaneManager)
        {
            _arPlaneManager = aRPlaneManager;
        }

        public void OnUpdate()
        {
            List<ARPlane> seatPlanes = FindAllSeatPlanes();

            // Check for any seat planes that have not been assigned to a seat.
            foreach (ARPlane seatPlane in seatPlanes)
            {
                if (_seatGroupsAssignedToPlanes.Find(s => s.ARPlane == seatPlane) == null)
                {
                    OnPlaneAdded(seatPlane);
                }
            }
        }

        private List<ARPlane> FindAllSeatPlanes()
        {
            List<ARPlane> seatPlanes = new List<ARPlane>();

            foreach (ARPlane plane in _arPlaneManager.trackables)
            {
                if (plane.IsSittable())
                {
                    seatPlanes.Add(plane);
                }
            }

            return seatPlanes;
        }

        private void OnPlaneAdded(ARPlane plane)
        {
            if (!plane.IsSittable()) return;

            Seat seat = GameObject.Instantiate(seatPrefab);

            seat.AssignToARPlane(plane, seatHeightOffset);
            _seatGroupsAssignedToPlanes.Add(seat);
            seat.OnDestroyed += OnSeatDestroyed;
        }

        private void OnSeatDestroyed(Seat seat)
        {
            _seatGroupsAssignedToPlanes.Remove(seat);
        }
    }
}

