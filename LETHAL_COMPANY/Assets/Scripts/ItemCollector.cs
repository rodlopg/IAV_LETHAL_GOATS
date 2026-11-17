using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LethalCompanyAI
{
    /// <summary>
    /// Put this on the Player. When the player touches an object with the item tag, the object
    /// is destroyed and counted. Reaching Items Required fires AllItemsCollected.
    ///
    /// Setup: create the tag (default "Item"), tag every collectible with it, and give each
    /// collectible a Collider (ideally with Is Trigger ticked).
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemCollector : MonoBehaviour
    {
        [Header("Items")]
        public string itemTag = "Item";
        [Min(1)] public int itemsRequired = 5;

        [Header("Feedback")]
        public AudioClip pickupSound;
        [Range(0f, 1f)] public float pickupVolume = 1f;

        [Header("Events")]
        public UnityEvent<int, int> onItemCollected = new UnityEvent<int, int>(); // (collected, required)
        public UnityEvent onAllItemsCollected = new UnityEvent();

        /// <summary>Raised with (collected, required) after every pickup.</summary>
        public event Action<int, int> ItemCollected;
        /// <summary>Raised once when the required amount is reached.</summary>
        public event Action AllItemsCollected;

        public int Collected { get; private set; }
        public int Required { get { return itemsRequired; } }
        public bool Completed { get; private set; }

        // Destroy() is deferred to the end of the frame, so remember what we already took
        // to avoid double counting (trigger + controller hit in the same frame).
        private readonly HashSet<GameObject> taken = new HashSet<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other.gameObject);
        }

        // Fallback for items whose collider is NOT a trigger.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            TryCollect(hit.gameObject);
        }

        private void TryCollect(GameObject target)
        {
            if (Completed || target == null || taken.Contains(target))
                return;

            if (!target.CompareTag(itemTag))
                return;

            taken.Add(target);

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, target.transform.position, pickupVolume);

            Destroy(target);
            Collected++;

            onItemCollected.Invoke(Collected, itemsRequired);
            if (ItemCollected != null)
                ItemCollected(Collected, itemsRequired);

            if (Collected >= itemsRequired)
            {
                Completed = true;
                onAllItemsCollected.Invoke();
                if (AllItemsCollected != null)
                    AllItemsCollected();
            }
        }
    }
}