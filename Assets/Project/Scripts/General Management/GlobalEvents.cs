using UnityEngine;

namespace GeniesIRL.GlobalEvents 
{
    /// <summary>
    /// Fires when an Item is instantiated.
    /// </summary>
    public class ItemSpawned
    {
        public Item Item { get; }

        public ItemSpawned(Item item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// Event argument for when an item becomes at rest after being having been held by the user.
    /// </summary>
    public class UserPlacesItem
    {
        public Item Item { get; }

        public UserPlacesItem(Item item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// Event argument for when the user picks up an item.
    /// </summary>
    public class UserPicksUpItem
    {
        public Item Item { get; }

        public UserPicksUpItem(Item item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// Fires when OnDestroy is called on an item.
    /// </summary>
    public class ItemDestroyed
    {
        public Item Item { get; }

        public ItemDestroyed(Item item)
        {
            Item = item;
        }
    }

    public class NewSeatAppeared {}

    public class NewWallAppeared {}

    public class NewWindowAppeared{}

    public class NewCeilingAppeared{}

    public class NewTableAppeared{}

    public class DebugShowNavGrid
    {
        public bool Show { get; }

        public DebugShowNavGrid(bool show)
        {
            Show = show;
        }
    }

    public class DebugShowSpatialMesh
    {
        public bool Show { get; }

        public DebugShowSpatialMesh(bool show)
        {
            Show = show;
        }
    }

    public class DebugScanForNewSpatialMeshes
    {
        public bool Scan { get; }

        public DebugScanForNewSpatialMeshes(bool scan)
        {
            Scan = scan;
        }
    }

    public class DebugVisualizeARPlanes
    {
        public bool Show { get; }

        public DebugVisualizeARPlanes(bool show)
        {
            Show = show;
        }
    }

    public class DebugEnableSpatialMeshOcclusion
    {
        public bool Enable { get; }

        public DebugEnableSpatialMeshOcclusion(bool enable)
        {
            Enable = enable;
        }
    }

    public class DebugEnableNavMeshUpdates
    {
        public bool Enable { get; }

        public DebugEnableNavMeshUpdates(bool enable)
        {
            Enable = enable;
        }
    }

    public class GenieTeleportHereBtnPressed
    {

    }

    public class DebugShowSeatDebuggers
    {
        public bool Show { get; }

        public DebugShowSeatDebuggers(bool show)
        {
            Show = show;
        }
    }
}
