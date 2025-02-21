using UnityEngine;
using System.Collections.Generic;

namespace CardGame {
    public class Deck : MonoBehaviour {
        private List<Card> cards = new List<Card>();
        private readonly string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        private readonly string[] suits = { "Spades", "Hearts", "Diamonds", "Clubs" };

        void Awake() {
            Debug.Log($"Deck Awake - Looking for cards under {gameObject.name}");
            LoadCards();
        }

        private void OnDestroy() {
            // Clean up any remaining cards
            foreach (Card card in cards) {
                if (card != null && card.gameObject != null) {
                    Destroy(card.gameObject);
                }
            }
            cards.Clear();
        }

        private void LoadCards() {
            // Clear existing cards
            foreach (Card card in cards) {
                if (card != null && card.gameObject != null) {
                    Destroy(card.gameObject);
                }
            }
            cards.Clear();

            // Find all potential card objects under this deck by name pattern
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            Dictionary<string, bool> uniqueCards = new Dictionary<string, bool>();

            foreach (Transform child in allChildren) {
                if (child.name.StartsWith("PlayingCards_")) {
                    // Check for duplicate cards
                    if (uniqueCards.ContainsKey(child.name)) {
                        Debug.LogError($"Duplicate card found: {child.name}");
                        continue;
                    }
                    uniqueCards[child.name] = true;

                    Card card = child.GetComponent<Card>();
                    if (card == null) {
                        card = child.gameObject.AddComponent<Card>();
                        Debug.Log($"Added Card component to {child.name}");
                    }
                    cards.Add(card);
                    card.SetFaceDown(true);
                    card.InitializeCard();
                    Debug.Log($"Added card {child.name} to deck");
                }
            }

            if (cards.Count == 0) {
                Debug.LogError("No cards found in deck! Make sure cards are named PlayingCards_[Rank][Suit]");
            } else if (cards.Count != 52) {
                Debug.LogWarning($"Unexpected number of cards in deck: {cards.Count}. Standard deck should have 52 cards.");
            } else {
                Debug.Log($"Loaded {cards.Count} cards into deck");
                ShuffleDeck();
            }
        }

        public Card DealNextCard() {
            if (cards.Count > 0) {
                Card card = cards[0];
                cards.RemoveAt(0);
                return card;
            }
            return null;
        }

        public Card DrawCard() {
            return DealNextCard();
        }

        public void ReturnCard(Card card) {
            if (card != null && !cards.Contains(card)) {
                cards.Add(card);
                card.transform.SetParent(transform);
            }
        }

        public void ShuffleDeck() {
            if (cards.Count == 0) {
                Debug.LogWarning("Attempted to shuffle empty deck");
                return;
            }

            // Fisher-Yates shuffle algorithm
            int n = cards.Count;
            while (n > 1) {
                n--;
                int k = Random.Range(0, n + 1);
                Card temp = cards[k];
                cards[k] = cards[n];
                cards[n] = temp;
            }

            // Validate shuffle
            HashSet<string> seenCards = new HashSet<string>();
            foreach (Card card in cards) {
                if (card == null) {
                    Debug.LogError("Null card found in deck after shuffle!");
                    continue;
                }
                string cardKey = $"{card.rank}_{card.suit}";
                if (seenCards.Contains(cardKey)) {
                    Debug.LogError($"Duplicate card found after shuffle: {cardKey}");
                }
                seenCards.Add(cardKey);
            }

            Debug.Log($"Deck shuffled. {cards.Count} cards verified.");
        }
    }
}