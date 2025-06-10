using System;
using System.Collections;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Responsible for giving the Genie the ability to spawn an item and present it to the user.
    /// </summary>
    [System.Serializable]
    public class GenieOfferItem
    {
        public event Action<Item> OnOfferAccepted;

        [Tooltip("When spawning an item, this is what will instantiate.")]
        public Item spawnedItemPrefab;

        // [Tooltip("The particle effect and sound that will play when the item is spawned.")]
        // public GameObject spawnFXPrefab;

        [Header("Audio")]
        public AudioClip offerItemAudioClip;
        public float offerItemAudioVolume = 1f;

        [NonSerialized]
        private Genie _genie;
        [NonSerialized]
        private GenieAnimation _genieAnimation;
        [NonSerialized]
        private GenieGrabber _genieGrabber;
        private Item _item;


        public void OnStart(Genie genie)
        {
            _genie = genie;
            _genieGrabber = _genie.genieGrabber;
            _genieAnimation = _genie.genieAnimation;
            _genieAnimation.animEventDispatcher.OnSpawnItemForUser += OnSpawnItemForUser;
            _genieAnimation.animEventDispatcher.OnTossItemBackwards += OnTossItemBackwards;
        }

        /// <summary>
        /// Spawns an item and offers it to the user.
        /// </summary>
        /// <returns></returns>
        public IEnumerator OfferItem_C()
        {   
            string triggerAndStateName;

            // Are we spawning a new item, or offering one we're already holding?
            Item heldItem = _genie.genieGrabber.HeldItem;

            if (heldItem == null)
            {
                // Spawn a new item
                triggerAndStateName = "SpawnAndOfferItem";
            }
            else 
            {
                // Offer the item we're already holding
                triggerAndStateName = "SimpleOfferItem";
                _item = heldItem;
            }

            // Set the animator trigger to start the animation.
            _genieAnimation.Animator.SetTrigger(triggerAndStateName);

            // Make eye contact with the user.
            Transform userHead = _genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper.Head;
            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(userHead);

            // Play the associated audio clip.
            _genie.genieAudio.PlayGeneralSingleShotSound(offerItemAudioClip, offerItemAudioVolume);

            // Wait for the animation to complete.
            yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(triggerAndStateName));
            yield return new WaitUntil(() => _genieAnimation.Animator.IsStateClipComplete(triggerAndStateName)); 

            // At this point, the held item must become "stealable" by the user.
            _item.OnItemStolenFromGenie += OnUserAcceptedItem;
            _item.TemporarilyAllowUserToGrabItemHeldByGenie(true);
        }

        /// <summary>
        /// If the Action is cancelled externally, we'll need to do some cleanup here. There is some overlap here with OfferRejected_C() -- perhaps at
        /// some point we can refactor to reduce redundancy.
        /// </summary>
        public void ExternallyCancelOffer()
        {
            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();

            if (_item != null)
            {
                _item.OnItemStolenFromGenie -= OnUserAcceptedItem;
            }

            _genieGrabber.InstantReleaseHeldItem();
        }

        /// <summary>
        /// Called by the OfferItemToUserAction when the Offer was rejected (i.e. the user didn't take the item).
        /// </summary>
        /// <returns></returns>
        public IEnumerator OfferRejected_C()
        {
            // Withdraw the offer.
             _item.OnItemStolenFromGenie -= OnUserAcceptedItem;
            _item.TemporarilyAllowUserToGrabItemHeldByGenie(false);

            // Look dejected.
            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();

            string triggerAndStateName = "ItemOfferRejected";

            // Set the animator trigger to start the animation.
            _genieAnimation.Animator.SetTrigger(triggerAndStateName);

            // Wait for the animation to complete.
            yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(triggerAndStateName)); 
            yield return new WaitUntil(() => _genieAnimation.Animator.IsStateClipComplete(triggerAndStateName)); 
        }

        private void OnUserAcceptedItem(Item item)
        {
            item.OnItemStolenFromGenie -= OnUserAcceptedItem;

            OnOfferAccepted?.Invoke(item);
        }

        public IEnumerator Celebrate_C() 
        {
            string triggerAndStateName = "ItemOfferAccepted";

            _genieAnimation.Animator.SetTrigger(triggerAndStateName);

            // Wait for the animation to complete.
            yield return new WaitUntil(() => _genieAnimation.Animator.IsInState(triggerAndStateName)); 
            yield return new WaitUntil(() => _genieAnimation.Animator.IsStateClipComplete(triggerAndStateName)); 

            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
        }

        private void OnTossItemBackwards()
        {
            // Triggered by the animation event dispatcher to signal when the item should be tossed backwards.
            if (_item != null)
            {
                _genieGrabber.InstantReleaseHeldItem();

                // Apply a force to the item to toss it backwards.
                Rigidbody itemRigidbody = _item.GetComponent<Rigidbody>();

                if (itemRigidbody != null)
                {
                    Vector3 forceDirection = -_genie.transform.forward; // Toss backwards
                    itemRigidbody.AddForce(forceDirection * 5f, ForceMode.Impulse);
                }

                _item = null;
            }
        }

        private void OnSpawnItemForUser()
        {
            // Triggered by the animation event dispatcher to signal when the item should be spawned.
            _item = GameObject.Instantiate(spawnedItemPrefab);
            _genieGrabber.InstantGrabAndTeleportToHand(GenieHand.Right, _item);
            //GameObject.Instantiate(spawnFXPrefab, _item.transform.position, Quaternion.identity);
        }
    }
}