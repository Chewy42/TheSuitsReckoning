using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardGame{

public class MapSceneManager : MonoBehaviour
{
    // Assign your background music mp3 in the inspector.
    public AudioClip backgroundMusic;
    // Volume intensity for the background music.
    public float volumeIntensity = 1f;

    public Camera mainCamera;

    public Transform camPos1;
    public Transform camPos2;
    public CanvasGroup Overlay;

    private AudioSource audioSource;

    private float lerpTime = 0f;
    private bool isLerping = false;

    void Start()
    {
        // Create and configure an AudioSource to play the background music.
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.volume = volumeIntensity;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.Play();

        // Start camera movement from first position to second position
        LerpCamera();
        
        // Hide overlay initially
        if (Overlay != null)
        {
            Overlay.alpha = 0f;
            Overlay.interactable = false;
            Overlay.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (isLerping)
        {
            lerpTime += Time.deltaTime;
            float percentComplete = lerpTime / GameParameters.MAP_SCENE_CAMERA_LERP_DURATION;
            float smoothedPercent = Mathf.SmoothStep(0f, 1f, percentComplete);
            
            mainCamera.transform.position = Vector3.Lerp(camPos1.position, camPos2.position, smoothedPercent);
            mainCamera.transform.rotation = Quaternion.Lerp(camPos1.rotation, camPos2.rotation, smoothedPercent);
            
            if (percentComplete >= 1.0f)
            {
                isLerping = false;
                mainCamera.transform.position = camPos2.position; // Ensure we reach exactly the target
                mainCamera.transform.rotation = camPos2.rotation; // Ensure we reach exactly the target rotation
                
                // Show overlay when camera reaches position 2
                if (Overlay != null)
                {
                    Overlay.alpha = 1f;
                    Overlay.interactable = true;
                    Overlay.blocksRaycasts = true;
                }
            }
        }
    }

    public void GoToBlackjack()
    {
        SceneManager.LoadScene("Blackjack");
    }

    //lerp from pos 1 to pos 2 over time specified in GameParameters
    public void LerpCamera()
    {
        lerpTime = 0f;
        isLerping = true;
        mainCamera.transform.position = camPos1.position; // Ensure we start at the beginning
        mainCamera.transform.rotation = camPos1.rotation; // Ensure we start at the beginning rotation
    }
}
}
