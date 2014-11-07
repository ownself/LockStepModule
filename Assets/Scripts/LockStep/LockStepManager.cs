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
	int numberOfPlayers = 0;
	public int GetPlayersNumber() {
		return numberOfPlayers;
	}

	// Use for confirm and ready from all the players
	List<int> readyPlayers;
	List<int> playersConfirmedImReady;
	// Use for store all the players in the session
	public Dictionary<int, LockStepPlayer> allPlayers;
	int localPlayerIndex;

	bool initialized = false;
	bool isDedicateServer;
	// Action List
	PendingActions pendingActions;
	ConfirmedActions confirmedActions;
	Queue<Action> actionsToSend;

	NetworkInterface networkInterface;

	//==============================================================================================
	// Variables for adjusting Lockstep and game frame length
	RollingAverage networkRollingAverage;
	RollingAverage runtimeRollingAverage;
	// used to find the maximum gameFrame runtime in the current lockstep turn
	long currentGameFrameRuntime;
	Stopwatch gameTurnSW;
	int initialLockStepTurnLength = 100; // in Milliseconds
	int initialGameFrameTurnLength = 25; // in Milliseconds
	int lockstepTurnLength;
	int gameFrameTurnLength;
	int gameFramesPerLockstepTurn;
	int lockstepsPerSecond;
	int gameFramesPerSecond;
	// int playerIDToProcessFirst = 0; // used to rotate what player's action gets processed first
	int gameFrame = 0; // current game frame number in the currect lockstep turn
	// Accumilated time in Milliseconds that have passed since the last time gameFrame was called
	int accumilatedTime = 0;

	int lockStepTurnID = firstLockStepTurnID;
	public int GetLockStepTurn() {
		return lockStepTurnID;
	}

	// Use this for initialization
	void Start () {
		enabled = false;
		networkInterface = GetComponent<NetworkInterface>();

		lockStepTurnID = firstLockStepTurnID;
		pendingActions = new PendingActions(this);
		confirmedActions = new ConfirmedActions(this);
		actionsToSend = new Queue<Action>();
		gameTurnSW = new Stopwatch();
		currentGameFrameRuntime = 0;
		allPlayers = new Dictionary<int, LockStepPlayer>();
	}

	public void EnableConfirmedActions(bool isEnable) {
		confirmedActions.EnableConfirmedActions(isEnable);
	}

	//==============================================================================================
	// Connect and Start the game
	//==============================================================================================
	public void InitPlayersReadyState() {
		if(initialized) { return; }

		readyPlayers = new List<int>(numberOfPlayers);
		playersConfirmedImReady = new List<int>(numberOfPlayers);

		initialized = true;
	}

	public void InitGame(int index, List<LockStepPlayer> players, bool isDedicated = false) {
		allPlayers.Clear();
		foreach (LockStepPlayer player in players) {
			// Debug.Log("Init Game with Player : " + player.GetPlayerIndex());
			allPlayers[player.GetPlayerIndex()] = player;
		}
		localPlayerIndex = index;
		isDedicateServer = isDedicated;
		numberOfPlayers = players.Count;
		if (numberOfPlayers == 0) {
			Debug.Log("Fatal Error! Incorrect number of players");
		}

		ResetGame();
		InitPlayersReadyState();
		if (confirmedActions.IsConfirmedActionsEnabled()) {
			networkInterface.CallReadyToStart(localPlayerIndex);
		} else {
			ReadyToStart(localPlayerIndex); // No confirmation, just start the game
		}
	}

	private void ResetGame() {
		// Debug.Log("Game Reseted My PlayerID: " + localPlayerIndex);
		lockStepTurnID = firstLockStepTurnID;
		pendingActions.Reset();
		confirmedActions .Reset();
		actionsToSend.Clear();

		gameTurnSW.Reset();
		currentGameFrameRuntime = 0;
		if (networkRollingAverage == null) {
			networkRollingAverage = new RollingAverage(numberOfPlayers, initialLockStepTurnLength);
		} else {
			networkRollingAverage.Reset();
		}
		if (runtimeRollingAverage == null) {
			runtimeRollingAverage = new RollingAverage(numberOfPlayers, initialLockStepTurnLength);
		} else {
			runtimeRollingAverage.Reset();
		}
	}

	private void CheckGameStart() {
		if (!confirmedActions.IsConfirmedActionsEnabled()) {
			StartGame();
			return;
		}
		if(playersConfirmedImReady == null) {
			// Debug.Log("WARNING!!! Unexpected null reference during game start.");
			return;
		}
		//check if all expected players confirmed our gamestart message
		if(playersConfirmedImReady.Count == numberOfPlayers) {
			//check if all expected players sent their gamestart message
			if(readyPlayers.Count == numberOfPlayers) {
				//we are ready to start
				// Debug.Log("All players are ready to start. Starting Game.");
				StartGame();
				//we no longer need these lists
				playersConfirmedImReady = null;
				readyPlayers = null;
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
		initialized = false;
		enabled = false;
	}

	private void DestroyAllPlayers() {
		foreach (KeyValuePair<int, LockStepPlayer> player in allPlayers) {
			player.Value.DestroyPlayer();
		}
	}

	public void DropPlayer(int droppedPlayerIndex) {
		networkInterface.CallDropPlayer(droppedPlayerIndex);
	}

	public void DestroyPlayerObject(int playerID) {
		allPlayers[playerID].DestroyPlayer();
		// TODO : Test if this will cause any problems
		allPlayers.Remove(playerID);
		numberOfPlayers--;
	}

	public void ReadyToStart(int playerID) {
		// Debug.Log("Player " + playerID + " is ready to start the game.");

		// Make sure initialization has already happened in case another player
		// Sends game start before we are ready to handle it
		InitPlayersReadyState();

		readyPlayers.Add(playerID);

		if (confirmedActions.IsConfirmedActionsEnabled()) {
			if(Network.isServer) {
				//don't need an rpc call if we are the server
				ConfirmPlayerReadyToServer(localPlayerIndex, playerID);
			} else {
				networkInterface.CallConfirmPlayerReadyToServer(localPlayerIndex, playerID);
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
		// if (localPlayerIndex.Equals(confirmedPlayerID)) {
			//don't need an rpc call if we are the server
			// ReceiptPlayerReadyToClient(confirmingPlayerID, confirmedPlayerID);
		// } else {
			networkInterface.CallReceiptPlayerReadyToClient(confirmingPlayerID, confirmedPlayerID);
		// }
	}

	public void ReceiptPlayerReadyToClient(int confirmingPlayerID, int confirmedPlayerID) {
		if (!localPlayerIndex.Equals(confirmedPlayerID)) { return; }
		//Debug.Log ("Player " + confirmingPlayerID + " confirmed I am ready to start the game.");
		playersConfirmedImReady.Add(confirmingPlayerID);
		//Check if we can start the game
		CheckGameStart();
	}

	//==============================================================================================
	// Lock-Step and Actions
	//==============================================================================================
	public void AddAction(Action action) {
		// Debug.Log(action.GetType().Name);
		if (!initialized) {
			// Debug.Log("Game has not started, action will be ignored.");
			return;
		}
		actionsToSend.Enqueue(action);
	}

	private bool LockStepTurn() {
		// Debug.Log("lockStepTurnID: " + lockStepTurnID);
		//Check if we can proceed with the next turn
		bool nextTurn = NextTurn();
		if (nextTurn) {
			SendPendingAction();
			//the first and second lockstep turn will not be ready to process yet
			if (lockStepTurnID >= firstLockStepTurnID + 3) {
				ProcessActions();
			}
		}
		//otherwise wait another turn to recieve all input from all players
		UpdateGameFrameRate();
		return nextTurn;
	}

	private bool NextTurn() {
		// Debug.Log("Next Turn Check: Current Turn - " + lockStepTurnID);
		// Debug.Log("    priorConfirmedCount - " + confirmedActions.playersConfirmedPriorAction.Count);
		// Debug.Log("    currentConfirmedCount - " + confirmedActions.playersConfirmedCurrentAction.Count);
		// Debug.Log("    allPlayerCurrentActionsCount - " + pendingActions.currentActions.Count);
		// Debug.Log("    allPlayerNextActionsCount - " + pendingActions.nextActions.Count);
		// Debug.Log("    allPlayerNextNextActionsCount - " + pendingActions.nextNextActions.Count);
		// Debug.Log("    allPlayernextNextNextActionsCount - " + pendingActions.nextNextNextActions.Count);

		if (confirmedActions.ReadyForNextTurn()) {
			if (pendingActions.ReadyForNextTurn()) {
				//increment the turn ID
				lockStepTurnID++;
				//move the confirmed actions to next turn
				confirmedActions.NextTurn();
				//move the pending actions to this turn
				pendingActions.NextTurn();

				return true;
			} else {
				StringBuilder sb = new StringBuilder();
				sb.Append("Have not recieved player(s) actions: ");
				foreach (int i in pendingActions.WhosNotReady()) {
					sb.Append(i + ", ");
				}
				// Debug.Log(sb.ToString ());
			}
		} else {
			StringBuilder sb = new StringBuilder();
			sb.Append("Have not recieved confirmation from player(s): ");
			foreach (int i in pendingActions.WhosNotReady()) {
				sb.Append(i + ", ");
			}
			// Debug.Log(sb.ToString ());
		}
		return false;
	}

	private void SendPendingAction() {
		if (isDedicateServer) { return; }
		Action action = null;
		if (actionsToSend.Count > 0) {
			action = actionsToSend.Dequeue();
		}
		//if no action for this turn, send the NoAction action
		if (action == null) {
			action = new NoAction();
		}
		//action.networkLagTime = Network.GetLastPing (Network.connections[0/*host player*/]);
		if (lockStepTurnID > firstLockStepTurnID + 1) {
			action.networkLagTime = confirmedActions.GetPriorTime();
		} else {
			action.networkLagTime = initialLockStepTurnLength;
		}
		action.gameLagTime = Convert.ToInt32(currentGameFrameRuntime);
		//clear the current runtime average
		currentGameFrameRuntime = 0;
		//add action to our own list of actions to process
		// pendingActions.AddAction(action, Convert.ToInt32(localPlayerIndex), lockStepTurnID, lockStepTurnID);
		//start the confirmed action timer for network average
		confirmedActions.StartTimer();
		//confirm our own action
		confirmedActions.ConfirmAction(localPlayerIndex, lockStepTurnID, lockStepTurnID);
		// confirmedActions.AddCurrentConfirmed(Network.player);
		//send action to all other players
		networkInterface.CallSendAction(lockStepTurnID, localPlayerIndex, BinarySerialization.SerializeObjectToByteArray(action));
		// Debug.Log("Sent " + (action.GetType().Name) + " action for turn " + lockStepTurnID);
	}
	
	private void ProcessActions() {
		//process action should be considered in runtime performance
		gameTurnSW.Start();

		//Rotate the order the player actions are processed so there is no advantage given to
		//any one player
		// TODO : Consider fairness between players, rotating first action
		foreach (KeyValuePair<int, Action> currentAction in pendingActions.currentActions) {
			if (currentAction.Value != null) {
				currentAction.Value.ProcessAction();
				runtimeRollingAverage.Add(currentAction.Value.gameLagTime, currentAction.Key);
				networkRollingAverage.Add(currentAction.Value.networkLagTime, currentAction.Key);
			}
		}

		//finished processing actions for this turn, stop the stopwatch
		gameTurnSW.Stop ();
	}

	public void RecieveAction(int lockStepTurn, int playerID, byte[] actionAsBytes) {
		//Debug.Log("Recieved Player " + playerID + "'s action for turn " + lockStepTurn + " on turn " + lockStepTurnID);
		Action action = BinarySerialization.DeserializeObject<Action>(actionAsBytes);
		if (action == null) {
			Debug.Log("Sending action failed");
			//TODO: Error handle invalid actions recieve
		} else {
			pendingActions.AddAction(action, Convert.ToInt32(playerID), lockStepTurnID, lockStepTurn);

			//send confirmation
			if (confirmedActions.IsConfirmedActionsEnabled()) {
				if (Network.isServer){
					//we don't need an rpc call if we are the server
					ConfirmActionServer(lockStepTurn, localPlayerIndex, playerID);
				} else {
					networkInterface.CallConfirmActionServer(lockStepTurn, localPlayerIndex, playerID);
				}
			}
		}
	}
	
	public void ConfirmActionServer(int lockStepTurn, int confirmingPlayerID, int confirmedPlayerID) {
		if (!Network.isServer) { return; } //Workaround - if server and client on same machine

		//Debug.Log("ConfirmActionServer called turn:" + lockStepTurn + " playerID:" + confirmingPlayerID);
		//Debug.Log("Sending Confirmation to player " + confirmedPlayerID);

		if (localPlayerIndex == confirmedPlayerID) {
			//we don't need an RPC call if this is the server
			ConfirmAction(lockStepTurn, confirmingPlayerID);
		} else {
			networkInterface.CallConfirmAction(allPlayers[confirmedPlayerID], lockStepTurn, confirmingPlayerID);
		}
	}
	
	public void ConfirmAction(int lockStepTurn, int confirmingPlayerID) {
		confirmedActions.ConfirmAction(confirmingPlayerID, lockStepTurnID, lockStepTurn);
	}

	//==============================================================================================
	// Game Frame
	//==============================================================================================
	private void UpdateGameFrameRate() {
		//Debug.Log ("Runtime Average is " + runtimeRollingAverage.GetMax ());
		//Debug.Log ("Network Average is " + networkRollingAverage.GetMax ());
		// lockstepTurnLength = (networkRollingAverage.GetMax(0) * 2/*two round trips*/) + 1/*minimum of 1 ms*/;
		lockstepTurnLength = networkRollingAverage.GetMax(initialLockStepTurnLength);
		gameFrameTurnLength = runtimeRollingAverage.GetMax(initialGameFrameTurnLength);
		
		//lockstep turn has to be at least as long as one game frame (Terrible machine with great network)
		if (gameFrameTurnLength > lockstepTurnLength) {
			lockstepTurnLength = gameFrameTurnLength;
		}

		gameFramesPerLockstepTurn = lockstepTurnLength / gameFrameTurnLength;
		if (gameFramesPerLockstepTurn == 0) {
			Debug.Log("Error!!! gameFramesPerLockstepTurn == 0");
		}
		//if gameFrame turn length does not evenly divide the lockstep turn, there is extra time left after the last
		//game frame. Add one to the game frame turn length so it will consume it and recalculate the Lockstep turn length
		if (lockstepTurnLength % gameFrameTurnLength > 0) {
			gameFrameTurnLength++;
			lockstepTurnLength = gameFramesPerLockstepTurn * gameFrameTurnLength;
		}

		lockstepsPerSecond = (1000 / lockstepTurnLength);
		if (lockstepsPerSecond == 0) {lockstepsPerSecond = 1;} //minimum per second
		
		gameFramesPerSecond = lockstepsPerSecond * gameFramesPerLockstepTurn;
	}

	//called once per unity frame
	public void Update() {
		//Basically same logic as FixedUpdate, but we can scale it by adjusting FrameLength
		accumilatedTime += Convert.ToInt32(Time.deltaTime * 1000); //convert sec to milliseconds

		//in case the FPS is too slow, we may need to update the game multiple times a frame
		while (accumilatedTime > gameFrameTurnLength) {
			GameFrameTurn ();
			accumilatedTime = accumilatedTime - gameFrameTurnLength;
		}
	}

	// Local player input
	void LateUpdate() {
		// TODO : Need split this into LockStepPlayerLocalClass
		if (isDedicateServer) { return; }
		if (Input.GetMouseButtonUp(0)) {
			Vector3 mousePosition = new Vector3();
			if (GetComponent<MousePickup>().GetPickUpPosition(out mousePosition)) {
				AddAction(new MovementAction(localPlayerIndex, mousePosition.x, mousePosition.z));
			}
		}
	}

	void GameFrameTurn() {
		//first frame is used to process actions
		if (gameFrame == 0) {
			if (!LockStepTurn()) {
				return;
			}
		}

		//start the stop watch to determine game frame runtime performance
		gameTurnSW.Start();

		// update game

		// List<IHasGameFrame> finished = new List<IHasGameFrame>();
		// foreach(IHasGameFrame obj in SceneManager.Manager.GameFrameObjects) {
		// 	obj.GameFrameTurn(gameFramesPerSecond);
		// 	if(obj.Finished) {
		// 		finished.Add (obj);
		// 	}
		// }

		// foreach(IHasGameFrame obj in finished) {
		// 	SceneManager.Manager.GameFrameObjects.Remove (obj);
		// }

		gameFrame++;
		if (gameFrame == gameFramesPerLockstepTurn) {
			gameFrame = 0;
		}
		//stop the stop watch, the gameFrame turn is over
		gameTurnSW.Stop();
		// update only if it's larger - we will use the game frame that took the longest in this lockstep turn
		// deltaTime is in secounds, convert to milliseconds
		long runtime = Convert.ToInt32((Time.deltaTime * 1000)) + gameTurnSW.ElapsedMilliseconds;
		// It seems not neccessary to check runtime's value
		// if (runtime > currentGameFrameRuntime) { 
		currentGameFrameRuntime = runtime;
		// }
		//clear for the next frame
		gameTurnSW.Reset();
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

		// for (int i = 0; i < pendingActions.currentActions.Count; i++) {
		foreach (KeyValuePair<int, Action> currentAction in pendingActions.currentActions) {
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

		if (lockstepTurnLength > 310) {
			GUI.contentColor = Color.red;
		} else if (lockstepTurnLength > 210) {
			GUI.contentColor = Color.yellow;
		} else {
			GUI.contentColor = Color.green;
		}
		text = "Lock Step Turn Length: " + lockstepTurnLength;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		if (gameFrameTurnLength > 60) {
			GUI.contentColor = Color.red;
		} else if (gameFrameTurnLength > 52) {
			GUI.contentColor = Color.yellow;
		} else {
			GUI.contentColor = Color.green;
		}
		text = "Game Fram Turn Length: " + gameFrameTurnLength;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		GUI.contentColor = Color.green;
		text = "Game Frames Per Lock Step : " + gameFramesPerLockstepTurn;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		text = "Lock Step Per Second : " + lockstepsPerSecond;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		text = "Game Frames Per Secon : " + gameFramesPerSecond;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;

		text = "Lock Step Turn : " + lockStepTurnID;
		GUI.Label(new Rect(labelX, labelY, LabelW, LabelH), text);
		labelY += LabelH;
	}
}
