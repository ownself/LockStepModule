using System.Collections;
using UnityEngine;

public class NetworkInterface : MonoBehaviour {

	NetworkView networkViewComponent;
	LockStepManager lockStepManager;
	ConnectionManager connectionManager;

	// Use this for initialization
	void Start () {
		networkViewComponent = GetComponent<NetworkView>();
		if (networkViewComponent == null) {
			Debug.Log("Fatal error : didn't find NetworkView");
		}

		lockStepManager = GetComponent<LockStepManager>();
		if (lockStepManager == null) {
			Debug.Log("Fatal error : didn't find LockStepManager");
		}

		connectionManager = GetComponent<ConnectionManager>();
		if (connectionManager == null) {
			Debug.Log("Fatal error : didn't find ConnectionManager");
		}
	}

	//==============================================================================================
	// For ConnectionManager
	//----------------------------------------------------------------------------------------------
	// Server set the number of players of this game to all the clients
	public void CallSetClientPlayerNumber(int num) {
		networkViewComponent.RPC("RespondSetClientPlayerNumber", RPCMode.OthersBuffered, num);
	}
	[RPC]
	void RespondSetClientPlayerNumber(int num) {
		connectionManager.SetClientPlayerNumber(num);
	}

	//----------------------------------------------------------------------------------------------
	// Register all the players on clients from server
	public void CallRegisterPlayerAll(NetworkPlayer player) {
		networkViewComponent.RPC("RespondRegisterPlayerAll", RPCMode.Others, player);
	}
	[RPC]
	void RespondRegisterPlayerAll(NetworkPlayer player) {
		connectionManager.RegisterPlayerAll(player);
	}

	//----------------------------------------------------------------------------------------------
	// Start the lock-step manager
	public void CallStartSession() {
		networkViewComponent.RPC("RespondStartSession", RPCMode.All);
	}
	[RPC]
	void RespondStartSession() {
		connectionManager.StartSession();
	}

	//==============================================================================================
	// For Lock-Step Manager
	//----------------------------------------------------------------------------------------------
	// Deal the player's disconnection
	public void CallDropPlayer(int droppedPlayerIndex) {
		networkViewComponent.RPC("RespondDestroyPlayerObject", RPCMode.All, droppedPlayerIndex);
	}

	[RPC]
	void RespondDestroyPlayerObject(int playerID) {
		lockStepManager.DestroyPlayerObject(playerID);
	}

	//----------------------------------------------------------------------------------------------
	// About to start the game
	public void CallReadyToStart(int playerID) {
		networkViewComponent.RPC("RespondReadyToStart", RPCMode.AllBuffered, playerID);
	}

	[RPC]
	void RespondReadyToStart(int playerID) {
		lockStepManager.ReadyToStart(playerID);
	}

	//----------------------------------------------------------------------------------------------
	// Tell server that someone has recieved otherone's ready message
	public void CallConfirmPlayerReadyToServer(int confirmingPlayerID, int confirmedPlayerID) {
		networkViewComponent.RPC("RespondConfirmPlayerReadyToServer", RPCMode.Server, confirmingPlayerID, confirmedPlayerID);
	}
	[RPC]
	void RespondConfirmPlayerReadyToServer(int confirmingPlayerID, int confirmedPlayerID) {
		lockStepManager.ConfirmPlayerReadyToServer(confirmingPlayerID, confirmedPlayerID);
	}

	//----------------------------------------------------------------------------------------------
	// Follow the above, server tell the otherone the someone has recieved your ready message
	public void CallReceiptPlayerReadyToClient(int confirmingPlayerID, int confirmedPlayerID) {
		networkViewComponent.RPC("RespondReceiptPlayerReadyToClient", RPCMode.AllBuffered, confirmingPlayerID, confirmedPlayerID);
	}
	[RPC]
	void RespondReceiptPlayerReadyToClient(int confirmingPlayerID, int confirmedPlayerID) {
		lockStepManager.ReceiptPlayerReadyToClient(confirmingPlayerID, confirmedPlayerID);
	}

	//----------------------------------------------------------------------------------------------
	// To send the action
	public void CallSendAction(int lockStepTurn, int playerID, byte[] actionAsBytes) {
		networkViewComponent.RPC("RespondSendAction", RPCMode.All, lockStepTurn, playerID, actionAsBytes);
	}
	[RPC]
	void RespondSendAction(int lockStepTurn, int playerID, byte[] actionAsBytes) {
		lockStepManager.RecieveAction(lockStepTurn, playerID, actionAsBytes);
	}

	//----------------------------------------------------------------------------------------------
	// Tell server that someone has recieved otherone's action
	public void CallConfirmActionServer(int lockStepTurn, int confirmingPlayerID, int confirmedPlayerID) {
		networkViewComponent.RPC("RespondConfirmActionServer", RPCMode.Server, lockStepTurn, confirmingPlayerID, confirmedPlayerID);
	}
	[RPC]
	void RespondConfirmActionServer(int lockStepTurn, int confirmingPlayerID, int confirmedPlayerID) {
		lockStepManager.ConfirmActionServer(lockStepTurn, confirmingPlayerID, confirmedPlayerID);
	}

	//----------------------------------------------------------------------------------------------
	// Follow the above, server tell the otherone the someone has recieved your action
	public void CallConfirmAction(LockStepPlayer player, int lockStepTurn, int confirmingPlayerID) {
		networkViewComponent.RPC("RespondConfirmAction", player.networkPlayer, lockStepTurn, confirmingPlayerID);
	}
	[RPC]
	void RespondConfirmAction(int lockStepTurn, int confirmingPlayerID) {
		lockStepManager.ConfirmAction(lockStepTurn, confirmingPlayerID);
	}

	// Update is called once per frame
	void Update () {

	}
}
