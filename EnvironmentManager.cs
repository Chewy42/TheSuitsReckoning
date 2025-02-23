using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class EnvironmentManager : MonoBehaviour
    {
        public Light tableSpotLight;
        [Range(0.5f, 5f)]
        public float animationDuration = 2f;
        
        [Header("Light Settings")]
        public float finalSpotAngle = 58f;
        public float finalIntensity = 1f;

        private Coroutine lightAnimationCoroutine;
        private float originalSpotAngle;
        private float originalIntensity;

        void Start()
        {
            if (tableSpotLight == null)
            {
                tableSpotLight = GetComponentInChildren<Light>();
                if (tableSpotLight == null)
                {
                    Debug.LogError("No spotlight found in EnvironmentManager or its children!");
                    return;
                }
            }
            
            // Store original values for cleanup
            originalSpotAngle = finalSpotAngle;
            originalIntensity = finalIntensity;

            // Initialize light with 0 values
            InitializeLightValues();
            
            // Start the smooth animation
            StartLightAnimation();
        }

        private void InitializeLightValues()
        {
            if (tableSpotLight != null)
            {
                tableSpotLight.spotAngle = 0;
                tableSpotLight.intensity = 0;
            }
        }

        private void StartLightAnimation()
        {
            if (lightAnimationCoroutine != null)
            {
                StopCoroutine(lightAnimationCoroutine);
            }
            lightAnimationCoroutine = StartCoroutine(SmoothLightAnimation());
        }
        
        private IEnumerator SmoothLightAnimation()
        {
            if (tableSpotLight == null)
            {
                Debug.LogError("Cannot animate light - spotlight reference is missing");
                yield break;
            }

            float elapsed = 0f;
            float startSpotAngle = tableSpotLight.spotAngle;
            float startIntensity = tableSpotLight.intensity;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                
                // Use smooth step for more natural easing
                float t = elapsed / animationDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                // Apply the interpolated values
                tableSpotLight.spotAngle = Mathf.Lerp(startSpotAngle, finalSpotAngle, smoothT);
                tableSpotLight.intensity = Mathf.Lerp(startIntensity, finalIntensity, smoothT);
                
                yield return null;
            }

            // Ensure final values are set exactly
            tableSpotLight.spotAngle = finalSpotAngle;
            tableSpotLight.intensity = finalIntensity;

            lightAnimationCoroutine = null;
        }

        void OnDisable()
        {
            StopAllCoroutines();
            lightAnimationCoroutine = null;

            // Restore light to full values when disabled
            if (tableSpotLight != null)
            {
                tableSpotLight.spotAngle = originalSpotAngle;
                tableSpotLight.intensity = originalIntensity;
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            lightAnimationCoroutine = null;
        }
    }
}
