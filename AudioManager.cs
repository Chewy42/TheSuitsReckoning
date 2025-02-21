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
        public float roundStartSoundDelay = 0.5f;

        [Header("Game Status")]
        public AudioClip[] winVoiceLines;
        public AudioClip[] loseVoiceLines;
        public AudioClip playerTurnSound;
        public AudioClip[] dealerThinkingSounds;

        [Header("Dealer Voice Lines")]
        public AudioClip[] genericVoiceClips;
        public AudioClip[] goodMoveVoiceClips;
        public AudioClip[] badMoveVoiceClips;
        public AudioClip shockedVoiceLine;

        [Range(0.8f, 1.2f)]
        public float voicePitchVariation = GameParameters.VOICE_PITCH_VARIATION;

        [Header("Generic Voice Line Settings")]
        [Tooltip("Minimum time between random generic voice lines (in seconds)")]
        public float minGenericVoiceLineInterval = GameParameters.MIN_GENERIC_VOICE_LINE_INTERVAL;

        [Tooltip("Maximum time between random generic voice lines (in seconds)")]
        public float maxGenericVoiceLineInterval = GameParameters.MAX_GENERIC_VOICE_LINE_INTERVAL;
        private float nextGenericVoiceLineTime;

        private AudioSource audioSource;
        private AudioSource voiceSource;
        private AudioClip currentVoiceLine;
        private int currentShuffleIndex = 0;
        private const int REQUIRED_SHUFFLES = 3;

        private Dictionary<SoundType, float> lastPlayTimes = new Dictionary<SoundType, float>();
        private Dictionary<AudioClip, float> lastVoiceLineTimes = new Dictionary<AudioClip, float>();
        private const float MIN_SOUND_INTERVAL = 0.1f;
        private const float MIN_VOICE_LINE_INTERVAL = 0.5f;
        private Queue<AudioClip> voiceLineQueue = new Queue<AudioClip>();
        private bool isProcessingVoiceQueue = false;

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

            // Initialize the time for the first generic voice line
            SetNextGenericVoiceLineTime();
        }

        private void Update()
        {
            // Check if it's time for a random generic voice line
            if (Time.time >= nextGenericVoiceLineTime)
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
            if (dealSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(dealSound);
            }
        }

        public void PlayCardReturnSound()
        {
            if (cardReturnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(cardReturnSound);
            }
        }

        public void PlayCardFlipSound()
        {
            if (cardFlipSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(cardFlipSound);
            }
        }

        public void PlayDealerThinkingSound()
        {
            if (
                dealerThinkingSounds != null
                && dealerThinkingSounds.Length > 0
                && audioSource != null
            )
            {
                audioSource.PlayOneShot(
                    dealerThinkingSounds[Random.Range(0, dealerThinkingSounds.Length)]
                );
            }
        }

        public void PlayRandomTargetSound1()
        {
            if (randomizeSound1 != null && audioSource != null)
            {
                audioSource.PlayOneShot(randomizeSound1);
            }
        }

        public void PlayRandomTargetSound2()
        {
            if (randomizeSound2 != null && audioSource != null)
            {
                audioSource.PlayOneShot(randomizeSound2);
            }
        }

        public void PlayWinVoiceLine()
        {
            if (winVoiceLines != null && winVoiceLines.Length > 0 && audioSource != null)
            {
                currentVoiceLine = winVoiceLines[Random.Range(0, winVoiceLines.Length)];
                audioSource.PlayOneShot(currentVoiceLine);
            }
        }

        public void PlayLoseVoiceLine()
        {
            if (loseVoiceLines != null && loseVoiceLines.Length > 0 && audioSource != null)
            {
                currentVoiceLine = loseVoiceLines[Random.Range(0, loseVoiceLines.Length)];
                audioSource.PlayOneShot(currentVoiceLine);
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

        private IEnumerator ProcessVoiceLineQueue()
        {
            if (isProcessingVoiceQueue) yield break;
            
            isProcessingVoiceQueue = true;
            while (voiceLineQueue.Count > 0)
            {
                AudioClip nextClip = voiceLineQueue.Dequeue();
                if (nextClip != null)
                {
                    // Check if enough time has passed since this voice line was last played
                    if (!lastVoiceLineTimes.ContainsKey(nextClip) || 
                        Time.time - lastVoiceLineTimes[nextClip] >= MIN_VOICE_LINE_INTERVAL)
                    {
                        currentVoiceLine = nextClip;
                        voiceSource.pitch = 1f + Random.Range(-voicePitchVariation, voicePitchVariation);
                        voiceSource.PlayOneShot(nextClip);
                        lastVoiceLineTimes[nextClip] = Time.time;
                        
                        // Wait for clip to finish
                        yield return new WaitForSeconds(nextClip.length);
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
            isProcessingVoiceQueue = false;
        }

        public void PlayVoiceLine(AudioClip voiceClip)
        {
            if (voiceClip == null || voiceSource == null) return;
            
            voiceLineQueue.Enqueue(voiceClip);
            StartCoroutine(ProcessVoiceLineQueue());
        }

        public void PlayRandomGenericVoiceLine()
        {
            if (genericVoiceClips != null && genericVoiceClips.Length > 0)
            {
                int randomIndex = Random.Range(0, genericVoiceClips.Length);
                PlayVoiceLine(genericVoiceClips[randomIndex]);
            }
        }

        public void PlayShockedVoiceLine()
        {
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
            // Prevent sound overlap by checking last play time
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
                        PlayDealSound();
                        break;
                    case SoundType.CardReturn:
                        PlayCardReturnSound();
                        break;
                    case SoundType.CardFlip:
                        PlayCardFlipSound();
                        break;
                    case SoundType.Shuffle:
                        StartCoroutine(PlayShuffleSounds());
                        break;
                    case SoundType.RandomizeTarget1:
                        PlayRandomTargetSound1();
                        break;
                    case SoundType.RandomizeTarget2:
                        PlayRandomTargetSound2();
                        break;
                    case SoundType.RoundStart:
                        if (roundStartSound != null && audioSource != null)
                        {
                            audioSource.PlayOneShot(roundStartSound);
                        }
                        break;
                    case SoundType.PlayerTurn:
                        if (playerTurnSound != null && audioSource != null)
                        {
                            audioSource.PlayOneShot(playerTurnSound);
                        }
                        break;
                    case SoundType.DealerThinking:
                        PlayDealerThinkingSound();
                        break;
                    case SoundType.Win:
                        PlayWinVoiceLine();
                        break;
                    case SoundType.Lose:
                        PlayLoseVoiceLine();
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
                audioSource.pitch = 1f;
            }
            if (voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.pitch = 1f;
            }
            voiceLineQueue.Clear();
            isProcessingVoiceQueue = false;
            currentVoiceLine = null;
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
    }
}
