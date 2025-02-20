using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame {

public class AudioManager : MonoBehaviour {
    [Header("Card Dealing Sounds")]
    [SerializeField] private List<AudioClip> dealSounds;  // List of dealing sound clips
    [Range(0f, 1f)]
    [SerializeField] private float dealVolume = 0.8f;
    private int lastDealSoundIndex = -1;  // Track the last played deal sound
    
    [Header("Card Shuffle Sounds")]
    [SerializeField] private List<AudioClip> shuffleSounds;
    [Range(0f, 1f)]
    [SerializeField] private float shuffleVolume = 0.8f;
    
    [Header("Button Sounds")]
    [SerializeField] private AudioClip hitButtonSound;
    [SerializeField] private AudioClip standButtonSound;
    [Range(0f, 1f)]
    [SerializeField] private float buttonVolume = 0.7f;

    [Header("Game State Sounds")]
    [SerializeField] private List<AudioClip> yourTurnSounds;
    [SerializeField] private List<AudioClip> endTurnSounds;
    [SerializeField] private List<AudioClip> nextRoundSounds;
    [Range(0f, 1f)]
    [SerializeField] private float gameStateVolume = 0.8f;
    
    [Header("Background Ambience")]
    [SerializeField] private List<AudioClip> backgroundLoops;
    [Range(0f, 1f)]
    [SerializeField] private float backgroundVolume = 0.3f;

    [Header("Dealer Voice Lines")]
    [SerializeField] private List<AudioClip> badMoveVoiceLines;
    [SerializeField] private List<AudioClip> goodMoveVoiceLines;
    [SerializeField] private List<AudioClip> genericVoiceLines;
    [SerializeField] private AudioClip winVoiceLine;
    [SerializeField] private AudioClip loseVoiceLine;
    [Range(0f, 1f)]
    [SerializeField] private float voiceVolume = 0.8f;

    [Header("Generic Voice Line Settings")]
    public float minGenericVoiceDelay = 5f;
    public float maxGenericVoiceDelay = 15f;

    [Header("Card Return Sounds")]
    [SerializeField] private List<AudioClip> cardReturnSounds;
    [Range(0f, 1f)]
    [SerializeField] private float returnVolume = 0.7f;
    
    [Header("Target Randomization Sounds")]
    [SerializeField] private AudioClip randomTargetSound1;
    [SerializeField] private AudioClip randomTargetSound2;

    private AudioSource cardAudioSource;
    private AudioSource voiceAudioSource;
    private AudioSource backgroundAudioSource;
    private Coroutine genericVoiceCoroutine;
    private bool isPlayerTurn = false;

    private bool isDealerTalking => voiceAudioSource != null && voiceAudioSource.isPlaying;
    private bool isShuffling = false;

    private AudioClip currentNextRoundSound;

    private void OnValidate() {
        // Update volumes immediately when changed in inspector, even during play mode
        if (cardAudioSource != null) {
            cardAudioSource.volume = dealVolume;
        }
        
        if (voiceAudioSource != null) {
            voiceAudioSource.volume = voiceVolume;
        }
        
        if (backgroundAudioSource != null) {
            backgroundAudioSource.volume = backgroundVolume;
        }
    }

    // Method to apply all volume settings at once
    public void ApplyAllVolumeSettings() {
        if (cardAudioSource != null) {
            cardAudioSource.volume = dealVolume;
        }
        
        if (voiceAudioSource != null) {
            // Only update voice source volume if it's not playing a game state sound
            if (voiceAudioSource.clip == null || 
                (!nextRoundSounds.Contains(voiceAudioSource.clip) && 
                 !endTurnSounds.Contains(voiceAudioSource.clip) && 
                 !yourTurnSounds.Contains(voiceAudioSource.clip))) {
                voiceAudioSource.volume = voiceVolume;
            }
        }
        
        if (backgroundAudioSource != null) {
            backgroundAudioSource.volume = backgroundVolume;
        }

        // Update game state volume if currently playing
        AudioSource gameStateSource = GetGameStateAudioSource();
        if (gameStateSource != null && gameStateSource.clip != null &&
            (nextRoundSounds.Contains(gameStateSource.clip) || 
             endTurnSounds.Contains(gameStateSource.clip) || 
             yourTurnSounds.Contains(gameStateSource.clip))) {
            gameStateSource.volume = gameStateVolume;
        }
    }

    void Start() {
        // One audio source for card sounds
        cardAudioSource = gameObject.AddComponent<AudioSource>();
        
        // Separate audio source for voice lines
        voiceAudioSource = gameObject.AddComponent<AudioSource>();
        
        // Background audio source for ambient loops
        backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        backgroundAudioSource.loop = true;
        
        // Apply all volume settings on start
        ApplyAllVolumeSettings();
        
        StartBackgroundMusic();
    }

    private void StartBackgroundMusic() {
        if (backgroundLoops != null && backgroundLoops.Count > 0) {
            int randomIndex = Random.Range(0, backgroundLoops.Count);
            backgroundAudioSource.clip = backgroundLoops[randomIndex];
            backgroundAudioSource.Play();
        }
    }

    public void UpdateBackgroundVolume(float volume) {
        backgroundVolume = Mathf.Clamp01(volume);
        if (backgroundAudioSource != null) {
            backgroundAudioSource.volume = backgroundVolume;
        }
    }

    public void PlayDealSound() {
        if (cardAudioSource != null && dealSounds != null && dealSounds.Count > 0) {
            cardAudioSource.pitch = 1f;
            int newIndex;
            if (dealSounds.Count == 1) {
                newIndex = 0;
            } else {
                // Keep generating a new random index until we get one different from the last played
                do {
                    newIndex = Random.Range(0, dealSounds.Count);
                } while (newIndex == lastDealSoundIndex);
            }
            
            lastDealSoundIndex = newIndex;
            cardAudioSource.volume = dealVolume;
            cardAudioSource.clip = dealSounds[newIndex];
            cardAudioSource.Play();
        }
    }

    public void ResetDealSoundSequence() {
        lastDealSoundIndex = -1;
    }

    public void PlayRandomFromList(List<AudioClip> clipList) {
        if (clipList == null || clipList.Count == 0 || voiceAudioSource == null) return;
        
        // Skip if dealer is already talking
        if (isDealerTalking) return;
        
        int randomIndex = Random.Range(0, clipList.Count);
        voiceAudioSource.volume = voiceVolume;
        voiceAudioSource.clip = clipList[randomIndex];
        voiceAudioSource.Play();
    }

    public void PlayBadMoveVoiceLine() {
        PlayRandomFromList(badMoveVoiceLines);
    }

    public void PlayGoodMoveVoiceLine() {
        PlayRandomFromList(goodMoveVoiceLines);
    }

    public void PlayGenericVoiceLine() {
        PlayRandomFromList(genericVoiceLines);
    }

    public void PlayWinVoiceLine() {
        if (voiceAudioSource != null && winVoiceLine != null) {
            // Always play win/lose lines even if talking (will interrupt current speech)
            voiceAudioSource.clip = winVoiceLine;
            voiceAudioSource.Play();
        }
    }

    public void PlayLoseVoiceLine() {
        if (voiceAudioSource != null && loseVoiceLine != null) {
            // Always play win/lose lines even if talking (will interrupt current speech)
            voiceAudioSource.clip = loseVoiceLine;
            voiceAudioSource.Play();
        }
    }

    public void StartPlayerTurn() {
        isPlayerTurn = true;
        PlayYourTurnSound();  // Play the "your turn" sound first
        
        // Wait for the your turn sound to finish before starting generic voice lines
        if (genericVoiceCoroutine != null) {
            StopCoroutine(genericVoiceCoroutine);
        }
        genericVoiceCoroutine = StartCoroutine(StartGenericVoicesAfterYourTurn());
    }

    private void PlayYourTurnSound() {
        if (yourTurnSounds != null && yourTurnSounds.Count > 0 && voiceAudioSource != null) {
            int randomIndex = Random.Range(0, yourTurnSounds.Count);
            voiceAudioSource.clip = yourTurnSounds[randomIndex];
            voiceAudioSource.volume = gameStateVolume; // Set the correct volume
            voiceAudioSource.Play();
        }
    }

    private IEnumerator StartGenericVoicesAfterYourTurn() {
        // Wait for the "your turn" voice line to finish if it's playing
        if (isDealerTalking) {
            yield return new WaitUntil(() => !isDealerTalking);
        }
        
        // Start the regular generic voice lines routine
        while (isPlayerTurn) {
            float delay = Random.Range(minGenericVoiceDelay, maxGenericVoiceDelay);
            yield return new WaitForSeconds(delay);
            if (isPlayerTurn && !isDealerTalking) { // Check if dealer isn't talking
                PlayGenericVoiceLine();
            }
        }
    }

    public void EndPlayerTurn() {
        isPlayerTurn = false;
        PlayEndTurnSound();
        if (genericVoiceCoroutine != null) {
            StopCoroutine(genericVoiceCoroutine);
            genericVoiceCoroutine = null;
        }
    }

    public void PlayEndTurnSound() {
        if (endTurnSounds != null && endTurnSounds.Count > 0 && voiceAudioSource != null) {
            int randomIndex = Random.Range(0, endTurnSounds.Count);
            voiceAudioSource.volume = gameStateVolume;
            voiceAudioSource.clip = endTurnSounds[randomIndex];
            voiceAudioSource.Play();
        }
    }

    private void PlayGameStateSound(AudioClip clip) {
        if (clip != null && voiceAudioSource != null) {
            voiceAudioSource.volume = gameStateVolume;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }
    }

    public void UpdateGameStateVolume(float volume) {
        gameStateVolume = Mathf.Clamp01(volume);
        // Immediately update volume if currently playing a game state sound
        AudioSource gameStateSource = GetGameStateAudioSource();
        if (gameStateSource != null && 
            gameStateSource.clip != null && 
            (nextRoundSounds.Contains(gameStateSource.clip) || 
             endTurnSounds.Contains(gameStateSource.clip) || 
             yourTurnSounds.Contains(gameStateSource.clip))) {
            gameStateSource.volume = gameStateVolume;
        }
    }

    public void PlayNextRoundSound() {
        if (nextRoundSounds != null && nextRoundSounds.Count > 0 && voiceAudioSource != null) { // Changed to use voiceAudioSource
            int randomIndex = Random.Range(0, nextRoundSounds.Count);
            currentNextRoundSound = nextRoundSounds[randomIndex];
            voiceAudioSource.volume = gameStateVolume; // Set the correct volume
            voiceAudioSource.clip = currentNextRoundSound;
            voiceAudioSource.Play();
        }
    }

    public float GetNextRoundSoundDuration() {
        if (currentNextRoundSound != null) {
            return currentNextRoundSound.length;
        }
        return 0f;
    }

    public void StartGame() {
        PlayNextRoundSound();
    }

    private IEnumerator PlayGenericVoiceLinesRoutine() {
        while (isPlayerTurn) {
            float delay = Random.Range(minGenericVoiceDelay, maxGenericVoiceDelay);
            yield return new WaitForSeconds(delay);
            if (isPlayerTurn && !isDealerTalking) { // Check if dealer isn't talking
                PlayGenericVoiceLine();
            }
        }
    }

    // Evaluates if a player's move was good based on their current hand value
    public void EvaluatePlayerMove(int currentHandValue, bool isHitAction) {
        // Skip evaluation voice lines if dealer is already talking
        if (isDealerTalking) return;

        if (isHitAction) {
            // If player hits when they have 17 or higher, it's generally a bad move
            if (currentHandValue >= 17) {
                PlayBadMoveVoiceLine();
            }
            // If player hits when they have 11 or lower, it's generally a good move
            else if (currentHandValue <= 11) {
                PlayGoodMoveVoiceLine();
            }
        } else { // Stand action
            // If player stands with 16 or lower, it's generally a bad move
            if (currentHandValue <= 16) {
                PlayBadMoveVoiceLine();
            }
            // If player stands with 17 or higher, it's generally a good move
            else if (currentHandValue >= 17) {
                PlayGoodMoveVoiceLine();
            }
        }
    }

    public bool IsShuffling() => isShuffling;

    public IEnumerator PlayShuffleSounds() {
        isShuffling = true;
        
        if (shuffleSounds != null && shuffleSounds.Count > 0 && cardAudioSource != null) {
            foreach (AudioClip shuffleClip in shuffleSounds) {
                if (shuffleClip != null) {
                    cardAudioSource.volume = shuffleVolume;
                    cardAudioSource.clip = shuffleClip;
                    cardAudioSource.Play();
                    
                    // Wait for the current shuffle sound to finish
                    yield return new WaitForSeconds(shuffleClip.length);
                }
            }
        }
        
        isShuffling = false;
    }

    public void PlayHitButtonSound() {
        if (hitButtonSound != null && cardAudioSource != null) {
            SafePlayOneShot(cardAudioSource, hitButtonSound, buttonVolume);
        }
    }

    public void PlayStandButtonSound() {
        if (standButtonSound != null && cardAudioSource != null) {
            SafePlayOneShot(cardAudioSource, standButtonSound, buttonVolume);
        }
    }

    public void PlayCardReturnSound() {
        if (cardReturnSounds != null && cardReturnSounds.Count > 0 && cardAudioSource != null) {
            int randomIndex = Random.Range(0, cardReturnSounds.Count);
            SafePlayOneShot(cardAudioSource, cardReturnSounds[randomIndex], returnVolume);
        }
    }

    public void PlayRandomTargetSound1() {
        if (cardAudioSource != null && randomTargetSound1 != null) {
            // Add slight pitch variation for more dynamic feel
            cardAudioSource.pitch = Random.Range(0.95f, 1.05f);
            SafePlayOneShot(cardAudioSource, randomTargetSound1, dealVolume);
        }
    }
    
    public void PlayRandomTargetSound2() {
        if (cardAudioSource != null && randomTargetSound2 != null) {
            // Reset pitch for final sound and play at slightly higher volume
            cardAudioSource.pitch = 1f;
            SafePlayOneShot(cardAudioSource, randomTargetSound2, dealVolume * 1.2f);
        }
    }

    // Add methods to update volumes at runtime if needed
    public void UpdateDealVolume(float volume) {
        dealVolume = Mathf.Clamp01(volume);
        if (cardAudioSource != null) {
            cardAudioSource.volume = dealVolume;
        }
    }

    public void UpdateShuffleVolume(float volume) {
        shuffleVolume = Mathf.Clamp01(volume);
        if (cardAudioSource != null) {
            cardAudioSource.volume = shuffleVolume;
        }
    }

    public void UpdateButtonVolume(float volume) {
        buttonVolume = Mathf.Clamp01(volume);
        if (cardAudioSource != null) {
            cardAudioSource.volume = buttonVolume;
        }
    }

    public void UpdateVoiceVolume(float volume) {
        voiceVolume = Mathf.Clamp01(volume);
        if (voiceAudioSource != null) {
            voiceAudioSource.volume = voiceVolume;
        }
    }

    public void UpdateBackgroundVolume() {
        if (backgroundAudioSource != null) {
            backgroundAudioSource.volume = backgroundVolume;
        }
    }

    public void UpdateReturnVolume(float volume) {
        returnVolume = Mathf.Clamp01(volume);
    }

    void OnDestroy() {
        // Clean up audio sources
        if (cardAudioSource != null) {
            cardAudioSource.Stop();
            Destroy(cardAudioSource);
        }
        
        if (voiceAudioSource != null) {
            voiceAudioSource.Stop();
            Destroy(voiceAudioSource);
        }
        
        if (backgroundAudioSource != null) {
            backgroundAudioSource.Stop();
            Destroy(backgroundAudioSource);
        }

        if (genericVoiceCoroutine != null) {
            StopCoroutine(genericVoiceCoroutine);
            genericVoiceCoroutine = null;
        }
    }

    void OnDisable() {
        // Stop all sounds when disabled
        if (cardAudioSource != null) cardAudioSource.Stop();
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        if (backgroundAudioSource != null) backgroundAudioSource.Stop();
        
        if (genericVoiceCoroutine != null) {
            StopCoroutine(genericVoiceCoroutine);
            genericVoiceCoroutine = null;
        }
    }

    private void SafePlayOneShot(AudioSource source, AudioClip clip, float volume) {
        if (source != null && clip != null && source.isActiveAndEnabled) {
            source.PlayOneShot(clip, volume);
        }
    }

    // New public method to get voice clip duration
    public float GetCurrentVoiceClipDuration() {
        return voiceAudioSource != null && voiceAudioSource.clip != null ? voiceAudioSource.clip.length : 0f;
    }

    // New public method to check if voice is playing
    public bool IsVoicePlaying() {
        return voiceAudioSource != null && voiceAudioSource.isPlaying;
    }

    private AudioSource GetGameStateAudioSource() {
        // Use voice audio source for all game state sounds
        return voiceAudioSource;
    }
}
}
