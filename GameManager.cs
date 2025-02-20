/* The above code is a C# script for a card game implemented in Unity. Here is a summary of what the
code is doing: */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame {
    public enum GameState {
        Initializing,
        WaitingToStart,
        InitialDeal,
        PlayerTurn,
        DealerTurn,
        GameOver
    }

    public class GameManager : MonoBehaviour {
        public Deck deck;
        public Dealer dealer;
        public TableCards tableCards;
        public Player_Blackjack player;
        public Dealer_Player_Blackjack dealerPlayer;
        public UIManager uiManager;
        private AudioManager audioManager;
        public bool dealerCardRevealed = false; // New flag to track if dealer's hidden card is revealed
        public bool SkipTutorial = false; // Add a public property to control tutorial skipping
        
        private GameState currentGameState = GameState.WaitingToStart;
        private bool isDealing = false;
        private bool isReturningCards = false;

        [Header("Target Score Settings")]
        public int currentRound = 1;
        public int round2MinTarget = 18;
        public int round2MaxTarget = 24;
        public int round3MinTarget = 15;
        public int round3MaxTarget = 27;
        private int currentTargetScore = 21;

        private bool isRandomizingTarget = false;
        public float randomizeDelay = 0.5f; // Changed from 0.2f to 0.5f for more visible changes
        private int randomizeTimes = 5;

        void Start() {
            Debug.Log("GameManager Start");
            currentGameState = GameState.Initializing; // Make sure we start in initializing state
            currentRound = 1;
            currentTargetScore = 21;
            isDealing = false;
            isRandomizingTarget = false;
            isReturningCards = false;
            dealerCardRevealed = false; // Reset flag when game starts
            InitializeComponents();
        }

        void InitializeComponents() {
            Debug.Log("Initializing components...");
            if (tableCards == null) tableCards = FindFirstObjectByType<TableCards>();
            if (deck == null) deck = FindFirstObjectByType<Deck>();
            if (dealer == null) dealer = FindFirstObjectByType<Dealer>();
            if (player == null) player = FindFirstObjectByType<Player_Blackjack>();
            if (dealerPlayer == null) dealerPlayer = FindFirstObjectByType<Dealer_Player_Blackjack>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();

            // Start initialization sequence
            StartCoroutine(ValidateComponentsNextFrame());
        }

        IEnumerator ValidateComponentsNextFrame() {
            yield return null;
            
            Debug.Log("Validating components...");
            if (deck == null || dealer == null || tableCards == null || player == null || dealerPlayer == null) {
                Debug.LogError("Missing required components in scene. Please ensure Deck, Dealer, TableCards, and Players are present.");
                yield break;
            }

            AssignCardSlots();
            
            // Wait a frame to ensure everything is set up
            yield return null;
            
            Debug.Log("Setting initial game state...");
            currentGameState = GameState.WaitingToStart;
            RefreshUI();
            
            // Automatically start the game
            Debug.Log("Starting initial game...");
            BeginGame();
        }

        void AssignCardSlots() {
            Debug.Log("Assigning card slots...");
            // Clear existing slots
            player.cardSlots.Clear();
            dealerPlayer.cardSlots.Clear();

            // Assign new slots from TableCards
            player.cardSlots.AddRange(tableCards.GetPlayerSlots());
            dealerPlayer.cardSlots.AddRange(tableCards.GetDealerSlots());

            Debug.Log($"Assigned {player.cardSlots.Count} slots to Player");
            Debug.Log($"Assigned {dealerPlayer.cardSlots.Count} slots to Dealer");
        }
        
        // New method to consolidate UI update calls.
        private void RefreshUI() {
            uiManager.UpdateUI();
        }

        // Public method to start the game when ready
        public void BeginGame() {
            Debug.Log($"BeginGame called. Current state: {currentGameState}");
            if (currentGameState != GameState.WaitingToStart) {
                Debug.LogWarning("Cannot start game - not in waiting state");
                return;
            }
            
            // Reset all states
            isDealing = false;
            isRandomizingTarget = false;
            isReturningCards = false;
            dealerCardRevealed = false;
            
            // Reset player and dealer
            player.ResetSlots();
            dealerPlayer.ResetSlots();
            
            // Force a UI refresh before starting initial deal
            if (uiManager != null) {
                uiManager.UpdateUI();
                uiManager.UpdateButtonStates();
            }
            
            StartInitialDeal();
        }

        private void StartInitialDeal() {
            Debug.Log("Starting initial deal...");
            if (!ValidateGameState()) {
                Debug.LogError("Failed to validate game state!");
                return;
            }
            
            Debug.Log("Game state validated, proceeding with initial deal");
            currentGameState = GameState.InitialDeal;
            player.ClearHand();
            dealerPlayer.ClearHand();
            dealer.ResetInitialDelay();
            dealerCardRevealed = false;
            
            // Ensure flags are reset before dealing
            isDealing = false;
            isRandomizingTarget = false;
            isReturningCards = false;
            
            deck.ShuffleDeck();
            StartCoroutine(DealInitialCards());
            RefreshUI();
        }

        private IEnumerator FlipDealerCard() {
            if (dealerPlayer.GetHand().Count >= 2) {
                Card card = dealerPlayer.GetHand()[1];
                if (card != null) {
                    dealerCardRevealed = true;
                    yield return StartCoroutine(card.FlipCard());
                }
            }
        }

        private IEnumerator DealInitialCards() {
            Debug.Log($"Starting initial deal. State: {currentGameState}, isDealing: {isDealing}");
            isDealing = true;
            
            if (uiManager != null) {
                uiManager.SetGameStatus("Shuffling Deck...");
            }

            // Deal the initial cards
            for (int i = 0; i < 2; i++) {
                // Deal to player first - always face up
                Card playerCard = deck.DealNextCard();
                if (playerCard != null) {
                    Transform playerSlot = player.GetNextAvailableSlot();
                    if (playerSlot != null) {
                        yield return StartCoroutine(dealer.DealCard(playerCard, playerSlot, false));
                        player.AddCardToHand(playerCard);
                        uiManager.UpdateUI();
                    }
                }

                yield return new WaitForSeconds(dealer.delayBetweenCards);

                // Then deal to dealer
                Card dealerCard = deck.DealNextCard();
                if (dealerCard != null) {
                    Transform dealerSlot = dealerPlayer.GetNextAvailableSlot();
                    if (dealerSlot != null) {
                        // Second card (i == 1) should be face down
                        bool shouldBeFaceDown = i == 1;
                        yield return StartCoroutine(dealer.DealCard(dealerCard, dealerSlot, shouldBeFaceDown));
                        dealerPlayer.AddCardToHand(dealerCard);
                        uiManager.UpdateUI();
                    }
                }

                if (i < 1) {
                    yield return new WaitForSeconds(dealer.delayBetweenCards);
                }
            }

            // Check for blackjack
            if (player.GetHandValue() == 21 || dealerPlayer.GetHandValue() == 21) {
                Debug.Log("Blackjack!");
                currentGameState = GameState.GameOver;
                yield return StartCoroutine(FlipDealerCard());
                string message = player.GetHandValue() == 21 ? "Blackjack! Player wins!" : "Blackjack! Dealer wins!";
                yield return StartCoroutine(HandleEndGame(message));
            } else {
                Debug.Log("No blackjack, starting player turn");
                isDealing = false;
                currentGameState = GameState.PlayerTurn;
                if (audioManager != null) {
                    audioManager.StartPlayerTurn();
                }
                // Force UI refresh to update button states
                if (uiManager != null) {
                    uiManager.UpdateUI();
                    // Call public method instead
                    RefreshUI();
                }
            }

            Debug.Log($"Deal complete. Current state: {currentGameState}, isDealing: {isDealing}");
        }

        private bool ValidateGameState() {
            return deck != null && dealer != null && tableCards != null && player != null && dealerPlayer != null;
        }

        private IEnumerator ReturnCardsToDeck() {
            isReturningCards = true;
            dealerCardRevealed = false;
            float returnDuration = 0.5f;

            // Get all cards from both player and dealer
            List<Card> allCards = new List<Card>();
            allCards.AddRange(player.GetHand());
            allCards.AddRange(dealerPlayer.GetHand());

            // Return each card to deck position
            foreach (Card card in allCards) {
                if (card != null && card.gameObject != null) {
                    // All cards should be face up when returning to deck
                    card.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                    
                    if (audioManager != null) {
                        audioManager.PlayCardReturnSound();
                    }

                    Vector3 startPos = card.transform.position;
                    Vector3 targetPos = deck.transform.position;
                    
                    float elapsedTime = 0f;
                    while (elapsedTime < returnDuration) {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / returnDuration;
                        
                        // Use smoothstep for more natural movement
                        float smoothT = t * t * (3f - 2f * t);
                        card.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                        yield return null;
                    }
                    
                    Destroy(card.gameObject);
                }
            }

            player.ClearHand();
            dealerPlayer.ClearHand();
            
            yield return new WaitForSeconds(0.5f);
            isReturningCards = false;
        }

        private IEnumerator HandleEndGame(string message) {
            Debug.Log($"EndGame started: {message}");
            currentGameState = GameState.GameOver;
            isDealing = true;
            
            if (message.Contains("Player wins") || message.Contains("Blackjack! Player wins")) {
                if (uiManager != null) {
                    uiManager.IncrementWins();
                    uiManager.SetGameStatus("Winner!");
                }
                if (audioManager != null) {
                    audioManager.PlayLoseVoiceLine();
                    yield return new WaitForSeconds(audioManager.GetCurrentVoiceClipDuration());
                }
            } else {
                // On loss, only update UI elements that don't depend on target score
                if (uiManager != null) {
                    uiManager.SetGameStatus("Round Lost");
                    uiManager.ResetWins();
                    uiManager.UpdateStatusDisplays(1, uiManager.currentWins);
                }
                if (audioManager != null && message.Contains("Dealer wins")) {
                    audioManager.PlayWinVoiceLine();
                    yield return new WaitForSeconds(audioManager.GetCurrentVoiceClipDuration());
                }
            }
            
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(ReturnCardsToDeck());

            // After cards are returned, handle score updates and round progression
            if (message.Contains("Player wins") || message.Contains("Blackjack! Player wins")) {
                if (currentRound < 3) {
                    currentRound++;
                    yield return new WaitForSeconds(0.5f);
                    yield return StartCoroutine(RandomizeNewTargetScore());
                } else {
                    currentRound = 1;
                    currentTargetScore = 21;
                    if (uiManager != null) {
                        uiManager.UpdateTargetScore(currentTargetScore);
                    }
                }
            } else {
                // Reset round and target score after cards are returned on loss
                currentRound = 1;
                currentTargetScore = 21;
                if (uiManager != null) {
                    uiManager.UpdateTargetScore(currentTargetScore);
                }
            }
            
            // Reset all state flags before starting new game
            isDealing = false;
            isRandomizingTarget = false;
            isReturningCards = false;
            dealerCardRevealed = false;
            // Set game state to waiting so that next game starts via a user interaction or tutorial exit
            currentGameState = GameState.WaitingToStart;
            
            // Reset hands for player and dealer
            player.ClearHand();
            dealerPlayer.ClearHand();
            
            Debug.Log("Game reset complete. Waiting to start new game...");
            yield return new WaitForSeconds(0.5f);
            // Removed BeginGame() call to avoid duplicate triggering.
            // Instead, the game will restart when the appropriate external event occurs (e.g., tutorial completion).
            RefreshUI();
        }
        
        // Add public getters for game state and scores
        public GameState GetCurrentGameState() {
            return currentGameState;
        }

        public int GetCurrentTargetScore() {
            return currentTargetScore;
        }

        public int GetPlayerScore() {
            return player != null ? player.GetHandValue() : 0;
        }

        public int GetDealerScore() {
            return dealerPlayer != null ? dealerPlayer.GetHandValue() : 0;
        }

        public bool IsDealing() {
            return isDealing;
        }

        public bool IsReturnOrRandomizing() {
            return isReturningCards || isRandomizingTarget;
        }

        public void OnPlayerHit() {
            if (currentGameState != GameState.PlayerTurn || isDealing) {
                Debug.LogWarning("Cannot hit - not player's turn or dealing in progress");
                return;
            }

            Card card = deck.DealNextCard();
            if (card != null) {
                Transform slot = player.GetNextAvailableSlot();
                if (slot != null) {
                    int previousScore = player.GetHandValue();
                    isDealing = true;
                    
                    // Update UI state before starting deal animation
                    if (uiManager != null) {
                        uiManager.UpdateButtonStates();
                    }
                    
                    StartCoroutine(ProcessHitCard(card, slot, previousScore));
                    
                    // Audio feedback
                    if (audioManager != null) {
                        audioManager.PlayHitButtonSound();
                    }
                }
            }
        }

        public void OnPlayerStand() {
            Debug.Log($"Stand button pressed. State: {currentGameState}, isDealing: {isDealing}");
            if (currentGameState != GameState.PlayerTurn || isDealing) {
                Debug.LogWarning("Cannot stand - not player's turn or dealing in progress");
                return;
            }

            // Change state and update UI before playing audio
            currentGameState = GameState.DealerTurn;
            if (uiManager != null) {
                uiManager.UpdateButtonStates();
            }
            
            // Audio feedback after UI update
            if (audioManager != null) {
                audioManager.PlayStandButtonSound();
                audioManager.EndPlayerTurn();
            }

            StartCoroutine(ProcessDealerTurn());
        }

        public void OnTutorialComplete() {
            if (currentGameState == GameState.WaitingToStart) {
                BeginGame();
            }
        }

        private IEnumerator ProcessHitCard(Card card, Transform slot, int previousScore) {
            // Player cards are always face up
            yield return StartCoroutine(dealer.DealCard(card, slot, false));
            player.AddCardToHand(card);
            uiManager.UpdateUI();
            
            if (audioManager != null) {
                audioManager.EvaluatePlayerMove(previousScore, true);
                if (player.GetHandValue() > currentTargetScore) {
                    audioManager.EndPlayerTurn();
                }
            }
            isDealing = false;

            // Check if player busted
            if (player.GetHandValue() > currentTargetScore) {
                currentGameState = GameState.GameOver;
                yield return StartCoroutine(HandleEndGame("Dealer wins! Player busted."));
            }
        }

        private IEnumerator ProcessDealerTurn() {
            yield return StartCoroutine(FlipDealerCard());
            
            while (dealerPlayer.ShouldHit()) {
                Card card = deck.DealNextCard();
                if (card != null) {
                    Transform slot = dealerPlayer.GetNextAvailableSlot();
                    if (slot != null) {
                        isDealing = true;
                        // Face down if it's dealer's second card in round 1, or third card in rounds 2-3
                        bool shouldBeFaceDown = (dealerPlayer.GetHand().Count == 1 && currentRound == 1) || 
                                             (dealerPlayer.GetHand().Count == 2 && (currentRound == 2 || currentRound == 3));
                        yield return StartCoroutine(dealer.DealCard(card, slot, shouldBeFaceDown));
                        dealerPlayer.AddCardToHand(card);
                        uiManager.UpdateUI();
                        isDealing = false;

                        // Add delay between dealer hits
                        yield return new WaitForSeconds(dealer.delayBetweenCards);
                    }
                }
            }

            // Determine winner
            int playerScore = player.GetHandValue();
            int dealerScore = dealerPlayer.GetHandValue();
            
            string endMessage;
            if (dealerScore > currentTargetScore) {
                endMessage = "Player wins! Dealer busted.";
            } else if (dealerScore >= playerScore) {
                endMessage = "Dealer wins!";
            } else {
                endMessage = "Player wins!";
            }
            
            yield return StartCoroutine(HandleEndGame(endMessage));
        }

        private IEnumerator RandomizeNewTargetScore() {
            isRandomizingTarget = true;
            int min = currentRound == 2 ? round2MinTarget : round3MinTarget;
            int max = currentRound == 2 ? round2MaxTarget : round3MaxTarget;

            // Initial delay for anticipation
            yield return new WaitForSeconds(0.5f);
            
            // Dynamic randomization timing
            float initialDelay = 0.15f; // Start faster
            float maxDelay = randomizeDelay;
            int iterations = randomizeTimes + 3; // Add more iterations for better effect
            
            int lastScore = currentTargetScore;
            
            for (int i = 0; i < iterations; i++) {
                // Calculate dynamic delay that gets longer towards the end
                float progress = (float)i / iterations;
                float currentDelay = Mathf.Lerp(initialDelay, maxDelay, progress);
                
                int randomScore;
                do {
                    randomScore = Random.Range(min, max + 1);
                } while (randomScore == lastScore); // Ensure we get a different number
                
                currentTargetScore = randomScore;
                lastScore = randomScore;
                
                if (uiManager != null) {
                    uiManager.UpdateTargetScore(currentTargetScore);
                }
                
                // Play appropriate sound based on iteration
                if (audioManager != null) {
                    if (i < iterations - 1) {
                        audioManager.PlayRandomTargetSound1();
                    } else {
                        // Final number sound
                        audioManager.PlayRandomTargetSound2();
                    }
                }
                
                yield return new WaitForSeconds(currentDelay);
            }
            
            // Extra pause on final number for emphasis
            yield return new WaitForSeconds(0.3f);
            isRandomizingTarget = false;
        }

        public void SetGameState(GameState state) {
            currentGameState = state;
        }
    }
}
