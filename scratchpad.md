Contenxts:
@/GameManager.cs  @/UIManager.cs  @/AudioManager.cs  @/Player.cs  @/Dealer_Player_Blackjack.cs  @/Player_Blackjack.cs  @/PlayerInteraction.cs  @/Dealer.cs  @/Deck.cs  @/Card.cs  @/TableCards.cs  @/EnvironmentManager.cs  @/GameParameters.cs @/scratchpad.md


Flow of logic for the game:
-- Primary Game Loop --
State 1: Initial/"Shuffling..."
- Load in all of the cards from the deck for the player/dealer
- Set the target score to 21 by default

(Assuming Round 1)
State 2: Dealing
- The cards are currently being dealt to the players
- Each time a card is dealt:
    - Play card dealt sound effect
    - After dealing a card, refresh the UI for the player and dealer scores
        - Reveal the total score of the Players cards
        - Reveal the total score of the Dealers card (Only show and include one card first, then, after the players turn, reveal the card and add it to the score sum).
- When the fourth/final card is dealt, play a sound effect to signal the player their turn

State 3: Your Turn
- Set players hit and stand buttons isInteractable to true
    - For reference im using the PanelButton from heatui so i just reference their methods
- The player can hit till they either land on the target score (win) or go higher than the target score (lose)
    - If they hit the target score or go higher, their turn instantly ends and sets their hit and stand buttons isInteractive to false
- If the player loses the game, the cards will go back to the deck, then reset the game to Round 1 then reset the target score to 21
- If the player wins, they progress to Round 2
- If the player stands, their turn will end

State 4: Dealers Turn
- Will start by revealing their second card
    - The second cards value should be added to the dealers sum and reflect on the UI
    - The cards rotation needs to be (-90) on the x axis to be considered facing up now that theyre revealed
- Then the dealer will use an algorithm to determine if it should hit or stand
    - The dealer should just keep hitting till they either:
        1. Get a higher score than the player (without going over the target score)
        2. Win (Instantly end round and reset game)
        3. Go higher than the target score (lose)
        Each of these options utilize voice lines in the AudioManager
- The dealer should take a second or two and change the staus text to Dealer Thinking. .. ... where a new dot gets added (up to 3) quickly over slight delays to add a thinking effect
- Then change the status text to Dealer Playing when they play their card then wait a second or two before proceeding
- Then after the previous delay, change the StatusText to Player Wins or Dealer Wins

State 5: Intermission
- Will just display nothing for the StatusText
- Will return the cards to the deck, and play the sound effects associated with actions
- Then when the cards are all back at the deck, the target score in the middle will say "Target Score: {num}"
    - The target score will randomize between two integers that define an interval to pick a random integer between
    - The target score will do this randomize 5 times, where each time it randomizes it will play a sound effect associated with it, and have a delay between the next one of about 0.35 seconds
    - Once on the 5th randomized number, the second audio effect will be played signaling the end, and keep the final randomized number for the target score. The delay between the 4th and 5th (last) randomized number should be slightly longer, approximately 0.5 seconds
- Then the sound effect for a new round starting will play
- Once thats done, we will start play the shuffling noises 3 times and repeat the dealing process

CHANGES IN ROUND 2 AND ROUND 3
- In Round 2 the player will be dealt 2 cards (faced up) and the dealer will be dealt 2 cards (faced up this time) and an extra card (in their 3rd slot) that will be the hidden/flipped over card. This means the two faced up cards will be included in the sum and should reflect in the ui but the 3rd card will be hidden till after the players turn and not in the sum. Once its the players turn, obviously, flip the card by changing its rotation to (90 since 90 on the x axis is faced up cards) to reveal it THEN the dealer will do its moves.

The goal is to also have a smooth, enjoyable playing experience that isn't glitchy or broken, so we will need to ensure a clear and logically written codebase that doesnt break. 

-- Primary Game Loop Ended --

The AudioManager will have the following variables for audio clips
Deal Sound
Card Return Sound
Card Flip Sound
Shuffle Sounds
Randomize Sound 1
Randomize Sound 2
Round Start Sound
RoundStartSoundDelay (0.5f in seconds not audio clip but related)
List of Win Voice Lines
List of Lose Voice Lines
Player Turn Sound
List of Dealer Thinking Sounds
List of Generic Voice Clips
List of Good Voice Clips
List of Bad Voice Clips


The Deck will
1. Load in all of the cards from the deck for the player/dealer via name convention (PlayingCards_QSpades or PlayingCards_6Diamond etc)
2. Shuffle the deck
3. Deal out cards to the player and dealer
4. Return the cards to the deck


The Environment will
1. Do a light flicker at the start of the game as an effect then just keep the light on

OOP Class:
- Player.cs
    - (Child) Dealer_Player_Blackjack.cs
    - (Child) Player_Blackjack.cs