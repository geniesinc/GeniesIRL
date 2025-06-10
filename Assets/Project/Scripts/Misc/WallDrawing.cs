using UnityEngine;

namespace GeniesIRL 
{
    public class WallDrawing : MonoBehaviour
    {
        [Tooltip("The radius of the drawing, which is used to determine proximity with other drawings, to prevent them from overlapping each other.")]
        public float drawingRadius = 0.25f;

        public Animation myAnimation;

        private void OnDrawGizmosSelected() 
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, drawingRadius);
        }
    }
}

