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

        private float lastUpdateTime = 0f;
        private Coroutine currentAnimationCoroutine;
        private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
        private bool isProcessingAnimations = false;

        private void Start()
        {
            ResetUI();
        }

        public void SetGameStatus(string status)
        {
            if (gameStatusText != null)
            {
                gameStatusText.SetText(status);
                Debug.Log($"Game status set to: {status}");
            }
        }

        public void IncrementWins()
        {
            if (winsText != null && gameManager != null)
            {
                int currentWins = gameManager.GetCurrentWins();
                Debug.Log($"Incrementing wins display to: {currentWins}");
                winsText.text = $"Wins: {currentWins}";
                StartCoroutine(EmphasisAnimation(winsText.transform));
            }
        }

        public void ResetWins()
        {
            if (winsText != null)
            {
                Debug.Log("Resetting wins display to 0");
                winsText.text = "Wins: 0";
                StartCoroutine(EmphasisAnimation(winsText.transform));
            }
        }

        public void UpdateScores()
        {
            if (Time.time - lastUpdateTime < GameParameters.MIN_UI_UPDATE_INTERVAL)
                return;

            if (gameManager == null)
            {
                Debug.LogError("Cannot update scores - GameManager reference is missing");
                return;
            }

            int playerScore = gameManager.GetPlayerScore();
            int dealerScore = gameManager.GetDealerScore();
            int targetScore = gameManager.GetCurrentTargetScore();

            Debug.Log($"Updating scores - Player: {playerScore}, Dealer: {dealerScore}, Target: {targetScore}");

            if (playerScoreText != null)
            {
                playerScoreText.text = playerScore.ToString();
                playerScoreText.color = GetScoreColor(playerScore, targetScore);
                if (playerScore > targetScore)
                {
                    StartCoroutine(EmphasisAnimation(playerScoreText.transform));
                }
            }

            if (dealerScoreText != null)
            {
                dealerScoreText.text = dealerScore.ToString();
                dealerScoreText.color = GetScoreColor(dealerScore, targetScore);
                if (dealerScore > targetScore)
                {
                    StartCoroutine(EmphasisAnimation(dealerScoreText.transform));
                }
            }

            lastUpdateTime = Time.time;
        }

        public void UpdateUI()
        {
            Debug.Log("UpdateUI called - Refreshing all displays");
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
            StartCoroutine(AnimateDealerThinking(new[] { "Dealer Thinking", "Dealer Thinking.", "Dealer Thinking..", "Dealer Thinking..." }));
        }

        public void ShowDealerPlaying()
        {
            SetGameStatus("Dealer Playing");
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
                Debug.Log($"Updating target score display to: {score}");
                targetScoreText.text = $"Target Score: {score}";
                StartCoroutine(EmphasisAnimation(targetScoreText.transform));
            }
        }

        public void UpdateStatusDisplays(int round, int wins, bool animated = false)
        {
            Debug.Log($"Updating status displays - Round: {round}/3, Wins: {wins}");
            
            if (roundText != null)
            {
                roundText.text = $"Round: {round}/3";
                if (animated)
                {
                    StartCoroutine(EmphasisAnimation(roundText.transform));
                }
            }
            
            if (winsText != null)
            {
                winsText.text = $"Wins: {wins}";
                if (animated)
                {
                    StartCoroutine(EmphasisAnimation(winsText.transform));
                }
            }
        }

        public void ResetUI()
        {
            Debug.Log("Performing full UI reset");

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
                targetScoreText.text = $"Target Score: {GameParameters.DEFAULT_TARGET_SCORE}";

            if (roundText != null)
                roundText.text = "Round: 1/3";

            ResetWins();
        }

        private IEnumerator EmphasisAnimation(Transform target)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 originalPosition = target.position;
            
            // Scale up
            float scaleUpTime = 0f;
            float scaleDuration = 0.2f;
            while (scaleUpTime < scaleDuration)
            {
                scaleUpTime += Time.deltaTime;
                float t = scaleUpTime / scaleDuration;
                float smoothT = t * t * (3f - 2f * t);
                float scale = 1f + Mathf.Sin(smoothT * Mathf.PI) * 0.2f;
                target.localScale = originalScale * scale;
                
                // Add slight upward movement
                float yOffset = Mathf.Sin(smoothT * Mathf.PI) * 5f;
                target.position = originalPosition + Vector3.up * yOffset;
                
                yield return null;
            }

            // Return to original
            target.localScale = originalScale;
            target.position = originalPosition;
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

            Debug.Log($"Starting target score randomization animation, final score will be: {finalScore}");
            
            int previousScore = currentTargetScore;
            HashSet<int> usedScores = new HashSet<int> { previousScore, finalScore };

            for (int i = 0; i < 5; i++)
            {
                int randomScore;
                do
                {
                    randomScore = Random.Range(
                        GameParameters.MIN_TARGET_SCORE,
                        GameParameters.MAX_TARGET_SCORE + 1
                    );
                } while (usedScores.contains(randomScore));
                
                usedScores.Add(randomScore);
                targetScoreText.text = $"Target Score: {randomScore}";

                if (i < 4)
                {
                    gameManager?.GetAudioManager()?.PlaySound(SoundType.RandomizeTarget1);
                    yield return new WaitForSeconds(0.35f);
                }
                else
                {
                    gameManager?.GetAudioManager()?.PlaySound(SoundType.RandomizeTarget2);
                    yield return new WaitForSeconds(0.5f);
                    targetScoreText.text = $"Target Score: {finalScore}";
                    StartCoroutine(EmphasisAnimation(targetScoreText.transform));
                    Debug.Log($"Target score randomization complete: {finalScore}");
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
        }
    }
}
