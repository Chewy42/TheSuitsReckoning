using System.Collections;
using System.Collections.Generic;  // Add this for generic collections
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // Add this line

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

        private void Start()
        {
            Debug.Log("UIManager Start");
            
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

            if (feedbackFormCanvasGroup != null)
            {
                feedbackFormCanvasGroup.alpha = 0f;
                feedbackFormCanvasGroup.interactable = false;
                feedbackFormCanvasGroup.blocksRaycasts = false;
            }
            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.alpha = 0f; // Initially hidden
                tutorialCanvasGroup.interactable = false;
                tutorialCanvasGroup.blocksRaycasts = false;
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
        }

        private void Update()
        {
            // Handle ESC key for pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Always allow unpausing if pause menu is visible
                bool isPauseMenuVisible = pauseMenuCanvasGroup != null && pauseMenuCanvasGroup.alpha > 0;
                
                // Only allow pausing during player's turn and when no animations are running
                bool canPause = gameManager != null && 
                              gameManager.GetCurrentGameState() == GameState.PlayerTurn &&
                              !gameManager.IsDealing() && 
                              !gameManager.IsReturnOrRandomizing() &&
                              !isProcessingAnimations;
                              
                if (isPauseMenuVisible || canPause)
                {
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
                    yield return new WaitForSeconds(0.5f);
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
            float duration = 0.2f;
            float elapsed = 0f;

            try
            {
                // Increase size
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (duration / 2);
                    float smoothT = t * t * (3f - 2f * t); // Smooth step interpolation
                    textComponent.fontSize = Mathf.Lerp(originalSize, targetSize, smoothT);
                    yield return null;
                }

                // Decrease size back to original
                elapsed = 0f;
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime;
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

        public void ShowFeedbackForm()
        {
            if (feedbackFormCanvasGroup != null)
            {
                // Ensure the feedback form is active and visible
                feedbackFormCanvasGroup.gameObject.SetActive(true);
                
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
                elapsed += Time.deltaTime;
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
                    yield return new WaitForSeconds(0.1f); // Small delay between animations
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
            Time.timeScale = 0;
            if (gameManager != null)
            {
                gameManager.SetGamePaused(true);
                gameManager.PauseForTutorial(true);
            }
        }

        private void UnpauseGame()
        {
            Time.timeScale = 1;
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
            yield return new WaitForSeconds(0.5f); // Short delay to let everything settle
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

            // Pause the game
            PauseGame();
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

            // If we've shown all screens, cleanup and unpause
            if (currentTutorialIndex >= tutorialScreens.Length)
            {
                CleanupTutorial();
                UnpauseGame();
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

            // If we've gone before the first screen, cleanup and unpause
            if (currentTutorialIndex < 0)
            {
                CleanupTutorial();
                UnpauseGame();
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
            
            // Ensure game is fully unpaused
            UnpauseGame();
        }
    }
}
