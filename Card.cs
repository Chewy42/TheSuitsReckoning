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

        public IEnumerator MoveToPosition(Vector3 targetPosition, bool useWorldSpace = true)
        {
            Vector3 startPosition = useWorldSpace ? transform.position : transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < GameParameters.CARD_MOVE_DURATION)
            {
                float t = elapsed / GameParameters.CARD_MOVE_DURATION;
                if (useWorldSpace)
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                else
                    transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (useWorldSpace)
                transform.position = targetPosition;
            else
                transform.localPosition = targetPosition;
        }

        public IEnumerator ReturnToDeck(Vector3 deckPosition)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;
            
            while (elapsed < GameParameters.CARD_RETURN_DURATION)
            {
                float t = elapsed / GameParameters.CARD_RETURN_DURATION;
                transform.position = Vector3.Lerp(startPosition, deckPosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = deckPosition;
        }

        public IEnumerator FlipCard(bool instant = false)
        {
            if (isBeingFlipped) yield break;
            isBeingFlipped = true;

            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(new Vector3(isFaceDown ? -90f : 90f, 0f, 0f));
            
            if (instant)
            {
                transform.rotation = endRotation;
            }
            else
            {
                float elapsed = 0f;
                
                while (elapsed < GameParameters.CARD_FLIP_DURATION)
                {
                    float t = elapsed / GameParameters.CARD_FLIP_DURATION;
                    transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                transform.rotation = endRotation;
            }

            isFaceDown = !isFaceDown;
            isBeingFlipped = false;
        }

        public void OnDestroy() {
            if (flipCoroutine != null) {
                StopCoroutine(flipCoroutine);
                flipCoroutine = null;
            }
        }

        public void SetFaceDown(bool faceDown)
        {
            isFaceDown = faceDown;
            transform.rotation = Quaternion.Euler(new Vector3(faceDown ? 90f : -90f, 0f, 0f));
        }

        public void ResetState() {
            if (transform != null) {
                // Reset position and rotation relative to parent
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(GameParameters.FACE_DOWN_X_ROTATION, 0f, 0f);
            }
            isFaceDown = true;
        }

        void OnTransformParentChanged() {
            if (transform.parent == null) {
                // Card has been detached, maintain world position/rotation
                return;
            }
            
            if (transform.parent.GetComponent<Deck>() != null) {
                // Card is being returned to deck, ensure proper positioning
                ResetState();
            }
        }

        private void OnEnable() {
            ResetState();
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
