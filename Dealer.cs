using UnityEngine;
using System.Collections;

namespace CardGame {
    public class Dealer : MonoBehaviour {
        [Tooltip("Initial delay before the first card is dealt")]
        public float initialDealDelay = 1f;
        
        [Tooltip("Delay in seconds between dealing each card (e.g., 0.35 for 350ms wait between cards)")]
        public float delayBetweenCards = 0.35f;

        [Tooltip("How fast the card moves to its position in seconds")]
        public float cardMoveSpeed = 0.15f;

        private bool hasInitialDelayPassed = false;
        private AudioManager audioManager;

        void Start() {
            audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null) {
                Debug.LogWarning("AudioManager not found in scene!");
            }
        }

        public IEnumerator DealCard(Card card, Transform destination, bool isFaceDown = false) {
            // Only apply initial delay for the first card
            if (!hasInitialDelayPassed) {
                yield return new WaitForSeconds(initialDealDelay);
                hasInitialDelayPassed = true;
            }

            // Play deal sound
            if (audioManager != null) {
                audioManager.PlayDealSound();
            }

            Vector3 startPos = card.transform.position;
            Vector3 endPos = destination.position;
            float elapsedTime = 0f;

            // Set proper rotation before movement starts
            card.transform.rotation = Quaternion.Euler(isFaceDown ? 90f : -90f, 0f, 0f);

            // Move card to position
            while (elapsedTime < cardMoveSpeed) {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / cardMoveSpeed;
                
                // Use smoothstep for more natural card movement
                float smoothT = t * t * (3f - 2f * t);
                card.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
                yield return null;
            }

            card.transform.position = endPos;
            card.transform.SetParent(destination);
        }

        public void ResetInitialDelay() {
            hasInitialDelayPassed = false;
            if (audioManager != null) {
                audioManager.ResetDealSoundSequence();
            }
        }
    }
}