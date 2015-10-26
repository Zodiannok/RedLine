using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TestViewPlayer : MonoBehaviour {

	public int PlayerIndex;

	public GameObject GameplayObject;
	public GameObject CardViewPrefab;

	private MainGameState _MainGameState;
	private CardManager _CardManager;

	private Text _NameText;
	private Transform _CardsPanel;

	private IList<int> _LastCachedCardList;

	// Use this for initialization
	void Start () {
		_MainGameState = GameplayObject.GetComponent<MainGameState> ();
		_CardManager = GameplayObject.GetComponent<CardManager> ();

		_NameText = GetComponentInChildren<Text> ();
		_CardsPanel = transform.FindChild ("Cards");
	}
	
	// Update is called once per frame
	void Update () {
		if (_MainGameState.CurrentState != MainGameState.GameState.PlayState) {
			return;
		}

		ICardPlayer player = _MainGameState.GetPlayerInPlay (PlayerIndex);
		if (player == null) {
			return;
		}

		_NameText.text = player.GetDisplayName ();
		var cardList = player.GetPlayerStatus().CardList;
		if (!cardList.Equals(_LastCachedCardList)) {
			_LastCachedCardList = cardList;

			// Populate card list.
			foreach (Transform child in _CardsPanel) {
				GameObject.Destroy(child);
			}
			foreach (int cardIndex in _LastCachedCardList) {
				CharacterCardBehavior cardInstance = _MainGameState.GetCardFromId(cardIndex);

				var cardView = Instantiate(CardViewPrefab);
				TestViewCardBehavior viewBehavior = cardView.GetComponent<TestViewCardBehavior>();
				viewBehavior.CardInstance = cardInstance;
				cardView.transform.parent = _CardsPanel;
			}
		}
	}
}
