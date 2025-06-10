using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace GeniesIRL 
{
    /// <summary>
    /// A simple class that listens for events from ARPlaneManager and fires global events.
    /// </summary>
    public class ARPlaneEventDispatcher : MonoBehaviour 
    {
        private ARPlaneManager _arPlaneManager;
        private void Awake() 
        {
            _arPlaneManager = GetComponent<ARPlaneManager>();
        }
        private void OnEnable() 
        {
            ARPlaneManager planeManager = GetComponent<ARPlaneManager>();

            if (planeManager != null)
            {
                planeManager.trackablesChanged.AddListener(OnPlanesChanged);
            }
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> arg)
        {
            bool newWindowFound = false;
            bool newCeilingFound = false;
            bool newWallFound = false;
            bool newTableFound = false;

            foreach (var plane in arg.added)
            {
                newWindowFound = plane.classifications == PlaneClassifications.WindowFrame;
                newCeilingFound = plane.classifications == PlaneClassifications.Ceiling;
                newWallFound = plane.classifications == PlaneClassifications.WallFace;
                newTableFound = plane.classifications == PlaneClassifications.Table;
            }

            if (newWindowFound) 
            {
                GlobalEventManager.Trigger(new GlobalEvents.NewWindowAppeared());
            }

            if (newCeilingFound) 
            {
                GlobalEventManager.Trigger(new GlobalEvents.NewCeilingAppeared());
            }

            if (newWallFound) 
            {
                GlobalEventManager.Trigger(new GlobalEvents.NewWallAppeared());
            }

            if (newTableFound) 
            {
                GlobalEventManager.Trigger(new GlobalEvents.NewTableAppeared());
            }
        }
    }
}