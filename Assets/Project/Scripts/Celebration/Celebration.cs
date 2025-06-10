using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace GeniesIRL 
{
    /// <summary>
    /// Facilitates the spawning of celebration effects in the world. As of writing, it spawns balloons but could be adapted to spawn more.
    /// Modified to spawn balloons in more evenly distributed positions with inspector-controlled jitter, force, and spawn collision checks.
    /// </summary>
    public class Celebration : MonoBehaviour
    {
        [Tooltip("The number of balloons to spawn per frame.")]
        public int balloonsPerFrame = 10;

        [Tooltip("Balloon density per unit area (in the x-z plane). The total number of balloons is computed as density * spawn area.")]
        public float balloonDensity = 0.1f;

        [Tooltip("The prefab for the balloon to spawn.")]
        public Item balloonPrefab;

        [Tooltip("The initial force magnitude applied to balloons.")]
        public float initialForceMagnitude = 10f;

        [Tooltip("The factor for random position jitter relative to each grid cell size.")]
        public float positionJitterFactor = 0.25f;

        [Tooltip("The Y-axis force range applied to balloons. For upward force, use positive values; for downward, use negatives.")]
        public Vector2 yForceRange = new Vector2(0.5f, 1f);

        [SerializeField] AudioSource _musicAudioSource;

        public float spaceUnerneathCeiling = 0.2f;

        public int maxTotalBalloons = 150;

        private int _balloonsSpawned = 0;
        private int _totalBalloons = 0;
        private bool _isCelebrating = false;
        private List<Vector3> _spawnPositions;
        private Bounds _spawnBounds;

        private Balloon[] _balloons;

        private void Update()
        {
            // Continue spawning balloons if a celebration is ongoing
            if (_isCelebrating)
            {
                SpawnBalloons();
            }
        }

        private void OnDestroy()
        {
            if(_musicAudioSource.isPlaying)
            {
                _musicAudioSource.Stop();
            }

            _isCelebrating = false;
            ClearCurrentBalloons();
        }

        public void Celebrate()
        {
            _isCelebrating = true;
            _balloonsSpawned = 0;

            ClearCurrentBalloons();

            ARNavigation aRNavigation = FindFirstObjectByType<ARNavigation>();
            _spawnBounds = aRNavigation.WorldBounds;
            
            // Compute total number of balloons based on density and spawn area,
            // but do not exceed maxTotalBalloons.
            float spawnArea = _spawnBounds.size.x * _spawnBounds.size.z;
            _totalBalloons = Mathf.Min(Mathf.RoundToInt(balloonDensity * spawnArea), maxTotalBalloons);
            
            _balloons = new Balloon[_totalBalloons];
            _spawnPositions = GenerateSpawnPositions();

            // StopAllCoroutines();
            // StartCoroutine(WaitAndDestroyBalloons_C());
        }

        // private IEnumerator WaitAndDestroyBalloons_C()
        // {
        //     yield return new WaitForSeconds(20f); // Wait for 5 seconds before destroying balloons

        //     ClearCurrentBalloons();
        // }

        private void ClearCurrentBalloons()
        {
            if (_balloons == null) return;
            
            foreach (Balloon balloon in _balloons)
            {
                if (balloon != null)
                {
                    Destroy(balloon.gameObject);
                }
            }
        }

        // Generates an evenly distributed list of spawn positions within spawnBounds.
        private List<Vector3> GenerateSpawnPositions()
        {
            List<Vector3> positions = new List<Vector3>();

            // Determine grid dimensions based on _totalBalloons and the aspect ratio of the spawn area.
            float aspectRatio = _spawnBounds.size.x / _spawnBounds.size.z;
            int gridColumns = Mathf.CeilToInt(Mathf.Sqrt(_totalBalloons * aspectRatio));
            int gridRows = Mathf.CeilToInt((float)_totalBalloons / gridColumns);

            float cellWidth = _spawnBounds.size.x / gridColumns;
            float cellDepth = _spawnBounds.size.z / gridRows;

            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    if (positions.Count >= _totalBalloons)
                        break;

                    // Calculate the center of the cell.
                    float x = _spawnBounds.min.x + cellWidth * col + cellWidth / 2f;
                    float z = _spawnBounds.min.z + cellDepth * row + cellDepth / 2f;

                    // Add some random jitter to avoid a perfectly grid-like appearance.
                    UnityEngine.Random.InitState((int)System.DateTime.Now.Millisecond); 
                    float jitterX = Random.Range(-cellWidth * positionJitterFactor, cellWidth * positionJitterFactor);
                    UnityEngine.Random.InitState((int)System.DateTime.Now.Millisecond); 
                    float jitterZ = Random.Range(-cellDepth * positionJitterFactor, cellDepth * positionJitterFactor);

                    Vector3 pos = new Vector3(x + jitterX, _spawnBounds.center.y, z + jitterZ);
                    positions.Add(pos);
                }
            }

            // Shuffle the positions list so balloons spawn in a random order.
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 temp = positions[i];
                int randomIndex = Random.Range(i, positions.Count);
                positions[i] = positions[randomIndex];
                positions[randomIndex] = temp;
            }

            return positions;
        }

        private void SpawnBalloons()
        {
            for (int i = 0; i < balloonsPerFrame; i++)
            {
                if (_balloonsSpawned >= _totalBalloons)
                {
                    _isCelebrating = false; // Stop the celebration once we've processed all spawn positions
                    break;
                }

                // Get spawn position from the precomputed list.
                Vector3 spawnPosition = _spawnPositions[_balloonsSpawned];
                spawnPosition.y = _spawnBounds.max.y - spaceUnerneathCeiling; // Set the Y position to just below the ceiling.

                // Create a random rotation for the balloon on all axes.
                Quaternion randomRotation = Random.rotation;

                // Instantiate the balloon prefab at the calculated position with a random rotation.
                //Balloon balloon = Instantiate(balloonPrefab, spawnPosition, randomRotation).GetComponent<Balloon>();
                Balloon balloon = Item.CreateFromItemSpawner(balloonPrefab, spawnPosition, randomRotation).GetComponent<Balloon>();

                // Apply random force to the balloon.
                Rigidbody balloonRigidbody = balloon.myRigidbody;
                if (balloonRigidbody != null)
                {
                    UnityEngine.Random.InitState((int)System.DateTime.Now.Millisecond); 
                    Vector3 randomForce = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(yForceRange.x, yForceRange.y),
                        Random.Range(-1f, 1f)
                    ) * initialForceMagnitude;

                    balloonRigidbody.AddForce(randomForce, ForceMode.Impulse);
                }

                _balloons[_balloonsSpawned] = balloon;
                _balloonsSpawned++;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw the spawn bounds in the scene view for visualization.
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_spawnBounds.center, _spawnBounds.size);
        }
    }
}
