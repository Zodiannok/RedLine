using UnityEngine;
using System.Collections;

// Represents a local player - a human player on the local client.
// There should usually be only one local player.
public class LocalPlayer : MonoBehaviour, ICardPlayer {

	public string PlayerName;

	private PlayerStatus _PlayerStatus;

	// Object construction
	void Awake () {
		_PlayerStatus = new PlayerStatus ();
	}

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	#region ICardPlayer Implementation

	public string GetDisplayName() {
		return PlayerName;
	}

	public bool IsConnected() {
		return true;
	}

	public bool IsInPlay() {
		return true;
	}

	public void OnRemove() {
	}

	public PlayerStatus GetPlayerStatus() {
		return _PlayerStatus;
	}

	#endregion
}
