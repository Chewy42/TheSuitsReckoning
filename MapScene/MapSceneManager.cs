using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSceneManager : MonoBehaviour
{
    // Assign your background music mp3 in the inspector.
    public AudioClip backgroundMusic;
    // Volume intensity for the background music.
    public float volumeIntensity = 1f;

    private AudioSource audioSource;

    void Start()
    {
        // Create and configure an AudioSource to play the background music.
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.volume = volumeIntensity;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.Play();
    }

    public void GoToBlackjack()
    {
        SceneManager.LoadScene("Blackjack");
    }
}
