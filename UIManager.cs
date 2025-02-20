using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using Michsky.UI.Heat;
using System.Collections;
using System.Collections.Generic;

namespace CardGame {
    // New data structure that combines a tutorial canvas group with an optional video clip
    [System.Serializable]
    public class TutorialScreenData {
        public CanvasGroup canvasGroup;
        public VideoClip videoClip; // Optional video clip
    }

    public class UIManager : MonoBehaviour {
        [Header("Score Displays")]
        public TMP_Text playerScoreText;
        public TMP_Text dealerScoreText;
        public TMP_Text targetScoreText;

        [Header("Game Status")]
        public TMP_Text roundText;
        public TMP_Text winText;
        public TMP_Text statusText;
        public int currentWins = 0; // Change from private to public

        [Header("Game References")]
        public GameManager gameManager;

        [Header("Game Buttons")]
        public PanelButton hitButton;
        public PanelButton standButton;

        [Header("Tutorial")]
        public CanvasGroup[] tutorialSteps;
        public bool skipTutorial = false; // New boolean to bypass tutorial
        private int currentTutorialIndex = -1;
        private bool isTutorialActive = false;

        [Header("Tutorial Screens")]
        // Combined list of tutorial screens containing both the canvas group and an optional video clip
        public List<TutorialScreenData> tutorialScreens;

        // Tutorial related fields
        public GameObject tutorialPanel;

        private float lastButtonClickTime = 0f;
        private const float MIN_CLICK_INTERVAL = 0.2f;

        void Start() {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

            UpdateTargetScore(21);
            SetupButtons();
            
            // Start tutorial if not skipped
            if (!skipTutorial) {
                StartTutorial();
            } else {
                EndTutorial();
            }

            UpdateStatusDisplays(1, 0);
            SetGameStatus("Shuffling Deck...");
        }

        void Update() {
            UpdateUI();
            UpdateButtonStates();
            UpdateGameStatus();
        }

        private void SetupButtons() {
            if (hitButton != null) {
                hitButton.onClick.RemoveAllListeners();
                hitButton.onClick.AddListener(OnHitClicked);
            }

            if (standButton != null) {
                standButton.onClick.RemoveAllListeners();
                standButton.onClick.AddListener(OnStandClicked);
            }

            UpdateButtonStates();
        }

        private void InitializeTutorial() {
            if (tutorialSteps != null) {
                foreach (var step in tutorialSteps) {
                    if (step != null) {
                        // Ensure tutorial steps are active for tutorial
                        step.gameObject.SetActive(true);
                        step.alpha = 0;
                        step.interactable = false;
                        step.blocksRaycasts = false;
                    }
                }
            }
        }

        public void UpdateButtonStates() {
            if (hitButton != null && standButton != null && gameManager != null) {
                bool isPlayerTurn = gameManager.GetCurrentGameState() == GameState.PlayerTurn;
                bool isDealing = gameManager.IsDealing();
                bool isReturningOrRandomizing = gameManager.IsReturnOrRandomizing();

                // Only enable buttons during player turn and when no animations are happening
                bool buttonsEnabled = isPlayerTurn && !isDealing && !isReturningOrRandomizing;

                if (!buttonsEnabled) {
                    StartCoroutine(DisableButtonsWithDelay(hitButton, standButton));
                } else {
                    // When enabling buttons, do it immediately but ensure they're in normal state
                    if (!hitButton.isInteractable) {
                        hitButton.OnPointerExit(null);  // Clear any lingering hover state
                        hitButton.IsInteractable(true);
                    }
                    if (!standButton.isInteractable) {
                        standButton.OnPointerExit(null);  // Clear any lingering hover state
                        standButton.IsInteractable(true);
                    }
                }
            }
        }

        private IEnumerator DisableButtonsWithDelay(PanelButton hit, PanelButton stand) {
            // Set state in GameManager properly instead of accessing private field
            if (gameManager != null) {
                gameManager.SetGameState(GameState.DealerTurn);
            }

            // Let any current animations complete
            yield return new WaitForSeconds(0.2f);

            // Reset to normal state first if in hover
            if (hit != null) {
                hit.OnPointerExit(null);
                yield return new WaitForSeconds(0.1f);
                hit.IsInteractable(false);
            }

            if (stand != null) {
                stand.OnPointerExit(null);
                yield return new WaitForSeconds(0.1f);
                stand.IsInteractable(false);
            }
        }

        public void UpdateUI() {
            int playerScore = gameManager.GetPlayerScore();
            int currentTarget = gameManager.GetCurrentTargetScore();
            
            if (gameManager.GetCurrentGameState() == GameState.PlayerTurn && !gameManager.dealerCardRevealed && gameManager.dealerPlayer.GetHand().Count > 1) {
                int revealedValue = CalculateDealerRevealedScore();
                dealerScoreText.text = revealedValue.ToString();
            } else {
                dealerScoreText.text = gameManager.GetDealerScore().ToString();
            }
            
            playerScoreText.text = playerScore.ToString();
            targetScoreText.text = $"Target Score: {currentTarget}";
        }

        public void UpdateTargetScore(int targetScore) {
            if (targetScoreText != null) {
                targetScoreText.text = $"Target Score: {targetScore}";
            }
        }

        private int CalculateDealerRevealedScore() {
            var dealerHand = gameManager.dealerPlayer.GetHand();
            if (dealerHand.Count > 0) {
                Card firstCard = dealerHand[0];
                return GetCardValue(firstCard);
            }
            return 0;
        }
        
        private int GetCardValue(Card card) {
            switch(card.rank) {
                case "Ace": return 11;
                case "King":
                case "Queen":
                case "Jack": return 10;
                default:
                    return int.TryParse(card.rank, out int value) ? value : 0;
            }
        }
        
        public void OnHitClicked() {
            if (Time.unscaledTime - lastButtonClickTime < MIN_CLICK_INTERVAL) {
                return; // Prevent rapid clicks
            }
            
            Debug.Log($"Hit button clicked. Interactable: {hitButton.isInteractable}, GameManager null: {gameManager == null}");
            if (gameManager != null && hitButton.isInteractable && hitButton.IsInNormalState()) {
                lastButtonClickTime = Time.unscaledTime;
                gameManager.OnPlayerHit();
            }
        }

        public void OnStandClicked() {
            if (Time.unscaledTime - lastButtonClickTime < MIN_CLICK_INTERVAL) {
                return; // Prevent rapid clicks
            }

            Debug.Log("OnStandClicked invoked");
            if (gameManager != null && standButton != null && standButton.isInteractable && standButton.IsInNormalState()) {
                lastButtonClickTime = Time.unscaledTime;
                // Disable the stand button immediately to prevent multiple triggers
                standButton.IsInteractable(false);
                gameManager.OnPlayerStand();
            }
        }

        // New Tutorial System
        public void StartTutorial() {
            if (tutorialSteps == null || tutorialSteps.Length == 0) {
                Debug.LogWarning("No tutorial steps found!");
                return;
            }
            
            // Hide all steps first
            foreach (var step in tutorialSteps) {
                if (step != null) {
                    step.alpha = 0;
                    step.interactable = false;
                    step.blocksRaycasts = false;
                }
            }
            
            isTutorialActive = true;
            currentTutorialIndex = 0;
            
            // Show the first tutorial step
            if (tutorialSteps[0] != null) {
                tutorialSteps[0].alpha = 1;
                tutorialSteps[0].interactable = true;
                tutorialSteps[0].blocksRaycasts = true;
            }

            if(!skipTutorial && tutorialPanel != null) {
                tutorialPanel.SetActive(true);
            } else if(tutorialPanel == null) {
                Debug.LogWarning("Tutorial panel is not assigned in UIManager.");
            }
        }

        public void NextTutorialStep() {
            if (!isTutorialActive || tutorialSteps == null) return;

            // Hide current step
            HideCurrentTutorialStep();

            currentTutorialIndex++;
            
            if (currentTutorialIndex >= tutorialSteps.Length) {
                EndTutorial();
            } else {
                ShowCurrentTutorialStep();
            }
        }

        private void ShowCurrentTutorialStep() {
            if (IsValidTutorialIndex()) {
                var step = tutorialSteps[currentTutorialIndex];
                if (step != null) {
                    step.alpha = 1;
                    step.interactable = true;
                    step.blocksRaycasts = true;
                }
            }
        }

        private void HideCurrentTutorialStep() {
            if (IsValidTutorialIndex()) {
                var step = tutorialSteps[currentTutorialIndex];
                if (step != null) {
                    step.alpha = 0;
                    step.interactable = false;
                    step.blocksRaycasts = false;
                }
            }
        }

        private bool IsValidTutorialIndex() {
            return currentTutorialIndex >= 0 && currentTutorialIndex < tutorialSteps.Length;
        }

        public void EndTutorial() {
            isTutorialActive = false;
            currentTutorialIndex = -1;
            
            // Hide and disable all tutorial steps
            if (tutorialSteps != null) {
                foreach (var step in tutorialSteps) {
                    if (step != null) {
                        step.alpha = 0;
                        step.interactable = false;
                        step.blocksRaycasts = false;
                        step.gameObject.SetActive(false);
                    }
                }
            }

            // Tell GameManager tutorial is complete
            if (gameManager != null) {
                gameManager.OnTutorialComplete();
            }

            if(tutorialPanel != null) {
                tutorialPanel.SetActive(false);
            }
            // Reset the skip flag to false for next round if needed
            skipTutorial = false;
        }

        // Helper to get the VideoPlayer component from the child GameObject named "Video"
        private VideoPlayer GetVideoPlayer(CanvasGroup tutorialCanvas) {
            Transform videoTransform = tutorialCanvas.gameObject.transform.Find("Video");
            return videoTransform != null ? videoTransform.GetComponent<VideoPlayer>() : null;
        }

        // Modified: Show a specific tutorial screen by its index using combined tutorialScreens list
        public void ShowTutorialScreen(int index) {
            if (index < 0 || index >= tutorialScreens.Count) return;

            // Hide all tutorial screens
            foreach (var screen in tutorialScreens) {
                if (screen.canvasGroup != null) {
                    screen.canvasGroup.alpha = 0;
                    screen.canvasGroup.interactable = false;
                    screen.canvasGroup.blocksRaycasts = false;

                    VideoPlayer vp = GetVideoPlayer(screen.canvasGroup);
                    if (vp != null) {
                        vp.Stop();
                    }
                }
            }

            // Activate the selected tutorial screen
            var activeScreen = tutorialScreens[index];
            if (activeScreen.canvasGroup != null) {
                activeScreen.canvasGroup.alpha = 1;
                activeScreen.canvasGroup.interactable = true;
                activeScreen.canvasGroup.blocksRaycasts = true;

                // If an optional video clip exists, get the VideoPlayer and play it on loop
                if (activeScreen.videoClip != null) {
                    VideoPlayer vp = GetVideoPlayer(activeScreen.canvasGroup);
                    if (vp != null) {
                        vp.clip = activeScreen.videoClip;
                        vp.isLooping = true;
                        vp.Play();
                    }
                }
            }
        }

        // Modified: Hide a specific tutorial screen using combined tutorialScreens list
        public void HideTutorialScreen(int index) {
            if (index < 0 || index >= tutorialScreens.Count) return;
            var screen = tutorialScreens[index];
            if (screen.canvasGroup != null) {
                screen.canvasGroup.alpha = 0;
                screen.canvasGroup.interactable = false;
                screen.canvasGroup.blocksRaycasts = false;

                VideoPlayer vp = GetVideoPlayer(screen.canvasGroup);
                if (vp != null) {
                    vp.Stop();
                }
            }
        }

        // Optional: method to allow toggling tutorial skip from a UI checkbox
        public void SetSkipTutorial(bool skip) {
            skipTutorial = skip;
        }

        private void UpdateGameStatus() {
            if (gameManager != null) {
                GameState state = gameManager.GetCurrentGameState();
                if (gameManager.IsDealing() && !gameManager.IsReturnOrRandomizing()) {
                    SetGameStatus("Shuffling Deck...");
                } else {
                    switch (state) {
                        case GameState.PlayerTurn:
                            SetGameStatus("Your Turn");
                            break;
                        case GameState.DealerTurn:
                            SetGameStatus("Dealer's Turn");
                            break;
                        case GameState.GameOver:
                            // Status will be set by EndGame method
                            break;
                    }
                }
            }
        }

        public void UpdateStatusDisplays(int round, int wins) {
            if (roundText != null)
                roundText.text = $"Round: {round}/3";
            if (winText != null)
                winText.text = $"Wins: {wins}/3";
        }

        public void SetGameStatus(string status) {
            if (statusText != null)
                statusText.text = status;
        }

        public void IncrementWins() {
            currentWins++;
            if(gameManager != null)
                UpdateStatusDisplays(gameManager.currentRound, currentWins);
        }

        public void ResetWins() {
            currentWins = 0;
            UpdateStatusDisplays(1, currentWins);
        }
    }
}