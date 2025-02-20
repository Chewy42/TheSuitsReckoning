using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup menuCanvas;
    public CanvasGroup creditsCanvas;  // Reference to the credits overlay

    [Header("Audio Settings")]
    public AudioSource musicSource;
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    public AudioClip menuMusic;

    private void Start()
    {
        SetupAudio();
        ShowMenu();
        
        // Make sure credits are hidden at start
        if (creditsCanvas != null)
        {
            creditsCanvas.alpha = 0f;
            creditsCanvas.interactable = false;
            creditsCanvas.blocksRaycasts = false;
        }
    }

    private void SetupAudio()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    private void ShowMenu()
    {
        if (menuCanvas != null)
        {
            menuCanvas.alpha = 1f;
            menuCanvas.interactable = true;
            menuCanvas.blocksRaycasts = true;
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameIntroCinematic");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void ShowCredits()
    {
        if (menuCanvas != null)
        {
            // Hide main menu
            menuCanvas.alpha = 0f;
            menuCanvas.interactable = false;
            menuCanvas.blocksRaycasts = false;
        }

        if (creditsCanvas != null)
        {
            // Show credits
            creditsCanvas.alpha = 1f;
            creditsCanvas.interactable = true;
            creditsCanvas.blocksRaycasts = true;
        }
    }

    public void HideCredits()
    {
        if (creditsCanvas != null)
        {
            // Hide credits
            creditsCanvas.alpha = 0f;
            creditsCanvas.interactable = false;
            creditsCanvas.blocksRaycasts = false;
        }

        if (menuCanvas != null)
        {
            // Show main menu
            menuCanvas.alpha = 1f;
            menuCanvas.interactable = true;
            menuCanvas.blocksRaycasts = true;
        }
    }
}
