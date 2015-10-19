using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerStatus {

	// Reference to main game state.
	public MainGameState GameState { get; internal set; }

	public int PlayerIndex { get; internal set; }

	// Player's available fund.
	public int Fund { get; internal set; }

	public IList<int> CardList { get { return _CardList; } }

	// Gameplay data for each of the game phases.

	private int _NextAgent;

	// A list of cards that the player has. Cards are represented with their in-game card ID.
	private List<int> _CardList;

	public PlayerStatus() {
		_CardList = new List<int> ();
	}

	// Clear all cards from this player.
	public void ClearCards() {
		_CardList.Clear ();
	}

	// Add a card to the player's card list.
	public void AddCard(int cardId) {
		if (!_CardList.Contains (cardId)) {
			_CardList.Add(cardId);
		}
	}

	// Remove a card from the player's card list.
	public void RemoveCard(int cardId) {
		_CardList.Remove (cardId);
	}

	// Which of my cards is selected for the next customer?
	// Returns the card ID.
	public int GetAgentAssignedForNextCustomer() {
		if (_CardList.Contains(_NextAgent)) {
			return _NextAgent;
		}
		return -1;
	}

	// Sets the agent assigned for next customer by card ID.
	// Setting it to -1 will always reset next agent to an invalid one.
	// Returns bool - indicating if the next agent is valid.
	public bool SetAgentAssignedForNextCustomer(int cardID) {
		if (_CardList.Contains (cardID)) {
			_NextAgent = cardID;
			return true;
		}
		_NextAgent = -1;
		return false;
	}
	
}
