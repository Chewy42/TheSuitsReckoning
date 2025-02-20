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

    // Start is called before the first frame update
    void Start()
    {
        // Find the spotlight if not assigned
        if (tableSpotLight == null) {
            for (int i = 0; i < transform.childCount; i++) {
                if (transform.GetChild(i).CompareTag("TableSpotLight")) {
                    tableSpotLight = transform.GetChild(i).GetComponent<Light>();
                    break;
                }
            }
        }
        
        if (tableSpotLight != null)
        {
            // Initialize light with 0 values
            tableSpotLight.spotAngle = 0;
            tableSpotLight.intensity = 0;
            
            // Start the smooth animation
            StartLightAnimation();
        }
        else
        {
            Debug.LogWarning("TableSpotLight not found.");
        }
    }

    private void StartLightAnimation() {
        if (lightAnimationCoroutine != null) {
            StopCoroutine(lightAnimationCoroutine);
        }
        lightAnimationCoroutine = StartCoroutine(SmoothLightAnimation());
    }
    
    private IEnumerator SmoothLightAnimation() {
        float elapsed = 0f;
        
        try {
            while (elapsed < animationDuration) {
                elapsed += Time.deltaTime;
                
                // Use smooth step for more natural easing
                float t = elapsed / animationDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                if (tableSpotLight != null) {
                    // Apply the interpolated values
                    tableSpotLight.spotAngle = Mathf.Lerp(0f, finalSpotAngle, smoothT);
                    tableSpotLight.intensity = Mathf.Lerp(0f, finalIntensity, smoothT);
                }
                
                yield return new WaitForEndOfFrame();
            }
            
            // Ensure final values are set exactly
            if (tableSpotLight != null) {
                tableSpotLight.spotAngle = finalSpotAngle;
                tableSpotLight.intensity = finalIntensity;
            }
        }
        finally {
            lightAnimationCoroutine = null;
        }
    }

    void OnDisable() {
        if (lightAnimationCoroutine != null) {
            StopCoroutine(lightAnimationCoroutine);
            lightAnimationCoroutine = null;
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
