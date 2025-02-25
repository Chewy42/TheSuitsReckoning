using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// Static class containing all game-wide parameters and constants
    /// </summary>
    public static class GameParameters
    {
        #region Card Movement Parameters
        /// <summary>
        /// Initial delay before the first card is dealt (seconds)
        /// </summary>
        public const float INITIAL_DEAL_DELAY = 1f;

        /// <summary>
        /// Delay between dealing each card (seconds)
        /// </summary>
        public const float DELAY_BETWEEN_CARDS = 0.15f;

        /// <summary>
        /// Duration for a card to move to its position (seconds)
        /// </summary>
        public const float CARD_MOVE_DURATION = 0.25f;

        /// <summary>
        /// Duration for a card to return to deck (seconds)
        /// </summary>
        public const float CARD_RETURN_DURATION = 0.15f;

        /// <summary>
        /// Delay between returning cards to deck (seconds)
        /// </summary>
        public const float CARD_RETURN_DELAY = 0.2f;

        /// <summary>
        /// Duration of card flip animation (seconds)
        /// </summary>
        public const float CARD_FLIP_DURATION = 0.15f;

        /// <summary>
        /// Delay between sequential card returns to deck (seconds)
        /// </summary>
        public const float SEQUENTIAL_CARD_RETURN_DELAY = 0.2f;

        /// <summary>
        /// Duration for a single card return animation (seconds)
        /// </summary>
        public const float CARD_RETURN_ANIMATION_DURATION = 0.2f;
        #endregion

        #region Dealer Parameters
        /// <summary>
        /// Time dealer waits before making a decision (seconds)
        /// </summary>
        public const float DEALER_PLAY_DELAY = 1f;

        /// <summary>
        /// Time for dealer thinking animation (seconds)
        /// </summary>
        public const float DEALER_THINKING_ANIMATION_TIME = 0.15f;
        #endregion

        #region Voice Line Parameters
        /// <summary>
        /// Minimum time between generic voice lines (seconds)
        /// </summary>
        public const float MIN_GENERIC_VOICE_LINE_INTERVAL = 20.0f;

        /// <summary>
        /// Maximum time between generic voice lines (seconds)
        /// </summary>
        public const float MAX_GENERIC_VOICE_LINE_INTERVAL = 20.0f;

        #endregion

        #region Game Score Parameters
        /// <summary>
        /// Default target score for the game
        /// </summary>
        public const int DEFAULT_TARGET_SCORE = 21;

        /// <summary>
        /// Minimum possible target score when randomizing
        /// </summary>
        public const int MIN_TARGET_SCORE = 28;

        /// <summary>
        /// Maximum possible target score when randomizing
        /// </summary>
        public const int MAX_TARGET_SCORE = 36;
        #endregion

        #region Card Animation Parameters
        /// <summary>
        /// X rotation when card is face up (degrees)
        /// </summary>
        public const float FACE_UP_X_ROTATION = -90f;
        
        /// <summary>
        /// X rotation when card is face down (degrees)
        /// </summary>
        public const float FACE_DOWN_X_ROTATION = 90f;

        /// <summary>
        /// X rotation when card is in deck (degrees)
        /// </summary>
        public const float CARD_IN_DECK_X_ROTATION = 90f;

        /// <summary>
        /// Delay before playing card deal animation
        /// </summary>
        public const float CARD_DEAL_DELAY = 0.1f;
        #endregion

        #region Round Parameters
        /// <summary>
        /// Initial number of cards dealt to each player
        /// </summary>
        public const int INITIAL_CARDS_PER_PLAYER = 2;

        /// <summary>
        /// Delay before playing round start sound
        /// </summary>
        public const float ROUND_START_SOUND_DELAY = 0.5f;

        /// <summary>
        /// Maximum number of rounds
        /// </summary>
        public const int MAX_ROUNDS = 3;

        /// <summary>
        /// Delay before transitioning to the next round (seconds)
        /// </summary>
        public const float ROUND_TRANSITION_DELAY = 0.4f;

        /// <summary>
        /// Time to display round end screen (seconds)
        /// </summary>
        public const float ROUND_END_DISPLAY_TIME = 1.0f;

        /// <summary>
        /// Delay before starting intermission after game ends (seconds)
        /// </summary>
        public const float END_GAME_DELAY = 2f;
        #endregion

        #region Game Rules
        /// <summary>
        /// Number of required shuffle sounds to play
        /// </summary>
        public const int REQUIRED_SHUFFLES = 3;
        #endregion

        #region Animation Parameters
        /// <summary>
        /// Height of the arc for card movement
        /// </summary>
        public const float CARD_MOVEMENT_ARC_HEIGHT = 2f;

        /// <summary>
        /// Duration of feedback form fade animation (seconds)
        /// </summary>
        public const float FEEDBACK_FORM_FADE_DURATION = 1f;
        #endregion

        #region Timing Safety Parameters
        /// <summary>
        /// Maximum allowed time for a card flip animation (seconds)
        /// </summary>
        public const float MAX_CARD_FLIP_TIME = 0.5f;

        /// <summary>
        /// Maximum allowed time for dealer's decision making (seconds)
        /// </summary>
        public const float MAX_DEALER_DECISION_TIME = 1f;

        /// <summary>
        /// Time to wait before auto-standing if player is inactive (seconds)
        /// </summary>
        public const float PLAYER_INACTIVITY_TIMEOUT = 20.0f;

        /// <summary>
        /// Time to wait between UI updates to prevent visual flickering
        /// </summary>
        public const float MIN_UI_UPDATE_INTERVAL = 0.1f;
        #endregion

        #region Game Balance Parameters
        /// <summary>
        /// Dealer will always hit below this score in round 1
        /// </summary>
        public const int DEALER_BASE_HIT_THRESHOLD = 17;

        /// <summary>
        /// Dealer becomes more aggressive after this score difference
        /// </summary>
        public const int DEALER_AGGRESSIVE_SCORE_GAP = 3;

        /// <summary>
        /// Maximum allowed cards per player
        /// </summary>
        public const int MAX_CARDS_PER_PLAYER = 5;
        #endregion

        #region Error Prevention
        /// <summary>
        /// Maximum attempts to deal a card before failing
        /// </summary>
        public const int MAX_DEAL_ATTEMPTS = 3;

        /// <summary>
        /// Maximum attempts to shuffle deck before warning
        /// </summary>
        public const int MAX_SHUFFLE_ATTEMPTS = 5;

        /// <summary>
        /// Maximum concurrent animations allowed
        /// </summary>
        public const int MAX_CONCURRENT_ANIMATIONS = 3;
        #endregion

        #region UI Text
        /// <summary>
        /// Text displayed while the deck is shuffling.
        /// </summary>
        public const string SHUFFLING_TEXT = "Shuffling...";

        /// <summary>
        /// Text displayed when a round is complete.
        /// </summary>
        public const string ROUND_COMPLETE_TEXT = "Round Complete!";

        /// <summary>
        /// Text displayed when the game is complete.
        /// </summary>
        public const string GAME_COMPLETE_TEXT = "Congratulations! Game Complete!";

        /// <summary>
        /// Text displayed for player's turn.
        /// </summary>
        public const string YOUR_TURN_TEXT = "Your turn";

        /// <summary>
        /// Text displayed when the dealer is thinking.
        /// </summary>
        public const string DEALER_THINKING_TEXT = "Dealer Thinking...";

        /// <summary>
        /// Format for the win message when the player wins by dealer bust.
        /// </summary>
        public const string PLAYER_WINS_DEALER_BUSTS_FORMAT = "Player wins! Dealer busted with {0}";

        /// <summary>
        /// Format for the win message when the dealer wins by player bust.
        /// </summary>
        public const string DEALER_WINS_PLAYER_BUSTS_FORMAT = "Dealer wins! Player busted with {0}";

        /// <summary>
        /// Format for the win message when dealer wins with a score.
        /// </summary>
        public const string DEALER_WINS_FORMAT = "Dealer wins with {0}!";

        /// <summary>
        /// Format for the win message when player wins with a score.
        /// </summary>
        public const string PLAYER_WINS_FORMAT = "Player wins with {0}!";

        /// <summary>
        /// Format for the message when there is a tie.
        /// </summary>
        public const string TIE_DEALER_WINS_FORMAT = "Tie at {0}! Dealer wins!";

        /// <summary>
        /// Prefix text for target score.
        /// </summary>
        public const string TARGET_SCORE_PREFIX = "Target Score: ";

        /// <summary>
        /// Format for round text.
        /// </summary>
        public const string ROUND_TEXT_FORMAT = "Round: {0}/{1}";

        /// <summary>
        /// Format for wins text.
        /// </summary>
        public const string WINS_TEXT_FORMAT = "Wins: {0}";
#endregion

        #region Camera Parameters
        /// <summary>
        /// Duration in seconds for the camera to move from position 1 to position 2 in the map scene
        /// </summary>
        public const float MAP_SCENE_CAMERA_LERP_DURATION = 3.5f;
        #endregion

        #region Round State Validation
        /// <summary>
        /// Validate that round numbers are within expected range
        /// </summary>
        public static bool IsValidRoundNumber(int round)
        {
            return round >= 1 && round <= MAX_ROUNDS;
        }

        /// <summary>
        /// Validate that a target score is within allowed range
        /// </summary>
        public static bool IsValidTargetScore(int score)
        {
            return score >= MIN_TARGET_SCORE && score <= MAX_TARGET_SCORE;
        }

        /// <summary>
        /// Get appropriate delay for current round transition
        /// </summary>
        public static float GetRoundTransitionDelay(int currentRound)
        {
            return ROUND_TRANSITION_DELAY * (1 + (currentRound * 0.2f));
        }
        #endregion
    }
}
