using Pathfinding;
using UnityEngine;

namespace GeniesIRL
{
    /// <summary>
    /// AIPath that always asks for partial paths when the goal is unreachable.
    /// Drop this component on the agent **instead of** the stock AIPath(2D/3D) component
    /// (the inspector will look identical).
    /// </summary>
    public class AIPathIRL : AIPath
    {

        public override void SearchPath()
        {
            if (!canSearch) return;

            // Figure out where the path should start/end (AIPath already
            // contains this helper for us)
            Vector3 start, end;
            CalculatePathRequestEndpoints(out start, out end);

            // Build the ABPath and turn on the flag _before_ we hand it off
            var p = ABPath.Construct(start, end, null);
            p.calculatePartial = true;      // **<- important line**
#if ASTAR_PRO
            // Pass the path to the base-class helper (this enqueues it on the Seeker)
            SetPath(p, false); // <-- This only compiles in PRO
#else
            SetPath(p);
#endif
        }

    }
}


