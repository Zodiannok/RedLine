using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Main game state API.
// Call functions in this class to setup, view, and update game state.
public class MainGameState : MonoBehaviour {

	// Which overall game state is the game in?
	enum GameState {
		// Game is in setup phase.
		SetupState,

		// Game is waiting for all players.
		ConnectionState,

		// Game is being played.
		PlayState,

		// Game is done and being shutdown.
		ShutdownState,
	}

	// Which phase is the game in?
	enum RoundPhase {
		LimboPhase,
		ActionPhase,
		BusinessPhase,
		RecruitPhase,
		EventPhase,
	}

	public static readonly int MaximumPlayers = 4;

	// The current and maximum round count.
	// By default we have a year (365 rounds). Each round contains multiple phases as defined in RoundPhase enum.
	// Round number is 1-based and inclusive for the sake of sanity - we have round 1 to round 365.
	public int CurrentRound { get; private set; }
	public int MaximumRound { get; private set; }

	public int CardDeckSizeLimit { get; private set; }
	public int InitialHandCards { get; private set; }
	public int StartingFund { get; private set; }
	public int CustomersPerTurn { get; private set; }

	public GameObject CardPrefab { get; set; }

	private bool _IsInGame;
	private bool _IsHost;
	private GameState _CurrentState;
	private RoundPhase _CurrentPhase;
	private CardManager _CardManager;
	private RelationMap _RelationMap;
	private ICardPlayer[] _Players;

	// List of randomly picked cards used in the current game.
	private List<long> _CardDeck;
	private List<CharacterCardBehavior> _CardInstances;

	// Action phase data

	// Business phase data
	private int _CurrentCustomerIndex;
	private int[] _CustomersList;

	// Use this for initialization
	void Start () {
		CurrentRound = 1;
		MaximumRound = 365;
		CardDeckSizeLimit = 64;
		InitialHandCards = 3;
		StartingFund = 10000;
		CustomersPerTurn = 3;

		_IsInGame = false;
		_IsHost = true;
		_CurrentState = GameState.ShutdownState;
		_CurrentPhase = RoundPhase.LimboPhase;
		_CardManager = GetComponent<CardManager> ();
		_RelationMap = GetComponent<RelationMap> ();
		_Players = new ICardPlayer[MaximumPlayers];

		LoadInitialData ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void LoadInitialData() {
		// Initialize game data.
		_CardManager.LoadAttributeAbilityData ();
		
		// TODO: Get the list of cards and load all.
		_CardManager.LoadCard ("CureWhite.png");
	}

	#region Game State Machine

	// Enter setup state from shutdown state.
	public bool EnterSetup() {
		if (_CurrentState != GameState.ShutdownState) {
			return false;
		}

		_CardDeck = new List<long> ();

		// Pick a maximum of CardDeckSizeLimit cards
		IEnumerable<CardBaseData> cards = _CardManager.ListCards ();
		foreach (CardBaseData card in cards) {
			_CardDeck.Add(card.CardType);
		}
		while (_CardDeck.Count > CardDeckSizeLimit) {
			_CardDeck.RemoveAt(Random.Range(0, _CardDeck.Count));
		}

		_CurrentState = GameState.SetupState;
		return true;
	}

	// Enter connection state from setup state.
	public bool EnterConnection() {
		if (_CurrentState != GameState.SetupState) {
			return false;
		}

		for (int i = 0; i < MaximumPlayers; ++i) {
			_Players [i] = null;
		}

		// TODO: Wait for the game to add players (especially remote players).
		AddLocalPlayer (0);
		AddEmptyPlayer (1);
		AddEmptyPlayer (2);
		AddEmptyPlayer (3);

		_CurrentState = GameState.ConnectionState;
		return true;
	}

	// Add a local player to a player slot.
	public bool AddLocalPlayer(int playerSlot) {
		// Out of bound check
		if (playerSlot < 0 || playerSlot >= MaximumPlayers) {
			return false;
		}

		// Can't replace an existing player.
		if (_Players [playerSlot] != null) {
			return false;
		}

		LocalPlayer player = gameObject.AddComponent<LocalPlayer>();
		_Players [playerSlot] = player;
		player.GetPlayerStatus ().PlayerIndex = playerSlot;
		
		return true;
	}

	// Add an empty player to a player slot.
	public bool AddEmptyPlayer(int playerSlot) {
		// Out of bound check
		if (playerSlot < 0 || playerSlot >= MaximumPlayers) {
			return false;
		}
		
		// Can't replace an existing player.
		if (_Players [playerSlot] != null) {
			return false;
		}

		EmptyPlayer player = gameObject.AddComponent<EmptyPlayer>();
		_Players [playerSlot] = player;
		player.GetPlayerStatus ().PlayerIndex = playerSlot;

		return true;
	}
	
	// Remove a player from a slot.
	public void RemovePlayer(int playerSlot) {
		if (playerSlot >= 0 && playerSlot < MaximumPlayers) {
			if (_Players[playerSlot] != null) {
				// Extra cleanups.
				_Players[playerSlot].OnRemove();

				Destroy (_Players[playerSlot] as Object);
				_Players[playerSlot] = null;
			}
		}
	}

	// Check if all players have connected to the game.
	public bool IsAllConnected() {
		if (_CurrentState != GameState.ConnectionState) {
			return false;
		}

		for (int i = 0; i < MaximumPlayers; ++i) {
			if (_Players[i] == null || !_Players[i].IsConnected()) {
				return false;
			}
		}
		return true;
	}

	// Enter play state from connection state
	public bool EnterPlay() {
		if (_CurrentState != GameState.ConnectionState) {
			return false;
		}

		_CurrentState = GameState.PlayState;

		// Initialize game state machine.
		CurrentRound = 0;
		MaximumRound = 365;
		_CurrentPhase = RoundPhase.LimboPhase;
		_IsInGame = true;

		// Construct cards.
		_CardInstances = new List<CharacterCardBehavior> ();

		foreach (long cardType in _CardDeck) {
			GameObject cardObj = Instantiate(CardPrefab);
			CharacterCardBehavior cardBehavior = cardObj.GetComponent<CharacterCardBehavior>();
			int cardId = _CardInstances.Count;
			cardBehavior.InitializeData(cardId, _CardManager, cardType);
			_CardInstances.Add(cardBehavior);
		}
		
		// Initialize relation map.
		_RelationMap.Clear ();
		_RelationMap.SetupInitialRelations (_CardManager, _CardDeck);

		// Initialize players.
		foreach (ICardPlayer player in _Players) {
			player.GetPlayerStatus().Fund = StartingFund;
		}

		// Distribute cards to each player.
		int totalCardDistributions = MaximumPlayers * InitialHandCards;
		if (totalCardDistributions > _CardInstances.Count) {
			totalCardDistributions = _CardInstances.Count;
		}

		return true;
	}

	// Check if the game is still ongoing.
	public bool IsInGame() {
		if (_CurrentState != GameState.PlayState) {
			return false;
		}

		return _IsInGame;
	}

	// Enter shutdown state from play state
	public bool EnterShutdown() {
		if (_CurrentState != GameState.PlayState) {
			return false;
		}

		// TODO: Clean up the game state, storing game result in a result data structure.

		_CurrentState = GameState.ShutdownState;
		return true;
	}

	#endregion

	#region Round Phase State Machine

	// Enter action phase from limbo phase
	public bool EnterRoundAction() {
		if (_CurrentPhase != RoundPhase.LimboPhase) {
			return false;
		}

		// TODO: Allow all players to choose one action.

		_CurrentPhase = RoundPhase.ActionPhase;
		return true;
	}

	// Enter business phase from action phase
	public bool EnterRoundBusiness() {
		if (_CurrentPhase != RoundPhase.ActionPhase) {
			return false;
		}

		// End of action phase. Apply all actions.

		// Randomly select customers.
		_CurrentCustomerIndex = 0;
		_CustomersList = new int[CustomersPerTurn];
		// TODO: Implement random selection.
		for (int i = 0; i < CustomersPerTurn; ++i) {
			_CustomersList[i] = 0;
		}

		_CurrentPhase = RoundPhase.BusinessPhase;
		return true;
	}

	// Query the current customer's card ID, or -1 if not applicable.
	public int GetCurrentCustomer() {
		if (_CurrentPhase != RoundPhase.BusinessPhase || _CurrentCustomerIndex >= CustomersPerTurn) {
			return -1;
		}

		return _CustomersList [_CurrentCustomerIndex];
	}

	// Go to the next customer. Returns false if no more customer is available.
	public bool NextCustomer() {
		++_CurrentCustomerIndex;
		return _CurrentCustomerIndex < CustomersPerTurn;
	}

	// Enter recruit phase from business phase.
	// This occurs regardless of whether we can recruit or not. If we can't recruit this round, then this phase is immediately done (not skipped).
	public bool EnterRoundRecruit() {
		if (_CurrentPhase != RoundPhase.BusinessPhase) {
			return false;
		}

		if (CanRecruitThisRound ()) {
			// Randomly select characters available for recruiting.
		}

		_CurrentPhase = RoundPhase.RecruitPhase;
		return true;
	}

	// Check if we can recruit this round.
	// For now, we can recruit every 30 days.
	public bool CanRecruitThisRound() {
		return CurrentRound % 30 == 0;
	}

	// Enter event phase from recruit phase.
	public bool EnterRoundEvent() {
		if (_CurrentPhase != RoundPhase.RecruitPhase) {
			return false;
		}

		// Finalize recruiting.
		if (CanRecruitThisRound ()) {
		}

		// Pick random event for each player.

		_CurrentPhase = RoundPhase.EventPhase;
		return true;
	}

	// Next turn. Enter limbo phase and increment round counter.
	public bool NextRound() {
		if (_CurrentPhase != RoundPhase.EventPhase) {
			return false;
		}

		// Increment round counter.
		CurrentRound += 1;
		if (CurrentRound > MaximumRound) {
			// We are done with the game!
			_IsInGame = false;
		}

		// Enters the next limbo.
		_CurrentPhase = RoundPhase.LimboPhase;
		return true;
	}

	#endregion

	#region Queries

	public CharacterCardBehavior GetCardFromId(int cardId) {
		if (cardId < 0 || cardId >= _CardInstances.Count) {
			return null;
		}
		return _CardInstances [cardId];
	}

	#endregion

	#region Game Operations

	#endregion
}
