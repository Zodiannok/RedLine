using UnityEngine;
using System.Collections;

public interface ICardPlayer {
	// Check if the player is connected.
	bool IsConnected();

	// Extra handling to remove the player.
	void OnRemove();

	PlayerStatus GetPlayerStatus();
}
