using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame;

public class Dealer_Player_Blackjack : Player
{
    [HideInInspector]
    public bool shouldHit = true;
    
    [Header("Decision Timing")]
    public float minDecisionDelay = 0.5f;
    public float maxDecisionDelay = 1.5f;

    [Header("Strategy Settings")]
    [Tooltip("How many points below target score the dealer will stand")]
    public int safetyMargin = 4;
    [Tooltip("Maximum value dealer will hit on with a soft hand")]
    public int maxSoftHitValue = 17;

    void Start()
    {
        playerType = PlayerType.Dealer;
    }

    public int GetHandValue()
    {
        int value = 0;
        int aces = 0;

        foreach (Card card in hand)
        {
            switch (card.rank)
            {
                case "Ace":
                    aces++;
                    value += 11;
                    break;
                case "King":
                case "Queen":
                case "Jack":
                    value += 10;
                    break;
                default:
                    value += int.Parse(card.rank);
                    break;
            }
        }

        while (value > 21 && aces > 0)
        {
            value -= 10;
            aces--;
        }

        // Update the legacy shouldHit flag based on dynamic target
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        int targetScore = gameManager != null ? gameManager.GetCurrentTargetScore() : 21;
        shouldHit = value < (targetScore - safetyMargin);
        
        return value;
    }

    /// <summary>
    /// Determines if the dealer should hit. Applies slightly more intelligent logic:
    /// hit if under 17 or if exactly 17 but counting as a soft 17 (an Ace valued as 11).
    /// </summary>
    public bool ShouldHit()
    {
        int value = 0;
        int soft = 0; // count of aces still counted as 11
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        int targetScore = gameManager != null ? gameManager.GetCurrentTargetScore() : 21;
        
        // Calculate minimum value to stand based on target score
        int minStandValue = targetScore - safetyMargin;
        
        foreach (Card card in hand)
        {
            if (card.rank == "Ace")
            {
                value += 11;
                soft++;
            }
            else if (card.rank == "King" || card.rank == "Queen" || card.rank == "Jack")
            {
                value += 10;
            }
            else
            {
                value += int.Parse(card.rank);
            }
        }

        // Adjust for aces where necessary
        while (value > targetScore && soft > 0)
        {
            value -= 10;
            soft--;
        }

        // More aggressive strategy with soft hands
        if (soft > 0)
        {
            // Hit on soft hands if below maxSoftHitValue (relative to target)
            int adjustedMaxSoft = Mathf.Min(maxSoftHitValue, targetScore - 2);
            return value <= adjustedMaxSoft;
        }

        // Stand on hard hands at minStandValue or higher
        return value < minStandValue;
    }
}
