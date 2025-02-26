using System.Collections;
using System.Collections.Generic;  // Add this for generic collections
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // Add this line
using UnityEngine.EventSystems; // Add this for EventTrigger
using UnityEngine.UI; // Add this for UI components

namespace CardGame
{
    [System.Serializable]
    public struct TutorialScreen
    {
        public CanvasGroup canvasGroup;
        public VideoClip videoClip;
    }

    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        public GameManager gameManager;
        public TextMeshProUGUI playerScoreText;
        public TextMeshProUGUI dealerScoreText;
        public TextMeshProUGUI gameStatusText;
        public TextMeshProUGUI targetScoreText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI winsText;
        public CanvasGroup feedbackFormCanvasGroup;
        public CanvasGroup tutorialCanvasGroup;
        public CanvasGroup pauseMenuCanvasGroup;  // Add this line
        public CanvasGroup settingsCanvasGroup;  // Add settings canvas group
        public UnityEngine.UI.Scrollbar masterVolumeScrollbar;
        public UnityEngine.UI.Scrollbar dialogueVolumeScrollbar;
        public UnityEngine.UI.Scrollbar soundEffectsVolumeScrollbar;
        public UnityEngine.UI.Scrollbar bgMusicVolumeScrollbar;
        public UnityEngine.UI.Scrollbar uiSoundVolumeScrollbar;
        public AudioManager audioManager;
        [SerializeField] private TextMeshProUGUI gameplayTimeText;

        [Header("Animation Settings")]
        [SerializeField] private bool doubleDealingSpeedEnabled = false;
        [SerializeField] private float animationSpeedMultiplier = 1.0f;
        // This multiplier will be used to control animation speed (0.5 = double speed)

        [Header("Audio Settings")]
        // Remove unused slider variables
        // Alternative scrollbar references

        [Header("Tutorial")]
        [SerializeField] private TutorialScreen[] _tutorialScreens;  // Serialized reference
        private TutorialScreen[] tutorialScreens;  // Runtime reference
        private int currentTutorialIndex = -1;
        private bool hasTutorialBeenShown = false;
        private VideoPlayer currentVideoPlayer;
        private RenderTexture renderTexture;

        private float lastUpdateTime = 0f;
        private Coroutine currentAnimationCoroutine;
        private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
        private bool isProcessingAnimations = false;
        private Dictionary<Transform, Coroutine> activeEmphasisAnimations = new Dictionary<Transform, Coroutine>();
        
        // Target score pulsing animation
        private Coroutine targetScorePulseCoroutine;
        private bool isTargetScorePulsing = false;

        private void Start()
        {
            Debug.Log("UIManager Start");
            
            // Ensure normal speed is set by default
            doubleDealingSpeedEnabled = false;
            animationSpeedMultiplier = 1.0f;
            
            // Find references if not set in inspector
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
                if (gameManager == null)
                {
                    Debug.LogError("UIManager: GameManager not found!");
                }
            }
            
            if (audioManager == null)
            {
                audioManager = FindAnyObjectByType<AudioManager>();
                if (audioManager == null)
                {
                    Debug.LogWarning("UIManager: AudioManager not found!");
                }
            }
            
            // Cache tutorial screens
            if (_tutorialScreens != null && _tutorialScreens.Length > 0)
            {
                tutorialScreens = new TutorialScreen[_tutorialScreens.Length];
                _tutorialScreens.CopyTo(tutorialScreens, 0);
                Debug.Log($"Cached {tutorialScreens.Length} tutorial screens");
            }
            else
            {
                Debug.LogError("No tutorial screens assigned in inspector!");
                tutorialScreens = new TutorialScreen[0];
            }

            // Ensure all tutorial screens are initially hidden
            foreach (var screen in tutorialScreens)
            {
                if (screen.canvasGroup != null)
                {
                    Debug.Log($"Initializing tutorial screen {screen.canvasGroup.gameObject.name}");
                    screen.canvasGroup.alpha = 0f;
                    screen.canvasGroup.interactable = false;
                    screen.canvasGroup.blocksRaycasts = false;
                }
            }
            
            ResetUI();
            
            // Initialize audio sliders if they exist
            InitializeAudioSliders();
            
            // Start the target score pulsing animation
            StartTargetScorePulseAnimation();
        }

        private void Update()
        {
            // Handle ESC key for pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // First check if tutorial is active - close it if it is
                if (IsTutorialActive())
                {
                    CloseTutorialIfActive();
                }
                // If tutorial is not active, toggle the pause menu
                else
                {
                    // Always allow toggling the pause menu with ESC
                    TogglePauseMenu();
                }
            }

            // Check if current tutorial video should be playing
            if (currentVideoPlayer != null && currentTutorialIndex >= 0 && currentTutorialIndex < tutorialScreens?.Length)
            {
                var currentScreen = tutorialScreens[currentTutorialIndex];
                
                // Check if screen is visible
                bool isVisible = currentScreen.canvasGroup != null && 
                               currentScreen.canvasGroup.alpha > 0 && 
                               tutorialCanvasGroup != null && 
                               tutorialCanvasGroup.alpha > 0;

                if (!isVisible)
                {
                    // Stop video if screen is not visible
                    currentVideoPlayer.Stop();
                }
                else if (!currentVideoPlayer.isPlaying)
                {
                    // Start video if screen is visible but video is not playing
                    currentVideoPlayer.Play();
                }
            }
            
            // Check if we need to start or stop the target score pulse animation
            UpdateTargetScorePulseState();
        }

        public void SetGameStatus(string status)
        {
            if (string.IsNullOrEmpty(status) || status.ToLower().Contains("tutorial") || 
                gameStatusText == null || gameManager.IsGameCompleted())
            {
                return;
            }
            
            gameStatusText.SetText(status);
        }

        public void IncrementWins()
        {
            if (winsText == null || gameManager == null) return;
            
            int currentWins = gameManager.GetCurrentWins();
            winsText.text = currentWins.ToString();
            StartEmphasisAnimation(winsText.transform);
        }

        public void ResetWins()
        {
            if (winsText != null)
            {
                winsText.text = "0";
            }
        }

        public void UpdateScores()
        {
            if (Time.time - lastUpdateTime < GameParameters.MIN_UI_UPDATE_INTERVAL || 
                gameManager == null || gameManager.IsGameCompleted())
            {
                return;
            }

            int playerScore = gameManager.GetPlayerScore();
            int dealerScore = gameManager.GetDealerScore();
            int targetScore = gameManager.GetCurrentTargetScore();

            UpdateScoreText(playerScoreText, playerScore, targetScore);
            UpdateScoreText(dealerScoreText, dealerScore, targetScore);

            lastUpdateTime = Time.time;
        }

        private void UpdateScoreText(TextMeshProUGUI scoreText, int score, int targetScore)
        {
            if (scoreText == null) return;

            scoreText.text = score.ToString();
            scoreText.color = GetScoreColor(score, targetScore);
            
            if (score >= targetScore)
            {
                StartEmphasisAnimation(scoreText.transform);
            }
        }

        public void UpdateUI()
        {
            UpdateScores();
            UpdateStatusDisplays(gameManager?.GetCurrentRound() ?? 1, gameManager?.GetCurrentWins() ?? 0);
        }

        private Color GetScoreColor(int score, int target)
        {
            if (score > target) return Color.red;
            if (score == target) return Color.green;
            return Color.white;
        }

        public void ShowDealerThinking()
        {
            if (!gameManager.IsGameCompleted())
            {
                StartCoroutine(AnimateDealerThinking(new[] { "Dealer Thinking", "Dealer Thinking.", "Dealer Thinking..", "Dealer Thinking..." }));
            }
        }

        public void ShowDealerPlaying()
        {
            if (!gameManager.IsGameCompleted())
            {
                SetGameStatus("Dealer Playing");
            }
        }

        private IEnumerator AnimateDealerThinking(string[] thinkingTexts)
        {
            if (gameStatusText == null || gameManager == null)
                yield break;

            while (gameManager.GetCurrentGameState() == GameState.DealerTurn)
            {
                foreach (string text in thinkingTexts)
                {
                    if (gameManager.GetCurrentGameState() != GameState.DealerTurn)
                        yield break;

                    gameStatusText.text = text;
                    yield return GetAdjustedWaitForSeconds(0.5f);
                }
            }
        }

        public void UpdateTargetScore(int score)
        {
            if (targetScoreText != null)
            {
                targetScoreText.text = score.ToString();
                StartEmphasisAnimation(targetScoreText.transform);
            }
        }

        public void UpdateStatusDisplays(int round, int wins, bool animated = false)
        {
            if (roundText != null)
            {
                roundText.text = $"{round}/{GameParameters.MAX_ROUNDS}";
                if (animated)
                {
                    StartEmphasisAnimation(roundText.transform);
                }
            }
            
            if (winsText != null)
            {
                winsText.text = wins.ToString();
                if (animated)
                {
                    StartEmphasisAnimation(winsText.transform);
                }
            }

            // Show feedback form if player beats round 3
            if (round > GameParameters.MAX_ROUNDS)
            {
                ShowFeedbackForm();
            }
        }

        public void ResetUI()
        {
            Debug.Log("ResetUI called");
            // Reset tutorial state
            hasTutorialBeenShown = false;
            currentTutorialIndex = -1;
            
            if (tutorialScreens != null)
            {
                Debug.Log($"Resetting {tutorialScreens.Length} tutorial screens");
                foreach (var screen in tutorialScreens)
                {
                    if (screen.canvasGroup != null)
                    {
                        screen.canvasGroup.alpha = 0f;
                        screen.canvasGroup.interactable = false;
                        screen.canvasGroup.blocksRaycasts = false;
                    }
                }
            }
            
            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.alpha = 0f; // Initially hide the tutorial canvas
                tutorialCanvasGroup.interactable = false;
                tutorialCanvasGroup.blocksRaycasts = false;
            }

            if (gameStatusText != null)
                gameStatusText.text = "";

            if (playerScoreText != null)
            {
                playerScoreText.text = "0";
                playerScoreText.color = Color.white;
            }

            if (dealerScoreText != null)
            {
                dealerScoreText.text = "0";
                dealerScoreText.color = Color.white;
            }

            if (targetScoreText != null)
                targetScoreText.text = GameParameters.DEFAULT_TARGET_SCORE.ToString();

            if (roundText != null)
                roundText.text = $"1/{GameParameters.MAX_ROUNDS}";

            if (winsText != null)
                winsText.text = "0";
        }

        private void StopEmphasisAnimation(Transform target)
        {
            if (activeEmphasisAnimations.TryGetValue(target, out Coroutine routine))
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
                activeEmphasisAnimations.Remove(target);
            }
        }

        private IEnumerator EmphasisAnimation(Transform target)
        {
            if (target == null) yield break;

            // Get the TextMeshPro component
            var textComponent = target.GetComponent<TextMeshProUGUI>();
            if (textComponent == null) yield break;

            // Stop any existing animation on this target
            StopEmphasisAnimation(target);

            float originalSize = textComponent.fontSize;
            float targetSize = originalSize + 2f;
            
            // Always use normal speed for emphasis animation (0.2f without adjustment)
            float duration = 0.2f;
            float elapsed = 0f;

            try
            {
                // Increase size
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime; // Use normal Time.deltaTime instead of adjusted
                    float t = elapsed / (duration / 2);
                    float smoothT = t * t * (3f - 2f * t); // Smooth step interpolation
                    textComponent.fontSize = Mathf.Lerp(originalSize, targetSize, smoothT);
                    yield return null;
                }

                // Decrease size back to original
                elapsed = 0f;
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime; // Use normal Time.deltaTime instead of adjusted
                    float t = elapsed / (duration / 2);
                    float smoothT = t * t * (3f - 2f * t); // Smooth step interpolation
                    textComponent.fontSize = Mathf.Lerp(targetSize, originalSize, smoothT);
                    yield return null;
                }
            }
            finally
            {
                // Always ensure we reset to original size
                if (textComponent != null)
                {
                    textComponent.fontSize = originalSize;
                }
                if (activeEmphasisAnimations.ContainsKey(target))
                {
                    activeEmphasisAnimations.Remove(target);
                }
            }
        }

        private void StartEmphasisAnimation(Transform target)
        {
            if (target != null)
            {
                var routine = StartCoroutine(EmphasisAnimation(target));
                activeEmphasisAnimations[target] = routine;
            }
        }

        public void ShowFeedbackForm(string gameplayTime = "")
        {
            if (feedbackFormCanvasGroup != null)
            {
                // Ensure the feedback form is active and visible
                feedbackFormCanvasGroup.gameObject.SetActive(true);
                
                // Set the gameplay time if provided
                if (!string.IsNullOrEmpty(gameplayTime) && gameplayTimeText != null)
                {
                    gameplayTimeText.text = gameplayTime;
                    gameplayTimeText.gameObject.SetActive(true);
                }
                
                // Hide all other UI elements immediately
                if (playerScoreText != null) playerScoreText.gameObject.SetActive(false);
                if (dealerScoreText != null) dealerScoreText.gameObject.SetActive(false);
                if (gameStatusText != null) gameStatusText.gameObject.SetActive(false);
                if (targetScoreText != null) targetScoreText.gameObject.SetActive(false);
                if (roundText != null) roundText.gameObject.SetActive(false);
                if (winsText != null) winsText.gameObject.SetActive(false);

                // Start the fade animation
                StopAllCoroutines(); // Stop any existing fade animations
                StartCoroutine(FadeFeedbackForm());
            }
        }

        private IEnumerator FadeFeedbackForm()
        {
            if (feedbackFormCanvasGroup == null) yield break;

            feedbackFormCanvasGroup.alpha = 0f;
            feedbackFormCanvasGroup.interactable = true;
            feedbackFormCanvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < GameParameters.FEEDBACK_FORM_FADE_DURATION)
            {
                elapsed += GetAdjustedDeltaTime();
                float t = elapsed / GameParameters.FEEDBACK_FORM_FADE_DURATION;
                float smoothT = t * t * (3f - 2f * t); // Smooth step interpolation
                feedbackFormCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
                yield return null;
            }

            // Ensure we end at exactly 1
            feedbackFormCanvasGroup.alpha = 1f;
        }

        private IEnumerator ProcessAnimationQueue()
        {
            if (isProcessingAnimations) yield break;
            
            isProcessingAnimations = true;
            while (animationQueue.Count > 0)
            {
                var animation = animationQueue.Dequeue();
                if (animation != null)
                {
                    yield return StartCoroutine(animation);
                    yield return GetAdjustedWaitForSeconds(0.1f); // Small delay between animations
                }
            }
            isProcessingAnimations = false;
        }

        private void QueueAnimation(IEnumerator animation)
        {
            animationQueue.Enqueue(animation);
            if (!isProcessingAnimations)
            {
                StartCoroutine(ProcessAnimationQueue());
            }
        }

        public IEnumerator AnimateTargetScoreRandomization(int finalScore)
        {
            if (targetScoreText == null)
                yield break;

            int previousScore = gameManager.GetCurrentTargetScore();
            HashSet<int> usedScores = new HashSet<int> { previousScore, finalScore };
            
            // Always use normal speed for randomization (0.3f without adjustment)
            float animationDelay = 0.3f;

            for (int i = 0; i < 5; i++)
            {
                int randomScore;
                do
                {
                    randomScore = Random.Range(
                        GameParameters.MIN_TARGET_SCORE,
                        GameParameters.MAX_TARGET_SCORE + 1
                    );
                } while (usedScores.Contains(randomScore));
                
                usedScores.Add(randomScore);
                targetScoreText.text = randomScore.ToString();

                if (i < 4)
                {
                    gameManager?.GetAudioManager()?.PlaySound(SoundType.RandomizeTarget1);
                    StartEmphasisAnimation(targetScoreText.transform);
                    
                    // Use normal speed wait regardless of double speed setting
                    yield return new WaitForSeconds(animationDelay);
                }
                else
                {
                    gameManager?.GetAudioManager()?.PlaySound(SoundType.RandomizeTarget2);
                    targetScoreText.text = finalScore.ToString();
                    StartEmphasisAnimation(targetScoreText.transform);
                }
            }
        }

        private void PauseGame()
        {
            // Do not set Time.timeScale = 0, so the game continues running in the background
            // Time.timeScale = 0; - Removed this line
            
            if (gameManager != null)
            {
                // Only set the UI pause state, but don't actually pause the game
                gameManager.SetGamePaused(true);
                
                // Don't pause for tutorial either
                // gameManager.PauseForTutorial(true); - Removed this line
            }
        }

        private void UnpauseGame()
        {
            // No need to reset Time.timeScale since we're not changing it
            // Time.timeScale = 1; - Removed this line
            
            if (gameManager != null)
            {
                // Ensure both pause states are cleared
                gameManager.SetGamePaused(false);
                gameManager.PauseForTutorial(false);
                
                // Force update the game state if we're in dealer's turn
                if (gameManager.GetCurrentGameState() == GameState.DealerTurn)
                {
                    // Stop any existing dealer animations
                    StopAllCoroutines();
                    
                    // Force restart the dealer's turn
                    StartCoroutine(ResumeDealerTurn());
                }
            }
        }
        
        private IEnumerator ResumeDealerTurn()
        {
            yield return GetAdjustedWaitForSeconds(0.5f); // Short delay to let everything settle
            ShowDealerPlaying();
            gameManager?.StartCoroutine(gameManager.DealerPlayLoop());
        }

        public void ShowTutorial()
        {
            Debug.Log("ShowTutorial called");
            
            if (tutorialScreens == null || tutorialScreens.Length == 0)
            {
                Debug.LogError($"No tutorial screens available. Cached screens: {(tutorialScreens == null ? "null" : tutorialScreens.Length.ToString())}");
                return;
            }
            
            if (tutorialCanvasGroup == null)
            {
                Debug.LogError("Tutorial canvas group is null");
                return;
            }

            // Clean up any existing tutorial state
            CleanupTutorial();

            Debug.Log($"Showing tutorial with {tutorialScreens.Length} screens");

            // Show tutorial canvas group
            tutorialCanvasGroup.alpha = 1f;
            tutorialCanvasGroup.interactable = true;
            tutorialCanvasGroup.blocksRaycasts = true;

            // Start with first screen
            currentTutorialIndex = 0;
            if (currentTutorialIndex < tutorialScreens.Length)
            {
                // Show first screen
                var firstScreen = tutorialScreens[currentTutorialIndex];
                if (firstScreen.canvasGroup != null)
                {
                    Debug.Log($"Showing tutorial screen {currentTutorialIndex}: {firstScreen.canvasGroup.gameObject.name}");
                    firstScreen.canvasGroup.alpha = 1f;
                    firstScreen.canvasGroup.interactable = true;
                    firstScreen.canvasGroup.blocksRaycasts = true;
                    
                    // Play video if available
                    PlayTutorialVideo(firstScreen);
                }
                else
                {
                    Debug.LogError($"Tutorial screen {currentTutorialIndex} is null");
                }
            }
            else
            {
                Debug.LogError($"Invalid tutorial screen index: {currentTutorialIndex} (total screens: {tutorialScreens.Length})");
            }

            // Do not pause the game
            // PauseGame(); - Removed this line
        }

        // Method to show tutorial from menu
        public void ShowTutorialFromMenu()
        {
            // Clean up any existing tutorial state
            CleanupTutorial();

            // Show tutorial regardless of whether it's been shown before
            ShowTutorial();
        }

        public void NextTutorialStep()
        {
            Debug.Log("NextTutorialStep called");
            if (tutorialScreens == null || tutorialScreens.Length == 0 || tutorialCanvasGroup == null)
            {
                Debug.LogError("Cannot proceed with tutorial: required components missing");
                return;
            }

            // Hide current tutorial screen and stop video if one is active
            if (currentTutorialIndex >= 0 && currentTutorialIndex < tutorialScreens.Length)
            {
                var currentScreen = tutorialScreens[currentTutorialIndex];
                if (currentScreen.canvasGroup != null)
                {
                    currentScreen.canvasGroup.alpha = 0f;
                    currentScreen.canvasGroup.interactable = false;
                    currentScreen.canvasGroup.blocksRaycasts = false;
                }
                
                // Stop current video if playing
                if (currentVideoPlayer != null)
                {
                    currentVideoPlayer.Stop();
                    currentVideoPlayer = null;
                }
            }

            currentTutorialIndex++;

            // If we've shown all screens, cleanup and exit
            if (currentTutorialIndex >= tutorialScreens.Length)
            {
                CleanupTutorial();
                // Do not unpause the game
                // UnpauseGame(); - Removed this line
                return;
            }

            // Show next screen
            var nextScreen = tutorialScreens[currentTutorialIndex];
            if (nextScreen.canvasGroup != null)
            {
                Debug.Log($"Showing tutorial screen {currentTutorialIndex}: {nextScreen.canvasGroup.gameObject.name}");
                nextScreen.canvasGroup.alpha = 1f;
                nextScreen.canvasGroup.interactable = true;
                nextScreen.canvasGroup.blocksRaycasts = true;

                // Play video if available
                PlayTutorialVideo(nextScreen);
            }
            else
            {
                Debug.LogError($"Tutorial screen {currentTutorialIndex} is null");
            }
        }

        public void PreviousTutorialStep()
        {
            Debug.Log("PreviousTutorialStep called");
            if (tutorialScreens == null || tutorialScreens.Length == 0 || tutorialCanvasGroup == null)
            {
                Debug.LogError("Cannot go back in tutorial: required components missing");
                return;
            }

            // Hide current tutorial screen and stop video if one is active
            if (currentTutorialIndex >= 0 && currentTutorialIndex < tutorialScreens.Length)
            {
                var currentScreen = tutorialScreens[currentTutorialIndex];
                if (currentScreen.canvasGroup != null)
                {
                    currentScreen.canvasGroup.alpha = 0f;
                    currentScreen.canvasGroup.interactable = false;
                    currentScreen.canvasGroup.blocksRaycasts = false;
                }
                
                // Stop current video if playing
                if (currentVideoPlayer != null)
                {
                    currentVideoPlayer.Stop();
                    currentVideoPlayer = null;
                }
            }

            currentTutorialIndex--;

            // If we've gone before the first screen, cleanup and exit
            if (currentTutorialIndex < 0)
            {
                CleanupTutorial();
                // Do not unpause the game
                // UnpauseGame(); - Removed this line
                return;
            }

            // Show previous screen
            var prevScreen = tutorialScreens[currentTutorialIndex];
            if (prevScreen.canvasGroup != null)
            {
                Debug.Log($"Showing tutorial screen {currentTutorialIndex}: {prevScreen.canvasGroup.gameObject.name}");
                prevScreen.canvasGroup.alpha = 1f;
                prevScreen.canvasGroup.interactable = true;
                prevScreen.canvasGroup.blocksRaycasts = true;

                // Play video if available
                PlayTutorialVideo(prevScreen);
            }
            else
            {
                Debug.LogError($"Tutorial screen {currentTutorialIndex} is null");
            }
        }

        private void PlayTutorialVideo(TutorialScreen screen)
        {
            // Stop any currently playing video and cleanup
            if (currentVideoPlayer != null)
            {
                currentVideoPlayer.Stop();
                if (renderTexture != null)
                {
                    currentVideoPlayer.targetTexture = null;
                    RenderTexture.ReleaseTemporary(renderTexture);
                    renderTexture = null;
                }
                currentVideoPlayer = null;
            }

            // Early return if no video to play
            if (screen.videoClip == null)
            {
                Debug.LogWarning("No video clip assigned to tutorial screen");
                return;
            }

            // Ensure we have a canvas group
            if (screen.canvasGroup == null)
            {
                Debug.LogError("Cannot play video: Screen canvas group is null");
                return;
            }

            // Find the VideoContainer/Video GameObject and its VideoPlayer component
            Transform videoContainerTransform = screen.canvasGroup.transform.Find("VideoContainer");
            if (videoContainerTransform != null)
            {
                Transform videoTransform = videoContainerTransform.Find("Video");
                if (videoTransform != null)
                {
                    VideoPlayer videoPlayer = videoTransform.GetComponent<VideoPlayer>();
                    UnityEngine.UI.RawImage rawImage = videoTransform.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                    UnityEngine.UI.AspectRatioFitter aspectFitter = videoTransform.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                    
                    if (videoPlayer != null)
                    {
                        Debug.Log($"Setting up video player for tutorial screen: {screen.canvasGroup.gameObject.name}");
                        
                        try
                        {
                            // Create a render texture matching the video dimensions
                            renderTexture = RenderTexture.GetTemporary((int)screen.videoClip.width, (int)screen.videoClip.height, 24);
                            renderTexture.Create();

                            // Setup the RawImage to display the video
                            rawImage.color = Color.white;
                            
                            // Setup AspectRatioFitter to maintain 16:9 aspect ratio
                            aspectFitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
                            aspectFitter.aspectRatio = 16f / 9f; // Force 1920x1080 aspect ratio

                            // Basic video player setup
                            videoPlayer.source = VideoSource.VideoClip;
                            videoPlayer.clip = screen.videoClip;
                            videoPlayer.isLooping = true;
                            videoPlayer.playOnAwake = false;
                            videoPlayer.waitForFirstFrame = true;
                            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                            videoPlayer.targetTexture = renderTexture;
                            rawImage.texture = renderTexture;
                            
                            // Add event handlers
                            videoPlayer.errorReceived += (vp, message) => {
                                Debug.LogError($"Video player error: {message}");
                            };

                            videoPlayer.prepareCompleted += (vp) => {
                                Debug.Log($"Video prepared successfully. Duration: {vp.clip.length}s, Resolution: {vp.clip.width}x{vp.clip.height}");
                                vp.Play();
                            };

                            videoPlayer.started += (vp) => {
                                Debug.Log("Video playback started");
                            };

                            videoPlayer.loopPointReached += (vp) => {
                                Debug.Log("Video reached loop point");
                            };

                            // Store reference and prepare
                            currentVideoPlayer = videoPlayer;
                            Debug.Log("Preparing video for playback...");
                            videoPlayer.Prepare();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error setting up video player: {e.Message}\nStack trace: {e.StackTrace}");
                            if (renderTexture != null)
                            {
                                RenderTexture.ReleaseTemporary(renderTexture);
                                renderTexture = null;
                            }
                            if (currentVideoPlayer != null)
                            {
                                currentVideoPlayer.Stop();
                                currentVideoPlayer = null;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("VideoPlayer component not found on Video object");
                    }
                }
                else
                {
                    Debug.LogError("Video object not found in VideoContainer");
                }
            }
            else
            {
                Debug.LogError("VideoContainer not found in tutorial screen");
            }
        }

        // Helper method to check if a tutorial screen should be visible
        private bool IsTutorialScreenVisible(TutorialScreen screen)
        {
            return screen.canvasGroup != null && 
                   screen.canvasGroup.alpha > 0 && 
                   tutorialCanvasGroup != null && 
                   tutorialCanvasGroup.alpha > 0;
        }

        public void TogglePauseMenu()
        {
            if (pauseMenuCanvasGroup == null) return;

            bool isPaused = pauseMenuCanvasGroup.alpha > 0;
            
            // Toggle pause menu visibility
            pauseMenuCanvasGroup.alpha = isPaused ? 0 : 1;
            pauseMenuCanvasGroup.interactable = !isPaused;
            pauseMenuCanvasGroup.blocksRaycasts = !isPaused;

            // Toggle game pause state
            if (isPaused)
            {
                UnpauseGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void ResumeGame()
        {
            if (pauseMenuCanvasGroup == null) return;

            // Hide pause menu
            pauseMenuCanvasGroup.alpha = 0;
            pauseMenuCanvasGroup.interactable = false;
            pauseMenuCanvasGroup.blocksRaycasts = false;

            // Unpause game
            UnpauseGame();
        }

        public void RestartScene()
        {
            Debug.Log("Restarting scene...");
            
            // Destroy all objects in the scene
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Destroy(obj);
            }
            
            // Load the scene fresh
            SceneManager.LoadScene("Blackjack", LoadSceneMode.Single);
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void OpenFeedbackFormLink()
        {
            string feedbackFormUrl = "https://docs.google.com/forms/d/e/1FAIpQLSfSTrQjMyD1nWCmv_YHwdG6Q66BQG_CYTqCiEFsj9vc-G3g_Q/viewform";
            Application.OpenURL(feedbackFormUrl);
            Debug.Log("Opening feedback form in browser");
        }

        private void OnDisable()
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }
            animationQueue.Clear();
            isProcessingAnimations = false;

            // Clean up any running animations
            foreach (var animation in activeEmphasisAnimations.Values)
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
            }
            activeEmphasisAnimations.Clear();

            // Stop any playing video
            if (currentVideoPlayer != null)
            {
                currentVideoPlayer.Stop();
                currentVideoPlayer = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            
            // Stop the target score pulse animation
            StopTargetScorePulseAnimation();
        }

        private void CleanupTutorial()
        {
            // Stop any playing video
            if (currentVideoPlayer != null)
            {
                currentVideoPlayer.Stop();
                currentVideoPlayer = null;
            }

            // Hide tutorial canvas
            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.alpha = 0f;
                tutorialCanvasGroup.interactable = false;
                tutorialCanvasGroup.blocksRaycasts = false;
            }

            // Hide all tutorial screens
            if (tutorialScreens != null)
            {
                foreach (var screen in tutorialScreens)
                {
                    if (screen.canvasGroup != null)
                    {
                        screen.canvasGroup.alpha = 0f;
                        screen.canvasGroup.interactable = false;
                        screen.canvasGroup.blocksRaycasts = false;
                    }
                }
            }

            // Reset tutorial state
            currentTutorialIndex = -1;
            hasTutorialBeenShown = true;  // Mark tutorial as shown
            
            // Make sure the game isn't paused for tutorial
            if (gameManager != null)
            {
                gameManager.PauseForTutorial(false);
            }
        }
        
        // Checks if the target score pulse animation should be running based on game state
        private void UpdateTargetScorePulseState()
        {
            if (gameManager == null || targetScoreText == null) return;
            
            GameState currentState = gameManager.GetCurrentGameState();
            bool shouldPulse = (currentState == GameState.Playing || 
                               currentState == GameState.InitialDeal || 
                               currentState == GameState.PlayerTurn || 
                               currentState == GameState.DealerTurn) && 
                               !gameManager.IsReturnOrRandomizing();
            
            if (shouldPulse && !isTargetScorePulsing)
            {
                StartTargetScorePulseAnimation();
            }
            else if (!shouldPulse && isTargetScorePulsing)
            {
                StopTargetScorePulseAnimation();
            }
        }
        
        // Starts the continuous pulse animation for the target score
        private void StartTargetScorePulseAnimation()
        {
            if (targetScoreText == null || isTargetScorePulsing) return;
            
            StopTargetScorePulseAnimation(); // Ensure any existing animation is stopped
            targetScorePulseCoroutine = StartCoroutine(TargetScorePulseAnimation());
            isTargetScorePulsing = true;
            
            Debug.Log("Started target score pulse animation");
        }
        
        // Stops the continuous pulse animation for the target score
        private void StopTargetScorePulseAnimation()
        {
            if (targetScorePulseCoroutine != null)
            {
                StopCoroutine(targetScorePulseCoroutine);
                targetScorePulseCoroutine = null;
                Debug.Log("Stopped target score pulse animation");
            }
            
            // Reset to original appearance
            if (targetScoreText != null)
            {
                // Reset scale in case it was modified in previous versions
                targetScoreText.transform.localScale = Vector3.one;
                
                // Reset color to white if it was changed
                targetScoreText.color = Color.white;
                
                // Reset font size to a reasonable default if needed
                // We don't know the original size here, so we'll use a common default
                // The coroutine will set the proper size when it runs again
                if (targetScoreText.fontSize > 50) // If it seems enlarged
                {
                    targetScoreText.fontSize = 36; // Common default size
                }
            }
            
            isTargetScorePulsing = false;
        }
        
        // Continuously pulses the target score text to draw attention to it
        private IEnumerator TargetScorePulseAnimation()
        {
            if (targetScoreText == null) yield break;
            
            Debug.Log("Starting target score pulse animation");
            
            // Store original color and create target color with higher intensity
            Color originalColor = targetScoreText.color;
            Color brightColor = new Color(
                Mathf.Min(originalColor.r + 0.3f, 1f),
                Mathf.Min(originalColor.g + 0.3f, 1f),
                Mathf.Min(originalColor.b + 0.3f, 1f),
                originalColor.a
            );
            
            // Store original font size and calculate target size
            float originalFontSize = targetScoreText.fontSize;
            float targetFontSize = originalFontSize * 1.1f; // 10% larger font
            
            // Always use normal speed for pulse animation (1.0f without adjustment)
            float pulseDuration = 1.0f;
            
            Debug.Log($"Pulse animation setup - Original color: {originalColor}, Bright color: {brightColor}");
            Debug.Log($"Original font size: {originalFontSize}, Target font size: {targetFontSize}");
            
            while (true) // Continuous animation until stopped
            {
                // Check if we should stop the animation
                if (gameManager != null && gameManager.IsReturnOrRandomizing())
                {
                    // Reset to original values
                    targetScoreText.color = originalColor;
                    targetScoreText.fontSize = originalFontSize;
                    isTargetScorePulsing = false;
                    Debug.Log("Stopping target score pulse animation due to randomization");
                    yield break;
                }
                
                // Brighten and increase font size phase
                float elapsed = 0f;
                while (elapsed < pulseDuration / 2)
                {
                    elapsed += Time.deltaTime; // Use normal Time.deltaTime instead of adjusted
                    float t = elapsed / (pulseDuration / 2);
                    float smoothT = Mathf.SmoothStep(0, 1, t); // Smooth step for more natural animation
                    
                    // Lerp color and font size
                    targetScoreText.color = Color.Lerp(originalColor, brightColor, smoothT);
                    targetScoreText.fontSize = Mathf.Lerp(originalFontSize, targetFontSize, smoothT);
                    
                    yield return null;
                }
                
                // Dim and decrease font size phase
                elapsed = 0f;
                while (elapsed < pulseDuration / 2)
                {
                    elapsed += Time.deltaTime; // Use normal Time.deltaTime instead of adjusted
                    float t = elapsed / (pulseDuration / 2);
                    float smoothT = Mathf.SmoothStep(0, 1, t);
                    
                    // Lerp color and font size back
                    targetScoreText.color = Color.Lerp(brightColor, originalColor, smoothT);
                    targetScoreText.fontSize = Mathf.Lerp(targetFontSize, originalFontSize, smoothT);
                    
                    yield return null;
                }
                
                // Small pause between pulses - use normal speed
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Method to show the settings menu
        public void ShowSettings()
        {
            if (settingsCanvasGroup == null) return;
            
            // Update slider values to match current audio settings
            UpdateAudioSliderValues();

            // Show settings
            settingsCanvasGroup.alpha = 1f;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
            
            // No need to hide main menu as it doesn't exist in this context
        }

        // Update slider values to match current audio settings
        private void UpdateAudioSliderValues()
        {
            try
            {
                if (audioManager == null)
                {
                    audioManager = FindAnyObjectByType<AudioManager>();
                    if (audioManager == null)
                    {
                        Debug.LogWarning("AudioManager not found. Cannot update audio slider values.");
                        return;
                    }
                }

                Debug.Log("Updating audio slider values from AudioManager");

                if (masterVolumeScrollbar != null)
                {
                    masterVolumeScrollbar.value = audioManager.masterVolume;
                    Debug.Log($"Updated master volume scrollbar to {audioManager.masterVolume}");
                }

                if (dialogueVolumeScrollbar != null)
                {
                    dialogueVolumeScrollbar.value = audioManager.voiceLineVolume;
                    Debug.Log($"Updated dialogue volume scrollbar to {audioManager.voiceLineVolume}");
                }

                if (soundEffectsVolumeScrollbar != null)
                {
                    soundEffectsVolumeScrollbar.value = audioManager.cardSoundVolume;
                    Debug.Log($"Updated sound effects volume scrollbar to {audioManager.cardSoundVolume}");
                }
                
                if (bgMusicVolumeScrollbar != null)
                {
                    bgMusicVolumeScrollbar.value = audioManager.bgMusicVolume;
                    Debug.Log($"Updated bg music volume scrollbar to {audioManager.bgMusicVolume}");
                }
                
                if (uiSoundVolumeScrollbar != null)
                {
                    uiSoundVolumeScrollbar.value = audioManager.uiSoundVolume;
                    Debug.Log($"Updated UI sound volume scrollbar to {audioManager.uiSoundVolume}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating audio slider values: {e.Message}");
            }
        }
        
        // Method to hide the settings menu
        public void HideSettings()
        {
            if (settingsCanvasGroup == null) return;

            // Save audio settings when closing the settings menu
            if (audioManager != null)
            {
                audioManager.SaveAllAudioSettings();
            }

            // Hide settings
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        // Method to toggle the settings menu visibility
        public void ToggleSettings()
        {
            if (settingsCanvasGroup == null) return;

            bool isVisible = settingsCanvasGroup.alpha > 0;
            
            if (isVisible)
            {
                HideSettings();
            }
            else
            {
                ShowSettings();
            }
        }

        // Initialize audio sliders with current values from AudioManager
        private void InitializeAudioSliders()
        {
            try
            {
                if (audioManager == null)
                {
                    audioManager = FindAnyObjectByType<AudioManager>();
                    if (audioManager == null)
                    {
                        Debug.LogWarning("AudioManager not found. Audio settings will not work.");
                        return;
                    }
                }

                Debug.Log("Initializing audio sliders with values from AudioManager");
                
                // Set up master volume scrollbar
                if (masterVolumeScrollbar != null)
                {
                    // Use the value from AudioManager which has already loaded from PlayerPrefs
                    masterVolumeScrollbar.value = audioManager.masterVolume;
                    Debug.Log($"Setting master volume scrollbar to {audioManager.masterVolume}");
                    
                    // Remove any existing listeners to prevent duplicates
                    masterVolumeScrollbar.onValueChanged.RemoveAllListeners();
                    masterVolumeScrollbar.onValueChanged.AddListener(OnMasterVolumeChanged);
                }

                // Set up dialogue volume scrollbar
                if (dialogueVolumeScrollbar != null)
                {
                    // Use the value from AudioManager which has already loaded from PlayerPrefs
                    dialogueVolumeScrollbar.value = audioManager.voiceLineVolume;
                    Debug.Log($"Setting dialogue volume scrollbar to {audioManager.voiceLineVolume}");
                    
                    // Remove any existing listeners to prevent duplicates
                    dialogueVolumeScrollbar.onValueChanged.RemoveAllListeners();
                    dialogueVolumeScrollbar.onValueChanged.AddListener(OnDialogueVolumeChanged);
                }

                // Set up sound effects volume scrollbar
                if (soundEffectsVolumeScrollbar != null)
                {
                    // Use the value from AudioManager which has already loaded from PlayerPrefs
                    soundEffectsVolumeScrollbar.value = audioManager.cardSoundVolume;
                    Debug.Log($"Setting sound effects volume scrollbar to {audioManager.cardSoundVolume}");
                    
                    // Remove any existing listeners to prevent duplicates
                    soundEffectsVolumeScrollbar.onValueChanged.RemoveAllListeners();
                    soundEffectsVolumeScrollbar.onValueChanged.AddListener(OnSoundEffectsVolumeChanged);
                }
                
                // Set up background music volume scrollbar
                if (bgMusicVolumeScrollbar != null)
                {
                    // Use the value from AudioManager which has already loaded from PlayerPrefs
                    bgMusicVolumeScrollbar.value = audioManager.bgMusicVolume;
                    Debug.Log($"Setting bg music volume scrollbar to {audioManager.bgMusicVolume}");
                    
                    // Remove any existing listeners to prevent duplicates
                    bgMusicVolumeScrollbar.onValueChanged.RemoveAllListeners();
                    bgMusicVolumeScrollbar.onValueChanged.AddListener(OnBGMusicVolumeChanged);
                }
                
                // Set up UI sound volume scrollbar
                if (uiSoundVolumeScrollbar != null)
                {
                    // Use the value from AudioManager which has already loaded from PlayerPrefs
                    uiSoundVolumeScrollbar.value = audioManager.uiSoundVolume;
                    Debug.Log($"Setting UI sound volume scrollbar to {audioManager.uiSoundVolume}");
                    
                    // Remove any existing listeners to prevent duplicates
                    uiSoundVolumeScrollbar.onValueChanged.RemoveAllListeners();
                    uiSoundVolumeScrollbar.onValueChanged.AddListener(OnUISoundVolumeChanged);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing audio sliders: {e.Message}");
            }
        }

        // Called when master volume scrollbar value changes
        public void OnMasterVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetMasterVolume(value);
                audioManager.SaveAllAudioSettings();
            }
        }

        // Called when dialogue volume scrollbar value changes
        public void OnDialogueVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetDialogueVolume(value);
                audioManager.SaveAllAudioSettings();
            }
        }

        // Called when sound effects volume scrollbar value changes
        public void OnSoundEffectsVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetSoundEffectsVolume(value);
                audioManager.SaveAllAudioSettings();
            }
        }
        
        // Called when background music volume scrollbar value changes
        public void OnBGMusicVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetBGMusicVolume(value);
                audioManager.SaveAllAudioSettings();
            }
        }
        
        // Called when UI sound volume scrollbar value changes
        public void OnUISoundVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetUISoundVolume(value);
                audioManager.SaveAllAudioSettings();
            }
        }
        
        // Set up audio for a UI button
        public void SetupButtonAudio(UnityEngine.UI.Button button)
        {
            if (audioManager == null || button == null) return;
            
            // Add hover sound
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }
            
            // Add pointer enter event (hover)
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { audioManager.PlayButtonHoverSound(); });
            trigger.triggers.Add(enterEntry);
            
            // Add click event
            button.onClick.AddListener(() => audioManager.PlayButtonClickSound());
        }
        
        // Set up audio for all buttons in the scene
        public void SetupAllButtonsAudio()
        {
            if (audioManager == null) return;
            
            // Find all buttons in the scene
            UnityEngine.UI.Button[] allButtons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            foreach (var button in allButtons)
            {
                SetupButtonAudio(button);
            }
            
            Debug.Log($"Set up audio for {allButtons.Length} buttons");
        }

        // Enable double dealing speed
        public void EnableDoubleDealingSpeed()
        {
            doubleDealingSpeedEnabled = true;
            animationSpeedMultiplier = 0.5f;
        }

        // Disable double dealing speed
        public void DisableDoubleDealingSpeed()
        {
            doubleDealingSpeedEnabled = false;
            animationSpeedMultiplier = 1.0f;
        }

        // Helper method to get adjusted wait time for animations
        private float GetAdjustedAnimationTime(float originalTime)
        {
            if (doubleDealingSpeedEnabled)
            {
                return originalTime * animationSpeedMultiplier;
            }
            return originalTime;
        }

        // Helper method to create WaitForSeconds with adjusted time
        private WaitForSeconds GetAdjustedWaitForSeconds(float seconds)
        {
            return new WaitForSeconds(GetAdjustedAnimationTime(seconds));
        }

        // Helper method to get adjusted deltaTime for animations
        private float GetAdjustedDeltaTime()
        {
            return Time.deltaTime * (doubleDealingSpeedEnabled ? 1.0f / animationSpeedMultiplier : 1.0f);
        }

        // Check if tutorial is currently active
        public bool IsTutorialActive()
        {
            return tutorialCanvasGroup != null && 
                   tutorialCanvasGroup.alpha > 0 && 
                   currentTutorialIndex >= 0;
        }

        // Method to close tutorial if it's active
        public void CloseTutorialIfActive()
        {
            if (IsTutorialActive())
            {
                Debug.Log("Closing active tutorial");
                CleanupTutorial();
                
                // Make sure the game isn't paused
                if (gameManager != null)
                {
                    gameManager.SetGamePaused(false);
                    gameManager.PauseForTutorial(false);
                }
                
                // Display a brief message
                if (gameStatusText != null)
                {
                    gameStatusText.text = "Tutorial closed";
                    StartCoroutine(ClearStatusAfterDelay(2.0f));
                }
            }
        }

        // Toggle double dealing speed with feedback
        public void ToggleDoubleDealingSpeed()
        {
            // If tutorial is active, close it first
            if (IsTutorialActive())
            {
                CloseTutorialIfActive();
                return;
            }

            if (doubleDealingSpeedEnabled)
            {
                DisableDoubleDealingSpeed();
                if (gameStatusText != null)
                {
                    gameStatusText.text = "Normal Speed";
                    StartCoroutine(ClearStatusAfterDelay(2.0f));
                }
            }
            else
            {
                EnableDoubleDealingSpeed();
                if (gameStatusText != null)
                {
                    gameStatusText.text = "Double Speed Enabled";
                    StartCoroutine(ClearStatusAfterDelay(2.0f));
                }
            }
        }

        // Helper method to clear status text after a delay
        private IEnumerator ClearStatusAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateUI();
        }

        // Helper method to get the current animation speed multiplier
        public float GetAnimationSpeedMultiplier()
        {
            return animationSpeedMultiplier;
        }

        // Check if double dealing speed is enabled
        public bool IsDoubleDealingSpeedEnabled()
        {
            return doubleDealingSpeedEnabled;
        }
    }
}
