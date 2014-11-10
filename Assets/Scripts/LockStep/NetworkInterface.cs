using System.Collections;
using UnityEngine;

public class NetworkInterface : MonoBehaviour {

	NetworkView _networkViewComponent;
	LockStepManager _lockStepManager;
	ConnectionManager _connectionManager;

	// Use this for initialization
	void Start () {
		_networkViewComponent = GetComponent<NetworkView>();
		if (_networkViewComponent == null) {
			Debug.Log("Fatal error : didn't find NetworkView");
		}

		_lockStepManager = GetComponent<LockStepManager>();
		if (_lockStepManager == null) {
			Debug.Log("Fatal error : didn't find LockStepManager");
		}

		_connectionManager = GetComponent<ConnectionManager>();
		if (_connectionManager == null) {
			Debug.Log("Fatal error : didn't find ConnectionManager");
		}
	}

	//==============================================================================================
	// For ConnectionManager
	//----------------------------------------------------------------------------------------------
	// Server set the number of players of this game to all the clients
	public void CallSetClientPlayerNumber(int num) {
		_networkViewComponent.RPC("RespondSetClientPlayerNumber", RPCMode.OthersBuffered, num);
	}
	[RPC]
	void RespondSetClientPlayerNumber(int num) {
		_connectionManager.SetClientPlayerNumber(num);
	}

	//----------------------------------------------------------------------------------------------
	// Register all the players on clients from server
	public void CallRegisterPlayerAll(NetworkPlayer player) {
		_networkViewComponent.RPC("RespondRegisterPlayerAll", RPCMode.Others, player);
	}
	[RPC]
	void RespondRegisterPlayerAll(NetworkPlayer player) {
		_connectionManager.RegisterPlayerAll(player);
	}

	//----------------------------------------------------------------------------------------------
	// Start the lock-step manager
	public void CallStartSession() {
		_networkViewComponent.RPC("RespondStartSession", RPCMode.All);
	}
	[RPC]
	void RespondStartSession() {
		_connectionManager.StartSession();
	}

	//==============================================================================================
	// For Lock-Step Manager
	//----------------------------------------------------------------------------------------------
	// Deal the player's disconnection
	public void CallDropPlayer(int droppedPlayerIndex) {
		_networkViewComponent.RPC("RespondDestroyPlayerObject", RPCMode.All, droppedPlayerIndex);
	}

	[RPC]
	void RespondDestroyPlayerObject(int playerID) {
		_lockStepManager.DestroyPlayerObject(playerID);
	}

	//----------------------------------------------------------------------------------------------
	// About to start the game
	public void CallReadyToStart(int playerID) {
		_networkViewComponent.RPC("RespondReadyToStart", RPCMode.AllBuffered, playerID);
	}

	[RPC]
	void RespondReadyToStart(int playerID) {
		_lockStepManager.ReadyToStart(playerID);
	}

	//----------------------------------------------------------------------------------------------
	// Tell server that someone has recieved otherone's ready message
	public void CallConfirmPlayerReadyToServer(int confirmingPlayerID, int confirmedPlayerID) {
		_networkViewComponent.RPC("RespondConfirmPlayerReadyToServer", RPCMode.Server, confirmingPlayerID, confirmedPlayerID);
	}
	[RPC]
	void RespondConfirmPlayerReadyToServer(int confirmingPlayerID, int confirmedPlayerID) {
		_lockStepManager.ConfirmPlayerReadyToServer(confirmingPlayerID, confirmedPlayerID);
	}

	//----------------------------------------------------------------------------------------------
	// Follow the above, server tell the otherone the someone has recieved your ready message
	public void CallReceiptPlayerReadyToClient(int confirmingPlayerID, int confirmedPlayerID) {
		_networkViewComponent.RPC("RespondReceiptPlayerReadyToClient", RPCMode.AllBuffered, confirmingPlayerID, confirmedPlayerID);
	}
	[RPC]
	void RespondReceiptPlayerReadyToClient(int confirmingPlayerID, int confirmedPlayerID) {
		_lockStepManager.ReceiptPlayerReadyToClient(confirmingPlayerID, confirmedPlayerID);
	}

	//----------------------------------------------------------------------------------------------
	// To send the action
	public void CallSendAction(int lockStepTurn, int playerID, byte[] actionAsBytes) {
		_networkViewComponent.RPC("RespondSendAction", RPCMode.All, lockStepTurn, playerID, actionAsBytes);
	}
	[RPC]
	void RespondSendAction(int lockStepTurn, int playerID, byte[] actionAsBytes) {
		_lockStepManager.RecieveAction(lockStepTurn, playerID, actionAsBytes);
	}

	//----------------------------------------------------------------------------------------------
	// Tell server that someone has recieved otherone's action
	public void CallConfirmActionServer(int lockStepTurn, int confirmingPlayerID, int confirmedPlayerID) {
		_networkViewComponent.RPC("RespondConfirmActionServer", RPCMode.Server, lockStepTurn, confirmingPlayerID, confirmedPlayerID);
	}
	[RPC]
	void RespondConfirmActionServer(int lockStepTurn, int confirmingPlayerID, int confirmedPlayerID) {
		_lockStepManager.ConfirmActionServer(lockStepTurn, confirmingPlayerID, confirmedPlayerID);
	}

	//----------------------------------------------------------------------------------------------
	// Follow the above, server tell the otherone the someone has recieved your action
	public void CallConfirmAction(LockStepPlayer player, int lockStepTurn, int confirmingPlayerID) {
		_networkViewComponent.RPC("RespondConfirmAction", player.networkPlayer, lockStepTurn, confirmingPlayerID);
	}
	[RPC]
	void RespondConfirmAction(int lockStepTurn, int confirmingPlayerID) {
		_lockStepManager.ConfirmAction(lockStepTurn, confirmingPlayerID);
	}

	// Update is called once per frame
	void Update () {

	}
}
