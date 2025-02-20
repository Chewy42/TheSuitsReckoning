using UnityEngine;
using System.Collections;

namespace CardGame {
    public class Card : MonoBehaviour {
        public string suit;
        public string rank;
        public GameObject card;

        public IEnumerator FlipCard() {
            float flipDuration = 0.5f;
            float elapsedTime = 0f;
            
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(-90f, 0f, 0f); // Always flip to face up
            
            while (elapsedTime < flipDuration) {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / flipDuration;
                
                // Use smoothstep for more natural flipping motion
                float smoothT = t * t * (3f - 2f * t);
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, smoothT);
                
                yield return null;
            }
            
            // Ensure card ends up exactly face up
            transform.rotation = endRotation;
        }
    } 
}