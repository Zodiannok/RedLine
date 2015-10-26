using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Main game state API.
// Call functions in this class to setup, view, and update game state.
public class MainGameState : MonoBehaviour {

	// Which overall game state is the game in?
	internal enum GameState {
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
	internal enum RoundPhase {
		// Waiting for actual gameplay phase to start
		LimboPhase,

		// Weekday action choice.
		ActionPhase,

		// Weekday business, try to get customers.
		BusinessPhase,

		// Weekend shopping.
		ShopPhase,

		// Weekend recruit, fight over new recruits.
		RecruitPhase,

		// The very end of weekend. Roll random events.
		EventPhase,
	}

	public static readonly int MaximumPlayers = 4;

	// The current and maximum round count.
	// By default we have a year (365 rounds). Each round contains multiple phases as defined in RoundPhase enum.
	// Round number is 1-based and inclusive for the sake of sanity - we have round 1 to round 365.
	public int CurrentRound { get; private set; }
	public int MaximumRound = 365;

	// Game options.
	public int CardDeckSizeLimit = 64;
	public int InitialHandCards = 3;
	public int StartingFund = 10000;
	public int CustomersPerTurn = 3;

	// Card object to instantiate.
	public GameObject CardPrefab;

	internal GameState CurrentState { get { return _CurrentState; } }
	internal RoundPhase CurrentPhase { get { return _CurrentPhase; } }

	private bool _InitialDataLoaded;
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

		_InitialDataLoaded = false;
		_IsInGame = false;
		_IsHost = true;
		_CurrentState = GameState.ShutdownState;
		_CurrentPhase = RoundPhase.LimboPhase;
		_CardManager = GetComponent<CardManager> ();
		_RelationMap = GetComponent<RelationMap> ();
		_Players = new ICardPlayer[MaximumPlayers];

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void LoadInitialData() {
		// Initialize game data.
		_CardManager.LoadAttributeAbilityData ();
		_InitialDataLoaded = true;
	}

	#region Game State Machine

	// Enter setup state from shutdown state.
	public bool EnterSetup() {
		if (_CurrentState != GameState.ShutdownState) {
			return false;
		}

		if (!_InitialDataLoaded) {
			LoadInitialData ();
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

		_CurrentState = GameState.ConnectionState;
		return true;
	}

	// Add a local player to a player slot.
	public bool AddLocalPlayer(int playerSlot, string playerName) {
		// Out of bound check
		if (playerSlot < 0 || playerSlot >= MaximumPlayers) {
			return false;
		}

		// Can't replace an existing player.
		if (_Players [playerSlot] != null) {
			return false;
		}

		LocalPlayer player = gameObject.AddComponent<LocalPlayer>();
		player.PlayerName = playerName;
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
		CurrentRound = 1;
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
		var playersInPlay = GetPlayersInPlay ();
		foreach (int playerIndex in playersInPlay) {
			_Players[playerIndex].GetPlayerStatus().Fund = StartingFund;
		}

		// Distribute cards to each player.
		int totalCardDistributions = playersInPlay.Count * InitialHandCards;
		if (totalCardDistributions > _CardInstances.Count) {
			totalCardDistributions = _CardInstances.Count;
		}
		// Shuffle deck (Fisher-Yates).
		List<int> deck = new List<int> ();
		for (int i = 0; i < _CardInstances.Count; ++i) {
			int j = Random.Range(0, i + 1);
			if (j != i) {
				deck.Add(deck[j]);
				deck[j] = i;
			} else {
				deck.Add(i);
			}
		}
		for (int i = 0; i < totalCardDistributions; ++i) {
			int playerIndex = playersInPlay[i % playersInPlay.Count];
			int cardIndex = deck[i];
			_CardInstances[cardIndex].Owner = playerIndex;
			_Players[playerIndex].GetPlayerStatus().AddCard(cardIndex);
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

	// Enter action phase from limbo phase. Require weekdays.
	public bool EnterRoundAction() {
		if (_CurrentPhase != RoundPhase.LimboPhase || !IsWeekday()) {
			return false;
		}

		// TODO: Allow all players to choose one action.

		_CurrentPhase = RoundPhase.ActionPhase;
		return true;
	}

	// Apply all actions selected by players.
	public void ApplyActions() {
		// TODO: Apply player selected actions.
	}

	// Enter business phase from action phase
	public bool EnterRoundBusiness() {
		if (_CurrentPhase != RoundPhase.ActionPhase) {
			return false;
		}

		// Randomly select customers.
		var customers = GetRandomUnassignedCustomers (CustomersPerTurn);
		_CustomersList = new int[customers.Count];
		for (int i = 0; i < customers.Count; ++i) {
			_CustomersList[i] = customers[i];;
		}
		_CurrentCustomerIndex = 0;

		_CurrentPhase = RoundPhase.BusinessPhase;
		return true;
	}

	// Get the list of customers.
	public IList<int> Customers {
		get {
			return _CustomersList;
		}
	}

	// Get the index of current customer.
	public int CurrentCustomerIndex {
		get {
			return _CurrentCustomerIndex;
		}
	}

	// Query if there are more customers
	public bool HasMoreCustomer {
		get {
			return _CurrentCustomerIndex < _CustomersList.Length;
		}
	}

	// Query the current customer's card ID, or -1 if not applicable.
	public int GetCurrentCustomer() {
		if (_CurrentPhase != RoundPhase.BusinessPhase || _CurrentCustomerIndex >= CustomersPerTurn) {
			return -1;
		}

		return _CustomersList [_CurrentCustomerIndex];
	}

	// Resolve the current customer and go to the next customer. Returns false if no more customer is available.
	public bool ResolveCurrentCustomer() {
		if (!HasMoreCustomer) {
			return false;
		}

		int currentCustomer = GetCurrentCustomer ();

		int bestAppeal = -1;
		int bestPlayer = -1;
		int bestCard = -1;

		foreach (int playerIndex in GetPlayersInPlay()) {
			int playerCard = _Players[playerIndex].GetPlayerStatus().GetAgentAssignedForNextCustomer();
			if (playerCard != -1) {
				// Validate that the card can actually be picked.
				var card = GetCardFromId(playerCard);
				if (!card || card.Exhausted) {
					continue;
				}

				// Evaluate the appeal of the agent card to the customer.
				int cardAppeal = EvaluateAppeal(currentCustomer, playerCard);
				if (cardAppeal > bestAppeal) {
					bestAppeal = cardAppeal;
					bestPlayer = playerIndex;
					bestCard = playerCard;
				}
			}
		}

		// The customer goes to the best player (if there is one).
		// Reward the player.
		if (bestPlayer != -1) {
			// Use (exhaust) the card picked.
			var card = GetCardFromId(bestCard);
			if (card) {
				card.Exhausted = true;
			}

			// TODO: reward the player.
		}

		++_CurrentCustomerIndex;
		return HasMoreCustomer;
	}

	// Enter shop phase from limbo. Require weekends.
	public bool EnterRoundShop() {
		if (_CurrentPhase != RoundPhase.LimboPhase || IsWeekday()) {
			return false;
		}

		_CurrentPhase = RoundPhase.ShopPhase;
		return true;
	}

	// Enter recruit phase from shopping phase.
	public bool EnterRoundRecruit() {
		if (_CurrentPhase != RoundPhase.ShopPhase) {
			return false;
		}

		_CurrentPhase = RoundPhase.RecruitPhase;
		return true;
	}

	// Finalize recruiting.
	public void FinalizeRecruiting() {
		// Give the current recruit to the player with highest influence.
	}

	// Enter event phase from recruit phase.
	public bool EnterRoundEvent() {
		if (_CurrentPhase != RoundPhase.RecruitPhase) {
			return false;
		}

		// Pick random event for each player.

		_CurrentPhase = RoundPhase.EventPhase;
		return true;
	}

	// Resolve events.
	public void ResolveRoundEvents() {
	}

	// Next turn. Enter limbo phase and increment round counter.
	public bool NextRound() {
		// Needs business phase on weekdays and event phase on weekends.
		if (IsWeekday ()) {
			if (_CurrentPhase != RoundPhase.BusinessPhase) {
				return false;
			}
		} else { 
			if (_CurrentPhase != RoundPhase.EventPhase) {
				return false;
			}
		}

		// Increment round counter.
		CurrentRound += 1;
		if (CurrentRound > MaximumRound) {
			// We are done with the game!
			_IsInGame = false;
		}

		// Reset exhaust state of all player cards.
		RefreshExhaustState ();

		// Enters the next limbo.
		_CurrentPhase = RoundPhase.LimboPhase;
		return true;
	}

	#endregion

	#region Queries

	// Check if today is a weekday (day % 7 is not 0)
	public bool IsWeekday() {
		return CurrentRound % 7 != 0;
	}

	public CharacterCardBehavior GetCardFromId(int cardId) {
		if (cardId < 0 || cardId >= _CardInstances.Count) {
			return null;
		}
		return _CardInstances [cardId];
	}

	// In the following sections, "unassigned" means cards that are not owned by a player.

	// Return the cardID of a random unassigned agent
	// Return -1 if no agents are unassigned.
	public int GetRandomUnassignedAgent() {
		var agents = GetRandomUnassignedAgents (1);
		foreach (int a in agents) {
			return a;
		}
		return -1;
	}

	// Return a list of N random unassigned agents.
	public IList<int> GetRandomUnassignedAgents(int count) {
		var freeAgents = GetAllUnassignedAgents () as List<int>;
		// Randomly shuffle and take the first *count numbers.
		InplaceShuffle (freeAgents);
		if (count < freeAgents.Count) {
			return freeAgents.GetRange(0, count);
		} else {
			return freeAgents;
		}
	}

	public IList<int> GetAllUnassignedAgents() {
		// Get all free agents.
		List<int> freeAgents = new List<int> ();
		for (int i = 0; i < _CardInstances.Count; ++i) {
			if (_CardInstances[i].Owner == -1)
			{
				long type = _CardInstances[i].CardType;
				if (_CardManager.GetCardBaseData(type).IsAgent) {
					freeAgents.Add(i);
				}
			}
		}
		return freeAgents;
	}

	// Return the cardID of a random unassigned customer
	// Return -1 if no customers are unassigned.
	public int GetRandomUnassignedCustomer() {
		var customers = GetRandomUnassignedCustomers (1);
		foreach (int c in customers) {
			return c;
		}
		return -1;
	}

	// Return a list of N random unassigned customers.
	public IList<int> GetRandomUnassignedCustomers(int count) {
		var customers = GetAllUnassignedCustomers () as List<int>;
		// Randomly shuffle and take the first *count numbers.
		InplaceShuffle (customers);
		if (count < customers.Count) {
			return customers.GetRange(0, count);
		} else {
			return customers;
		}
	}
	
	public IList<int> GetAllUnassignedCustomers() {
		// Get all free agents.
		List<int> customers = new List<int> ();
		for (int i = 0; i < _CardInstances.Count; ++i) {
			if (_CardInstances[i].Owner == -1)
			{
				long type = _CardInstances[i].CardType;
				if (_CardManager.GetCardBaseData(type).IsCustomer) {
					customers.Add(i);
				}
			}
		}
		return customers;
	}

	// Get the player indices that are in play.
	// This is used to skip empty players.
	// To get the players that are still alive, use GetPlayersAlive() instead.
	public IList<int> GetPlayersInPlay() {
		List<int> players = new List<int> ();
		for (int i = 0; i < _Players.Length; ++i) {
			if (_Players[i].IsInPlay()) {
				players.Add(i);
			}
		}
		return players;
	}

	// Get the player in the specified slot.
	// If the player is not in game, return null too.
	public ICardPlayer GetPlayerInPlay(int playerIndex) {
		ICardPlayer cardPlayer = _Players[playerIndex];
		if (cardPlayer != null && cardPlayer.IsInPlay ()) {
			return cardPlayer;
		}
		return null;
	}

	#endregion

	#region Game Operations
	
	public static void InplaceShuffle<T>(IList<T> list) {
		// In-place shuffle with Fisher-Yates.
		for (int i = 0; i < list.Count; ++i) {
			int j = Random.Range(0, i + 1);
			if (i != j) {
				T source = list[i];
				list[i] = list[j];
				list[j] = source;
			}
		}
	}

	public int EvaluateAppeal(int customerCardID, int agentCardID) {
		// TODO: Evaluate appeal value.
		return 100;
	}

	// Reset all player cards' exhaust state to false, allowing them to be picked again.
	public void RefreshExhaustState() {
		foreach (CharacterCardBehavior card in _CardInstances) {
			if (card.Owner != -1) {
				card.Exhausted = false;
			}
		}
	}

	#endregion
}
