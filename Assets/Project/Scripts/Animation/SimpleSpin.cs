using UnityEngine;

public class SimpleSpin : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 1f;

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }
}
