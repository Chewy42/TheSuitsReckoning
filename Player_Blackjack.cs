using UnityEngine;
using static UnityEngine.Object;

namespace CardGame
{
    public class Player_Blackjack : Player
    {
        void Start()
        {
            playerType = PlayerType.Player;
        }

        private int CalculateAceValue(int currentTotal, int targetScore, int remainingAces)
        {
            if (remainingAces == 0 || currentTotal > targetScore) {
                return currentTotal;
            }

            // Try using current ace as 1 instead of 11
            int valueAs1 = currentTotal - 10;
            
            // If using 1 puts us at or under target with remaining aces, recurse
            if (valueAs1 <= targetScore) {
                return CalculateAceValue(valueAs1, targetScore, remainingAces - 1);
            }
            
            return currentTotal;
        }

        public override int GetHandValue()
        {
            int value = 0;
            int aces = 0;
            var gameManager = FindFirstObjectByType<GameManager>();
            int targetScore = gameManager != null ? gameManager.GetCurrentTargetScore() : 21;

            // First pass: count non-ace cards and identify aces
            foreach (Card card in hand.Where(c => c != null && !c.IsFaceDown()))
            {
                string cardRank = card.rank.ToUpper().Trim();
                
                if (cardRank == "A" || cardRank == "ACE")
                {
                    aces++;
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
            if (aces > 0)
            {
                value = CalculateAceValue(value, targetScore, aces);
            }

            return value;
        }
    }
}
