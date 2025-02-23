using UnityEngine;
using System.Collections;

namespace CardGame {
    public class Dealer : MonoBehaviour {
        [Tooltip("Delay in seconds between dealing each card")]
        private float delayBetweenCards => GameParameters.DELAY_BETWEEN_CARDS;

        [Tooltip("Duration for a card to move to its position in seconds")]
        private float cardMoveDuration => GameParameters.CARD_MOVE_DURATION;

        private CardGame.AudioManager audioManager;
        private bool isDealingCard = false;

        void Start() {
            audioManager = FindFirstObjectByType<CardGame.AudioManager>();
            if (audioManager == null) {
                Debug.LogWarning("AudioManager not found in scene!");
            }
        }

        public IEnumerator DealCard(Card card, Transform targetSlot, bool isFaceDown)
        {
            if (isDealingCard || card == null || targetSlot == null)
            {
                Debug.LogError($"Cannot deal card. isDealingCard: {isDealingCard}, card null: {card == null}, slot null: {targetSlot == null}");
                yield break;
            }

            isDealingCard = true;

            // Play the card dealing sound
            audioManager?.PlaySound(SoundType.CardDeal);

            // Store initial position and set up card
            Vector3 startPos = card.transform.position;
            Vector3 endPos = targetSlot.position;
            card.transform.SetParent(null);

            // Set the initial rotation based on whether the card should be face down
            card.transform.rotation = Quaternion.Euler(isFaceDown ? 90f : -90f, 0f, 0f);

            float elapsedTime = 0f;
            while (elapsedTime < cardMoveDuration && card != null)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / cardMoveDuration;
                
                // Use linear interpolation for faster, snappier movement
                Vector3 arcPos = Vector3.Lerp(startPos, endPos, t);
                
                // Add a slight arc to the movement
                arcPos.y += Mathf.Sin(t * Mathf.PI) * GameParameters.CARD_MOVEMENT_ARC_HEIGHT;
                
                card.transform.position = arcPos;
                yield return null;
            }

            // Ensure final position and parent are set
            if (card != null && targetSlot != null)
            {
                card.transform.position = endPos;
                card.transform.SetParent(targetSlot);
            }

            isDealingCard = false;
            yield return new WaitForSeconds(delayBetweenCards);
        }

        public void ResetInitialDelay() {
            if (audioManager != null) {
                audioManager.ResetDealSoundSequence();
            }
        }
    }
}
