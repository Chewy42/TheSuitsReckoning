using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using UnityEngine;

namespace CardGame
{
    public enum GameState
    {
        // Setup states
        Initializing,     // Initial game setup
        WaitingToStart,   // Waiting for game to begin
        
        // Active gameplay states
        Playing,          // General gameplay state
        InitialDeal,      // Dealing initial cards
        PlayerTurn,       // Player's turn to act
        DealerTurn,       // Dealer's turn to act
        
        // End states
        GameOver,         // Round has ended
        Intermission,     // Between rounds
        Completed         // Game has been completed
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        #region Component References
        [Header("Game Components")]
        [SerializeField, Tooltip("Reference to the player component")] 
        private Player playerPlayer;
        
        [SerializeField, Tooltip("Reference to the dealer's player component")]
        private Player dealerPlayer;
        
        [SerializeField, Tooltip("Reference to the dealer component")]
        private Dealer dealer;
        
        [SerializeField, Tooltip("Reference to the deck component")]
        private Deck deck;
        
        [SerializeField, Tooltip("Reference to the UI manager")]
        private UIManager uiManager;
        
        [SerializeField, Tooltip("Reference to the audio manager")]
        private AudioManager audioManager;
        
        [SerializeField, Tooltip("Reference to the table cards component")]
        private TableCards tableCards;
        #endregion

        #region Game State Variables
        [Header("Game State")]
        private GameState currentGameState = GameState.Initializing;
        private bool isDealing;
        private bool isReturningCards;
        private bool isRandomizingTarget;
        private bool isPlayerWinner;
        private bool isPush; // New flag to track push/tie games
        private bool isTransitioningState;
        private bool isShuttingDown;
        private bool isPausedForTutorial = false;
        private bool isPaused = false;
        private bool hasShownFirstGameTutorial = false;
        #endregion

        #region Game Progress
        [Header("Game Progress")]
        [SerializeField, Tooltip("Current round number")] 
        private int currentRound = 1;
        
        [SerializeField, Tooltip("Current target score to reach")] 
        private int currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
        
        private int currentWins;
        #endregion

        #region Settings
        [Header("Dealer Settings")]
        [SerializeField, Tooltip("Delay between dealer's actions")] 
        private float dealerPlayDelay = GameParameters.DEALER_PLAY_DELAY;
        #endregion

        #region UI References
        [Header("Player Interaction")]
        [SerializeField, Tooltip("Reference to the hit button")] 
        private PanelButton hitButton;
        
        [SerializeField, Tooltip("Reference to the stand button")] 
        private PanelButton standButton;
        #endregion

        #region Coroutines
        private HashSet<Coroutine> activeCoroutines = new HashSet<Coroutine>();
        private Coroutine dealerPlayCoroutine;
        private Coroutine intermissionCoroutine;
        #endregion

        #region Animation Speed Helpers
        // Helper method to get adjusted delay based on animation speed setting
        private float GetAdjustedDelay(float originalDelay)
        {
            if (uiManager != null && uiManager.IsDoubleDealingSpeedEnabled())
            {
                return originalDelay * uiManager.GetAnimationSpeedMultiplier();
            }
            return originalDelay;
        }

        // Helper method to create WaitForSeconds with adjusted time
        private WaitForSeconds GetAdjustedWaitForSeconds(float seconds)
        {
            return new WaitForSeconds(GetAdjustedDelay(seconds));
        }
        #endregion

        #region Properties
        /// <summary>Gets the current round number</summary>
        public int GetCurrentRound() => currentRound;

        /// <summary>Gets the current number of consecutive wins</summary>
        public int GetCurrentWins() => currentWins;

        /// <summary>Gets whether cards are currently being dealt</summary>
        public bool IsDealing() => isDealing;

        /// <summary>Gets whether the game is in a completed state</summary>
        public bool IsGameCompleted() => currentGameState == GameState.GameOver;

        /// <summary>Gets whether cards are being returned or target score is being randomized</summary>
        public bool IsReturnOrRandomizing() => isReturningCards || isRandomizingTarget;

        /// <summary>Gets the current game state</summary>
        public GameState GetCurrentGameState() => currentGameState;

        /// <summary>Gets the current target score to reach</summary>
        public int GetCurrentTargetScore() => currentTargetScore;

        /// <summary>Gets the player's current hand value</summary>
        public int GetPlayerScore() => playerPlayer?.GetHandValue() ?? 0;

        /// <summary>Gets the dealer's current hand value</summary>
        public int GetDealerScore() => dealerPlayer?.GetHandValue() ?? 0;

        /// <summary>Gets the dealer's current hand of cards</summary>
        public List<Card> GetDealerHand() => dealerPlayer?.GetAllCards() ?? new List<Card>();

        /// <summary>Gets the audio manager reference</summary>
        public AudioManager GetAudioManager() => audioManager;
        
        /// <summary>Gets whether the game is paused for a tutorial</summary>
        public bool IsPausedForTutorial()
        {
            return isPausedForTutorial;
        }
        
        public bool IsGamePaused()
        {
            return isPaused;
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple GameManager instances detected. Destroying duplicate on {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Reset all static/persistent state
            currentRound = 1;
            currentWins = 0;
            currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
            isPlayerWinner = false;
            
            InitializeComponents();
            Debug.Log("GameManager initialized successfully");
        }

        private void Start()
        {
            // Initialize all components first
            InitializeComponents();
            
            // Begin the game directly
            BeginGame();
            Debug.Log("Game started - Beginning initial setup");
        }

        private IEnumerator InitializeNewGame()
        {
            // Wait a frame to ensure all components are properly initialized
            yield return null;
            
            // Reset game state
            ResetGame(true);
            
            // Initialize UI
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            uiManager?.UpdateTargetScore(currentTargetScore);
            
            // Begin the game
            BeginGame();
            
            Debug.Log("Game initialization complete");
        }
        
        /// <summary>
        /// Initializes all required game components, finding them in the scene if not already assigned
        /// </summary>
        private void InitializeComponents()
        {
            // Find components if not assigned in inspector
            tableCards = FindAndValidateComponent(tableCards, "TableCards");
            playerPlayer = FindAndValidateComponent(playerPlayer as Player_Blackjack, "Player") as Player;
            dealerPlayer = FindAndValidateComponent(dealerPlayer as Dealer_Player_Blackjack, "Dealer Player") as Player;
            dealer = FindAndValidateComponent(dealer, "Dealer");
            deck = FindAndValidateComponent(deck, "Deck");
            audioManager = FindAndValidateComponent(audioManager, "Audio Manager");
            uiManager = FindAndValidateComponent(uiManager, "UI Manager");

            // Validate UI buttons
            if (hitButton == null || standButton == null)
            {
                Debug.LogError("Player interaction buttons not assigned in GameManager!");
            }
        }

        /// <summary>
        /// Helper method to find and validate components
        /// </summary>
        private T FindAndValidateComponent<T>(T component, string componentName) where T : Component
        {
            if (component == null)
            {
                component = FindFirstObjectByType<T>();
                if (component == null)
                {
                    Debug.LogError($"{componentName} component not found in scene!");
                }
            }
            return component;
        }
        #endregion

        #region Game State Management
        /// <summary>
        /// Deals the initial cards to both player and dealer
        /// </summary>
        private IEnumerator DealInitialCards()
        {
            yield return GetAdjustedWaitForSeconds(GameParameters.INITIAL_DEAL_DELAY);

            // Deal first card to player
            yield return StartCoroutine(DealCardToPlayer());
            yield return GetAdjustedWaitForSeconds(GameParameters.DELAY_BETWEEN_CARDS);

            // Deal first card to dealer
            yield return StartCoroutine(DealCardToDealer());
            yield return GetAdjustedWaitForSeconds(GameParameters.DELAY_BETWEEN_CARDS);

            // Deal second card to player
            yield return StartCoroutine(DealCardToPlayer());
            
            // Check if player got perfect score
            int playerScore = playerPlayer.GetHandValue();
            if (playerScore == currentTargetScore)
            {
                audioManager?.PlayShockedVoiceLine();
                yield return StartCoroutine(HandleEndGame($"Player wins with perfect {currentTargetScore}!"));
                yield break;
            }
            
            yield return GetAdjustedWaitForSeconds(GameParameters.DELAY_BETWEEN_CARDS);

            // Deal second card to dealer (face down)
            Card dealerCard = deck.DrawCard();
            if (dealerCard != null)
            {
                Transform slot = dealerPlayer.GetNextAvailableSlot();
                if (slot != null)
                {
                    Debug.Log("Setting dealer's second card face down");
                    dealerCard.SetFaceDown(true);
                    
                    // Verify the card is face down
                    Debug.Log($"Dealer's second card: {dealerCard.rank} of {dealerCard.suit}, Face down: {dealerCard.IsFaceDown()}, Rotation: {dealerCard.transform.rotation.eulerAngles}");
                    
                    yield return StartCoroutine(dealer.DealCard(dealerCard, slot, true));
                    dealerPlayer.AddCardToHand(dealerCard);
                    
                    // Verify again after dealing
                    Debug.Log($"After dealing - Dealer's second card: {dealerCard.rank} of {dealerCard.suit}, Face down: {dealerCard.IsFaceDown()}, Rotation: {dealerCard.transform.rotation.eulerAngles}");
                }
            }

            isDealing = false;
            
            // Properly transition to player turn
            yield return StartCoroutine(TransitionToState(GameState.PlayerTurn));

            // Show tutorial if this is round 1 and we haven't shown it yet
            Debug.Log($"Checking tutorial condition - Current Round: {currentRound}");
            if (currentRound == 1 && !hasShownFirstGameTutorial)
            {
                Debug.Log("Showing tutorial for first game...");
                hasShownFirstGameTutorial = true;
                uiManager?.ShowTutorial();
            }
            
            uiManager?.UpdateUI();
        }

        /// <summary>
        /// Resets the state variables for a new round
        /// </summary>
        private void ResetRoundState()
        {
            isDealing = false;
            isReturningCards = false;
            isRandomizingTarget = false;
            isTransitioningState = false;
            isPlayerWinner = false;
            isPush = false; // Reset push state
            isPausedForTutorial = false;
        }

        /// <summary>
        /// Resets the game state, optionally performing a full reset
        /// </summary>
        /// <param name="fullReset">If true, resets progress except for wins</param>
        private void ResetGame(bool fullReset = false)
        {
            // Always reset round to 1 if dealer won or it's a full reset
            bool shouldResetRound = fullReset || !isPlayerWinner;
            
            if (shouldResetRound)
            {
                currentRound = 1;
                // Round 1 always uses default target score (21)
                currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
                Debug.Log("Reset to Round 1 - Using default target score (21)");
            }

            if (fullReset)
            {
                // Only reset tutorial flag, keep wins persistent
                hasShownFirstGameTutorial = false;
            }

            // Reset state
            ResetRoundState();
            
            // Clear hands
            ClearPlayerHands();
            
            // Reset UI
            ResetUI();
            
            // Reset dealer and begin new game
            dealer?.ResetInitialDelay();
        }

        /// <summary>
        /// Clears the hands of both player and dealer
        /// </summary>
        private void ClearPlayerHands()
        {
            playerPlayer?.ClearHand();
            dealerPlayer?.ClearHand();
        }

        /// <summary>
        /// Resets all UI elements to their default state
        /// </summary>
        private void ResetUI()
        {
            if (uiManager != null)
            {
                uiManager.ResetUI();
                uiManager.UpdateStatusDisplays(currentRound, currentWins);
                uiManager.UpdateTargetScore(currentTargetScore);
                uiManager.SetGameStatus(GameParameters.SHUFFLING_TEXT);
            }
        }

        /// <summary>
        /// Begins a new game, initializing all necessary components
        /// </summary>
        public void BeginGame()
        {
            // Stop any existing coroutines
            StopAllCoroutines();
            
            // Reset the game state
            ResetGame(true);
            
            if (!ValidateGameComponents()) return;

            // Initialize UI and game components
            InitializeCardSlots();
            ResetPlayerSlots();
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            uiManager?.UpdateTargetScore(currentTargetScore);

            // Start the game initialization sequence
            StartCoroutine(InitializeGameSequence());
            
            Debug.Log("Game initialization started");
        }

        private IEnumerator InitializeGameSequence()
        {
            // First transition to waiting state
            yield return StartCoroutine(TransitionToState(GameState.WaitingToStart));
            
            // Set dealing flag and shuffle
            isDealing = true;
            deck?.ShuffleDeck();
            
            // Start dealing cards
            yield return StartCoroutine(DealInitialCards());
        }

        /// <summary>
        /// Validates that all required game components are present
        /// </summary>
        private bool ValidateGameComponents()
        {
            if (tableCards == null)
            {
                Debug.LogError("TableCards reference is missing in GameManager!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initializes the card slots for both player and dealer
        /// </summary>
        private void InitializeCardSlots()
        {
            if (tableCards != null)
            {
                playerPlayer.cardSlots = tableCards.GetPlayerSlots();
                dealerPlayer.cardSlots = tableCards.GetDealerSlots();
            }
        }

        /// <summary>
        /// Resets the card slots for both player and dealer
        /// </summary>
        private void ResetPlayerSlots()
        {
            playerPlayer?.ResetSlots();
            dealerPlayer?.ResetSlots();
            uiManager?.UpdateScores();
        }
        #endregion

        private void StopAllGameCoroutines()
        {
            if (dealerPlayCoroutine != null)
            {
                StopCoroutine(dealerPlayCoroutine);
                dealerPlayCoroutine = null;
            }
            if (intermissionCoroutine != null)
            {
                StopCoroutine(intermissionCoroutine);
                intermissionCoroutine = null;
            }
        }

        private void SafeStartCoroutine(IEnumerator routine)
        {
            if (!isShuttingDown)
            {
                var coroutine = StartCoroutine(routine);
                activeCoroutines.Add(coroutine);
            }
        }

        private void SafeStopCoroutine(Coroutine routine)
        {
            if (routine != null && activeCoroutines.Contains(routine))
            {
                StopCoroutine(routine);
                activeCoroutines.Remove(routine);
            }
        }

        private void StopAllActiveCoroutines()
        {
            foreach (var coroutine in activeCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            activeCoroutines.Clear();
        }

        private IEnumerator TransitionToState(GameState newState)
        {
            if (isTransitioningState)
            {
                Debug.LogWarning($"Already transitioning state, ignoring request to transition to {newState}");
                yield break;
            }

            isTransitioningState = true;
            GameState previousState = currentGameState;
            
            try
            {
                Debug.Log($"Transitioning from {previousState} to {newState}");
                
                // Cleanup previous state
                switch (previousState)
                {
                    case GameState.PlayerTurn:
                        SetPlayerButtonsInteractable(false);
                        break;
                    case GameState.DealerTurn:
                        StopAllGameCoroutines();
                        break;
                }

                // Initialize new state
                currentGameState = newState;
                switch (newState)
                {
                    case GameState.WaitingToStart:
                        uiManager?.SetGameStatus(GameParameters.SHUFFLING_TEXT);
                        break;
                    case GameState.PlayerTurn:
                        SetPlayerButtonsInteractable(true);
                        uiManager?.SetGameStatus("Your turn");
                        break;
                    case GameState.DealerTurn:
                        uiManager?.ShowDealerThinking();
                        break;
                    case GameState.GameOver:
                        SetPlayerButtonsInteractable(false);
                        StopAllGameCoroutines();
                        break;
                    case GameState.Completed:
                        SetPlayerButtonsInteractable(false);
                        break;
                }

                yield return null; // Allow frame to complete
            }
            finally
            {
                isTransitioningState = false;
            }
        }

        private IEnumerator HandleRoundEnd()
        {
            Debug.Log($"Handling round end - Current Round: {currentRound}, Player Winner: {isPlayerWinner}, Push: {isPush}");
            
            // Wait for any existing voice lines to finish
            if (audioManager != null)
            {
                float currentVoiceDuration = audioManager.GetCurrentVoiceClipDuration();
                if (currentVoiceDuration > 0)
                {
                    yield return GetAdjustedWaitForSeconds(currentVoiceDuration);
                }
            }

            if (isPlayerWinner)
            {
                // Play win voice line since dealer lost
                audioManager?.PlayWinVoiceLine();
                
                if (currentRound < GameParameters.MAX_ROUNDS)
                {
                    Debug.Log($"Advancing to round {currentRound + 1}");
                    yield return GetAdjustedWaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                    yield return StartCoroutine(ProcessIntermission());
                }
                else
                {
                    Debug.Log("Game complete!");
                    uiManager?.SetGameStatus("Congratulations! Game Complete!");
                    yield return GetAdjustedWaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                    
                    // Instead of resetting, transition to completed state
                    yield return StartCoroutine(TransitionToState(GameState.Completed));
                    
                    // Disable all player controls
                    SetPlayerButtonsInteractable(false);
                    
                    // Clear the table
                    isReturningCards = true;
                    yield return StartCoroutine(ReturnAllCardsToDeck());
                    isReturningCards = false;
                    
                    // Reset slots but don't start a new game
                    playerPlayer?.ResetSlots();
                    dealerPlayer?.ResetSlots();
                }
            }
            else if (isPush)
            {
                // It's a push - restart the current round without resetting progress
                audioManager?.PlayRandomGenericVoiceLine();
                Debug.Log($"Push - Restarting round {currentRound}");
                yield return GetAdjustedWaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                
                // Clear the table but don't reset round number
                isReturningCards = true;
                yield return StartCoroutine(ReturnAllCardsToDeck());
                isReturningCards = false;
                
                // Reset player states after cards are returned
                playerPlayer?.ResetSlots();
                dealerPlayer?.ResetSlots();
                
                // Start the same round again
                yield return StartCoroutine(TransitionToState(GameState.Playing));
                InitializeCardSlots();
                
                // Update UI
                uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
                uiManager?.SetGameStatus($"Push! Restarting Round {currentRound}...");
                yield return GetAdjustedWaitForSeconds(1f);
                uiManager?.SetGameStatus("Shuffling...");
                
                // Ensure deck is properly shuffled
                deck?.ShuffleDeck();
                
                // Deal cards
                isDealing = true;
                yield return StartCoroutine(DealInitialCards());
                
                // Enable player controls
                SetPlayerButtonsInteractable(true);
            }
            else
            {
                // Play lose voice line since dealer won
                audioManager?.PlayLoseVoiceLine();
                Debug.Log("Dealer won - Resetting to round 1");
                yield return GetAdjustedWaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                ResetGame(true); // This will reset round to 1 and clear all progress
            }
        }

        private IEnumerator ProcessIntermission()
        {
            Debug.Log($"Starting intermission - Current Round: {currentRound}");
            
            yield return StartCoroutine(TransitionToState(GameState.Intermission));
            isReturningCards = true;

            // Disable player controls during intermission
            SetPlayerButtonsInteractable(false);

            // Clear the UI and update round display
            uiManager?.SetGameStatus("Round Complete!");
            yield return GetAdjustedWaitForSeconds(1f);

            // Return all cards and wait for completion
            yield return StartCoroutine(ReturnAllCardsToDeck());
            
            isReturningCards = false;
            
            // Check if we've completed round 3 and player has won
            if (currentRound >= 3 && isPlayerWinner)
            {
                // Game is complete, transition to completed state
                yield return StartCoroutine(TransitionToState(GameState.Completed));
                uiManager?.SetGameStatus("Congratulations! You've beaten the dealer!");
                
                // Ensure all UI elements are properly hidden
                yield return GetAdjustedWaitForSeconds(1f);
                
                // Show feedback form
                uiManager?.ShowFeedbackForm();
                
                // Play win sound and voice line
                audioManager?.PlaySound(SoundType.Win);
                audioManager?.PlayWinVoiceLine();
                
                yield break;
            }
            
            // Handle round progression:
            // - If player won, increment round
            // - If it's a push, keep current round
            // - If dealer won, round already reset to 1 in HandleDealerWin
            if (isPlayerWinner)
            {
                currentRound++;
                Debug.Log($"Player won - advancing to round {currentRound}");
            }
            else if (isPush)
            {
                Debug.Log($"Push - keeping current round {currentRound}");
            }
            else
            {
                Debug.Log($"Dealer won - round was reset to 1");
            }
            
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            Debug.Log($"Round progression - Current Round: {currentRound}, Wins: {currentWins}");

            // Play round transition sounds
            audioManager?.PlaySound(SoundType.RoundStart);
            yield return GetAdjustedWaitForSeconds(GameParameters.ROUND_START_SOUND_DELAY);

            // Only randomize target score for rounds 2 and 3
            if (currentRound > 1)
            {
                isRandomizingTarget = true;
                yield return StartCoroutine(RandomizeTargetScore());
                isRandomizingTarget = false;
            }
            else
            {
                // Round 1 always uses default target score (21)
                currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
                uiManager?.UpdateTargetScore(currentTargetScore);
            }
            
            // Update UI for new round
            uiManager?.SetGameStatus("New Round Starting...");
            yield return GetAdjustedWaitForSeconds(1f);
            uiManager?.SetGameStatus("Shuffling...");
            
            // Ensure deck is properly shuffled
            deck?.ShuffleDeck();
            
            // Reinitialize for new round
            yield return StartCoroutine(TransitionToState(GameState.Playing));
            InitializeCardSlots();
            yield return StartCoroutine(DealInitialCards());
            
            // Enable player controls
            SetPlayerButtonsInteractable(true);
        }

        private IEnumerator RandomizeTargetScore()
        {
            int previousScore = currentTargetScore;
            int newTargetScore;
            
            do
            {
                newTargetScore = Random.Range(
                    GameParameters.MIN_TARGET_SCORE,
                    GameParameters.MAX_TARGET_SCORE + 1
                );
            } while (newTargetScore == previousScore);

            currentTargetScore = newTargetScore;
            Debug.Log($"New target score: {currentTargetScore}");
            
            yield return StartCoroutine(uiManager.AnimateTargetScoreRandomization(newTargetScore));
        }

        private IEnumerator PlayRoundStartSounds()
        {
            audioManager?.PlaySound(SoundType.RoundStart);
            yield return GetAdjustedWaitForSeconds(GameParameters.ROUND_START_SOUND_DELAY);
            
            if (currentRound > 1)
            {
                audioManager?.PlayRandomGenericVoiceLine();
                yield return GetAdjustedWaitForSeconds(1f);
            }
        }

        public IEnumerator ReturnCardToDeck(Card card)
        {
            if (card == null) yield break;

            yield return StartCoroutine(deck.ReturnCard(card));
            yield return GetAdjustedWaitForSeconds(GameParameters.CARD_RETURN_DELAY);
        }

        private IEnumerator ReturnAllCardsToDeck()
        {
            isReturningCards = true;
            var allCards = new List<Card>();
            
            // Get all cards and detach from current slots but keep references
            foreach (var card in playerPlayer.GetAllCards().Concat(dealerPlayer.GetAllCards()))
            {
                if (card != null)
                {
                    allCards.Add(card);
                    // Detach from current parent but don't destroy
                    card.transform.SetParent(null);
                }
            }

            // Return cards one by one with animation and sound
            foreach (Card card in allCards)
            {
                if (card != null && card.gameObject != null)
                {
                    // Keep the current card orientation instead of forcing face down
                    // card.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    audioManager?.PlaySound(SoundType.CardReturn);
                    yield return StartCoroutine(card.ReturnToDeck(deck.transform.position));
                    if (card != null && card.gameObject != null)
                    {
                        // Keep the current card orientation after animation completes
                        // card.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                        card.transform.SetParent(deck.transform);
                    }
                    // Only parent to deck after animation completes
                    yield return GetAdjustedWaitForSeconds(GameParameters.SEQUENTIAL_CARD_RETURN_DELAY);
                }
            }

            // Now safe to clear hands as cards are properly parented
            dealerPlayer.ClearHand();
            playerPlayer.ClearHand();
            ResetPlayerSlots();

            deck.LoadCards();
            deck.ShuffleDeck();

            isReturningCards = false;
            Debug.Log("All cards returned to deck and deck reshuffled");
        }

        public IEnumerator DealCardToPlayer()
        {
            Card card = deck.DrawCard();
            if (card == null)
            {
                Debug.LogError("No card available for player");
                yield break;
            }

            Transform slot = playerPlayer.GetNextAvailableSlot();
            if (slot == null)
            {
                Debug.LogError("No slot available for player card");
                yield break;
            }

            card.SetFaceDown(false);
            yield return StartCoroutine(dealer.DealCard(card, slot, false));
            playerPlayer.AddCardToHand(card);
            uiManager?.UpdateUI();
        }

        public IEnumerator DealCardToDealer()
        {
            Card card = deck.DrawCard();
            if (card == null)
            {
                Debug.LogError("No card available for dealer");
                yield break;
            }

            Transform slot = dealerPlayer.GetNextAvailableSlot();
            if (slot == null)
            {
                Debug.LogError("No slot available for dealer card");
                yield break;
            }

            // In blackjack, only the dealer's second card should be face down
            // First card is always face up
            bool faceDown = isDealing && dealerPlayer.GetAllCards().Count == 1;
            
            Debug.Log($"Dealing card to dealer - Face down: {faceDown}, Card count: {dealerPlayer.GetAllCards().Count}");
            
            card.SetFaceDown(faceDown);
            yield return StartCoroutine(dealer.DealCard(card, slot, faceDown));
            dealerPlayer.AddCardToHand(card);
            uiManager?.UpdateUI();
        }

        private IEnumerator ProcessDealerTurn()
        {
            Debug.Log("Starting dealer's turn");
            StopAllGameCoroutines();
            
            // Ensure we're in dealer turn state before proceeding
            if (currentGameState != GameState.DealerTurn)
            {
                yield return StartCoroutine(TransitionToState(GameState.DealerTurn));
            }
            
            uiManager?.SetGameStatus("Dealer's Turn");
            
            // Play a card reveal sound before flipping
            audioManager?.PlaySound(SoundType.CardFlip);
            
            // Reveal dealer's hidden cards
            yield return StartCoroutine(FlipDealerCard());
            yield return GetAdjustedWaitForSeconds(1f);

            int playerScore = playerPlayer.GetHandValue();
            int dealerScore = dealerPlayer.GetHandValue();
            
            if (playerScore > currentTargetScore)
            {
                string message = $"Dealer wins! Player busted with {playerScore}";
                yield return StartCoroutine(HandleEndGame(message));
                yield break;
            }

            // Start dealer play loop as a tracked coroutine
            dealerPlayCoroutine = StartCoroutine(_DealerPlayLoop());
            yield return dealerPlayCoroutine;
        }

        private IEnumerator _DealerPlayLoop()
        {
            Debug.Log("Starting dealer play loop");
            
            while (currentGameState == GameState.DealerTurn && !isPaused && !isPausedForTutorial)
            {
                // Check if dealer should hit
                if (dealerPlayer.ShouldHit())
                {
                    uiManager?.ShowDealerThinking();
                    
                    // Use the animation speed multiplier from UIManager if available
                    float adjustedDelay = dealerPlayDelay;
                    if (uiManager != null && uiManager.IsDoubleDealingSpeedEnabled())
                    {
                        adjustedDelay *= uiManager.GetAnimationSpeedMultiplier();
                    }
                    yield return GetAdjustedWaitForSeconds(adjustedDelay);
                    
                    // Deal a card to dealer
                    yield return StartCoroutine(DealCardToDealer());
                    
                    // Check for bust or target score
                    int dealerScore = dealerPlayer.GetHandValue();
                    if (dealerScore > currentTargetScore)
                    {
                        yield return StartCoroutine(HandleEndGame($"Player wins! Dealer busted with {dealerScore}"));
                        yield break;
                    }
                    else if (dealerScore == currentTargetScore)
                    {
                        yield return StartCoroutine(HandleEndGame($"Dealer wins with {dealerScore}!"));
                        yield break;
                    }
                }
                else
                {
                    // Dealer stands, determine winner
                    int dealerScore = dealerPlayer.GetHandValue();
                    int playerScore = playerPlayer.GetHandValue();
                    yield return StartCoroutine(HandleEndGame(DetermineWinMessage(dealerScore, playerScore)));
                    yield break;
                }
                
                // Use the animation speed multiplier for this delay as well
                float adjustedLoopDelay = 0.5f;
                if (uiManager != null && uiManager.IsDoubleDealingSpeedEnabled())
                {
                    adjustedLoopDelay *= uiManager.GetAnimationSpeedMultiplier();
                }
                yield return GetAdjustedWaitForSeconds(adjustedLoopDelay);
            }
        }
        
        /// <summary>
        /// Makes DealerPlayLoop accessible to UIManager
        /// </summary>
        public IEnumerator DealerPlayLoop()
        {
            return _DealerPlayLoop();
        }

        private string DetermineWinMessage(int dealerScore, int playerScore)
        {
            if (dealerScore > currentTargetScore)
            {
                ProcessWin(true);
                return $"Player wins! Dealer busted with {dealerScore}";
            }
            
            if (playerScore > currentTargetScore)
            {
                ProcessWin(false);
                return $"Dealer wins! Player busted with {playerScore}";
            }
            
            if (dealerScore > playerScore)
            {
                ProcessWin(false);
                return $"Dealer wins with {dealerScore}!";
            }
            
            if (playerScore > dealerScore)
            {
                ProcessWin(true);
                return $"Player wins with {playerScore}!";
            }
            
            // Handle tie - now call it a Push and set a special flag
            ProcessPush();
            return $"Push at {playerScore}! Tie game!";
        }

        private IEnumerator FlipDealerCard()
        {
            Debug.Log("FlipDealerCard called - Starting to flip dealer cards");
            
            if (dealerPlayer == null)
            {
                Debug.LogError("Dealer player is null");
                yield break;
            }

            var dealerCards = dealerPlayer.GetAllCards();
            Debug.Log($"Dealer has {dealerCards.Count} cards in hand");
            
            if (dealerCards.Count == 0)
            {
                Debug.LogError("Dealer has no cards");
                yield break;
            }
            
            // Log all dealer cards and their face-down status
            for (int i = 0; i < dealerCards.Count; i++)
            {
                var card = dealerCards[i];
                if (card == null)
                {
                    Debug.LogError($"Dealer card at index {i} is null");
                    continue;
                }
                
                Debug.Log($"Dealer card {i}: {card.rank} of {card.suit}, Face down: {card.IsFaceDown()}, Rotation: {card.transform.rotation.eulerAngles}");
            }

            // Find all face down cards
            var faceDownCards = dealerCards.Where(card => card != null && card.IsFaceDown()).ToList();
            
            Debug.Log($"Found {faceDownCards.Count} face down dealer cards");
            
            // If no face-down cards were found but we have at least 2 cards, flip the second card
            if (faceDownCards.Count == 0 && dealerCards.Count >= 2)
            {
                Debug.Log("No face down cards found, but flipping the second card anyway");
                var secondCard = dealerCards[1];
                
                // Force the card to be face down before flipping
                secondCard.SetFaceDown(true);
                
                // Add to face down cards list
                faceDownCards.Add(secondCard);
            }
            else if (faceDownCards.Count == 0)
            {
                Debug.LogWarning("No face down dealer cards to flip and not enough cards to force flip");
                yield break;
            }
            
            // Add a small dramatic pause before flipping
            yield return GetAdjustedWaitForSeconds(0.5f);
            
            // Flip each face down card with a sound effect
            foreach (var card in faceDownCards)
            {
                Debug.Log($"Flipping dealer card: {card.rank} of {card.suit}");
                
                // Play a sound effect for the card flip
                audioManager?.PlaySound(SoundType.CardFlip);
                
                // Flip the card
                yield return StartCoroutine(card.FlipCard());
                
                // Log the card state after flipping
                Debug.Log($"After flip - Card: {card.rank} of {card.suit}, Face down: {card.IsFaceDown()}, Rotation: {card.transform.rotation.eulerAngles}");
                
                // Update UI after the card is flipped
                uiManager?.UpdateScores();
                
                // Add a small delay between flips if there are multiple cards
                if (faceDownCards.Count > 1)
                {
                    yield return GetAdjustedWaitForSeconds(GameParameters.CARD_FLIP_DURATION);
                }
            }
            
            Debug.Log("Finished flipping dealer cards");
            
            // Play a dealer voice line after all cards are flipped
            audioManager?.PlayRandomGenericVoiceLine();
        }

        /// <summary>
        /// Deals a card to a specific slot
        /// </summary>
        private IEnumerator DealCardToSlot(Card card, Transform slot, bool faceDown = false)
        {
            if (card == null || slot == null) yield break;

            card.SetFaceDown(faceDown);
            yield return StartCoroutine(dealer.DealCard(card, slot, faceDown));
        }

        /// <summary>
        /// Processes a win or loss for the player
        /// </summary>
        /// <param name="isPlayerWin">True if the player won, false if the dealer won</param>
        private void ProcessWin(bool isPlayerWin)
        {
            isPlayerWinner = isPlayerWin;
            Debug.Log($"Game result: {(isPlayerWin ? "Player wins" : "Dealer wins")}");
            
            if (isPlayerWin)
            {
                HandlePlayerWin();
            }
            else
            {
                HandleDealerWin();
            }
        }

        /// <summary>
        /// Handles processing when the player wins
        /// </summary>
        private void HandlePlayerWin()
        {
            currentWins++;
            Debug.Log($"Player won - Wins incremented to {currentWins}");
            uiManager?.IncrementWins();
            
            // Force UI update to ensure win count is displayed
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            
            // Play dealer's lose voice line
            audioManager?.PlayLoseVoiceLine();
        }

        /// <summary>
        /// Handles processing when the dealer wins
        /// </summary>
        private void HandleDealerWin()
        {
            // Set round to 1 on dealer win, but don't reset wins
            currentRound = 1;
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            
            // Play dealer's win voice line
            audioManager?.PlayWinVoiceLine();
        }

        /// <summary>
        /// Determines the winner based on player and dealer scores
        /// </summary>
        private string DetermineWinner(int playerScore, int dealerScore)
        {
            Debug.Log($"Determining winner - Player: {playerScore}, Dealer: {dealerScore}, Target: {currentTargetScore}");
            
            if (dealerScore > currentTargetScore)
            {
                ProcessWin(true);
                return $"Player wins! Dealer busted with {dealerScore}";
            }
            
            if (playerScore > currentTargetScore)
            {
                ProcessWin(false);
                return $"Dealer wins! Player busted with {playerScore}";
            }
            
            if (dealerScore > playerScore)
            {
                ProcessWin(false);
                return $"Dealer wins with {dealerScore}!";
            }
            
            if (playerScore > dealerScore)
            {
                ProcessWin(true);
                return $"Player wins with {playerScore}!";
            }
            
            // Handle tie - dealer wins by house rules
            ProcessWin(false);
            return $"Tie at {playerScore}! Dealer wins!";
        }

        /// <summary>
        /// Handles the end of a game, processing the win/loss and transitioning to the next state
        /// </summary>
        private IEnumerator HandleEndGame(string message)
        {
            Debug.Log($"HandleEndGame: {message}");
            isDealing = false;
            SetPlayerButtonsInteractable(false);

            // Process the win/loss based on the message
            ProcessWinFromMessage(message);

            // Update UI and wait for transitions
            yield return GetAdjustedWaitForSeconds(1f);
            uiManager?.SetGameStatus(message);

            yield return GetAdjustedWaitForSeconds(2f);
            StartCoroutine(ProcessIntermission());
        }

        #region Game End Processing
        /// <summary>
        /// Processes a win/loss based on the end game message
        /// </summary>
        private void ProcessWinFromMessage(string message)
        {
            if (message.Contains("Player wins"))
            {
                ProcessWin(true);
            }
            else if (message.Contains("Dealer wins"))
            {
                ProcessWin(false);
            }
            else if (message.Contains("Push"))
            {
                ProcessPush();
            }
        }
        #endregion

        #region Player Actions
        public void OnPlayerHit()
        {
            if (currentGameState != GameState.PlayerTurn || isDealing)
            {
                Debug.Log($"Hit ignored - State: {currentGameState}, IsDealing: {isDealing}");
                return;
            }

            Card card = deck.DrawCard();
            if (card != null)
            {
                Debug.Log($"Drew card for hit: {card.rank} of {card.suit}");
                SetPlayerButtonsInteractable(false);
                isDealing = true;

                Transform slot = playerPlayer.GetNextAvailableSlot();
                if (slot != null)
                {
                    StartCoroutine(DealHitCard(card, slot));
                }
            }
        }

        private IEnumerator DealHitCard(Card card, Transform slot)
        {
            yield return StartCoroutine(DealCardToSlot(card, slot, false));
            
            // Add card to player's hand and update UI
            playerPlayer.AddCardToHand(card);
            uiManager?.UpdateUI();
            
            // Check if player hit target score or busted
            yield return StartCoroutine(CheckForTargetScore(true));
            
            if (currentGameState == GameState.PlayerTurn)  // Only re-enable buttons if game hasn't ended
            {
                // Enable buttons for next action
                SetPlayerButtonsInteractable(true);
            }
            
            isDealing = false;
        }

        public void OnPlayerStand()
        {
            if (currentGameState != GameState.PlayerTurn || isDealing)
                return;

            SetPlayerButtonsInteractable(false);
            StartCoroutine(ProcessDealerTurn());
        }
        #endregion

        #region UI and Audio
        public void PauseForTutorial(bool pause)
        {
            isPausedForTutorial = pause;
            SetPlayerButtonsInteractable(!pause);
        }

        public void SetPlayerButtonsInteractable(bool interactable)
        {
            if (isPausedForTutorial)
            {
                interactable = false;
            }
            if (isPaused)
            {
                interactable = false;
            }
            hitButton?.IsInteractable(interactable);
            standButton?.IsInteractable(interactable);
        }

        public void PlayGoodMoveLine()
        {
            audioManager?.PlayRandomVoiceLine(audioManager.goodMoveVoiceClips);
        }

        public void PlayBadMoveLine()
        {
            audioManager?.PlayRandomVoiceLine(audioManager.badMoveVoiceClips);
        }
        
        public void SetGamePaused(bool paused)
        {
            isPaused = paused;
            
            // Disable player interaction while paused
            SetPlayerButtonsInteractable(!paused);
            
            // Stop any ongoing dealer actions if needed
            if (paused && dealer != null)
            {
                StopAllCoroutines();
            }
        }
        #endregion

        /// <summary>
        /// Checks if a player has hit the target score and handles the win condition
        /// </summary>
        private IEnumerator CheckForTargetScore(bool isPlayerTurn)
        {
            int playerScore = playerPlayer.GetHandValue();
            int dealerScore = dealerPlayer.GetHandValue();
            int targetScore = currentTargetScore;

            // Check if current player hit target score
            if (isPlayerTurn && playerScore == targetScore)
            {
                // Play shocked voice line first for instant win
                audioManager?.PlayShockedVoiceLine();
                yield return GetAdjustedWaitForSeconds(0.5f); // Give time for shocked voice line to play
                ProcessWin(true);
                yield return StartCoroutine(HandleEndGame($"Player wins with perfect {targetScore}!"));
            }
            else if (!isPlayerTurn && dealerScore == targetScore)
            {
                ProcessWin(false);
                yield return StartCoroutine(HandleEndGame($"Dealer wins with perfect {targetScore}!"));
            }
            // Check if player busted
            else if (isPlayerTurn && playerScore > targetScore)
            {
                ProcessWin(false);
                yield return StartCoroutine(HandleEndGame($"Dealer wins! Player busted with {playerScore}"));
            }
            // Check if dealer busted
            else if (!isPlayerTurn && dealerScore > targetScore)
            {
                ProcessWin(true);
                yield return StartCoroutine(HandleEndGame($"Player wins! Dealer busted with {dealerScore}"));
            }
        }

        /// <summary>
        /// Handles processing when it's a push (tie)
        /// </summary>
        private void ProcessPush()
        {
            // Set the flags accordingly - not a player win, but a push
            isPlayerWinner = false;
            isPush = true;
            Debug.Log("Game result: Push (Tie)");
            
            // Don't reset wins when it's a push, just play voice line
            audioManager?.PlayRandomGenericVoiceLine();
        }
    }
}
