using System;
using UnityEngine;
using GeniesIRL.GlobalEvents;
using System.Collections;
using Pathfinding;

namespace GeniesIRL 
{
    /// <summary>
    /// The auto item spawner waits until the Genie spawns her first item, then it periodically spawns items around the room when the user isn't looking.
    /// </summary>
    public class AutoItemSpawner : GeniesIrlSubManager
    {
        [SerializeField] private float minTimeBetweenSpawns = 10f;
        [SerializeField] private float maxTimeBetweenSpawns = 20f;
        [SerializeField] private LayerMask avoidLayers = 1<< 0 | 1 << 29; // Avoid Spatial layer and Genie
        [SerializeField] private Item itemPrefab;

        public override void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            base.OnSceneBootstrapped(bootstrapper);

            // Wait for the first item to spawn before kicking off the auto-spawning. This allows the Genie to be the first one to spawn
            // an item, which is nice for the user experience.
            GlobalEventManager.Subscribe<ItemSpawned>(OnItemSpawned);
        }

        private void OnItemSpawned(ItemSpawned spawned)
        {
            if (spawned.Item.IsDebugMode) return; // Don't react to debug items that were brought into the scene by a developer for testing.

            GlobalEventManager.Unsubscribe<ItemSpawned>(OnItemSpawned);
            Debug.Log("Spawning Items periodically...");
            StartCoroutine(SpawnItemsPeriodically_C());
        }

        private IEnumerator SpawnItemsPeriodically_C()
        {
            while (true)
            {
                float timeToWait = UnityEngine.Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns);
                //float timeToWait = 1f;
                yield return new WaitForSeconds(timeToWait);

                Debug.Log("Checking for place to spawn item.");
                if (IsThereAPlaceToSpawnAnItem(out Pose pose))
                {
                    Debug.Log("Spawning item");
                    Item.CreateFromItemSpawner(itemPrefab, pose.position, pose.rotation);
                }
            }
        }

        private bool IsThereAPlaceToSpawnAnItem(out Pose pose)
        {
            pose = new Pose();

            // Pick a random point behind the user.
            Transform userHead = Bootstrapper.XRNode.xrInputWrapper.Head;

            if (userHead.forward == Vector3.up || userHead.forward == Vector3.down)
            {
                return false;
            }

            Vector3 userBackDir = -userHead.forward;
            userBackDir.y = 0;
            userBackDir.Normalize();

            Vector3 userPos = userHead.position;
            userPos.y = 0;

            float angle = UnityEngine.Random.Range(-90f, 90f);
            Vector3 newDir = Quaternion.AngleAxis(angle, Vector3.up) * userBackDir;

            float distFromUser = 1.5f; 

            Vector3 pos = userPos + newDir * distFromUser;

            float floorY = Bootstrapper.XRNode.xrFloorManager.FloorY;

            float heightOffFloor = itemPrefab.Collider.bounds.size.y / 2 + 0.1f;
            pos.y = heightOffFloor + floorY;

            Debug.DrawLine(userPos, pos, Color.red, 5f);

            float randObjectYaw = UnityEngine.Random.Range(0f, 360f);
            Quaternion objectRot = Quaternion.Euler(0, randObjectYaw, 0);

            // Do Box Check to make sure this spot is clear.
            if (Physics.CheckBox(pos, itemPrefab.Collider.bounds.extents, objectRot, avoidLayers))
            {
                return false;
            }

            // Make sure there's some ground below us.
            if (!Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 0.5f, avoidLayers))
            {
                return false;
            }

            // Make sure it's reasonably close to a walkable node.
            GridNode node = GenieNavigation.GetNearestWalkableNode(pos);

            if (node == null)
            {
                return false;
            }   

            if (!VectorUtils.IsWithinDistanceXZ((Vector3)node.position, pos, 0.2f))
            {
                return false;
            }

            pose.position = pos;
            pose.rotation = objectRot;
            return true;
        }
    }
}

