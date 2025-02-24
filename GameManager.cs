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
            yield return new WaitForSeconds(GameParameters.INITIAL_DEAL_DELAY);

            // Deal first card to player
            yield return StartCoroutine(DealCardToPlayer());
            yield return new WaitForSeconds(GameParameters.DELAY_BETWEEN_CARDS);

            // Deal first card to dealer
            yield return StartCoroutine(DealCardToDealer());
            yield return new WaitForSeconds(GameParameters.DELAY_BETWEEN_CARDS);

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
            
            yield return new WaitForSeconds(GameParameters.DELAY_BETWEEN_CARDS);

            // Deal second card to dealer (face down)
            Card dealerCard = deck.DrawCard();
            if (dealerCard != null)
            {
                Transform slot = dealerPlayer.GetNextAvailableSlot();
                if (slot != null)
                {
                    dealerCard.SetFaceDown(true);
                    yield return StartCoroutine(dealer.DealCard(dealerCard, slot, true));
                    dealerPlayer.AddCardToHand(dealerCard);
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
            isPausedForTutorial = false;
        }

        /// <summary>
        /// Resets the game state, optionally performing a full reset
        /// </summary>
        /// <param name="fullReset">If true, resets all progress including rounds and wins</param>
        private void ResetGame(bool fullReset = false)
        {
            // Always reset round to 1 if dealer won (when player has 0 wins)
            bool shouldResetRound = fullReset || currentWins == 0;
            
            if (shouldResetRound)
            {
                currentRound = 1;
                // Round 1 always uses default target score (21)
                currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
                Debug.Log("Reset to Round 1 - Using default target score (21)");
            }

            if (fullReset)
            {
                currentWins = 0;
                hasShownFirstGameTutorial = false;  // Reset tutorial flag on full reset
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
            Debug.Log($"Handling round end - Current Round: {currentRound}, Player Winner: {isPlayerWinner}");
            
            // Wait for any existing voice lines to finish
            if (audioManager != null)
            {
                float currentVoiceDuration = audioManager.GetCurrentVoiceClipDuration();
                if (currentVoiceDuration > 0)
                {
                    yield return new WaitForSeconds(currentVoiceDuration);
                }
            }

            if (isPlayerWinner)
            {
                // Play win voice line since dealer lost
                audioManager?.PlayWinVoiceLine();
                
                if (currentRound < GameParameters.MAX_ROUNDS)
                {
                    Debug.Log($"Advancing to round {currentRound + 1}");
                    yield return new WaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                    yield return StartCoroutine(ProcessIntermission());
                }
                else
                {
                    Debug.Log("Game complete!");
                    uiManager?.SetGameStatus("Congratulations! Game Complete!");
                    yield return new WaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                    
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
            else
            {
                // Play lose voice line since dealer won
                audioManager?.PlayLoseVoiceLine();
                Debug.Log("Dealer won - Resetting to round 1");
                yield return new WaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
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
            yield return new WaitForSeconds(1f);

            // Return all cards with proper cleanup
            yield return StartCoroutine(ReturnAllCardsToDeck());
            
            // Reset player states after cards are returned
            playerPlayer?.ResetSlots();
            dealerPlayer?.ResetSlots();
            
            isReturningCards = false;
            
            // Check if we've completed round 3 and player has won
            if (currentRound >= 3 && isPlayerWinner)
            {
                // Game is complete, transition to completed state
                yield return StartCoroutine(TransitionToState(GameState.Completed));
                uiManager?.SetGameStatus("Congratulations! You've beaten the dealer!");
                
                // Ensure all UI elements are properly hidden
                yield return new WaitForSeconds(1f);
                
                // Show feedback form
                uiManager?.ShowFeedbackForm();
                
                // Play win sound and voice line
                audioManager?.PlaySound(SoundType.Win);
                audioManager?.PlayWinVoiceLine();
                
                yield break; // Exit the coroutine here to prevent further processing
            }
            
            // If player lost, we've already reset currentRound to 0 in HandleDealerWin
            // If player won, increment the round
            if (isPlayerWinner)
            {
                currentRound++;
            }
            else
            {
                currentRound = 1; // Ensure we're on round 1 after a loss
            }
            
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            Debug.Log($"Advanced to round {currentRound}");

            // Play round transition sounds
            audioManager?.PlaySound(SoundType.RoundStart);
            yield return new WaitForSeconds(GameParameters.ROUND_START_SOUND_DELAY);

            // Only randomize target score for rounds 2 and 3
            if (currentRound > 1)
            {
                // Randomize new target score
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
            yield return new WaitForSeconds(1f);
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
            yield return new WaitForSeconds(GameParameters.ROUND_START_SOUND_DELAY);
            
            if (currentRound > 1)
            {
                audioManager?.PlayRandomGenericVoiceLine();
                yield return new WaitForSeconds(1f);
            }
        }

        public IEnumerator ReturnCardToDeck(Card card)
        {
            if (card == null) yield break;

            yield return StartCoroutine(deck.ReturnCard(card));
            yield return new WaitForSeconds(GameParameters.CARD_RETURN_DELAY);
        }

        private IEnumerator ReturnAllCardsToDeck()
        {
            isReturningCards = true;

            // Get all cards from both players
            var allCards = new List<Card>();
            allCards.AddRange(playerPlayer.GetAllCards());
            allCards.AddRange(dealerPlayer.GetAllCards());

            // Start all card return animations simultaneously
            var returnTasks = new List<Coroutine>();
            foreach (Card card in allCards)
            {
                if (card != null)
                {
                    // Set parent and start return animation
                    card.transform.SetParent(deck.transform);
                    returnTasks.Add(StartCoroutine(card.ReturnToDeck(deck.transform.position)));
                }
            }

            // Clear hands immediately since we have the cards in our allCards list
            dealerPlayer.ClearHand();
            playerPlayer.ClearHand();
            ResetPlayerSlots();

            // Wait for all return animations to complete
            yield return new WaitForSeconds(GameParameters.CARD_RETURN_DURATION);

            // First reload the cards to ensure all are found
            deck.LoadCards();
            
            // Then shuffle the deck
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

            // During dealer's turn, all cards should be face up
            bool faceDown = false;
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
            
            yield return StartCoroutine(FlipDealerCard());
            yield return new WaitForSeconds(1f);

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
                    yield return new WaitForSeconds(dealerPlayDelay);
                    
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
                
                yield return new WaitForSeconds(0.5f);
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
            
            // Handle tie - dealer wins by house rules
            ProcessWin(false);
            return $"Tie at {playerScore}! Dealer wins!";
        }

        private IEnumerator FlipDealerCard()
        {
            if (dealerPlayer == null) yield break;

            var dealerCards = dealerPlayer.GetAllCards();
            if (dealerCards.Count == 0) yield break;

            // Find all face down cards
            var faceDownCards = dealerCards.Where(card => card != null && card.IsFaceDown()).ToList();
            
            // Flip each face down card with a small delay between flips
            foreach (var card in faceDownCards)
            {
                audioManager?.PlaySound(SoundType.CardFlip);
                yield return StartCoroutine(card.FlipCard());
                uiManager?.UpdateScores();
                yield return new WaitForSeconds(GameParameters.CARD_FLIP_DURATION);
            }
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
            currentWins = 0;
            currentRound = 0; // Reset to 0 since we increment in ProcessIntermission
            uiManager?.ResetWins();
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
            yield return new WaitForSeconds(1f);
            uiManager?.SetGameStatus(message);

            yield return new WaitForSeconds(2f);
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
                yield return new WaitForSeconds(0.5f); // Give time for shocked voice line to play
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
    }
}
