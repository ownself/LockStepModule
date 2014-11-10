using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class LockStepManager : MonoBehaviour {

	public static readonly int firstLockStepTurnID = 0;
	int _numberOfPlayers = 0;
	public int GetPlayersNumber() {
		return _numberOfPlayers;
	}

	// Use for confirm and ready from all the players
	List<int> _readyPlayers;
	List<int> _playersConfirmedImReady;
	// Use for store all the players in the session
	public Dictionary<int, LockStepPlayer> allPlayers;
	int _localPlayerIndex;

	bool _initialized = false;
	bool _isDedicateServer;
	// Action List
	PendingActions _pendingActions;
	ConfirmedActions _confirmedActions;
	Queue<Action> _actionsToSend;

	NetworkInterface _networkInterface;

	//==============================================================================================
	// Variables for adjusting Lockstep and game frame length
	RollingAverage _networkRollingAverage;
	RollingAverage _runtimeRollingAverage;
	// used to find the maximum gameFrame runtime in the current lockstep turn
	long _currentGameFrameRuntime;
	Stopwatch _gameTurnSW;
	int _initialLockStepTurnLength = 100; // in Milliseconds
	int _initialGameFrameTurnLength = 25; // in Milliseconds
	int _lockstepTurnLength;
	int _gameFrameTurnLength;
	int _gameFramesPerLockstepTurn;
	int _lockstepsPerSecond;
	int _gameFramesPerSecond;
	// int playerIDToProcessFirst = 0; // used to rotate what player's action gets processed first
	int _gameFrame = 0; // current game frame number in the currect lockstep turn
	// Accumilated time in Milliseconds that have passed since the last time gameFrame was called
	int _accumilatedTime = 0;

	int _lockStepTurnID = firstLockStepTurnID;
	public int GetLockStepTurn() {
		return _lockStepTurnID;
	}

	// Use this for initialization
	void Start () {
		enabled = false;
		_networkInterface = GetComponent<NetworkInterface>();

		_lockStepTurnID = firstLockStepTurnID;
		_pendingActions = new PendingActions(this);
		_confirmedActions = new ConfirmedActions(this);
		_actionsToSend = new Queue<Action>();
		_gameTurnSW = new Stopwatch();
		_currentGameFrameRuntime = 0;
		allPlayers = new Dictionary<int, LockStepPlayer>();
	}

	public void EnableConfirmedActions(bool isEnable) {
		_confirmedActions.EnableConfirmedActions(isEnable);
	}

	//==============================================================================================
	// Connect and Start the game
	//==============================================================================================
	public void InitPlayersReadyState() {
		if(_initialized) { return; }

		_readyPlayers = new List<int>(_numberOfPlayers);
		_playersConfirmedImReady = new List<int>(_numberOfPlayers);

		_initialized = true;
	}

	public void InitGame(int index, List<LockStepPlayer> players, bool isDedicated = false) {
		allPlayers.Clear();
		foreach (LockStepPlayer player in players) {
			// Debug.Log("Init Game with Player : " + player.GetPlayerIndex());
			allPlayers[player.GetPlayerIndex()] = player;
		}
		_localPlayerIndex = index;
		_isDedicateServer = isDedicated;
		_numberOfPlayers = players.Count;
		if (_numberOfPlayers == 0) {
			Debug.Log("Fatal Error! Incorrect number of players");
		}

		ResetGame();
		InitPlayersReadyState();
		if (_confirmedActions.IsConfirmedActionsEnabled()) {
			_networkInterface.CallReadyToStart(_localPlayerIndex);
		} else {
			ReadyToStart(_localPlayerIndex); // No confirmation, just start the game
		}
	}

	private void ResetGame() {
		// Debug.Log("Game Reseted My PlayerID: " + _localPlayerIndex);
		_lockStepTurnID = firstLockStepTurnID;
		_pendingActions.Reset();
		_confirmedActions .Reset();
		_actionsToSend.Clear();

		_gameTurnSW.Reset();
		_currentGameFrameRuntime = 0;
		if (_networkRollingAverage == null) {
			_networkRollingAverage = new RollingAverage(_numberOfPlayers, _initialLockStepTurnLength);
		} else {
			_networkRollingAverage.Reset();
		}
		if (_runtimeRollingAverage == null) {
			_runtimeRollingAverage = new RollingAverage(_numberOfPlayers, _initialLockStepTurnLength);
		} else {
			_runtimeRollingAverage.Reset();
		}
	}

	private void CheckGameStart() {
		if (!_confirmedActions.IsConfirmedActionsEnabled()) {
			StartGame();
			return;
		}
		if(_playersConfirmedImReady == null) {
			// Debug.Log("WARNING!!! Unexpected null reference during game start.");
			return;
		}
		//check if all expected players confirmed our gamestart message
		if(_playersConfirmedImReady.Count == _numberOfPlayers) {
			//check if all expected players sent their gamestart message
			if(_readyPlayers.Count == _numberOfPlayers) {
				//we are ready to start
				// Debug.Log("All players are ready to start. Starting Game.");
				StartGame();
				//we no longer need these lists
				_playersConfirmedImReady = null;
				_readyPlayers = null;
			}
		}
	}

	private void StartGame() {
		//start the LockStep Turn loop
		// Debug.Log("All Ready Start!!!!");
		enabled = true;
		InitPlayers();
	}

	private void InitPlayers() {
		// Debug.Log("InitPlayers");

		foreach (KeyValuePair<int, LockStepPlayer> player in allPlayers) {
			player.Value.SpawnPlayer();
		}
	}

	public void QuitGame() {
		// Debug.Log("QuitGame in LSM");
		DestroyAllPlayers();
		_initialized = false;
		enabled = false;
	}

	private void DestroyAllPlayers() {
		foreach (KeyValuePair<int, LockStepPlayer> player in allPlayers) {
			player.Value.DestroyPlayer();
		}
	}

	public void DropPlayer(int droppedPlayerIndex) {
		_networkInterface.CallDropPlayer(droppedPlayerIndex);
	}

	public void DestroyPlayerObject(int playerID) {
		allPlayers[playerID].DestroyPlayer();
		// TODO : Test if this will cause any problems
		allPlayers.Remove(playerID);
		_numberOfPlayers--;
	}

	public void ReadyToStart(int playerID) {
		// Debug.Log("Player " + playerID + " is ready to start the game.");

		// Make sure initialization has already happened in case another player
		// Sends game start before we are ready to handle it
		InitPlayersReadyState();

		_readyPlayers.Add(playerID);

		if (_confirmedActions.IsConfirmedActionsEnabled()) {
			if(Network.isServer) {
				//don't need an rpc call if we are the server
				ConfirmPlayerReadyToServer(_localPlayerIndex, playerID);
			} else {
				_networkInterface.CallConfirmPlayerReadyToServer(_localPlayerIndex, playerID);
			}
		}

		//Check if we can start the game
		CheckGameStart();
	}

	public void ConfirmPlayerReadyToServer(int confirmingPlayerID, int confirmedPlayerID) {
		//Only Server's responsible for all the confirm information
		if (!Network.isServer) { return; }

		// Debug.Log("Server Message: Player " + confirmingPlayerID + " is confirming Player " +
		// 	confirmedPlayerID + " is ready to start the game.");

		//validate ID
		if (!allPlayers.ContainsKey(confirmingPlayerID)) {
			// TODO: error handling
			Debug.Log("Server Message: WARNING!!! Unrecognized confirming playerID: " + confirmingPlayerID);
			return;
		}
		if (!allPlayers.ContainsKey(confirmedPlayerID)) {
			// TODO: error handling
			Debug.Log("Server Message: WARNING!!! Unrecognized confirmed playerID: " + confirmingPlayerID);
			return;
		}

		//relay message to confirmed client
		// if (_localPlayerIndex.Equals(confirmedPlayerID)) {
			//don't need an rpc call if we are the server
			// ReceiptPlayerReadyToClient(confirmingPlayerID, confirmedPlayerID);
		// } else {
			_networkInterface.CallReceiptPlayerReadyToClient(confirmingPlayerID, confirmedPlayerID);
		// }
	}

	public void ReceiptPlayerReadyToClient(int confirmingPlayerID, int confirmedPlayerID) {
		if (!_localPlayerIndex.Equals(confirmedPlayerID)) { return; }
		//Debug.Log ("Player " + confirmingPlayerID + " confirmed I am ready to start the game.");
		_playersConfirmedImReady.Add(confirmingPlayerID);
		//Check if we can start the game
		CheckGameStart();
	}

	//==============================================================================================
	// Lock-Step and Actions
	//==============================================================================================
	public void AddAction(Action action) {
		// Debug.Log(action.GetType().Name);
		if (!_initialized) {
			// Debug.Log("Game has not started, action will be ignored.");
			return;
		}
		_actionsToSend.Enqueue(action);
	}

	private bool LockStepTurn() {
		// Debug.Log("_lockStepTurnID: " + _lockStepTurnID);
		//Check if we can proceed with the next turn
		bool nextTurn = NextTurn();
		if (nextTurn) {
			SendPendingAction();
			//the first and second lockstep turn will not be ready to process yet
			if (_lockStepTurnID >= firstLockStepTurnID + 3) {
				ProcessActions();
			}
		}
		//otherwise wait another turn to recieve all input from all players
		UpdateGameFrameRate();
		return nextTurn;
	}

	private bool NextTurn() {
		// Debug.Log("Next Turn Check: Current Turn - " + _lockStepTurnID);
		// Debug.Log("    priorConfirmedCount - " + _confirmedActions.playersConfirmedPriorAction.Count);
		// Debug.Log("    currentConfirmedCount - " + _confirmedActions.playersConfirmedCurrentAction.Count);
		// Debug.Log("    allPlayerCurrentActionsCount - " + _pendingActions.currentActions.Count);
		// Debug.Log("    allPlayerNextActionsCount - " + _pendingActions.nextActions.Count);
		// Debug.Log("    allPlayerNextNextActionsCount - " + _pendingActions.nextNextActions.Count);
		// Debug.Log("    allPlayernextNextNextActionsCount - " + _pendingActions.nextNextNextActions.Count);

		if (_confirmedActions.ReadyForNextTurn()) {
			if (_pendingActions.ReadyForNextTurn()) {
				//increment the turn ID
				_lockStepTurnID++;
				//move the confirmed actions to next turn
				_confirmedActions.NextTurn();
				//move the pending actions to this turn
				_pendingActions.NextTurn();

				return true;
			} else {
				StringBuilder sb = new StringBuilder();
				sb.Append("Have not recieved player(s) actions: ");
				foreach (int i in _pendingActions.WhosNotReady()) {
					sb.Append(i + ", ");
				}
				// Debug.Log(sb.ToString ());
			}
		} else {
			StringBuilder sb = new StringBuilder();
			sb.Append("Have not recieved confirmation from player(s): ");
			foreach (int i in _pendingActions.WhosNotReady()) {
				sb.Append(i + ", ");
			}
			// Debug.Log(sb.ToString ());
		}
		return false;
	}

	private void SendPendingAction() {
		if (_isDedicateServer) { return; }
		Action action = null;
		if (_actionsToSend.Count > 0) {
			action = _actionsToSend.Dequeue();
		}
		//if no action for this turn, send the NoAction action
		if (action == null) {
			action = new NoAction();
		}
		//action.networkLagTime = Network.GetLastPing (Network.connections[0/*host player*/]);
		if (_lockStepTurnID > firstLockStepTurnID + 1) {
			action.networkLagTime = _confirmedActions.GetPriorTime();
		} else {
			action.networkLagTime = _initialLockStepTurnLength;
		}
		action.gameLagTime = Convert.ToInt32(_currentGameFrameRuntime);
		//clear the current runtime average
		_currentGameFrameRuntime = 0;
		//add action to our own list of actions to process
		// _pendingActions.AddAction(action, Convert.ToInt32(_localPlayerIndex), _lockStepTurnID, _lockStepTurnID);
		//start the confirmed action timer for network average
		_confirmedActions.StartTimer();
		//confirm our own action
		_confirmedActions.ConfirmAction(_localPlayerIndex, _lockStepTurnID, _lockStepTurnID);
		// _confirmedActions.AddCurrentConfirmed(Network.player);
		//send action to all other players
		_networkInterface.CallSendAction(_lockStepTurnID, _localPlayerIndex, BinarySerialization.SerializeObjectToByteArray(action));
		// Debug.Log("Sent " + (action.GetType().Name) + " action for turn " + _lockStepTurnID);
	}
	
	private void ProcessActions() {
		//process action should be considered in runtime performance
		_gameTurnSW.Start();

		//Rotate the order the player actions are processed so there is no advantage given to
		//any one player
		// TODO : Consider fairness between players, rotating first action
		foreach (KeyValuePair<int, Action> currentAction in _pendingActions.currentActions) {
			if (currentAction.Value != null) {
				currentAction.Value.ProcessAction();
				_runtimeRollingAverage.Add(currentAction.Value.gameLagTime, currentAction.Key);
				_networkRollingAverage.Add(currentAction.Value.networkLagTime, currentAction.Key);
			}
		}

		//finished processing actions for this turn, stop the stopwatch
		_gameTurnSW.Stop ();
	}

	public void RecieveAction(int lockStepTurn, int playerID, byte[] actionAsBytes) {
		//Debug.Log("Recieved Player " + playerID + "'s action for turn " + lockStepTurn + " on turn " + _lockStepTurnID);
		Action action = BinarySerialization.DeserializeObject<Action>(actionAsBytes);
		if (action == null) {
			Debug.Log("Sending action failed");
			//TODO: Error handle invalid actions recieve
		} else {
			_pendingActions.AddAction(action, Convert.ToInt32(playerID), _lockStepTurnID, lockStepTurn);

			//send confirmation
			if (_confirmedActions.IsConfirmedActionsEnabled()) {
				if (Network.isServer){
					//we don't need an rpc call if we are the server
					ConfirmActionServer(lockStepTurn, _localPlayerIndex, playerID);
				} else {
					_networkInterface.CallConfirmActionServer(lockStepTurn, _localPlayerIndex, playerID);
				}
			}
		}
	}
	
	public void ConfirmActionServer(int lockStepTurn, int confirmingPlayerID, int confirmedPlayerID) {
		if (!Network.isServer) { return; } //Workaround - if server and client on same machine

		//Debug.Log("ConfirmActionServer called turn:" + lockStepTurn + " playerID:" + confirmingPlayerID);
		//Debug.Log("Sending Confirmation to player " + confirmedPlayerID);

		if (_localPlayerIndex == confirmedPlayerID) {
			//we don't need an RPC call if this is the server
			ConfirmAction(lockStepTurn, confirmingPlayerID);
		} else {
			_networkInterface.CallConfirmAction(allPlayers[confirmedPlayerID], lockStepTurn, confirmingPlayerID);
		}
	}
	
	public void ConfirmAction(int lockStepTurn, int confirmingPlayerID) {
		_confirmedActions.ConfirmAction(confirmingPlayerID, _lockStepTurnID, lockStepTurn);
	}

	//==============================================================================================
	// Game Frame
	//==============================================================================================
	private void UpdateGameFrameRate() {
		//Debug.Log ("Runtime Average is " + _runtimeRollingAverage.GetMax ());
		//Debug.Log ("Network Average is " + _networkRollingAverage.GetMax ());
		// _lockstepTurnLength = (_networkRollingAverage.GetMax(0) * 2/*two round trips*/) + 1/*minimum of 1 ms*/;
		_lockstepTurnLength = _networkRollingAverage.GetMax(_initialLockStepTurnLength);
		_gameFrameTurnLength = _runtimeRollingAverage.GetMax(_initialGameFrameTurnLength);
		
		//lockstep turn has to be at least as long as one game frame (Terrible machine with great network)
		if (_gameFrameTurnLength > _lockstepTurnLength) {
			_lockstepTurnLength = _gameFrameTurnLength;
		}

		_gameFramesPerLockstepTurn = _lockstepTurnLength / _gameFrameTurnLength;
		if (_gameFramesPerLockstepTurn == 0) {
			Debug.Log("Error!!! _gameFramesPerLockstepTurn == 0");
		}
		//if gameFrame turn length does not evenly divide the lockstep turn, there is extra time left after the last
		//game frame. Add one to the game frame turn length so it will consume it and recalculate the Lockstep turn length
		if (_lockstepTurnLength % _gameFrameTurnLength > 0) {
			_gameFrameTurnLength++;
			_lockstepTurnLength = _gameFramesPerLockstepTurn * _gameFrameTurnLength;
		}

		_lockstepsPerSecond = (1000 / _lockstepTurnLength);
		if (_lockstepsPerSecond == 0) {_lockstepsPerSecond = 1;} //minimum per second
		
		_gameFramesPerSecond = _lockstepsPerSecond * _gameFramesPerLockstepTurn;
	}

	//called once per unity frame
	public void Update() {
		//Basically same logic as FixedUpdate, but we can scale it by adjusting FrameLength
		_accumilatedTime += Convert.ToInt32(Time.deltaTime * 1000); //convert sec to milliseconds

		//in case the FPS is too slow, we may need to update the game multiple times a frame
		while (_accumilatedTime > _gameFrameTurnLength) {
			GameFrameTurn ();
			_accumilatedTime = _accumilatedTime - _gameFrameTurnLength;
		}
	}

	// Local player input
	void LateUpdate() {
		// TODO : Need split this into LockStepPlayerLocalClass
		if (_isDedicateServer) { return; }
		if (Input.GetMouseButtonUp(0)) {
			Vector3 mousePosition = new Vector3();
			if (GetComponent<MousePickup>().GetPickUpPosition(out mousePosition)) {
				AddAction(new MovementAction(_localPlayerIndex, mousePosition.x, mousePosition.z));
			}
		}
	}

	void GameFrameTurn() {
		//first frame is used to process actions
		if (_gameFrame == 0) {
			if (!LockStepTurn()) {
				return;
			}
		}

		//start the stop watch to determine game frame runtime performance
		_gameTurnSW.Start();

		// update game

		// List<IHasGameFrame> finished = new List<IHasGameFrame>();
		// foreach(IHasGameFrame obj in SceneManager.Manager.GameFrameObjects) {
		// 	obj.GameFrameTurn(_gameFramesPerSecond);
		// 	if(obj.Finished) {
		// 		finished.Add (obj);
		// 	}
		// }

		// foreach(IHasGameFrame obj in finished) {
		// 	SceneManager.Manager.GameFrameObjects.Remove (obj);
		// }

		_gameFrame++;
		if (_gameFrame == _gameFramesPerLockstepTurn) {
			_gameFrame = 0;
		}
		//stop the stop watch, the gameFrame turn is over
		_gameTurnSW.Stop();
		// update only if it's larger - we will use the game frame that took the longest in this lockstep turn
		// deltaTime is in secounds, convert to milliseconds
		long runtime = Convert.ToInt32((Time.deltaTime * 1000)) + _gameTurnSW.ElapsedMilliseconds;
		// It seems not neccessary to check runtime's value
		// if (runtime > _currentGameFrameRuntime) { 
		_currentGameFrameRuntime = runtime;
		// }
		//clear for the next frame
		_gameTurnSW.Reset();
	}

	void OnGUI() {
		GUI.enabled = true;
		DrawLockStepStatus();
		DrawPlayerStatus();
	}

	private void DrawPlayerStatus() {
		int LabelW = 200;
		int LabelH = 20;
		int labelX = Screen.width - LabelW - 20;
		int labelY = 10;
		string text;

		// for (int i = 0; i < _pendingActions.currentActions.Count; i++) {
		foreach (KeyValuePair<int, Action> currentAction in _pendingActions.currentActions) {
			if (currentAction.Value == null) {
				continue;
			}
			int playerFrameTime = currentAction.Value.gameLagTime + currentAction.Value.networkLagTime;
			if (playerFrameTime > 370) {
				GUI.contentColor = Color.red;
			} else if (playerFrameTime > 262) {
				GUI.contentColor = Color.yellow;
			} else {
				GUI.contentColor = Color.green;
			}
			text = "Player " + currentAction.Key + "'s Fram Time : " + playerFrameTime;
			GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
			labelY += LabelH;
		}
	}

	private void DrawLockStepStatus() {
		int labelX = 10;
		int labelY = 10;
		int LabelW = 200;
		int LabelH = 20;
		string text;

		if (_lockstepTurnLength > 310) {
			GUI.contentColor = Color.red;
		} else if (_lockstepTurnLength > 210) {
			GUI.contentColor = Color.yellow;
		} else {
			GUI.contentColor = Color.green;
		}
		text = "Lock Step Turn Length: " + _lockstepTurnLength;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		if (_gameFrameTurnLength > 60) {
			GUI.contentColor = Color.red;
		} else if (_gameFrameTurnLength > 52) {
			GUI.contentColor = Color.yellow;
		} else {
			GUI.contentColor = Color.green;
		}
		text = "Game Fram Turn Length: " + _gameFrameTurnLength;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		GUI.contentColor = Color.green;
		text = "Game Frames Per Lock Step : " + _gameFramesPerLockstepTurn;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		text = "Lock Step Per Second : " + _lockstepsPerSecond;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		text = "Game Frames Per Secon : " + _gameFramesPerSecond;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		text = "Lock Step Turn : " + _lockStepTurnID;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;
	}
}
