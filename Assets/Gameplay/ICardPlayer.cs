using UnityEngine;
using System.Collections;

public interface ICardPlayer {
	// Check if the player is connected.
	bool IsConnected();

	// Check if the player is in play.
	bool IsInPlay();

	// Extra handling to remove the player.
	void OnRemove();

	PlayerStatus GetPlayerStatus();
}
