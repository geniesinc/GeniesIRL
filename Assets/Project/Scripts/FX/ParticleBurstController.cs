using UnityEngine;
using System.Collections.Generic;

public class ParticleBurstController : MonoBehaviour
{
    public enum EmissionShape
    {
        Sphere,
        Cone
    }

    // Calling System.Action directly so that we don't
    // need to disambugiate between UnityEngine.Random and System.Random...
    public event System.Action OnParticleBurstDestroyed;

    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int particleCount = 100;
    [SerializeField] private float burstRadius = 0.5f;
    [SerializeField] private EmissionShape emissionShape = EmissionShape.Sphere;
    [SerializeField] private float particleSpeed = 2f;
    [SerializeField] private float maxLifetime = 2f;
    [SerializeField] private Vector3 gravity = new Vector3(0, -9.81f, 0);
    [SerializeField] private float howMuchVelocityMaintainsOverTime = 0.98f;
    [SerializeField] private float rotationDegreePerSecond = 360f;
    [SerializeField] private bool shirnkOverLifetime = false;

    // Randomization
    private float varianceMin = 0.8f;
    private float varianceMax = 1.2f;
    private float randomVariance {
        get { return Random.Range(varianceMin, varianceMax); }
    }

    private List<Particle> particles = new List<Particle>();

    private void Start()
    {
        MakeBurst();
    }

    private void Update()
    {
        // Update all particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle particle = particles[i];

            // Apply gravity to the particle's velocity
            particle.velocity += gravity * Time.deltaTime;

            // Apply air resistance (reduces speed over time)
            particle.velocity *= howMuchVelocityMaintainsOverTime;

            // Move particle
            particle.instance.transform.position += particle.velocity * Time.deltaTime;

              // Apply twirl (fluttering motion)
            particle.instance.transform.Rotate(particle.rotationAxis,
                                                   particle.rotationSpeed * Time.deltaTime,
                                                   Space.World);

            // Decrease lifetime
            particle.lifetime -= Time.deltaTime;

            // Optionally fade the particle out over time
            if(shirnkOverLifetime)
            {
                particle.instance.transform.localScale =
                            Vector3.Lerp(particle.instance.transform.localScale,
                                       Vector3.zero,
                                       Time.deltaTime);
            }

            // Destroy particle if its lifetime is over
            if (particle.lifetime <= 0)
            {
                Destroy(particle.instance);
                particles.RemoveAt(i);
            }
        }

        // Destroy the burst object when all particles are gone
        if(particles.Count == 0)
        {
            OnParticleBurstDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }

    private void MakeBurst()
    {
        for (int i = 0; i < particleCount; i++)
        {
            // Randomize the direction of the particle
            Vector3 direction = Random.onUnitSphere;
            // Adjust the direction based on the emission shape
            if (emissionShape == EmissionShape.Cone)
            {
                // Somewhat cone-like; good enough until more controls are needed!
                direction.y = Mathf.Clamp01(2f * Mathf.Abs(direction.y));
            }

            Vector3 position = transform.position +
                                    direction *  burstRadius * randomVariance;
            
            GameObject particle = Instantiate(particlePrefab, transform);
            particle.transform.position = position;
            particle.transform.localRotation = Quaternion.Euler(direction);
            particle.transform.localScale = Vector3.one * randomVariance;

            // Calculate velocity for this particle
            Vector3 velocity = direction * particleSpeed * randomVariance;

            // Randomized rotation axis and speed for fluttering motion
            Vector3 rotationAxis = Random.onUnitSphere; // Random axis for each petal
            float rotationSpeed = Random.Range(-rotationDegreePerSecond, rotationDegreePerSecond); // Random twirl speed

            // Add the particle to the list
            particles.Add(new Particle(particle,
                                        velocity * randomVariance,
                                        maxLifetime * randomVariance, 
                                        rotationAxis,
                                        rotationSpeed));
        }
    }


    private class Particle
    {
        public GameObject instance;
        public Vector3 velocity;
        public float lifetime;
        public Vector3 rotationAxis;
        public float rotationSpeed;
    

        public Particle(GameObject instance, Vector3 velocity, float lifetime, Vector3 rotationAxis, float rotationSpeed)
        {
            this.instance = instance;
            this.velocity = velocity;
            this.lifetime = lifetime;
            this.rotationAxis = rotationAxis;
            this.rotationSpeed = rotationSpeed;
        }
    }
}
