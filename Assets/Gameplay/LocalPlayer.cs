using UnityEngine;
using System.Collections;

// Represents a local player - a human player on the local client.
// There should usually be only one local player.
public class LocalPlayer : MonoBehaviour, ICardPlayer {

	private PlayerStatus _PlayerStatus;

	// Use this for initialization
	void Start () {
		_PlayerStatus = new PlayerStatus ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	#region ICardPlayer Implementation

	public bool IsConnected() {
		return true;
	}

	public void OnRemove() {
	}

	public PlayerStatus GetPlayerStatus() {
		return _PlayerStatus;
	}

	#endregion
}
