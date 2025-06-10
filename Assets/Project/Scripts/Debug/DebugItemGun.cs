using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Debug Utility that spawns and fires Items at the Genie with various speeds when the user presses a key. The Monobehavior can be attached 
    /// anywhere in the scene -- it simply uses the ctive camera to determine spawn point and directionality of the item. Just make sure to delete it
    /// from the main scene when you're done using it!
    /// </summary>
    public class DebugItemGun : MonoBehaviour
    {
        public KeyCode keyToFire = KeyCode.F;
        public Item itemPrefab;

        public float spawnDistFromCamera = 0.25f;

        public float speed = 5f;

        private void Update()
        {
            if (Input.GetKeyDown(keyToFire))
            {
                FireItem();
            }
        }

        private void FireItem()
        {
            Transform camTransform = Camera.main.transform;

            Vector3 spawnPoint = camTransform.position + camTransform.forward * spawnDistFromCamera;

            Item item = Item.CreateFromItemSpawner(itemPrefab, spawnPoint, Quaternion.identity);
            item.state = Item.ItemState.DroppedByUserAndInMotion;
            item.GetComponent<Rigidbody>().linearVelocity = camTransform.forward * speed;
        }
    }
}

