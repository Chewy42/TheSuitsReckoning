using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class GameIntroManager : MonoBehaviour
{
    public GameIntroCameraManager cameraManager;
    public float initialDelay = 2f;
    
    [Header("TV Screen Settings")]
    public MeshRenderer tvScreenRenderer;  // Reference to the TV screen's mesh renderer
    public VideoPlayer videoPlayer;        // Reference to the video player component
    private bool introStarted = false;
    private bool videoFinished = false;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource voiceoverSource;
    [SerializeField] private AudioSource musicSource;
    [Range(0f, 1f)]
    public float voiceoverVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    public AudioClip introVoiceover;
    [Space]
    public AudioClip backgroundMusic;      // The background music track
    public float musicDelay = 5f;         // Delay before music starts

    private void Start()
    {
        if (cameraManager == null)
        {
            Debug.LogError("Camera Manager is not assigned in GameIntroManager");
            return;
        }
        
        // Setup voiceover source
        if (voiceoverSource == null)
        {
            voiceoverSource = gameObject.AddComponent<AudioSource>();
        }
        voiceoverSource.volume = voiceoverVolume;

        // Setup background music source
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.volume = musicVolume;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            StartCoroutine(PlayBackgroundMusic());
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // Automatically start the intro sequence
        StartIntroSequence();
    }

    private void Update()
    {
        // Check for backspace key to skip intro
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            // Stop all coroutines to prevent any ongoing sequences
            StopAllCoroutines();
            
            // Stop video and audio
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
            if (voiceoverSource != null)
            {
                voiceoverSource.Stop();
            }
            if (musicSource != null)
            {
                musicSource.Stop();
            }
            
            // Switch to game menu immediately
            SwitchToGameMenu();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;
    }

    public void StartIntroSequence()
    {
        if (!introStarted)
        {
            introStarted = true;
            StartCoroutine(PlayIntroSequence());
        }
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("Main");
    }

    private void SwitchToGameMenu()
    {
        // Stop video player to free up resources
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        // Load the GameMenu scene
        SceneManager.LoadScene("GameMenu");
    }

    private IEnumerator PlayIntroSequence()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(initialDelay);

        // Start camera movement
        cameraManager.MoveCameraToPosition(0);

        // If we have a voiceover, play it
        if (introVoiceover != null)
        {
            voiceoverSource.clip = introVoiceover;
            voiceoverSource.volume = voiceoverVolume;
            voiceoverSource.Play();
        }
        else
        {
            Debug.LogWarning("No intro voiceover assigned");
        }

        // Wait for both video and camera movement to finish
        while (!videoFinished || cameraManager.IsMoving)
        {
            yield return null;
        }

        // Switch to the game menu scene after everything is done
        SwitchToGameMenu();
    }

    private IEnumerator PlayBackgroundMusic()
    {
        yield return new WaitForSeconds(musicDelay);
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    // Public methods to update volumes at runtime
    public void SetVoiceoverVolume(float volume)
    {
        voiceoverVolume = Mathf.Clamp01(volume);
        if (voiceoverSource != null)
        {
            voiceoverSource.volume = voiceoverVolume;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }
}
