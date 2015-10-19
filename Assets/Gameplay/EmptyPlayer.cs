using UnityEngine;
using System.Collections;

// Represents an empty player - the player only acts as a placeholder.
public class EmptyPlayer : MonoBehaviour, ICardPlayer {

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

	public bool IsInPlay() {
		return false;
	}
	
	public void OnRemove() {
	}
	
	public PlayerStatus GetPlayerStatus() {
		return _PlayerStatus;
	}
	
	#endregion
}
