using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CardGame{
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

    // Start is called before the first frame update
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
        tableSpotLight.spotAngle = 0;
        tableSpotLight.intensity = 0;
        
        // Start the smooth animation
        StartLightAnimation();
    }

    private void StartLightAnimation() {
        if (lightAnimationCoroutine != null) {
            StopCoroutine(lightAnimationCoroutine);
        }
        lightAnimationCoroutine = StartCoroutine(SmoothLightAnimation());
    }
    
    private IEnumerator SmoothLightAnimation() {
        float elapsed = 0f;
        
        while (elapsed < animationDuration && tableSpotLight != null) {
            try {
                elapsed += Time.deltaTime;
                
                // Use smooth step for more natural easing
                float t = elapsed / animationDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                // Apply the interpolated values
                tableSpotLight.spotAngle = Mathf.Lerp(0f, finalSpotAngle, smoothT);
                tableSpotLight.intensity = Mathf.Lerp(0f, finalIntensity, smoothT);
            }
            catch (System.Exception e) {
                Debug.LogError($"Error in light animation: {e.Message}");
                yield break;
            }
            
            yield return null;
        }
        
        // Ensure final values are set exactly
        if (tableSpotLight != null) {
            tableSpotLight.spotAngle = finalSpotAngle;
            tableSpotLight.intensity = finalIntensity;
        }
        
        lightAnimationCoroutine = null;
    }

    void OnDisable() {
        if (lightAnimationCoroutine != null) {
            StopCoroutine(lightAnimationCoroutine);
            lightAnimationCoroutine = null;
        }

        // Restore light to full values when disabled
        if (tableSpotLight != null) {
            tableSpotLight.spotAngle = originalSpotAngle;
            tableSpotLight.intensity = originalIntensity;
        }
    }

    void OnDestroy() {
        if (lightAnimationCoroutine != null) {
            StopCoroutine(lightAnimationCoroutine);
            lightAnimationCoroutine = null;
        }
    }
}
}
