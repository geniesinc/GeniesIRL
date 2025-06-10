using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Uses input data like AR planes to help the Genie understand the environment, so they can interact with it meaningfully. 
    /// Notably, it does NOT handle AR pathfinding, which is the job of ARNavigation.
    /// </summary>
    public class ARSurfaceUnderstanding : GeniesIrlSubManager
    {
        public SeatProcessor seatProcessor;

        public WallProcessor wallProcessor;

        public WindowProcessor windowProcessor;

        public CeilingProcessor ceilingProcessor;

        public ItemPlacementOnHorizontalSurfaces itemPlacementOnHorizontalSurfaces;

        public override void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            base.OnSceneBootstrapped(bootstrapper);

            seatProcessor.OnSceneBootstrapped(bootstrapper.XRNode.arPlaneManager);
            wallProcessor.OnSceneBootstrapped(bootstrapper.XRNode.arPlaneManager, bootstrapper.XRNode.xrFloorManager);
            windowProcessor.OnSceneBootstrapped(bootstrapper.XRNode.arPlaneManager);
            ceilingProcessor.OnSceneBootstrapped(bootstrapper.XRNode.arPlaneManager);
            itemPlacementOnHorizontalSurfaces.OnSceneBootstrapped(bootstrapper.XRNode.arPlaneManager, bootstrapper.XRNode.xrFloorManager);
        }

        private void Update()
        {
            seatProcessor.OnUpdate();
            wallProcessor.OnUpdate();
            windowProcessor.OnUpdate();
            itemPlacementOnHorizontalSurfaces.OnUpdate();
        }
    }
}