using UnityEngine;
using System.Collections;

// Represents an empty player - the player only acts as a placeholder.
public class EmptyPlayer : MonoBehaviour, ICardPlayer {

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
		return "Empty";
	}
	
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
