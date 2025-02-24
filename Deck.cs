using UnityEngine;
using System.Collections;
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

        public void LoadCards() {
            // Clear existing cards list but don't destroy the objects
            cards.Clear();

            // Find ALL GameObjects that match our naming convention
            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Dictionary<string, bool> uniqueCards = new Dictionary<string, bool>();

            foreach (var obj in allObjects) {
                if (obj.name.StartsWith("PlayingCards_")) {
                    // Check for duplicate cards
                    if (uniqueCards.ContainsKey(obj.name)) {
                        Debug.LogError($"Duplicate card found: {obj.name}");
                        continue;
                    }
                    uniqueCards[obj.name] = true;

                    // Get or add Card component
                    Card card = obj.GetComponent<Card>();
                    if (card == null) {
                        card = obj.AddComponent<Card>();
                        Debug.Log($"Added Card component to {obj.name}");
                    }

                    // Set the card's parent back to the deck
                    obj.transform.SetParent(transform);
                    cards.Add(card);
                    card.SetFaceDown(true);
                    card.InitializeCard();
                    Debug.Log($"Added card {obj.name} to deck");
                }
            }

            if (cards.Count == 0) {
                Debug.LogError("No cards found in scene! Make sure cards are named PlayingCards_[Rank][Suit]");
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

        public System.Collections.IEnumerator ReturnCard(Card card) {
            if (card != null && !cards.Contains(card)) {
                cards.Add(card);
                // Reset card position and state
                card.transform.SetParent(transform);
                yield return card.ReturnToDeck(transform.position);
                card.SetFaceDown(true);
                card.gameObject.SetActive(true);
                Debug.Log($"Card {card.name} returned to deck at position {card.transform.position}");
            }
        }

        public void ReloadAndShuffle() {
            LoadCards();
            ShuffleDeck();
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