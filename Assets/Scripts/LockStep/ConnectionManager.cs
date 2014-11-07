using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ConnectionManager : MonoBehaviour {

	//Networking Setting
	// public string connectionIP = "127.0.0.1";
	int connectionPort = 25001;
	string gameTypeName = "Ownself_LockStep_Module";
	string gameServerName = "Lock Step";
	string gameServerPort = "25001";
	bool refreshingHostList = false;
	bool isDedicateServer = false;
	HostData[] hostData;

	int numberOfPlayers = 2;
	public int GetPlayersNumber() {
		return numberOfPlayers;
	}
	int buttonOfPlayersSelected;
	public List<NetworkPlayer> allLobbyPlayers;

	LockStepManager lockStepManager;
	//Network View, for RPC function calling
	NetworkInterface networkInterface;

	// Use this for initialization
	void Start () {
		//Get network view
		lockStepManager = GetComponent<LockStepManager>();
		networkInterface = GetComponent<NetworkInterface>();
		buttonOfPlayersSelected = numberOfPlayers - 1;
	}

	// Update is called once per frame
	void Update () {
		if (refreshingHostList) {
			// Check the host polling list
			if (MasterServer.PollHostList().Length > 0) {
				// Debug.Log("HostList Length: " + MasterServer.PollHostList().Length);
				refreshingHostList = false;
				hostData = MasterServer.PollHostList();
			}
		}
	}

	//======================================================
	// Connection & Disconnection
	//======================================================
	// Only on Server
	void OnPlayerDisconnected(NetworkPlayer player) {
		// Debug.Log("Clean up after player " + player);
		lockStepManager.DropPlayer(Convert.ToInt32(player.ToString()));
		numberOfPlayers--;
		Network.RemoveRPCs(player);
	}

	// Only on Client
	void OnDisconnectedFromServer(NetworkDisconnection info) {
		numberOfPlayers = 1;
		lockStepManager.QuitGame();
	}

	// Initiatively disconnect
	void DisconnectFromServer() {
		Network.Disconnect(200);
	}

	// TODO : Need to handle the connected player after game started
	void OnPlayerConnected(NetworkPlayer player) {
		// Here we add a new player
		// Debug.Log("New player connected : " + player.ToString());
		allLobbyPlayers.Add(player);
		// Once all expected players have joined, send all clients the list of players
		if(allLobbyPlayers.Count == numberOfPlayers) {
			foreach(NetworkPlayer p in allLobbyPlayers) {
				// Debug.Log("Calling RegisterPlayerAll...");
				networkInterface.CallRegisterPlayerAll(p);
			}
			// start the game
			// Debug.Log("Ready to Start Sesion : " + allLobbyPlayers.Count + "/" + numberOfPlayers);
			networkInterface.CallStartSession();
		}
	}

	//======================================================
	// Game Initialization
	//======================================================
	void OnServerInitialized() {
		// Debug.Log("Server initialized");
		// Debug.Log("Expected player count : " + numberOfPlayers);
		// Notify any delegates that we are connected to the game
		SetClientPlayerNumber(numberOfPlayers);
		if (!isDedicateServer) {
			// Here we add host as a new player
			allLobbyPlayers.Add(Network.player);
		}
		// Send the number of players for this session and add it but RPC buffer
		networkInterface.CallSetClientPlayerNumber(numberOfPlayers);
		if(!isDedicateServer && numberOfPlayers == 1) {
			StartSession();
		}
	}

	public void SetClientPlayerNumber(int num) {
		// Debug.Log("Set Client Player numbers" + num);
		numberOfPlayers = num;
		allLobbyPlayers = new List<NetworkPlayer>();
	}

	public void RegisterPlayerAll(NetworkPlayer player) {
		// Debug.Log("Register Player All called for " + player.ToString());
		allLobbyPlayers.Add(player);
	}
	
	public void StartSession() {
		// send the start of game event
		// Debug.Log("Calling Start Game PRC");
		if (lockStepManager != null) {
			int localPlayerIndex = Convert.ToInt32(Network.player.ToString());
			lockStepManager.InitGame(localPlayerIndex, GetLockStepPlayers(allLobbyPlayers), isDedicateServer);
		} else {
			Debug.Log("Fatal error, LockStepManager hasn't been initialized");
		}
	}

	List<LockStepPlayer> GetLockStepPlayers(List<NetworkPlayer> networkPlayers) {
		List<LockStepPlayer> allPlayers = new List<LockStepPlayer>();
		foreach (NetworkPlayer networkPlayer in networkPlayers) {
			LockStepPlayer player = new LockStepPlayer();
			player.InitPlayer(Convert.ToInt32(networkPlayer.ToString()));
			player.networkPlayer = networkPlayer ;
			allPlayers.Add(player);
		}
		// Debug.Log("There is " + allPlayers.Count + " Players joined game");
		return allPlayers;
	}

	//======================================================
	// Server Initialization & Join
	//======================================================
	void StartHosting() {
		// Debug.Log("Starting Host");
		Network.InitializeServer(isDedicateServer ? numberOfPlayers : numberOfPlayers - 1, connectionPort, !Network.HavePublicAddress());
		MasterServer.RegisterHost(gameTypeName, gameServerName, isDedicateServer ? "Dedicated" : "Standalone");
	}

	private void SearchHosting() {
		hostData = null;
		MasterServer.RequestHostList(gameTypeName);
		refreshingHostList = true;
	}

	void JoinHosting(HostData host) {
		// Debug.Log("Joining Host");
		// Network.Connect(connectionIP, connectionPort);
		isDedicateServer = false;
		Network.Connect(host);
		hostData = null;
	}

	//======================================================
	// Debug UI
	//======================================================
	void OnGUI() {
		float btnX = Screen.width / 2 - 50;
		float btnY = 50;
		float btnW = 100;
		float btnH = 50;

		if (!Network.isClient && !Network.isServer) {
			string[] toolbarStrings = new string[] {"Single Player", "2 Players", "3 Players", "4 Players"};
			buttonOfPlayersSelected = GUI.Toolbar(new Rect(btnX - btnW * 1.5f, btnY, btnW * 4, btnH / 2), buttonOfPlayersSelected, toolbarStrings);
			numberOfPlayers = buttonOfPlayersSelected + 1;
			isDedicateServer = GUI.Toggle (new Rect (btnX - btnW - 60, btnY * 1.5f + btnH, btnW + 40, btnH), isDedicateServer, "Dedicate Server");
			if (GUI.Button(new Rect(btnX, btnY * 1.2f + btnH, btnW, btnH), "Start Server")) {
				// Debug.Log("Starting Server");
				StartHosting();
			}
			gameServerName = GUI.TextField(new Rect(btnX + btnW * 1.2f, btnY * 1.5f + btnH, btnW, btnH / 2), gameServerName);
			gameServerPort = GUI.TextField(new Rect(btnX + btnW * 2.2f, btnY * 1.5f + btnH, btnW, btnH / 2), gameServerPort);
			connectionPort = Convert.ToInt32(gameServerPort);
			// if (numberOfPlayers > 1 && GUI.Button(new Rect(btnX, btnY * 2.4f + btnH, btnW, btnH), "Client Connect")) {
			// 	JoinHosting();
			// }
			if (GUI.Button(new Rect(btnX, btnY * 2.4f + btnH, btnW, btnH), "Search Hosts")) {
				// Debug.Log("Refreshing Hosts");
				SearchHosting();
			}
			if (hostData != null) {
				int i =0;
				int length = hostData.Length;
				foreach (HostData hd in hostData) {
					int connectedPlayers = hd.connectedPlayers;
					int playerLimit = hd.playerLimit;
					if (hd.comment == "Dedicated") { connectedPlayers--; playerLimit--; }
					if (GUI.Button (new Rect(btnX - length * btnW + btnW * 1.2f * i, btnY * 3.6f + btnH, btnW, btnH),
						hd.gameName + ":" + connectedPlayers + "/" + playerLimit)) {
						// Debug.Log("Connecting to server");
						JoinHosting(hd);
					}
					i++;
				}
			}
			GUI.Label(new Rect(btnX, 10, 300, 20), "Status: Disconnected");
		}
		else if (Network.isClient) {
			GUI.Label(new Rect(btnX, 10, 300, 20), "Status: Connected as Client");
			if (GUI.Button(new Rect(btnX, 30, 120, 20), "Disconnect")) {
				DisconnectFromServer();
			}
		}
		else if (Network.isServer) {
			GUI.Label(new Rect(btnX, 10, 300, 20), "Status: Hosting");
			if (GUI.Button(new Rect(btnX, 30, 120, 20), "Disconnect")) {
				DisconnectFromServer();
			}
		}
		else {
			GUI.Label(new Rect(btnX, 10, 300, 20), "Status: Something Wrong" + Network.isServer + Network.isClient);
		}
	}
}
