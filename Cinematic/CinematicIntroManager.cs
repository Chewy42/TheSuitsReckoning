using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CinematicIntroManager : MonoBehaviour
{
    public CinematicIntroCameraManager cameraManager;
    public float initialDelay = 2f;
    public float responseDelay = 2f; // Delay between Mom's line and Player's response
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip Mom_1;
    public AudioClip Player_1;
    public AudioClip Player_2;
    public AudioClip Mom_2;
    public AudioClip Mom_3;
    public AudioClip Player_3;
    public AudioClip Mom_4;
    public AudioClip Player_4;
    public AudioClip tvOffSound;
    public float tvOffDelay = 1f;
    public float player2Delay = 2f;
    public float mom3Delay = 2f;
    public float player3Delay = 2f;
    public float mom4Delay = 2f;
    public float player4Delay = 2f;  // Added delay for Player_4

    [Header("TV Screen Settings")]
    public MeshRenderer tvScreenRenderer;
    public Material offMaterial;

    [Header("Bowl Movement Settings")]
    public GameObject bowl;
    public Transform bowlStartPosition;
    public Transform bowlEndPosition;
    public float bowlMoveDuration = 1f;

    [Header("Final Camera Settings")]
    public float finalCameraMoveDuration = 3f;
    public float targetFOV = 2.5f;

    [Header("Final UI and Audio")]
    public CanvasGroup finalCanvas;
    public AudioClip finalAudio;
    public float finalAudioDuration = 10f;

    [Header("Flickering Lights")]
    public List<Light> flickeringLights = new List<Light>();
    [Tooltip("Minimum time between flickers")]
    public float minFlickerInterval = 0.3f;
    [Tooltip("Maximum time between flickers")]
    public float maxFlickerInterval = 2.0f;
    private Dictionary<Light, float> lightBaseIntensities = new Dictionary<Light, float>();
    private List<Coroutine> flickerCoroutines = new List<Coroutine>();

    private bool cinematicStarted = false;

    private void Start()
    {
        if (cameraManager == null)
        {
            Debug.LogError("Camera Manager is not assigned in CinematicIntroManager");
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize the final canvas as hidden
        if (finalCanvas != null)
        {
            finalCanvas.alpha = 0f;
            finalCanvas.interactable = false;
            finalCanvas.blocksRaycasts = false;
        }

        // Initialize flickering lights
        InitializeFlickeringLights();

        // Automatically start the cinematic sequence
        StartCinematicSequence();
    }

    private void InitializeFlickeringLights()
    {
        // Stop any existing coroutines
        if (flickerCoroutines.Count > 0)
        {
            foreach (var coroutine in flickerCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            flickerCoroutines.Clear();
        }

        lightBaseIntensities.Clear();

        // Start new flicker coroutines for each light with random initial delay
        foreach (var light in flickeringLights)
        {
            if (light != null)
            {
                lightBaseIntensities[light] = light.intensity;
                // Add random initial delay so lights don't start in sync
                float initialDelay = Random.Range(0f, maxFlickerInterval);
                flickerCoroutines.Add(StartCoroutine(FlickerLight(light, initialDelay)));
            }
        }
    }

    private IEnumerator FlickerLight(Light light, float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            float waitTime = Random.Range(minFlickerInterval, maxFlickerInterval);
            yield return new WaitForSeconds(waitTime);

            // 30% chance of doing a multi-flicker sequence
            bool multiFlicker = Random.value < 0.3f;
            
            if (multiFlicker)
            {
                int flickerCount = Random.Range(2, 5); // 2-4 rapid flickers
                for (int i = 0; i < flickerCount; i++)
                {
                    yield return StartCoroutine(SingleFlickerSequence(light, 0.05f)); // Faster flickers
                    yield return new WaitForSeconds(0.07f); // Brief pause between rapid flickers
                }
                // Small chance (20%) to stutter back on
                if (Random.value < 0.2f)
                {
                    yield return new WaitForSeconds(0.2f);
                    yield return StartCoroutine(SingleFlickerSequence(light, 0.1f));
                }
            }
            else
            {
                yield return StartCoroutine(SingleFlickerSequence(light, Random.Range(0.1f, 0.2f)));
            }
        }
    }

    private IEnumerator SingleFlickerSequence(Light light, float flickerDuration)
    {
        float originalIntensity = lightBaseIntensities[light];
        float minIntensity = originalIntensity * Random.Range(0.05f, 0.2f); // More dramatic dim
        float elapsedTime = 0f;

        // Fade down with varying speed
        float fadeDownDuration = flickerDuration * Random.Range(0.3f, 0.5f);
        while (elapsedTime < fadeDownDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDownDuration;
            // Sharper easing for more sudden drops
            float easeT = 1f - Mathf.Pow(1f - t, 2f);
            light.intensity = Mathf.Lerp(originalIntensity, minIntensity, easeT);
            yield return null;
        }

        // Fade up with varying speed
        elapsedTime = 0f;
        float fadeUpDuration = flickerDuration * Random.Range(0.5f, 0.7f);
        while (elapsedTime < fadeUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeUpDuration;
            // Cubic easing for smoother recovery
            float easeT = t * t * t;
            light.intensity = Mathf.Lerp(minIntensity, originalIntensity, easeT);
            yield return null;
        }

        // Ensure final intensity is correct
        light.intensity = originalIntensity;
    }

    private void OnDisable()
    {
        // Stop all flicker coroutines when disabled
        foreach (var coroutine in flickerCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        flickerCoroutines.Clear();

        // Reset light intensities
        foreach (var light in flickeringLights)
        {
            if (light != null && lightBaseIntensities.ContainsKey(light))
                light.intensity = lightBaseIntensities[light];
        }
    }

    public void StartCinematicSequence()
    {
        if (!cinematicStarted)
        {
            cinematicStarted = true;
            StartCoroutine(PlayCinematicSequence());
        }
    }

    private IEnumerator PlayCinematicSequence()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(initialDelay);

        // Play Mom's first line
        if (Mom_1 != null)
        {
            audioSource.clip = Mom_1;
            audioSource.Play();
            yield return new WaitForSeconds(Mom_1.length);
        }

        // Wait for response delay before Player_1
        yield return new WaitForSeconds(responseDelay);

        // Play Player's response
        if (Player_1 != null)
        {
            audioSource.clip = Player_1;
            audioSource.Play();
            yield return new WaitForSeconds(Player_1.length);
        }

        // Wait before playing TV off sound
        yield return new WaitForSeconds(tvOffDelay);

        // Play TV off sound and change screen material
        if (tvOffSound != null)
        {
            audioSource.clip = tvOffSound;
            audioSource.Play();
            
            if (tvScreenRenderer != null && offMaterial != null)
            {
                tvScreenRenderer.material = offMaterial;
            }
            yield return new WaitForSeconds(tvOffSound.length);
        }

        // Move camera to second position
        cameraManager.MoveCameraToPosition(1);

        // Wait for camera movement to complete
        while (cameraManager.IsMoving)
        {
            yield return null;
        }

        // Wait before Player 2's line
        yield return new WaitForSeconds(player2Delay);

        // Play Player 2's line
        if (Player_2 != null)
        {
            audioSource.clip = Player_2;
            audioSource.Play();
            yield return new WaitForSeconds(Player_2.length);
        }

        // Wait before Mom 3's line
        yield return new WaitForSeconds(mom3Delay);

        // Play Mom 3's line
        if (Mom_3 != null)
        {
            audioSource.clip = Mom_3;
            audioSource.Play();
            yield return new WaitForSeconds(Mom_3.length);
        }

        // Wait before Player 3's line
        yield return new WaitForSeconds(player3Delay);

        // Play Player 3's line with bowl movement
        if (Player_3 != null)
        {
            audioSource.clip = Player_3;
            audioSource.Play();

            // Start bowl movement simultaneously with Player_3 audio
            if (bowl != null && bowlStartPosition != null && bowlEndPosition != null)
            {
                StartCoroutine(MoveBowl());
            }
            yield return new WaitForSeconds(Player_3.length);
        }

        // Wait before Mom 4's line
        yield return new WaitForSeconds(mom4Delay);

        // Play Mom 4's line
        if (Mom_4 != null)
        {
            audioSource.clip = Mom_4;
            audioSource.Play();
            yield return new WaitForSeconds(Mom_4.length);
        }

        // Wait before Player 4's line
        yield return new WaitForSeconds(player4Delay);

        // Play Player 4's line and move camera simultaneously
        if (Player_4 != null)
        {
            audioSource.clip = Player_4;
            audioSource.Play();
            
            // Start camera movement at the same time
            cameraManager.MoveCameraToPositionWithFOV(2, Player_4.length, targetFOV);
            
            yield return new WaitForSeconds(Player_4.length);
        }

        // Wait for camera movement to complete, then show UI and play final audio
        while (cameraManager.IsMoving)
        {
            yield return null;
        }

        // Play the final audio for exactly 10 seconds
        if (finalAudio != null)
        {
            audioSource.clip = finalAudio;
            audioSource.Play();
            yield return new WaitForSeconds(1f);

            // Show the final canvas
            if (finalCanvas != null)
            {
                finalCanvas.alpha = 1f;
                finalCanvas.interactable = true;
                finalCanvas.blocksRaycasts = true;
            }
            yield return new WaitForSeconds(finalAudioDuration);
            audioSource.Stop();
        }

        // Load the main scene
        SceneManager.LoadScene("MapScene");

        

        
    }

    private IEnumerator MoveBowl()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = bowlStartPosition.position;
        Quaternion startRotation = bowlStartPosition.rotation;

        while (elapsedTime < bowlMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / bowlMoveDuration;
            
            // Use smoothstep interpolation for smoother movement
            float smoothT = t * t * (3f - 2f * t);
            
            bowl.transform.position = Vector3.Lerp(startPosition, bowlEndPosition.position, smoothT);
            bowl.transform.rotation = Quaternion.Lerp(startRotation, bowlEndPosition.rotation, smoothT);
            
            yield return null;
        }

        // Ensure bowl reaches exact end position
        bowl.transform.position = bowlEndPosition.position;
        bowl.transform.rotation = bowlEndPosition.rotation;
    }
}
