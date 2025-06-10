using System;
using Pathfinding;
using UnityEngine;

namespace GeniesIRL 
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(DynamicGridObstacle))]
    public class UserObstacleAStar : MonoBehaviour
    {
        private CapsuleCollider _capsuleCollider;
        private DynamicGridObstacle _dynamicGridObstacle;

        private Transform _userHead;
        [NonSerialized]
        private FloorManager _xrFloorManager;

        public void OnSpawned(XRNode xrNode)
        {
            _userHead = xrNode.xrInputWrapper.Head;
            _xrFloorManager = xrNode.xrFloorManager;
        }

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _dynamicGridObstacle = GetComponent<DynamicGridObstacle>();
            
        }

        private void LateUpdate()
        {
            if (_userHead == null) return;

            Vector3 userPositionXZ = _userHead.position;
            userPositionXZ.y = _xrFloorManager.FloorY;    
            transform.position = userPositionXZ;
        }
    }
}
