using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Used as part of the effort to be able to detect seat directions.
    /// </summary>
    public class DebugSeatCaster : MonoBehaviour
    {
        public SeatValidation seatValidation;

        private void Update()
        {
            // Radius determined by the localScale.x of this transform
            seatValidation.Radius = transform.localScale.x/2;
            seatValidation.Center = transform.position;

            seatValidation.Validate(out Vector3 seatingDirection);
        }
    }
}

