using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerStatus {

	// Reference to main game state.
	public MainGameState GameState { get; set; }

	public int PlayerIndex { get; set; }

	// Player's available fund.
	public int Fund { get; set; }

	public IEnumerable<int> CardList { get { return _CardList; } }

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
}
