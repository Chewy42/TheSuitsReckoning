using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace CardGame
{
    public enum SoundType
    {
        Deal,
        CardDeal,
        CardReturn,
        CardFlip,
        Shuffle,
        RandomizeTarget1,
        RandomizeTarget2,
        RoundStart,
        PlayerTurn,
        DealerThinking,
        Win,
        Lose,
        ButtonClick,
        ButtonHover,
    }

    public class AudioManager : MonoBehaviour
    {
        [Header("Card Sounds")]
        public AudioClip dealSound;
        public AudioClip cardReturnSound;
        public AudioClip cardFlipSound;
        public AudioClip[] shuffleSounds;

        [Header("Round Progress")]
        public AudioClip randomizeSound1;
        public AudioClip randomizeSound2;
        public AudioClip roundStartSound;
        public float roundStartSoundDelay = 0.3f;

        [Header("Game Status")]
        public AudioClip[] winVoiceLines;
        public AudioClip[] loseVoiceLines;
        public AudioClip playerTurnSound;
        public AudioClip[] dealerThinkingSounds;

        [Header("UI Sounds")]
        public AudioClip buttonClickSound;
        public AudioClip buttonHoverSound;
        [Range(0f, 1f)] public float uiSoundVolume = 0.5f;

        [Header("Dealer Voice Lines")]
        public AudioClip[] genericVoiceClips;
        public AudioClip[] goodMoveVoiceClips;
        public AudioClip[] badMoveVoiceClips;
        public AudioClip shockedVoiceLine;

        [Header("Audio Settings")]
        [Range(0f, 1f)] public float cardSoundVolume = 0.7f;
        [Range(0f, 1f)] public float voiceLineVolume = 1f;
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgMusicVolume = 0.5f;
        private const float MIN_SOUND_INTERVAL = 0.05f;
        private const float MIN_VOICE_LINE_INTERVAL = 0.3f;
        private float minGenericVoiceLineInterval = GameParameters.MIN_GENERIC_VOICE_LINE_INTERVAL;
        private float maxGenericVoiceLineInterval = GameParameters.MAX_GENERIC_VOICE_LINE_INTERVAL;
        private float nextGenericVoiceLineTime;

        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private AudioSource audioSource;
        private AudioSource voiceSource;
        public AudioSource bgAudioSource;
        private AudioClip currentVoiceLine;
        private int currentShuffleIndex = 0;
        private const int REQUIRED_SHUFFLES = 3;

        private Dictionary<SoundType, float> lastPlayTimes = new Dictionary<SoundType, float>();
        private Dictionary<AudioClip, float> lastVoiceLineTimes = new Dictionary<AudioClip, float>();
        private Queue<AudioClip> voiceLineQueue = new Queue<AudioClip>();
        private Queue<(AudioClip clip, float delay)> soundQueue = new Queue<(AudioClip clip, float delay)>();

        private List<AudioSource> additionalAudioSources = new List<AudioSource>();

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;

            if (bgAudioSource == null)
            {
                GameObject bgAudioObj = GameObject.Find("BGAudio");
                if (bgAudioObj != null)
                {
                    bgAudioSource = bgAudioObj.GetComponent<AudioSource>();
                    if (bgAudioSource != null)
                    {
                        Debug.Log("Found BGAudio source");
                    }
                }
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
                if (gameManager == null)
                {
                    Debug.LogWarning("AudioManager: GameManager not found");
                }
            }
            
            UpdateAudioSourceVolumes();
            
            SetNextGenericVoiceLineTime();
        }

        private void Update()
        {
            if (Time.time >= nextGenericVoiceLineTime && (gameManager == null || !gameManager.IsPausedForTutorial()))
            {
                PlayRandomGenericVoiceLine();
                SetNextGenericVoiceLineTime();
            }
        }

        private void SetNextGenericVoiceLineTime()
        {
            float interval = Random.Range(minGenericVoiceLineInterval, maxGenericVoiceLineInterval);
            nextGenericVoiceLineTime = Time.time + interval;
        }

        public void PlayDealSound()
        {
            if (dealSound != null)
            {
                QueueSound(dealSound);
            }
        }

        public void PlayCardReturnSound()
        {
            if (cardReturnSound != null)
            {
                QueueSound(cardReturnSound);
            }
        }

        public void PlayCardFlipSound()
        {
            if (cardFlipSound != null)
            {
                QueueSound(cardFlipSound);
            }
        }

        public void PlayDealerThinkingSound()
        {
            if (dealerThinkingSounds != null && dealerThinkingSounds.Length > 0)
            {
                QueueSound(dealerThinkingSounds[Random.Range(0, dealerThinkingSounds.Length)]);
            }
        }

        public void PlayRandomTargetSound1()
        {
            if (randomizeSound1 != null)
            {
                QueueSound(randomizeSound1);
            }
        }

        public void PlayRandomTargetSound2()
        {
            if (randomizeSound2 != null)
            {
                QueueSound(randomizeSound2);
            }
        }

        public void PlayWinVoiceLine()
        {
            if (gameManager != null && gameManager.IsPausedForTutorial())
            {
                return;
            }

            if (winVoiceLines != null && winVoiceLines.Length > 0)
            {
                int randomIndex = Random.Range(0, winVoiceLines.Length);
                PlayVoiceLine(winVoiceLines[randomIndex]);
            }
        }

        public void PlayLoseVoiceLine()
        {
            if (gameManager != null && gameManager.IsPausedForTutorial())
            {
                return;
            }

            if (loseVoiceLines != null && loseVoiceLines.Length > 0)
            {
                int randomIndex = Random.Range(0, loseVoiceLines.Length);
                PlayVoiceLine(loseVoiceLines[randomIndex]);
            }
        }

        public float GetCurrentVoiceClipDuration()
        {
            return currentVoiceLine != null ? currentVoiceLine.length : 1f;
        }

        public void ResetDealSoundSequence()
        {
            currentShuffleIndex = 0;
        }

        public IEnumerator PlayShuffleSounds()
        {
            currentShuffleIndex = 0;
            while (
                currentShuffleIndex < REQUIRED_SHUFFLES
                && shuffleSounds != null
                && shuffleSounds.Length > 0
            )
            {
                if (audioSource != null)
                {
                    int index = Random.Range(0, shuffleSounds.Length);
                    audioSource.PlayOneShot(shuffleSounds[index]);
                    yield return new WaitForSeconds(shuffleSounds[index].length);
                }
                currentShuffleIndex++;
            }
        }

        private IEnumerator ProcessVoiceQueue()
        {
            while (voiceLineQueue.Count > 0)
            {
                var clip = voiceLineQueue.Dequeue();
                if (clip != null && !voiceSource.isPlaying)
                {
                    voiceSource.volume = voiceLineVolume * masterVolume;
                    voiceSource.clip = clip;
                    voiceSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }
        }

        private IEnumerator ProcessSoundQueue()
        {
            while (soundQueue.Count > 0)
            {
                var (clip, delay) = soundQueue.Dequeue();
                if (clip != null)
                {
                    audioSource.volume = cardSoundVolume * masterVolume;
                    audioSource.PlayOneShot(clip);
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        private void QueueSound(AudioClip clip, float delay = 0f)
        {
            if (clip == null || audioSource == null) return;
            
            soundQueue.Enqueue((clip, delay));
            StartCoroutine(ProcessSoundQueue());
        }

        public void PlayVoiceLine(AudioClip voiceClip)
        {
            if (voiceClip == null || voiceSource == null) return;
            
            voiceLineQueue.Enqueue(voiceClip);
            StartCoroutine(ProcessVoiceQueue());
        }

        public void PlayRandomGenericVoiceLine()
        {
            if (gameManager != null && gameManager.IsPausedForTutorial())
            {
                return;
            }

            if (genericVoiceClips != null && genericVoiceClips.Length > 0)
            {
                int randomIndex = Random.Range(0, genericVoiceClips.Length);
                PlayVoiceLine(genericVoiceClips[randomIndex]);
            }
        }

        public void PlayShockedVoiceLine()
        {
            if (gameManager != null && gameManager.IsPausedForTutorial())
            {
                return;
            }

            currentVoiceLine = shockedVoiceLine;
            PlayVoiceLine(shockedVoiceLine);
        }

        public void PlayRandomVoiceLine(AudioClip[] clips)
        {
            if (clips != null && clips.Length > 0)
            {
                int randomIndex = Random.Range(0, clips.Length);
                PlayVoiceLine(clips[randomIndex]);
            }
        }

        public void PlaySound(SoundType soundType)
        {
            if (lastPlayTimes.ContainsKey(soundType))
            {
                if (Time.time - lastPlayTimes[soundType] < MIN_SOUND_INTERVAL)
                    return;
            }
            lastPlayTimes[soundType] = Time.time;

            try
            {
                switch (soundType)
                {
                    case SoundType.Deal:
                        QueueSound(dealSound);
                        break;
                    case SoundType.CardDeal:
                        QueueSound(dealSound);
                        break;
                    case SoundType.CardReturn:
                        QueueSound(cardReturnSound);
                        break;
                    case SoundType.CardFlip:
                        QueueSound(cardFlipSound);
                        break;
                    case SoundType.Shuffle:
                        StartCoroutine(PlayShuffleSounds());
                        break;
                    case SoundType.RandomizeTarget1:
                        QueueSound(randomizeSound1);
                        break;
                    case SoundType.RandomizeTarget2:
                        QueueSound(randomizeSound2);
                        break;
                    case SoundType.RoundStart:
                        if (roundStartSound != null)
                        {
                            QueueSound(roundStartSound, roundStartSoundDelay);
                        }
                        break;
                    case SoundType.PlayerTurn:
                        if (playerTurnSound != null)
                        {
                            QueueSound(playerTurnSound);
                        }
                        break;
                    case SoundType.DealerThinking:
                        if (dealerThinkingSounds != null && dealerThinkingSounds.Length > 0)
                        {
                            QueueSound(dealerThinkingSounds[Random.Range(0, dealerThinkingSounds.Length)]);
                        }
                        break;
                    case SoundType.Win:
                        PlayWinVoiceLine();
                        break;
                    case SoundType.Lose:
                        PlayLoseVoiceLine();
                        break;
                    case SoundType.ButtonClick:
                        PlayButtonClickSound();
                        break;
                    case SoundType.ButtonHover:
                        PlayButtonHoverSound();
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error playing sound {soundType}: {e.Message}");
            }
        }

        public void StopAllSounds()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            foreach (var source in additionalAudioSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAudioSourceVolumes();
        }

        public void SetDialogueVolume(float volume)
        {
            voiceLineVolume = Mathf.Clamp01(volume);
            if (voiceSource != null)
            {
                voiceSource.volume = voiceLineVolume * masterVolume;
            }
        }

        public void SetSoundEffectsVolume(float volume)
        {
            cardSoundVolume = Mathf.Clamp01(volume);
            if (audioSource != null)
            {
                audioSource.volume = cardSoundVolume * masterVolume;
            }
        }

        public void SetBGMusicVolume(float volume)
        {
            bgMusicVolume = Mathf.Clamp01(volume);
            if (bgAudioSource != null)
            {
                bgAudioSource.volume = bgMusicVolume * masterVolume;
            }
        }

        public void SetUISoundVolume(float volume)
        {
            uiSoundVolume = Mathf.Clamp01(volume);
        }

        private void UpdateAudioSourceVolumes()
        {
            if (audioSource != null)
            {
                audioSource.volume = cardSoundVolume * masterVolume;
            }
            
            if (voiceSource != null)
            {
                voiceSource.volume = voiceLineVolume * masterVolume;
            }
            
            if (bgAudioSource != null)
            {
                bgAudioSource.volume = bgMusicVolume * masterVolume;
            }
            
            foreach (var source in additionalAudioSources)
            {
                if (source != null)
                {
                    source.volume = cardSoundVolume * masterVolume;
                }
            }
        }

        private void OnDisable()
        {
            StopAllSounds();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopAllSounds();
            }
        }

        public void PlayButtonClickSound()
        {
            if (buttonClickSound == null) return;
            
            AudioSource uiSource = GetAvailableAudioSource();
            if (uiSource != null)
            {
                uiSource.volume = uiSoundVolume * masterVolume;
                uiSource.PlayOneShot(buttonClickSound);
            }
        }
        
        public void PlayButtonHoverSound()
        {
            if (buttonHoverSound == null) return;
            
            AudioSource uiSource = GetAvailableAudioSource();
            if (uiSource != null)
            {
                uiSource.volume = uiSoundVolume * masterVolume;
                uiSource.PlayOneShot(buttonHoverSound);
            }
        }
        
        private AudioSource GetAvailableAudioSource()
        {
            foreach (var source in additionalAudioSources)
            {
                if (source != null && !source.isPlaying)
                {
                    return source;
                }
            }
            
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            additionalAudioSources.Add(newSource);
            return newSource;
        }
    }
}
