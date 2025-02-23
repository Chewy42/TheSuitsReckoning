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

            // Check if dealer has already won
            if (currentScore <= targetScore && (currentScore > playerScore || currentScore == playerScore))
            {
                Debug.Log($"Dealer already won with {currentScore} vs player's {playerScore} - stands");
                return false;
            }

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
            int value = 0;
            int numberOfAces = 0;
            var gameManager = FindFirstObjectByType<GameManager>();
            int targetScore = gameManager != null ? gameManager.GetCurrentTargetScore() : 21;

            var visibleCards = hand.Where(card => card != null && !card.IsFaceDown())
                                 .ToList();

            // First pass: count non-ace cards and identify aces
            foreach (Card card in visibleCards)
            {
                string cardRank = card.rank.ToUpper().Trim();
                
                if (cardRank == "A" || cardRank == "ACE")
                {
                    numberOfAces++;
                    value += 11;
                }
                else if (new[] { "K", "KING", "Q", "QUEEN", "J", "JACK" }.Contains(cardRank))
                {
                    value += 10;
                }
                else if (int.TryParse(cardRank, out int rankValue))
                {
                    value += rankValue;
                }
                else
                {
                    Debug.LogWarning($"Unable to parse card rank: {cardRank}, assuming 10");
                    value += 10;
                }
            }

            // Handle aces optimally
            while (value > targetScore && numberOfAces > 0)
            {
                value -= 10;  // Convert an ace from 11 to 1
                numberOfAces--;
            }

            return value;
        }
    }
}
