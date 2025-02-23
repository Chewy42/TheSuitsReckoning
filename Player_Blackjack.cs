using UnityEngine;
using static UnityEngine.Object;
using System.Linq;

namespace CardGame
{
    public class Player_Blackjack : Player
    {
        void Start()
        {
            playerType = PlayerType.Player;
        }

        private bool isHitting = false;

        public void SetHitting(bool hitting)
        {
            isHitting = hitting;
        }

        public override int GetHandValue()
        {
            int value = 0;
            int numberOfAces = 0;
            var gameManager = FindFirstObjectByType<GameManager>();
            int targetScore = gameManager != null ? gameManager.GetCurrentTargetScore() : 21;

            // First pass: count non-ace cards and identify aces
            foreach (Card card in hand.Where(c => c != null && !c.IsFaceDown()))
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
