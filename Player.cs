using System.Collections.Generic;
using UnityEngine;

namespace CardGame {
    public enum PlayerType {
        Player,
        Dealer
    }

    public class Player : MonoBehaviour {
        public List<Card> hand = new List<Card>();
        public PlayerType playerType = PlayerType.Player;
        [HideInInspector]
        public List<Transform> cardSlots = new List<Transform>();

        private int currentSlotIndex = 0;

        public void AddCardToHand(Card card) {
            hand.Add(card);
        }

        public void ClearHand() {
            hand.Clear();
            currentSlotIndex = 0;
            // Clear any cards in the slots
            foreach (Transform slot in cardSlots) {
                foreach (Transform child in slot) {
                    Destroy(child.gameObject);
                }
            }
        }

        public List<Card> GetHand() {
            return hand;
        }

        public Transform GetNextAvailableSlot() {
            if (currentSlotIndex >= cardSlots.Count) {
                Debug.LogWarning($"No more available slots for {playerType}");
                return null;
            }

            Transform slot = cardSlots[currentSlotIndex];
            currentSlotIndex++;
            return slot;
        }

        public void ResetSlots() {
            currentSlotIndex = 0;
        }
    }
}
