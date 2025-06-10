using System;
using UnityEngine;

namespace GeniesIRL 
{
    [RequireComponent(typeof(Item))]
    public class GenieGrabbable : MonoBehaviour
    {
        public event Action OnGenieGrabbed;

        /// <summary>
        /// Perameter is true if the item was stolen by the user, which at the time of writing should only happen if the Genie was offering it
        /// </summary>
        public event Action<bool> OnGenieReleased;

        public enum GrabState { NotGrabbed, Grabbed }

        public Item Item {
            get{
                if (_item == null) _item = GetComponent<Item>();
                return _item;
            }}

        public Vector3 inGenieHandOffset;
        public Vector3 inGenieHandRotationOffset;

        /// <summary>
        /// Value is set externally by a genie when it intends to grab the item.
        /// </summary>
        public bool IsTargetedByGenie { get; private set;}

        public GrabState grabState { get; private set; } = GrabState.NotGrabbed;

        private Item _item; 

        private Action<Item> _stolenCallback;
        
        /// <summary>
        /// Let the object know it's been grabbed.
        /// </summary>
        /// <param name="nearGrab">Mark the grab as being "near" or "far". This is used to determine lerp behavior.</param>
        /// <param name="stolenCallback">The callback to invoke if the item is stolen by the user.</param>
        public void PerformGrab(Action<Item> stolenCallback)
        {
            grabState = GrabState.Grabbed;

            Item.OnItemStolenFromGenie += OnItemStolenByUser;

            _stolenCallback = stolenCallback;

            OnGenieGrabbed?.Invoke();
        }

        public void PerformRelease(bool stolenByUser = false)
        {
            grabState = GrabState.NotGrabbed;

            _stolenCallback = null;

            OnGenieReleased?.Invoke(stolenByUser);
        }

        // At time of writing, this should only happen when the Genie is offering the item to the user.
        private void OnItemStolenByUser(Item item)
        {
            item.OnItemStolenFromGenie -= OnItemStolenByUser;

            _stolenCallback?.Invoke(item);

            PerformRelease(true);
        }

        /// <summary>
        /// Tells the Grabbable that the Genie intends to grab it.
        /// </summary>
        /// <param name="isTargeted"></param>
        public void MarkTargetedByGenie(bool isTargeted)
        {
            IsTargetedByGenie = isTargeted;
        }
    }
}

