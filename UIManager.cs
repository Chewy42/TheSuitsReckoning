using System.Collections;
using System.Collections.Generic;  // Add this for generic collections
using TMPro;
using UnityEngine;

namespace CardGame
{
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

        private float lastUpdateTime = 0f;
        private Coroutine currentAnimationCoroutine;
        private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
        private bool isProcessingAnimations = false;
        private Dictionary<Transform, Coroutine> activeEmphasisAnimations = new Dictionary<Transform, Coroutine>();

        private void Start()
        {
            ResetUI();
            if (feedbackFormCanvasGroup != null)
            {
                feedbackFormCanvasGroup.alpha = 0f;
                feedbackFormCanvasGroup.interactable = false;
                feedbackFormCanvasGroup.blocksRaycasts = false;
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
        }
    }
}
