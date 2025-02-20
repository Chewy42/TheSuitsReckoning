using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGame;

public class Player_Blackjack : Player
{
    void Start()
    {
        playerType = PlayerType.Player;
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

        return value;
    }
}
