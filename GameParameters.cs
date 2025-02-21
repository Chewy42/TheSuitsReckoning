using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// Static class containing all game-wide parameters and constants
    /// </summary>
    public static class GameParameters
    {
        #region Card Dealing Parameters
        /// <summary>
        /// Initial delay before the first card is dealt (seconds)
        /// </summary>
        public const float INITIAL_DEAL_DELAY = 0.3f;

        /// <summary>
        /// Delay between dealing each card (seconds)
        /// </summary>
        public const float DELAY_BETWEEN_CARDS = 0.05f;

        /// <summary>
        /// Speed at which cards move to their position (seconds)
        /// </summary>
        public const float CARD_MOVE_SPEED = 0.05f;
        #endregion

        #region Dealer Parameters
        /// <summary>
        /// Time dealer waits before making a decision (seconds)
        /// </summary>
        public const float DEALER_PLAY_DELAY = 1.0f;

        /// <summary>
        /// Time for dealer thinking animation (seconds)
        /// </summary>
        public const float DEALER_THINKING_ANIMATION_TIME = 0.5f;
        #endregion

        #region Voice Line Parameters
        /// <summary>
        /// Minimum time between generic voice lines (seconds)
        /// </summary>
        public const float MIN_GENERIC_VOICE_LINE_INTERVAL = 20.0f;

        /// <summary>
        /// Maximum time between generic voice lines (seconds)
        /// </summary>
        public const float MAX_GENERIC_VOICE_LINE_INTERVAL = 40.0f;

        /// <summary>
        /// Default voice pitch variation range
        /// </summary>
        public const float VOICE_PITCH_VARIATION = 1.0f;
        #endregion

        #region Game Score Parameters
        /// <summary>
        /// Default target score for the game
        /// </summary>
        public const int DEFAULT_TARGET_SCORE = 21;

        /// <summary>
        /// Minimum possible target score when randomizing
        /// </summary>
        public const int MIN_TARGET_SCORE = 15;

        /// <summary>
        /// Maximum possible target score when randomizing
        /// </summary>
        public const int MAX_TARGET_SCORE = 24;
        #endregion

        #region Card Animation Parameters
        /// <summary>
        /// Speed at which cards return to deck
        /// </summary>
        public const float CARD_RETURN_SPEED = 20f;

        /// <summary>
        /// Duration of card flip animation
        /// </summary>
        public const float CARD_FLIP_DURATION = 0.15f;

        /// <summary>
        /// Delay before playing card deal animation
        /// </summary>
        public const float CARD_DEAL_DELAY = 0.25f;
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
        public const float ROUND_TRANSITION_DELAY = 1.0f;

        /// <summary>
        /// Time to display round end screen (seconds)
        /// </summary>
        public const float ROUND_END_DISPLAY_TIME = 2.0f;

        /// <summary>
        /// Delay before returning cards to the deck (seconds)
        /// </summary>
        public const float CARD_RETURN_DELAY = 0.1f;
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
        public const float CARD_MOVEMENT_ARC_HEIGHT = 0.75f;
        #endregion

        #region Timing Safety Parameters
        /// <summary>
        /// Maximum allowed time for a card flip animation (seconds)
        /// </summary>
        public const float MAX_CARD_FLIP_TIME = 0.5f;

        /// <summary>
        /// Maximum allowed time for dealer's decision making (seconds)
        /// </summary>
        public const float MAX_DEALER_DECISION_TIME = 3.0f;

        /// <summary>
        /// Time to wait before auto-standing if player is inactive (seconds)
        /// </summary>
        public const float PLAYER_INACTIVITY_TIMEOUT = 30.0f;

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
