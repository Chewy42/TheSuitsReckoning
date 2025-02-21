using System.Linq;
using UnityEngine;

namespace CardGame
{
    public class Dealer_Player_Blackjack : Player
    {
        private GameManager gameManager;

        void Start()
        {
            playerType = PlayerType.Dealer;
            gameManager = FindFirstObjectByType<GameManager>();
            Debug.Log("Dealer_Player_Blackjack initialized");
        }

        public override bool ShouldHit()
        {
            int currentScore = GetHandValue();
            int targetScore = gameManager?.GetCurrentTargetScore() ?? 21;
            var player = FindFirstObjectByType<Player_Blackjack>();
            int playerScore = player?.GetHandValue() ?? 0;
            int currentRound = gameManager?.GetCurrentRound() ?? 1;

            Debug.Log($"Dealer deciding to hit - Round: {currentRound}, Current: {currentScore}, Target: {targetScore}, Player: {playerScore}");

            if (playerScore > targetScore)
            {
                Debug.Log("Player busted - dealer stands");
                return false;
            }

            if (currentScore >= targetScore)
            {
                Debug.Log("Dealer reached or exceeded target - stands");
                return false;
            }

            // More aggressive strategy in later rounds
            if (currentRound > 1)
            {
                if (currentScore < playerScore || (currentScore < targetScore - 2 && playerScore >= currentScore))
                {
                    Debug.Log($"Round {currentRound}: Dealer hitting to beat player score");
                    return true;
                }
            }
            else if (currentScore < 17)
            {
                Debug.Log("Dealer below 17 - must hit");
                return true;
            }

            int safeMargin = targetScore - currentScore;
            int scoreGap = playerScore - currentScore;

            bool shouldHit = (safeMargin >= 4 && scoreGap >= safeMargin - 1)
                || (currentScore < 18 && scoreGap >= 3)
                || (currentScore < playerScore && safeMargin >= 3);

            Debug.Log($"Dealer decision - Hit: {shouldHit}, SafeMargin: {safeMargin}, ScoreGap: {scoreGap}");
            return shouldHit;
        }

        public override int GetHandValue()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            int targetScore = gameManager?.GetCurrentTargetScore() ?? 21;
            int currentRound = gameManager?.GetCurrentRound() ?? 1;
            GameState currentState = gameManager?.GetCurrentGameState() ?? GameState.Initializing;
            
            // During player turn, only count visible cards based on current round
            if (currentState == GameState.PlayerTurn)
            {
                int value = 0;
                int aces = 0;
                int visibleCardCount = currentRound == 1 ? 1 : 2;

                var visibleCards = hand.Take(visibleCardCount)
                                     .Where(card => card != null && !card.IsFaceDown())
                                     .ToList();

                Debug.Log($"Dealer scoring visible cards - Round: {currentRound}, State: {currentState}, Visible cards: {visibleCards.Count}");

                foreach (Card card in visibleCards.OrderBy(c => c.rank != "Ace"))
                {
                    if (card.rank.ToUpper() == "ACE")
                    {
                        aces++;
                        value += 11;
                    }
                    else if (new[] { "KING", "QUEEN", "JACK" }.Contains(card.rank.ToUpper()))
                    {
                        value += 10;
                    }
                    else if (int.TryParse(card.rank, out int rankValue))
                    {
                        value += rankValue;
                    }
                    else
                    {
                        Debug.LogWarning($"Unable to parse dealer card rank: {card.rank}, assuming 10");
                        value += 10;
                    }

                    while (value > targetScore && aces > 0)
                    {
                        value -= 10;
                        aces--;
                    }
                }

                Debug.Log($"Dealer visible score: {value} (Round {currentRound})");
                return value;
            }
            
            // During dealer's turn or game end, count all cards
            return base.GetHandValue();
        }
    }
}
