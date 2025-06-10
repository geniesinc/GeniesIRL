using UnityEngine;

namespace GeniesIRL 
{
    public class Balloon : MonoBehaviour
    {
        public Rigidbody myRigidbody;
        [Header("Balloon Settings")]
        [Tooltip("The world-space Y value below which the balloon's collider will be activated.")]
        [SerializeField] private float collisionActivationHeight = 2f;

        [Tooltip("The collider associated with the balloon.")]
        [SerializeField] private Collider balloonCollider;

        [SerializeField] private float buoyantForce = 15f;

        [SerializeField] private Color[] colors;

        [SerializeField] private Renderer balloonRenderer;

        private void Awake()
        {
            if (balloonCollider == null)
            {
                Debug.LogError("Balloon Collider is not assigned!", this);
                return;
            }

            // Set random rotation
            transform.rotation = Random.rotation;

            // Set the collider to trigger initially
            balloonCollider.isTrigger = true;

            ApplyRandomColor();
        }

        private void FixedUpdate()
        {
            // Apply a constant upward force.
            myRigidbody.AddForce(Vector3.up * buoyantForce, ForceMode.Acceleration);

            // Simulate wind by applying a horizontal force with damping
            float dampingFactor = Mathf.Exp(-Time.time * 0.5f); // Exponential decay
            Vector3 windForce = new Vector3(
                (Mathf.PerlinNoise(Time.time, 0f) * 0.1f - 0.05f) * dampingFactor, 
                0f, 
                (Mathf.PerlinNoise(0f, Time.time) * 0.1f - 0.05f) * dampingFactor
            );
            myRigidbody.AddForce(windForce, ForceMode.Force);

            // Apply a random rotational force that dies down over time
            Vector3 randomTorque = new Vector3(
                (Mathf.PerlinNoise(Time.time * 0.5f, 0f) * 0.2f - 0.1f) * dampingFactor,
                (Mathf.PerlinNoise(0f, Time.time * 0.5f) * 0.2f - 0.1f) * dampingFactor,
                (Mathf.PerlinNoise(Time.time * 0.5f, Time.time * 0.5f) * 0.2f - 0.1f) * dampingFactor
            );
            myRigidbody.AddTorque(randomTorque, ForceMode.Force);

            if (transform.position.y <= collisionActivationHeight && balloonCollider.isTrigger)
            {
                // Disable the trigger when the balloon falls below the activation height
                balloonCollider.isTrigger = false;
            }
        }

        private void ApplyRandomColor()
        {
            if (colors.Length == 0)
            {
                Debug.LogWarning("No colors assigned to the balloon!", this);
                return;
            }

            // Pick a random color from the array
            Random.InitState((int)System.DateTime.Now.Ticks);
            Color randomColor = colors[Random.Range(0, colors.Length)];
            balloonRenderer.material.SetColor("_BaseColor", randomColor);
        }
    }
}