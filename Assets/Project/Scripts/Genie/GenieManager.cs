using System.Collections;
using System.Collections.Generic;
using GeniesIRL.GlobalEvents;
using Unity.PolySpatial;
using UnityEngine;

namespace GeniesIRL
{
    /// <summary>
    /// Facilitates the spawning of Genie characters, and keeps track of them after they spawn.
    /// </summary>
    public class GenieManager : GeniesIrlSubManager
    {
        /// <summary>
        /// Manager initializes in the TryingToSpawnGenie state. Then, once spawned, we're in the NormalPlay
        /// state until the user (or game) tries to Teleport the Genie to a new location.
        /// </summary>
        public enum GenieManagerState
        {
            AwaitingLaunchUxCompletion,
            TryingToSpawnGenie,
            NormalPlay,
            TryingToTeleportGenie
        }

        [Tooltip("Prefab spawns when the genie spawns or teleports. Contains particles and sound.")]
        public GameObject spawnOrTeleportFXPrefab;

        public Genie geniePrefab;

        public GeniePlacementValidation.GeniePlacementValidationSettings spawnPlacementSettings;

        [ReadOnly]
        public GenieManagerState state = GenieManagerState.AwaitingLaunchUxCompletion;

        //public Transform GenieDebugEffigy; // <-- You can use this to visualize where the Genie will spawn before it actually spawns. Just uncomment the Effigy lines in LateUpdate() and comment out SpawnGenie()

        [Header("Auto-Respawn Rules")]
        [Tooltip("If the Genie cannot path to the user at this distance, it will be respawned to be closer.")]
        public float islandDistanceThreshold = 2.5f;
        [Tooltip("The Genie must be on an island for this long before it will be respawned.")]
        public float maxIslandDuration = 4f;
        [Tooltip("If the floor height changes by this much, the Genie will be respawned")]
        public float floorYRespawnThreshold = 1f;
        public Genie currentGenie { get; private set; }
        // Avoid magic numbers
        private GeniePlacementValidation _geniePlacementValidation;
        private bool _wasGenieOnAnIsland = false;
        private float _islandStartTime = -1f;

        public override void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            base.OnSceneBootstrapped(bootstrapper);

            // For debugging and rapid iteration:
// #if UNITY_EDITOR
//             Bootstrapper.XRNode.xrInputWrapper.doublePinchDetection.OnDoublePinch += OnDoublePinch;
// #endif
        }

        private void Awake()
        {
            // Listen to when the user presses the "Teleport here" button in the main menu
            GlobalEventManager.Subscribe<GenieTeleportHereBtnPressed>(OnGenieTeleportHereBtnPressed);

            GeniesIrlBootstrapper.Instance.XRNode.volumeCamera.WindowStateChanged.AddListener(OnWindowEvent);
        }

        private void Start()
        {
            _geniePlacementValidation = new GeniePlacementValidation(Bootstrapper.ARNavigation); // Initialize GeniePlacementValidation
        }

        private void OnWindowEvent(VolumeCamera volumeCamera, VolumeCamera.WindowState s)
        {
            if (s.WindowEvent == VolumeCamera.WindowEvent.Focused && s.IsFocused)
            {
                // App focused has changed back to In Focus, meaning that the user has either navigated back to the app, or placed the headset back on.
                // When this happens, Unity resets the world origin, potentially sending the genie to god knows where. Ideally, we would want her to stay in exactly 
                // the same place she was before, doing whatever she was previously doing. This can theoretically be accomplished with World Anchors, but since the Genie
                // is not a static object, and since anchoring and de-anchoring is an asychronous process, we've decided not to go that route. There's also a possibility
                // that the app is refocusing in a completely new environment, in which case, we'd want to respawn the Genie anyway. More dev and design can definitely 
                // go into a more persistent and seamless de-focus/focus system, but this should work for now.
                if (currentGenie == null) return;

                if (state != GenieManagerState.NormalPlay) return; // Ignore if already trying to spawn/teleport a Genie.

                state = GenieManagerState.TryingToTeleportGenie;
            }
        }

        private void OnGenieTeleportHereBtnPressed(GenieTeleportHereBtnPressed pressed)
        {
            Debug.Log("OnGenieTeleportHereBtnPressed");

            if (state == GenieManagerState.TryingToSpawnGenie || state == GenieManagerState.TryingToTeleportGenie) return; // Ignore if already trying to spawn/teleport a Genie.

            if (currentGenie == null)
            {
                state = GenieManagerState.TryingToSpawnGenie;
            }
            else
            {
                state = GenieManagerState.TryingToTeleportGenie;
            }
        }

        private void LateUpdate()
        {
            Transform userHead = Bootstrapper.XRNode.xrInputWrapper.Head;

            switch (state)
            {
                case GenieManagerState.AwaitingLaunchUxCompletion:
                    // We can auto-spawn the Genie if the 'debug skip launch UX' is enabled and the launch UX is completed (which should basically be instantaneous)
                    if (Bootstrapper.LaunchUX.Complete && Bootstrapper.DebugSkipLaunchUX)
                    {
                        state = GenieManagerState.TryingToSpawnGenie;
                    }
                    break;
                case GenieManagerState.TryingToSpawnGenie:
                    if (_geniePlacementValidation.TryFindValidPlacementInFrontOfUser(userHead, spawnPlacementSettings, out Vector3 placementPoint))
                    {
                        //GenieDebugEffigy.position = placementPoint; // Uncomment if you want to use the Effigy to see where it will spawn instead of actually spawning it.
                        //GenieDebugEffigy.gameObject.SetActive(true);
                        SpawnGenie(placementPoint);
                        state = GenieManagerState.NormalPlay;
                    }
                    break;
                case GenieManagerState.NormalPlay:
                    RescueGenieIfOnAnIsland();
                    break;
                case GenieManagerState.TryingToTeleportGenie:
                    if (_geniePlacementValidation.TryFindValidPlacementInFrontOfUser(userHead, spawnPlacementSettings, out Vector3 placementPoint2))
                    {
                        TeleportGenie(placementPoint2);
                        state = GenieManagerState.NormalPlay;
                    }
                    break;
            }
        }

        private void SpawnGenie(Vector3 spawnPoint)
        {
            currentGenie = Instantiate(geniePrefab, spawnPoint, Quaternion.identity);
            currentGenie.OnSpawnedByGenieManager(this);

            currentGenie.genieLookAndYaw.InstantYawTowards(Bootstrapper.XRNode.xrInputWrapper.Head.position);
            ;
            Vector3 particlePos = spawnPoint + (Vector3.up * currentGenie.Height / 2f);
            Instantiate(spawnOrTeleportFXPrefab, particlePos, Quaternion.identity); // Play audio and visual FX
            _wasGenieOnAnIsland = false; // Reset the island check

            // Ensure we're listening to OnFloorRecalibrated events
            Bootstrapper.XRNode.xrFloorManager.OnFloorRecalibrated -= OnFloorRecalibrated;
            Bootstrapper.XRNode.xrFloorManager.OnFloorRecalibrated += OnFloorRecalibrated;
        }

        private void TeleportGenie(Vector3 teleportPoint)
        {
            if (currentGenie != null)
            {
                currentGenie.transform.position = teleportPoint;
            }

            currentGenie.OnTeleported(Bootstrapper.XRNode.xrInputWrapper.Head.position);

            Vector3 particlePos = teleportPoint + (Vector3.up * currentGenie.Height / 2f);
            Instantiate(spawnOrTeleportFXPrefab, particlePos, Quaternion.identity); // Play audio and visual FX
            _wasGenieOnAnIsland = false; // Reset the island check
        }

//         private void OnDoublePinch(object sender, DoublePinchDetection.DoublePinchEventArgs args)
//         {
//             // Double pinch means we're going to try to teleport a Genie, but we can only do this in NormalPlay (after the Genie is spawned)
//             if (state != GenieManagerState.NormalPlay) return;

//             // Temporarily disable double-pinch teleportation until we can get it working more consistently.
//             // state = GenieManagerState.TryingToTeleportGenie;

// #if UNITY_EDITOR
//             // Calculate the position in world space beneath the mouse that double-clicked
//             // If there is a hit, teleport to that location
//             RaycastHit hit;
//             if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100f))
//             {
//                 TeleportGenie(hit.point);
//             }
// #endif
//         }

        private float _prevFloorY = float.MinValue;

        private void OnFloorRecalibrated(float newFloorY)
        {
            // A change in floor Y means we have to teleport the Genie to the nearest walkable node,
            // but we can only do this in NormalPlay (after the Genie is spawned).
            if (state != GenieManagerState.NormalPlay) return;

            if (Mathf.Abs(_prevFloorY - newFloorY) < floorYRespawnThreshold) return; // Floor Y hasn't changed enough to warrant a respawn.

            Debug.Log("A substantially different floor height was detected. Teleporting Genie to new height.");

            state = GenieManagerState.TryingToTeleportGenie;

            _prevFloorY = newFloorY;
        }

        private void RescueGenieIfOnAnIsland()
        {
            if (currentGenie == null) return; // No Genie to rescue.

            Transform userHead = Bootstrapper.XRNode.xrInputWrapper.Head;

            float sqrDistanceXZ = VectorUtils.GetSquareDistanceXZ(currentGenie.transform.position, userHead.position);

            bool isOnAnIsland = sqrDistanceXZ > islandDistanceThreshold * islandDistanceThreshold
                && !Bootstrapper.ARNavigation.IsPathReachable(currentGenie.transform.position, userHead.position, islandDistanceThreshold); // <-- This is not reliable.

            if (isOnAnIsland != _wasGenieOnAnIsland)
            {
                if (isOnAnIsland)
                {
                    _islandStartTime = Time.time;
                    Debug.Log("Island Detected. Starting timer...");
                }
                else
                {
                    Debug.Log("No more island detected. Resetting timer...");
                }
            }

            if (!isOnAnIsland)
            {
                _islandStartTime = -1f; // Reset the timer if the Genie is no longer on an island.
            }
            else
            {
                if (_islandStartTime > 0 && Time.time - _islandStartTime > maxIslandDuration)
                {
                    Debug.Log("Genie is on an island! Respawning...");
                    state = GenieManagerState.TryingToTeleportGenie;
                }
            }

            _wasGenieOnAnIsland = isOnAnIsland;
        }

        /*
        private void OnDrawGizmos()
        {
            // This debug code allows us to get a visualization of the inner workings of GeniePlacementValidation.
            // Uncomment it, along with the related member variables inside GeniePlacementValidation to view Gizmo visualization.
            if (_geniePlacementValidation != null)
            {
                if (_geniePlacementValidation.Nodes != null)
                {
                    for (int i=0; i<_geniePlacementValidation.Nodes.Count; i++)
                    {
                        var node = _geniePlacementValidation.Nodes[i];
                        int nodeScore = _geniePlacementValidation.NodeScores[i];

                        Vector3 pos = (Vector3)node.position;

                        Color color = Color.black;

                        if (nodeScore > 0)
                        {
                            color = Color.Lerp(Color.yellow, Color.red, (float)nodeScore / 100f);
                        }

                        Gizmos.color = color;
                        Gizmos.DrawSphere(pos, 0.1f);
                    }
                }
            }
        }
        */
    }
}

