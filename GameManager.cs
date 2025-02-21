using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using UnityEngine;

namespace CardGame
{
    public enum GameState
    {
        Initializing,
        WaitingToStart,
        InitialDeal,
        PlayerTurn,
        DealerTurn,
        GameOver,
        Intermission,
    }

    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Player player;
        [SerializeField] private Player dealerPlayer;
        [SerializeField] private Dealer dealer;
        [SerializeField] private Deck deck;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private TableCards tableCards;

        [Header("Game State")]
        private GameState currentGameState = GameState.Initializing;
        private bool isDealing = false;
        private bool isReturningCards = false;
        private bool isRandomizingTarget = false;
        private bool isPlayerWinner = false;

        [Header("Game Progress")]
        [SerializeField] private int currentRound = 1;
        [SerializeField] private int currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
        private int currentWins = 0;

        [Header("Dealer Settings")]
        [SerializeField] private float dealerPlayDelay = GameParameters.DEALER_PLAY_DELAY;

        [Header("Player Interaction")]
        [SerializeField] private PanelButton hitButton;
        [SerializeField] private PanelButton standButton;

        private Coroutine dealerPlayCoroutine;
        private Coroutine intermissionCoroutine;

        private bool isTransitioningState = false;
        private HashSet<Coroutine> activeCoroutines = new HashSet<Coroutine>();
        private bool isShuttingDown = false;

        #region Properties
        public int GetCurrentRound() => currentRound;
        public int GetCurrentWins() => currentWins;
        public bool IsDealing() => isDealing;
        public bool IsReturnOrRandomizing() => isReturningCards || isRandomizingTarget;
        public GameState GetCurrentGameState() => currentGameState;
        public int GetCurrentTargetScore() => currentTargetScore;
        public int GetPlayerScore() => player != null ? player.GetHandValue() : 0;
        public int GetDealerScore() => dealerPlayer != null ? dealerPlayer.GetHandValue() : 0;
        public List<Card> GetDealerHand() => dealerPlayer != null ? dealerPlayer.GetHand() : new List<Card>();
        public AudioManager GetAudioManager() => audioManager;
        #endregion

        private void Awake()
        {
            InitializeComponents();
            Debug.Log("GameManager initialized");
        }

        private void Start()
        {
            BeginGame();
        }

        private void InitializeComponents()
        {
            tableCards = tableCards ?? FindFirstObjectByType<TableCards>();
            player = player ?? FindFirstObjectByType<Player_Blackjack>();
            dealerPlayer = dealerPlayer ?? FindFirstObjectByType<Dealer_Player_Blackjack>();
            dealer = dealer ?? FindFirstObjectByType<Dealer>();
            deck = deck ?? FindFirstObjectByType<Deck>();
            audioManager = audioManager ?? FindFirstObjectByType<AudioManager>();
            uiManager = uiManager ?? FindFirstObjectByType<UIManager>();
        }

        private void ResetRoundState()
        {
            Debug.Log($"Resetting round state - Current Round: {currentRound}, Wins: {currentWins}");
            isDealing = false;
            isReturningCards = false;
            isRandomizingTarget = false;
            isPlayerWinner = false;
        }

        private void ResetGame(bool fullReset)
        {
            Debug.Log($"ResetGame called - FullReset: {fullReset}, Current Round: {currentRound}, Current Wins: {currentWins}");
            
            if (fullReset)
            {
                currentRound = 1;
                currentTargetScore = GameParameters.DEFAULT_TARGET_SCORE;
                currentWins = 0;
                Debug.Log("Full game reset - All progress cleared");
            }

            ResetRoundState();
            
            // Clear hands and slots
            player?.ClearHand();
            dealerPlayer?.ClearHand();

            // Update UI
            uiManager?.ResetUI();
            uiManager?.UpdateStatusDisplays(currentRound, currentWins);
            uiManager?.UpdateTargetScore(currentTargetScore);
            uiManager?.SetGameStatus("Shuffling...");

            dealer?.ResetInitialDelay();
            BeginGame();
        }

        public void BeginGame()
        {
            Debug.Log($"Beginning game - Round: {currentRound}, Wins: {currentWins}");
            currentGameState = GameState.WaitingToStart;
            isDealing = true;

            if (tableCards == null)
            {
                Debug.LogError("TableCards reference is missing in GameManager!");
                return;
            }

            InitializeCardSlots();
            ResetPlayerSlots();

            deck?.ShuffleDeck();
            StartCoroutine(DealInitialCards());
        }

        private void InitializeCardSlots()
        {
            if (tableCards != null)
            {
                player.cardSlots = tableCards.GetPlayerSlots();
                dealerPlayer.cardSlots = tableCards.GetDealerSlots();
            }
        }

        private void ResetPlayerSlots()
        {
            player?.ResetSlots();
            dealerPlayer?.ResetSlots();
            uiManager?.UpdateScores();
        }

        private void ProcessWin(bool isPlayerWin)
        {
            isPlayerWinner = isPlayerWin;
            Debug.Log($"Processing win - Player Winner: {isPlayerWin}, Current Round: {currentRound}");
            
            if (isPlayerWin)
            {
                currentWins++;
                Debug.Log($"Player won - Wins incremented to {currentWins}");
                uiManager?.IncrementWins();
                
                // Force UI update to ensure win count is displayed
                uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            }
            else
            {
                Debug.Log("Player lost - resetting wins");
                currentWins = 0;
                uiManager?.ResetWins();
            }
        }

        private string DetermineWinner(int playerScore, int dealerScore)
        {
            Debug.Log($"Determining winner - Player: {playerScore}, Dealer: {dealerScore}, Target: {currentTargetScore}");
            
            string message;
            if (dealerScore > currentTargetScore)
            {
                message = $"Player wins! Dealer busted with {dealerScore}";
                ProcessWin(true);
            }
            else if (playerScore > currentTargetScore)
            {
                message = $"Dealer wins! Player busted with {playerScore}";
                ProcessWin(false);
            }
            else if (dealerScore > playerScore)
            {
                message = $"Dealer wins with {dealerScore}!";
                ProcessWin(false);
            }
            else if (playerScore > dealerScore)
            {
                message = $"Player wins with {playerScore}!";
                ProcessWin(true);
            }
            else
            {
                // Handle tie - dealer wins by house rules
                message = $"Tie at {playerScore}! Dealer wins!";
                ProcessWin(false);
            }

            return message;
        }

        private IEnumerator HandleEndGame(string message)
        {
            yield return StartCoroutine(TransitionGameState(GameState.GameOver));
            uiManager?.SetGameStatus(message);
            SetPlayerButtonsInteractable(false);
            
            // Update scores one final time to ensure accuracy
            uiManager?.UpdateScores();
            
            // Play appropriate voice line
            if (isPlayerWinner)
            {
                audioManager?.PlaySound(SoundType.Win);
            }
            else 
            {
                audioManager?.PlaySound(SoundType.Lose);
            }

            yield return new WaitForSeconds(GameParameters.ROUND_END_DISPLAY_TIME);
            yield return StartCoroutine(HandleRoundEnd());
        }

        private IEnumerator HandleRoundEnd()
        {
            Debug.Log($"Handling round end - Current Round: {currentRound}, Player Winner: {isPlayerWinner}");
            
            if (isPlayerWinner)
            {
                if (currentRound < GameParameters.MAX_ROUNDS)
                {
                    Debug.Log($"Advancing to round {currentRound + 1}");
                    yield return StartCoroutine(TransitionGameState(GameState.Intermission));
                    yield return StartCoroutine(ProcessIntermission());
                }
                else
                {
                    Debug.Log("Game complete!");
                    uiManager?.SetGameStatus("Congratulations! Game Complete!");
                    yield return new WaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                    ResetGame(true);
                }
            }
            else
            {
                yield return new WaitForSeconds(GameParameters.GetRoundTransitionDelay(currentRound));
                ResetGame(true);
            }
        }

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

        private IEnumerator TransitionGameState(GameState newState)
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
                }

                yield return null; // Allow frame to complete
            }
            finally
            {
                isTransitioningState = false;
            }
        }

        private void OnDestroy()
        {
            isShuttingDown = true;
            StopAllGameCoroutines();
            StopAllActiveCoroutines();
        }

        private void OnDisable()
        {
            isShuttingDown = true;
            StopAllGameCoroutines();
            StopAllActiveCoroutines();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
            StopAllGameCoroutines();
            StopAllActiveCoroutines();
        }

        private IEnumerator ProcessDealerTurn()
        {
            StopAllGameCoroutines();
            yield return StartCoroutine(TransitionGameState(GameState.DealerTurn));
            yield return StartCoroutine(FlipDealerCard());

            int playerScore = player.GetHandValue();
            if (playerScore > currentTargetScore)
            {
                string message = $"Dealer wins! Player busted with {playerScore}";
                Debug.Log(message);
                yield return StartCoroutine(HandleEndGame(message));
                yield break;
            }

            dealerPlayCoroutine = StartCoroutine(DealerPlayLoop());
            yield return dealerPlayCoroutine;
        }

        private IEnumerator ProcessIntermission()
        {
            Debug.Log($"Starting intermission - Current Round: {currentRound}");
            
            yield return StartCoroutine(TransitionGameState(GameState.Intermission));
            isReturningCards = true;

            // First clear the UI and update round display
            uiManager?.SetGameStatus("Round Complete!");
            uiManager?.UpdateStatusDisplays(currentRound + 1, currentWins, true);
            yield return new WaitForSeconds(1f);

            // Return cards to deck with animation
            yield return StartCoroutine(ReturnAllCardsToDeck());
            isReturningCards = false;
            
            // Increment round after cards are returned
            currentRound++;
            Debug.Log($"Advanced to round {currentRound}");

            // Randomize new target score
            isRandomizingTarget = true;
            yield return StartCoroutine(RandomizeTargetScore());
            isRandomizingTarget = false;

            // Play round start sounds and animations
            yield return StartCoroutine(PlayRoundStartSounds());
            
            // Update UI for new round
            uiManager?.UpdateStatusDisplays(currentRound, currentWins, true);
            uiManager?.SetGameStatus("Shuffling...");
            
            // Shuffle and deal
            deck?.ShuffleDeck();
            yield return StartCoroutine(DealInitialCards());
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

        private IEnumerator ReturnAllCardsToDeck()
        {
            Debug.Log("Starting to return all cards to deck");
            
            // Return dealer's cards first
            List<Card> dealerCards = new List<Card>(dealerPlayer.GetHand());
            foreach (Card card in dealerCards)
            {
                if (card != null)
                {
                    audioManager?.PlaySound(SoundType.CardReturn);
                    yield return StartCoroutine(ReturnCardToDeck(card));
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Then return player's cards
            List<Card> playerCards = new List<Card>(player.GetHand());
            foreach (Card card in playerCards)
            {
                if (card != null)
                {
                    audioManager?.PlaySound(SoundType.CardReturn);
                    yield return StartCoroutine(ReturnCardToDeck(card));
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Clear hands after all cards are returned
            dealerPlayer.ClearHand();
            player.ClearHand();
            
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator ReturnCardToDeck(Card card)
        {
            if (card == null || !card.gameObject || deck == null)
            {
                Debug.LogWarning("Attempted to return null or destroyed card to deck");
                yield break;
            }

            Vector3 deckPosition = deck.transform.position;
            float returnSpeed = GameParameters.CARD_RETURN_SPEED;

            while (card != null && card.gameObject)
            {
                if (Vector3.Distance(card.transform.position, deckPosition) <= 0.01f)
                    break;

                card.transform.position = Vector3.Lerp(
                    card.transform.position,
                    deckPosition,
                    Time.deltaTime * returnSpeed
                );

                yield return null;
            }

            if (card != null && card.gameObject)
            {
                card.transform.position = deckPosition;
                card.SetFaceDown(true);
                deck.ReturnCard(card);
            }
        }

        private IEnumerator DealCardToSlot(Card card)
        {
            if (card == null || player == null)
            {
                Debug.LogError($"DealCardToSlot - Card: {(card == null ? "null" : "valid")}, Player: {(player == null ? "null" : "valid")}");
                isDealing = false;
                SetPlayerButtonsInteractable(true);
                yield break;
            }

            Transform slot = player.GetNextAvailableSlot();
            if (slot != null)
            {
                audioManager?.PlaySound(SoundType.CardDeal);
                yield return StartCoroutine(dealer.DealCard(card, slot, false));
                player.AddCardToHand(card);
                uiManager?.UpdateUI();

                int playerScore = player.GetHandValue();
                Debug.Log($"After hit - Player score: {playerScore}, Target: {currentTargetScore}");
                
                isDealing = false;
                
                if (playerScore >= currentTargetScore)
                {
                    Debug.Log("Player hit target/bust - transitioning to dealer turn");
                    StartCoroutine(ProcessDealerTurn());
                }
                else
                {
                    Debug.Log("Hit complete - enabling player controls");
                    SetPlayerButtonsInteractable(true);
                }
            }
            else
            {
                Debug.LogError("No available slot found for hit card");
                isDealing = false;
                SetPlayerButtonsInteractable(true);
            }
        }

        private IEnumerator DealInitialCards()
        {
            Debug.Log($"Starting initial deal for round {currentRound}");
            yield return StartCoroutine(TransitionGameState(GameState.InitialDeal));
            isDealing = true;

            try
            {
                ResetPlayerSlots();

                // Deal player's cards (always face up)
                for (int i = 0; i < GameParameters.INITIAL_CARDS_PER_PLAYER; i++)
                {
                    var playerCard = deck.DrawCard();
                    if (playerCard != null)
                    {
                        audioManager?.PlaySound(SoundType.Deal);
                        yield return StartCoroutine(DealCardToPlayer(playerCard, player));
                        yield return new WaitForSeconds(0.5f);
                    }
                }

                // Deal dealer's cards based on round rules
                int dealerCards = currentRound >= 2 ? GameParameters.INITIAL_CARDS_PER_PLAYER + 1 : GameParameters.INITIAL_CARDS_PER_PLAYER;
                for (int i = 0; i < dealerCards; i++)
                {
                    var dealerCard = deck.DrawCard();
                    if (dealerCard != null)
                    {
                        audioManager?.PlaySound(SoundType.Deal);
                        bool shouldBeFaceDown = (currentRound == 1 && i != 0) || (currentRound >= 2 && i == dealerCards - 1);
                        yield return StartCoroutine(DealCardToDealer(dealerCard, shouldBeFaceDown));
                        yield return new WaitForSeconds(0.5f);
                    }
                }

                audioManager?.PlaySound(SoundType.PlayerTurn);
                yield return StartCoroutine(TransitionGameState(GameState.PlayerTurn));
                uiManager?.UpdateUI();
            }
            finally
            {
                isDealing = false;
            }
        }

        private IEnumerator DealCardToPlayer(Card card, Player targetPlayer)
        {
            if (card == null || dealer == null)
            {
                Debug.LogError("DealCardToPlayer: Invalid card or dealer reference");
                yield break;
            }

            Transform slot = targetPlayer.GetNextAvailableSlot();
            if (slot != null)
            {
                card.SetFaceDown(false); // Player cards are always face up
                yield return StartCoroutine(dealer.DealCard(card, slot, false));
                targetPlayer.AddCardToHand(card);
                Debug.Log($"Dealt player card: {card.rank} of {card.suit}, FaceDown: {card.IsFaceDown()}");
                uiManager?.UpdateUI();
            }
            else
            {
                Debug.LogError("No available slot found for player card");
            }
        }

        private IEnumerator DealCardToDealer(Card card, bool forceFaceDown)
        {
            if (card == null || dealer == null)
            {
                Debug.LogError("DealCardToDealer: Invalid card or dealer reference");
                yield break;
            }

            Transform slot = dealerPlayer.GetNextAvailableSlot();
            if (slot != null)
            {
                card.SetFaceDown(forceFaceDown);
                yield return StartCoroutine(dealer.DealCard(card, slot, forceFaceDown));
                dealerPlayer.AddCardToHand(card);
                Debug.Log($"Dealt dealer card: {card.rank} of {card.suit}, FaceDown: {forceFaceDown}, Round: {currentRound}");
                uiManager?.UpdateUI();
            }
            else
            {
                Debug.LogError("No available slot found for dealer card");
            }
        }

        private IEnumerator DealerPlayLoop()
        {
            while (dealerPlayer.ShouldHit())
            {
                uiManager?.ShowDealerPlaying();
                yield return new WaitForSeconds(dealerPlayDelay);

                Card card = deck.DrawCard();
                if (card != null)
                {
                    yield return StartCoroutine(DealCardToDealer(card, false));
                    yield return new WaitForSeconds(dealer.delayBetweenCards);

                    int dealerScore = dealerPlayer.GetHandValue();
                    if (dealerScore >= currentTargetScore)
                    {
                        yield return StartCoroutine(HandleEndGame(
                            dealerScore == currentTargetScore ? "Dealer wins!" : "Dealer busted!"
                        ));
                        yield break;
                    }
                }

                if (dealerPlayer.ShouldHit())
                {
                    uiManager?.ShowDealerThinking();
                    yield return new WaitForSeconds(1f);
                }
            }

            yield return new WaitForSeconds(dealerPlayDelay);
            string endMessage = DetermineWinner(player.GetHandValue(), dealerPlayer.GetHandValue());
            yield return StartCoroutine(HandleEndGame(endMessage));
        }

        private IEnumerator FlipDealerCard()
        {
            if (dealerPlayer == null)
                yield break;

            var dealerHand = dealerPlayer.GetHand();
            int cardIndexToFlip = currentRound >= 2 ? 2 : 1;

            if (dealerHand.Count <= cardIndexToFlip)
            {
                Debug.LogError($"Not enough dealer cards to flip. Cards: {dealerHand.Count}, Target: {cardIndexToFlip}");
                yield break;
            }

            audioManager?.PlaySound(SoundType.CardFlip);
            Card cardToFlip = dealerHand[cardIndexToFlip];

            if (cardToFlip != null)
            {
                cardToFlip.SetFaceDown(false);
                yield return StartCoroutine(cardToFlip.FlipCard());
                uiManager?.UpdateScores();
                yield return new WaitForSeconds(GameParameters.CARD_FLIP_DURATION);
            }
        }

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
                card.SetFaceDown(false);
                StartCoroutine(DealCardToSlot(card));
            }
            else
            {
                Debug.LogError("No card drawn from deck on hit!");
            }
        }

        public void OnPlayerStand()
        {
            if (currentGameState != GameState.PlayerTurn || isDealing)
                return;

            SetPlayerButtonsInteractable(false);
            StartCoroutine(ProcessDealerTurn());
        }
        #endregion

        public void SetPlayerButtonsInteractable(bool interactable)
        {
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
    }
}
