using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame{
    public class Deck : MonoBehaviour
    {
        private string prefix = "PlayingCards_";
        private List<Card> cards = new List<Card>();
        
        private void Start() {
            for (int i = 0; i < transform.childCount; i++) {
                // Get the card GameObject
                GameObject cardObject = transform.GetChild(i).gameObject;
                // Get the Card component if it exists; otherwise add it.
                Card card = cardObject.GetComponent<Card>();
                if(card == null)
                    card = cardObject.AddComponent<Card>();
                
                string name = cardObject.name;
                string cardName = name.Substring(prefix.Length);

                // Identify suit by checking known suffixes.
                string suit = "";
                // Updated candidate list with singular & plural forms.
                string[] suitCandidates = new string[] { "Spades", "Hearts", "Heart", "Diamonds", "Diamond", "Clubs", "Club" };
                string foundCandidate = "";
                foreach(string candidate in suitCandidates){
                    if(cardName.EndsWith(candidate)){
                        foundCandidate = candidate;
                        break;
                    }
                }
                if(!string.IsNullOrEmpty(foundCandidate)){
                    // Map to canonical suit names.
                    if(foundCandidate == "Club" || foundCandidate == "Clubs")
                        suit = "Clubs";
                    else if(foundCandidate == "Heart" || foundCandidate == "Hearts")
                        suit = "Hearts";
                    else if(foundCandidate == "Diamond" || foundCandidate == "Diamonds")
                        suit = "Diamonds";
                    else if(foundCandidate == "Spade" || foundCandidate == "Spades")
                        suit = "Spades";

                    string rankPart = cardName.Substring(0, cardName.Length - foundCandidate.Length);
                    switch(rankPart) {
                        case "Q":
                            card.rank = "Queen";
                            break;
                        case "K":
                            card.rank = "King";
                            break;
                        case "J":
                            card.rank = "Jack";
                            break;
                        case "A":
                            card.rank = "Ace";
                            break;
                        default:
                            card.rank = rankPart;
                            break;
                    }
                } else {
                    // Fallback: if no suit candidate found, assign full name as rank.
                    card.rank = cardName;
                }
                card.suit = suit;
                // Save Card instance instead of GameObject
                cards.Add(card);
            }
            //PrintAllCardsInDeck();
        }

        public void PrintAllCardsInDeck(){
            foreach(Card card in cards){
                print(card.rank + " of " + card.suit);
            }
        }

        public void ShuffleDeck() {
            int n = cards.Count;
            while (n > 1) {
                n--;
                int k = Random.Range(0, n + 1);
                Card temp = cards[k];
                cards[k] = cards[n];
                cards[n] = temp;
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
    }
}
