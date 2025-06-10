using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Determines which is the valid "forward" direction of the seat, if it has one.
    /// </summary>
    [System.Serializable]
    public class SeatValidation
    {
        public enum SeatType { Directional, NonDirectional, Unsittable}

        public float Radius {get; set;} = 0.5f;

        public Vector3 Center {get; set;}
        
        /// <summary>
        /// This value is updated each time Validate is called. At the time of writing, it's used for debugging.
        /// </summary>
        public bool IsSurroundedByObstructions {get; private set;}

        [Tooltip("Number of raycasts to fire in a circle (default = 8).")]
        public int numberOfRaycasts = 8;

        [Tooltip("Minimum size of contiguous 'open' rays to consider a valid opening (default = 3).")]
        public int minimumOpenSize = 3;

        [Tooltip("Layers to cast rays against.")]
        public LayerMask hitMask = 1 << 29;

        /// <summary>
        /// Determines the type of seat, in terms of directionality.
        /// </summary>
        /// <param name="seatingDirection"></param>
        /// <returns></returns>
        public SeatType Validate(out Vector3 seatingDirection) 
        {
            seatingDirection = default(Vector3);
            IsSurroundedByObstructions = false;

            // Step 1: Cast rays and keep track of hit vs. open
            bool[] hitArray = new bool[numberOfRaycasts];

            for (int i = 0; i < numberOfRaycasts; i++)
            {
                // Calculate the angle for this ray
                float angle = (360f / numberOfRaycasts) * i;
                float radian = angle * Mathf.Deg2Rad;

                // Determine direction from center based on angle
                Vector3 direction = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian));

                // Fire a raycast in the computed direction up to 'radius' distance
                if (Physics.Raycast(Center, direction, out RaycastHit hit, Radius, hitMask))
                {
                    // Mark this as a hit
                    hitArray[i] = true;

                    // Draw a red line from center to the hit point
                    Debug.DrawLine(Center, hit.point, Color.red);
                }
                else
                {
                    // Mark this as open (no hit)
                    hitArray[i] = false;
                }
            }

            // Special check: If all rays are open, we handle that separately
            int totalOpen = 0;
            for (int i = 0; i < numberOfRaycasts; i++)
            {
                if (!hitArray[i]) totalOpen++;
            }

            if (totalOpen == numberOfRaycasts)
            {
                // Case A: All rays are open -> draw green line straight up (for demonstration)
                Debug.DrawLine(Center, Center + Vector3.up * Radius, Color.green);
                return SeatType.NonDirectional;
            }

            // Step 2: Find largest contiguous run ignoring wrap
            int largestRunStart = -1;
            int largestRunSize = 0;

            int currentRunStart = -1;
            int currentRunSize = 0;

            for (int i = 0; i < numberOfRaycasts; i++)
            {
                if (!hitArray[i])
                {
                    // If this is the beginning of a new run
                    if (currentRunSize == 0)
                    {
                        currentRunStart = i;
                    }
                    currentRunSize++;
                }
                else
                {
                    // We just ended a run of open rays
                    if (currentRunSize > largestRunSize)
                    {
                        largestRunSize = currentRunSize;
                        largestRunStart = currentRunStart;
                    }
                    // Reset current run
                    currentRunSize = 0;
                }
            }

            // Edge case: if the open run continued till the very end
            if (currentRunSize > largestRunSize)
            {
                largestRunSize = currentRunSize;
                largestRunStart = currentRunStart;
            }

            // Step 3: Check wrap-around
            // Count how many are open at the end of the array
            int endOpenCount = 0;
            for (int i = numberOfRaycasts - 1; i >= 0; i--)
            {
                if (!hitArray[i]) endOpenCount++;
                else break;
            }

            // Count how many are open at the start of the array
            int startOpenCount = 0;
            for (int i = 0; i < numberOfRaycasts; i++)
            {
                if (!hitArray[i]) startOpenCount++;
                else break;
            }

            // If combining these two runs across the boundary yields a bigger run
            // (but cannot exceed numberOfRaycasts - we already handled the "all open" case)
            int wrappedRunSize = endOpenCount + startOpenCount;
            if (wrappedRunSize > largestRunSize && wrappedRunSize < numberOfRaycasts)
            {
                largestRunSize = wrappedRunSize;
                // The run "starts" near the end of the array (where endOpenCount begins)
                largestRunStart = numberOfRaycasts - endOpenCount;
            }

            // Step 4: If the largest run is smaller than minimumOpenSize, we do nothing
            if (largestRunSize < minimumOpenSize)
            {
                // No valid opening found -> no green line drawn
                IsSurroundedByObstructions = true;
                return SeatType.Unsittable;
            }

            // Step 5: Draw the green line pointing to the center of that opening.
            // We'll find the middle index of that run in a wrap-friendly way.
            //   runStart = largestRunStart
            //   runEnd = runStart + largestRunSize - 1 (but might exceed array length -> wrap)
            //   middleIndex = (runStart + (largestRunSize - 1)/2) mod numberOfRaycasts
            float runEnd = largestRunStart + largestRunSize - 1;
            float middleIndex = (largestRunStart + (runEnd)) / 2f; 
            // Note: The above is effectively the midpoint between start and end. Then we mod it:
            middleIndex = middleIndex % numberOfRaycasts;

            float angleStep = 360f / numberOfRaycasts;
            float middleAngle = angleStep * middleIndex;
            float middleRadian = middleAngle * Mathf.Deg2Rad;

            Vector3 bestDirection = new Vector3(Mathf.Cos(middleRadian), 0f, Mathf.Sin(middleRadian));

            // Finally, draw that green line from center to the edge of the circle in that direction
            Debug.DrawLine(Center, Center + bestDirection * Radius, Color.green);

            seatingDirection = bestDirection;
            return SeatType.Directional;
        }
    }
}
