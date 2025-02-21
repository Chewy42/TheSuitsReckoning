using UnityEngine;
using System.Collections;

namespace CardGame {
    public class Card : MonoBehaviour {
        public string suit;
        public string rank;
        public GameObject card;
        public bool isFaceDown = false;
        private bool isBeingFlipped = false;
        private Coroutine flipCoroutine;

        void Awake() {
            Debug.Log($"Card Awake: {gameObject.name}");
            if (!GetComponent<Card>()) {
                Debug.LogError($"Card component missing on {gameObject.name}");
            }
            // Remove InitializeCard call from Awake - let Deck handle initialization
        }

        public void InitializeCard() {
            // Parse rank and suit from the object name
            if (gameObject.name.StartsWith("PlayingCards_")) {
                string cardInfo = gameObject.name.Substring("PlayingCards_".Length);
                // Handle special cases for 10 first
                if (cardInfo.StartsWith("10")) {
                    rank = "10";
                    suit = cardInfo.Substring(2);
                } else {
                    string singleRank = cardInfo.Substring(0, 1);
                    // Map single-letter ranks to full names
                    rank = singleRank switch {
                        "K" => "King",
                        "Q" => "Queen",
                        "J" => "Jack",
                        "A" => "Ace",
                        _ => singleRank // Keep numeric values as is
                    };
                    suit = cardInfo.Substring(1);
                }
            } else {
                Debug.LogWarning($"Card {gameObject.name} does not follow the naming convention PlayingCards_[Rank][Suit]");
            }
        }

        public IEnumerator FlipCard() {
            if (isBeingFlipped) {
                Debug.LogWarning($"Card {gameObject.name} is already being flipped, ignoring request");
                yield break;
            }

            isBeingFlipped = true;
            float flipDuration = GameParameters.CARD_FLIP_DURATION;
            float elapsedTime = 0f;
            Vector3 originalScale = transform.localScale;
            
            try {
                // First half of flip
                while (elapsedTime < flipDuration / 2) {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / (flipDuration / 2);
                    float smoothT = t * t * (3f - 2f * t); // Smooth easing
                    
                    float scaleX = Mathf.Lerp(1f, 0f, smoothT);
                    transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
                    
                    yield return null;
                }
                
                // Change face state at the midpoint
                isFaceDown = !isFaceDown;
                transform.rotation = Quaternion.Euler(isFaceDown ? 90f : -90f, 0f, 0f);
                
                // Second half of flip
                while (elapsedTime < flipDuration) {
                    elapsedTime += Time.deltaTime;
                    float t = (elapsedTime - flipDuration / 2) / (flipDuration / 2);
                    float smoothT = t * t * (3f - 2f * t); // Smooth easing
                    
                    float scaleX = Mathf.Lerp(0f, 1f, smoothT);
                    transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
                    
                    yield return null;
                }
                
                // Ensure final state is exact
                transform.rotation = Quaternion.Euler(isFaceDown ? 90f : -90f, 0f, 0f);
                transform.localScale = originalScale;
                
                // Small bounce effect
                Vector3 startPos = transform.position;
                Vector3 bouncePos = startPos + Vector3.up * 0.05f;
                
                float bounceTime = 0f;
                float bounceDuration = 0.1f;
                
                while (bounceTime < bounceDuration) {
                    bounceTime += Time.deltaTime;
                    float t = bounceTime / bounceDuration;
                    float smoothT = 1f - ((1f - t) * (1f - t)); // Smooth out
                    transform.position = Vector3.Lerp(startPos, bouncePos, smoothT);
                    yield return null;
                }
                
                transform.position = startPos;
            }
            finally {
                isBeingFlipped = false;
            }
        }

        public void OnDestroy() {
            if (flipCoroutine != null) {
                StopCoroutine(flipCoroutine);
                flipCoroutine = null;
            }
        }

        public void SetFaceDown(bool faceDown) {
            isFaceDown = faceDown;
            transform.rotation = Quaternion.Euler(faceDown ? 90f : -90f, 0f, 0f);
        }

        public bool IsFaceDown() {
            return isFaceDown;
        }

        public int GetValue() {
            switch(rank) {
                case "A":
                case "Ace": return 11;
                case "K":
                case "King":
                case "Q":
                case "Queen":
                case "J":
                case "Jack": return 10;
                default:
                    if (int.TryParse(rank, out int value)) {
                        return value;
                    }
                    Debug.LogWarning($"Unable to parse card rank: {rank}");
                    return 0;
            }
        }
    } 
}