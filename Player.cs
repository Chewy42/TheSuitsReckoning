using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Object;

namespace CardGame {
    public enum PlayerType { Player, Dealer }

    public class Player : MonoBehaviour {
        public List<Card> hand = new List<Card>();
        public PlayerType playerType;
        public List<Transform> cardSlots = new List<Transform>();
        protected int nextSlotIndex = 0;

        void Awake()
        {
            // Don't reset slots in Awake, wait for Start
            Debug.Log($"Player {playerType} initialized");
        }

        void Start()
        {
            // Reset slots in Start after everything is initialized
            ResetSlots();
            Debug.Log($"Player {playerType} slots initialized - Slots: {cardSlots.Count}");
        }

        public virtual int GetHandValue() {
            var gameManager = FindFirstObjectByType<GameManager>();
            int targetScore = gameManager?.GetCurrentTargetScore() ?? 21;
            int value = 0;
            int numberOfAces = 0;

            // First pass: Count aces and sum up all other cards
            foreach (Card card in hand.Where(c => c != null && !c.IsFaceDown())) {
                if (IsAce(card.rank)) {
                    numberOfAces++;
                    value += 11; // Start with all Aces as 11
                } else {
                    value += GetCardValue(card.rank);
                }
            }

            // Convert Aces from 11 to 1 whenever we would bust
            while (value > targetScore && numberOfAces > 0) {
                value -= 10;  // Convert an ace from 11 to 1
                numberOfAces--;
                Debug.Log($"Converting Ace to 1 to prevent bust. New hand value: {value}");
            }

            return value;
        }

        private bool IsAce(string rank) {
            return rank.ToUpper() == "ACE" || rank.ToUpper() == "A";
        }

        private int GetCardValue(string rank) {
            switch(rank.ToUpper()) {
                case "ACE":
                case "A":
                    return 11; // Default Ace value, GetHandValue handles conversion to 1 if needed
                case "KING":
                case "K":
                case "QUEEN":
                case "Q":
                case "JACK":
                case "J":
                    return 10;
                default:
                    if (int.TryParse(rank, out int value)) {
                        return value;
                    }
                    Debug.LogWarning($"Unable to parse card rank: {rank}, assuming 10");
                    return 10;
            }
        }

        public virtual bool ShouldHit() => GetHandValue() < 17;

        public void AddCardToHand(Card card) {
            if (card == null) {
                Debug.LogError("Attempted to add null card to hand");
                return;
            }
            hand.Add(card);
            Debug.Log($"{playerType} received card: {card.rank} of {card.suit}");
        }

        public void ClearHand() {
            foreach (Card card in hand) {
                if (card != null && card.gameObject != null) {
                    GameManager.Instance.ReturnCardToDeck(card);
                }
            }
            hand.Clear();
            nextSlotIndex = 0;
            Debug.Log($"{playerType} hand cleared");
        }

        public List<Card> GetAllCards() => hand;

        public Transform GetNextAvailableSlot() {
            if (cardSlots == null || cardSlots.Count == 0) {
                Debug.LogError($"No card slots assigned for {playerType}");
                return null;
            }
    
            // If no available slot, reuse the oldest slot by clearing its children
            if (nextSlotIndex >= cardSlots.Count) {
                Debug.LogWarning($"No more slots available for {playerType}. Reusing the oldest slot.");
                Transform oldestSlot = cardSlots[0];
                for (int i = oldestSlot.childCount - 1; i >= 0; i--) {
                    Destroy(oldestSlot.GetChild(i).gameObject);
                }
                nextSlotIndex = 1;
                return oldestSlot;
            }
    
            // Get the next sequential slot
            Transform nextSlot = cardSlots[nextSlotIndex];
            if (nextSlot == null) {
                Debug.LogError($"Slot {nextSlotIndex} is null for {playerType}");
                return null;
            }
    
            nextSlotIndex++;
            return nextSlot;
        }

        public void ResetSlots() {
            if (cardSlots != null && cardSlots.Count > 0) {
                foreach (Transform slot in cardSlots) {
                    if (slot != null) {
                        for (int i = slot.childCount - 1; i >= 0; i--) {
                            var child = slot.GetChild(i);
                            if (child != null) {
                                Destroy(child.gameObject);
                            }
                        }
                    }
                }
                nextSlotIndex = 0;
                Debug.Log($"Reset all slots for {playerType}");
            }
            else {
                Debug.LogWarning($"No card slots found for {playerType}");
            }
        }

        public void DebugSlotState() {
            if (cardSlots != null) {
                for (int i = 0; i < cardSlots.Count; i++) {
                    Transform slot = cardSlots[i];
                    if (slot != null) {
                        Debug.Log($"{playerType} Slot {i}: {slot.childCount} cards");
                        for (int j = 0; j < slot.childCount; j++) {
                            var card = slot.GetChild(j).GetComponent<Card>();
                            if (card != null) {
                                Debug.Log($"  Card {j}: {card.rank} of {card.suit} (FaceDown: {card.IsFaceDown()})");
                            }
                        }
                    }
                    else {
                        Debug.LogError($"{playerType} Slot {i} is null");
                    }
                }
            }
            else {
                Debug.LogError($"No card slots array for {playerType}");
            }
        }
    }
}
