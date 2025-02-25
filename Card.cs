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
                
                // Handle special cases for 10 first since it's two digits
                if (cardInfo.StartsWith("10")) {
                    rank = "10";
                    suit = cardInfo.Substring(2);
                } else {
                    // For all other cases, first character is the rank
                    string rankChar = cardInfo.Substring(0, 1);
                    suit = cardInfo.Substring(1);
                    
                    // Map face cards and ace to their full names
                    rank = rankChar switch {
                        "K" => "King",
                        "Q" => "Queen",
                        "J" => "Jack",
                        "A" => "Ace",
                        _ => rankChar // Keep numeric values as is
                    };
                }
                
                Debug.Log($"Initialized card: {rank} of {suit}");
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
            // Wait for 0.15 seconds before starting the movement
            yield return new WaitForSeconds(0.15f);

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            
            while (elapsed < GameParameters.CARD_RETURN_DURATION)
            {
                float t = elapsed / GameParameters.CARD_RETURN_DURATION;
                transform.position = Vector3.Lerp(startPosition, deckPosition, t);
                // Keep the rotation consistent during the animation
                transform.rotation = startRotation;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = deckPosition;
            // Keep the current face up/down state instead of forcing face down
        }

        public IEnumerator FlipCard(bool instant = false)
        {
            if (isBeingFlipped)
            {
                Debug.LogWarning("Card is already being flipped, ignoring request");
                yield break;
            }
            
            isBeingFlipped = true;
            Debug.Log($"Starting to flip card: {rank} of {suit}, Currently face down: {isFaceDown}, Current rotation: {transform.rotation.eulerAngles}");

            // Determine the target rotation based on the current face down state
            float targetXRotation = isFaceDown ? -90f : 90f;  // Flip from face down to face up or vice versa
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(new Vector3(targetXRotation, 0f, 0f));
            
            Debug.Log($"Flipping from {startRotation.eulerAngles} to {endRotation.eulerAngles}");
            
            if (instant)
            {
                transform.rotation = endRotation;
                Debug.Log("Instant flip completed");
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
                
                // Ensure we reach the exact target rotation
                transform.rotation = endRotation;
                Debug.Log("Animated flip completed");
            }

            // Toggle the face down state
            isFaceDown = !isFaceDown;
            Debug.Log($"Card flipped. Now face down: {isFaceDown}, Final rotation: {transform.rotation.eulerAngles}");
            
            isBeingFlipped = false;
        }

        private IEnumerator FlipCardCoroutine() {
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startRotation = transform.rotation.eulerAngles;
            Vector3 endRotation = new Vector3(startRotation.x == 0 ? 180 : 0, startRotation.y, startRotation.z);

            while (elapsed < duration) {
                float t = elapsed / duration;
                transform.rotation = Quaternion.Euler(Vector3.Lerp(startRotation, endRotation, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = Quaternion.Euler(endRotation);
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
                transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            isFaceDown = true;
        }

        void OnTransformParentChanged() {
            if (transform.parent == null) {
                // Card has been detached, maintain world position/rotation
                return;
            }
            
            if (transform.parent.GetComponent<Deck>() != null) {
                // Card is being returned to deck, only reset position but keep orientation
                if (transform != null) {
                    // Reset position relative to parent but keep rotation
                    transform.localPosition = Vector3.zero;
                    // Don't reset rotation or face down state
                }
            }
        }

        private void OnEnable() {
            // Only reset state when the card is first enabled, not when returning to deck
            if (transform.parent != null && transform.parent.GetComponent<Deck>() != null) {
                // Only reset position but keep orientation
                if (transform != null) {
                    transform.localPosition = Vector3.zero;
                }
            }
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
