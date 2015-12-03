using UnityEngine;
using System.Collections;

// Controls the game flow after the game has started.
// Basically acts as a timer for different phases, forcing phase changes and round progression.
public class GameFlowControl : MonoBehaviour {

	// Pause/unpause the game
	public bool Paused { get; set; }

	public GameObject GameStateObject;

	// Player timer for each phase.
	public float ActionPhaseTimer = 120.0f;
	public float BusinessPhaseTimer = 60.0f;
	public float ShopPhaseTimer = 120.0f;
	public float RecruitPhaseTimer = 60.0f;

	// "Rest" timer between two rounds. Can also be prolonged by displaying events.
	public float RoundRestTimer = 15.0f;

	// "Rest" timer between phase changes. Can be used to display short UI notices.
	public float MinorRestTimer = 3.0f;

	private MainGameState _MainGameState;

	private float _CurrentPhaseTimerCountDown;

	enum GameFlowPhase {
		// Resting between rounds.
		RoundRest,

		// Resting between minor phase changes.
		MinorRest,

		// Action phase.
		ActionPhase,

		// Business phase - timer for each customer.
		BusinessPhase,

		// Shop phase.
		ShopPhase,

		// Recruit phase - timer for each recruit.
		RecruitPhase,

		// Game is done! We no longer need to update anything.
		EndPhase,
	}

	private GameFlowPhase _InternalPhase;

	// Use this for initialization
	void Start () {
		_MainGameState = GameStateObject.GetComponent<MainGameState> ();
		_InternalPhase = GameFlowPhase.EndPhase;
	}
	
	// Update is called once per frame
	void Update () {
		if (Paused) {
			return;
		}

		if (_InternalPhase == GameFlowPhase.EndPhase) {
			return;
		}

		if (_CurrentPhaseTimerCountDown > 0.0f) {
			_CurrentPhaseTimerCountDown -= Time.deltaTime;
			if (_CurrentPhaseTimerCountDown <= 0.0f) {
				TriggerNextPhase();
			}
		}
	}

	public bool IsInGame {
		get {
			return _InternalPhase != GameFlowPhase.EndPhase;
		}
	}

	public bool IsInRestPhase {
		get {
			return _InternalPhase == GameFlowPhase.RoundRest || _InternalPhase == GameFlowPhase.MinorRest;
		}
	}

	public float CurrentPhaseMaxTime { get; private set; }

	public float CurrentCountDown {
		get {
			return _CurrentPhaseTimerCountDown;
		}
	}

	public float CurrentTimer {
		get {
			return CurrentPhaseMaxTime - _CurrentPhaseTimerCountDown;
		}
	}

	// Trigger the next phase.
	void TriggerNextPhase() {
		switch (_InternalPhase) {
		case GameFlowPhase.RoundRest:
			AdvanceToNextRound();
			break;
		case GameFlowPhase.MinorRest:
			AdvanceToNextPhase();
			break;
		case GameFlowPhase.ActionPhase:
			HandleActionDone();
			break;
		case GameFlowPhase.BusinessPhase:
			HandleBusinessDone();
			break;
		case GameFlowPhase.ShopPhase:
			HandleShopDone();
			break;
		case GameFlowPhase.RecruitPhase:
			HandleRecruitDone();
			break;
		}
	}

	public void StartGameFlow() {
		if (_MainGameState.CurrentState != MainGameState.GameState.PlayState) {
			return;
		}
		SetPhaseTimer (GameFlowPhase.RoundRest);
	}

	void SetPhaseTimer(GameFlowPhase phase) {
		switch (phase) {
		case GameFlowPhase.RoundRest:
			_CurrentPhaseTimerCountDown = CurrentPhaseMaxTime = RoundRestTimer;
			break;
		case GameFlowPhase.MinorRest:
			_CurrentPhaseTimerCountDown = CurrentPhaseMaxTime = MinorRestTimer;
			break;
		case GameFlowPhase.ActionPhase:
			_CurrentPhaseTimerCountDown = CurrentPhaseMaxTime = ActionPhaseTimer;
			break;
		case GameFlowPhase.BusinessPhase:
			_CurrentPhaseTimerCountDown = CurrentPhaseMaxTime = BusinessPhaseTimer;
			break;
		case GameFlowPhase.ShopPhase:
			_CurrentPhaseTimerCountDown = CurrentPhaseMaxTime = ShopPhaseTimer;
			break;
		case GameFlowPhase.RecruitPhase:
			_CurrentPhaseTimerCountDown = CurrentPhaseMaxTime = RecruitPhaseTimer;
			break;
		case GameFlowPhase.EndPhase:
			_CurrentPhaseTimerCountDown = 0.0f;
			break;
		}
		_InternalPhase = phase;
	}

	void AdvanceToNextRound() {
		// Precondition: game must in limbo state.
		if (_MainGameState.CurrentPhase != MainGameState.RoundPhase.LimboPhase) {
			return;
		}

		// If the game is done, let's go to end phase.
		if (!_MainGameState.IsInGame ()) {
			SetPhaseTimer (GameFlowPhase.EndPhase);
			return;
		}

		// Based on the current date, go to the next phase.
		if (_MainGameState.IsWeekday ()) {
			_MainGameState.EnterRoundAction ();
			SetPhaseTimer (GameFlowPhase.ActionPhase);
			BroadcastMessage("OnEnterAction");
		} else {
			_MainGameState.EnterRoundShop ();
			SetPhaseTimer (GameFlowPhase.ShopPhase);
			BroadcastMessage("OnEnterShop");
		}
	}

	void AdvanceToNextPhase() {
		// Quite a bit complicated: choose the next phase to go to based on the current phase.
		var currentPhase = _MainGameState.CurrentPhase;

		switch (currentPhase) {
		case MainGameState.RoundPhase.ActionPhase:
			// This is when action phase transits into business phase.
			_MainGameState.EnterRoundBusiness();
			SetPhaseTimer(GameFlowPhase.BusinessPhase);
			BroadcastMessage("OnEnterBusiness");
			break;
		case MainGameState.RoundPhase.BusinessPhase:
			// This is the pause between different customers.
			// Check if we have the next one. If so, start timer for the next customer. Otherwise go to next round.
			if (_MainGameState.HasMoreCustomer) {
				SetPhaseTimer(GameFlowPhase.BusinessPhase);
				BroadcastMessage("OnEnterBusiness");
			} else {
				_MainGameState.NextRound();
				SetPhaseTimer(GameFlowPhase.RoundRest);
				BroadcastMessage("OnNextRound");
			}
			break;
		case MainGameState.RoundPhase.ShopPhase:
			// This is when shop phase transits into recruit phase.
			_MainGameState.EnterRoundRecruit();
			SetPhaseTimer(GameFlowPhase.RecruitPhase);
			BroadcastMessage("OnEnterRecruit");
			break;
		case MainGameState.RoundPhase.RecruitPhase:
			// This is the pause between different recruits.
			// TODO: Do similar logic as business phase.
			_MainGameState.EnterRoundEvent();
			_MainGameState.ResolveRoundEvents();
			_MainGameState.NextRound();
			SetPhaseTimer(GameFlowPhase.RoundRest);
			BroadcastMessage("OnNextRound");
			break;
		}
	}

	void HandleActionDone() {
		// Apply actions.
		_MainGameState.ApplyActions ();

		// Give the players a short time (for the UI to update them with information)
		SetPhaseTimer (GameFlowPhase.MinorRest);

		BroadcastMessage("OnLeaveAction");
	}

	void HandleBusinessDone() {
		// Resolve the current customer.
		_MainGameState.ResolveCurrentCustomer ();

		// Give the players a short time (for UI update and prepare them for the next customer)
		SetPhaseTimer (GameFlowPhase.MinorRest);

		BroadcastMessage("OnLeaveBusiness");
	}

	void HandleShopDone() {
		// TODO: Apply shopping changes.

		// Give the players a short time for UI update.
		SetPhaseTimer (GameFlowPhase.MinorRest);

		BroadcastMessage("OnLeaveShop");
	}

	void HandleRecruitDone() {
		// TODO: Resolve the current recruit.

		// Give the players a short time (for UI update and prepare them for the next recruit)
		SetPhaseTimer (GameFlowPhase.MinorRest);

		BroadcastMessage("OnLeaveRecruit");
	}
}
