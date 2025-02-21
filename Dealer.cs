using UnityEngine;
using System.Collections;

namespace CardGame {
    public class Dealer : MonoBehaviour {
        [Tooltip("Initial delay before the first card is dealt")]
        public float initialDealDelay = GameParameters.INITIAL_DEAL_DELAY;
        
        [Tooltip("Delay in seconds between dealing each card")]
        public float delayBetweenCards = GameParameters.DELAY_BETWEEN_CARDS;

        [Tooltip("How fast the card moves to its position in seconds")]
        public float cardMoveSpeed = GameParameters.CARD_MOVE_SPEED;

        private bool hasInitialDelayPassed = false;
        private CardGame.AudioManager audioManager;
        private bool isDealingCard = false;

        void Start() {
            audioManager = FindFirstObjectByType<CardGame.AudioManager>();
            if (audioManager == null) {
                Debug.LogWarning("AudioManager not found in scene!");
            }
        }

        public IEnumerator DealCard(Card card, Transform destination, bool isFaceDown = false) {
            if (isDealingCard) {
                Debug.LogWarning("Already dealing a card, waiting for completion");
                yield return new WaitUntil(() => !isDealingCard);
            }

            isDealingCard = true;

            try {
                if (card == null || destination == null) {
                    Debug.LogError("DealCard: Card or destination is null!");
                    yield break;
                }

                Debug.Log($"Dealing card {card.name} to slot {destination.name}, face down: {isFaceDown}");

                // Only apply initial delay for the first card
                if (!hasInitialDelayPassed) {
                    yield return new WaitForSeconds(initialDealDelay);
                    hasInitialDelayPassed = true;
                }

                if (audioManager != null) {
                    audioManager.PlayDealSound();
                }

                Vector3 startPos = card.transform.position;
                Vector3 endPos = destination.position;
                float elapsedTime = 0f;

                // Detach from current parent if any
                card.transform.SetParent(null);
                
                // Set proper rotation before movement starts
                // -90 on X axis for face up cards as specified in requirements
                card.transform.rotation = Quaternion.Euler(isFaceDown ? 90f : -90f, 0f, 0f);

                while (elapsedTime < cardMoveSpeed && card != null) {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / cardMoveSpeed;
                    
                    // Use linear interpolation for faster, snappier movement
                    Vector3 arcPos = Vector3.Lerp(startPos, endPos, t);
                    arcPos.y += Mathf.Sin(t * Mathf.PI) * GameParameters.CARD_MOVEMENT_ARC_HEIGHT;
                    
                    // Simplified rotation
                    float rotationAngle = Mathf.Sin(t * Mathf.PI) * 10f;
                    card.transform.rotation = Quaternion.Euler(
                        isFaceDown ? 90f : -90f,
                        rotationAngle,
                        0f
                    );
                    
                    card.transform.position = arcPos;
                    
                    yield return null;
                }

                if (card != null && destination != null) {
                    // Ensure final position and rotation are exact
                    card.transform.position = endPos;
                    card.transform.rotation = Quaternion.Euler(isFaceDown ? 90f : -90f, 0f, 0f);
                    
                    // Set parent and verify
                    card.transform.SetParent(destination);
                    if (card.transform.parent != destination) {
                        Debug.LogError($"Failed to parent card {card.name} to slot {destination.name}!");
                    } else {
                        Debug.Log($"Successfully dealt card {card.name} to slot {destination.name}");
                    }
                }

                yield return new WaitForSeconds(delayBetweenCards);
            }
            finally {
                isDealingCard = false;
            }
        }

        public void ResetInitialDelay() {
            hasInitialDelayPassed = false;
            if (audioManager != null) {
                audioManager.ResetDealSoundSequence();
            }
        }
    }
}
