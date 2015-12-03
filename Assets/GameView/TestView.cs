using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TestView : MonoBehaviour {
	
	public GameObject GamePlayObj;

	private MainGameState _GameState;
	private GameFlowControl _GameFlow;
	private Transform _Canvas;

	private CardManager _CardManager;
	private PlaceholderLibrary _PlaceholderLibrary;

	// Use this for initialization
	void Start () {
		_GameState = GamePlayObj.GetComponent<MainGameState> ();
		_GameFlow = GetComponent<GameFlowControl> ();
		_Canvas = transform.FindChild ("MenuCanvas");

		_CardManager = GamePlayObj.GetComponent<CardManager> ();
		_PlaceholderLibrary = gameObject.AddComponent<PlaceholderLibrary> ();

		Transform debugView = _Canvas.FindChild ("DebugView");
		Debug.Assert (debugView);
		if (debugView) {
			debugView.gameObject.SetActive(false);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (_GameFlow.IsInGame) {
			Transform textPanel = _Canvas.Find("DebugView/GameFlowStatus/GameFlowText");
			if (textPanel) {
				Text panelText = textPanel.GetComponent<Text>();
				if (panelText) {
					string gameStatusString = string.Format("Round {0}/{1}\n", _GameState.CurrentRound, _GameState.MaximumRound);
					gameStatusString += string.Format("{0}\n", _GameState.CurrentPhase);
					gameStatusString += string.Format("{0}\n", GetExtraStatusString());
					gameStatusString += string.Format("{0:0.0} / {1:0.0} seconds", _GameFlow.CurrentTimer, _GameFlow.CurrentPhaseMaxTime);

					panelText.text = gameStatusString;
				}
			}
		}
	}

	string GetExtraStatusString () {
		if (_GameFlow.IsInRestPhase) {
			return "Waiting";
		}
		var phase = _GameState.CurrentPhase;
		if (phase == MainGameState.RoundPhase.LimboPhase) {
			return "Pause Between Rounds";
		} else if (phase == MainGameState.RoundPhase.ActionPhase || phase == MainGameState.RoundPhase.ShopPhase) {
			return "Picking";
		} else if (phase == MainGameState.RoundPhase.BusinessPhase) {
			return string.Format("{0} of {1} Customers", _GameState.CurrentCustomerIndex + 1, _GameState.Customers.Count);
		} else if (phase == MainGameState.RoundPhase.RecruitPhase) {
			return string.Format("{0} of {1} Recruits", 0, 0);
		}

		return "";
	}

	public void OnButtonClicked_StartGame() {
		Debug.Assert (_GameState.CurrentState == MainGameState.GameState.ShutdownState);

		_PlaceholderLibrary.LoadCards (_CardManager);

		_GameState.EnterSetup ();

		Debug.Assert (_GameState.CurrentState == MainGameState.GameState.SetupState);

		Transform setupMenu = _Canvas.FindChild ("SetupMenu");
		Debug.Assert (setupMenu);
		if (setupMenu) {
			setupMenu.gameObject.SetActive(false);
		}

		_GameState.EnterConnection ();

		Debug.Assert (_GameState.CurrentState == MainGameState.GameState.ConnectionState);
		Debug.Assert (!_GameState.IsAllConnected ());

		// TODO: Add test connection menu.

		_GameState.AddLocalPlayer (0, "AAA");
		_GameState.AddEmptyPlayer (1);
		_GameState.AddEmptyPlayer (2);
		_GameState.AddEmptyPlayer (3);

		Debug.Assert (_GameState.IsAllConnected ());

		_GameState.EnterPlay ();

		Debug.Assert (_GameState.CurrentState == MainGameState.GameState.PlayState);
		Debug.Assert (_GameState.IsInGame ());
		Debug.Assert (_GameState.CurrentPhase == MainGameState.RoundPhase.LimboPhase);

		Transform debugView = _Canvas.FindChild ("DebugView");
		Debug.Assert (debugView);
		if (debugView) {
			debugView.gameObject.SetActive(true);
		}

		_GameFlow.StartGameFlow ();		
	}

	public void OnButtonClicked_DebugPauseUnpause() {
		bool paused = _GameFlow.Paused;

		Transform buttonText = _Canvas.Find ("DebugView/GameFlowStatus/Button/Text");

		if (paused) {
			_GameFlow.Paused = false;
			buttonText.GetComponent<Text> ().text = "Pause";
		} else {
			_GameFlow.Paused = true;
			buttonText.GetComponent<Text> ().text = "Unpause";
		}
	}

	void OnEnterAction() {
	}
}
