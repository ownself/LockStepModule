using UnityEngine;
using System.Collections;

public class LockStepPlayer {

	int playerIndex; // Index of current game
	public NetworkPlayer networkPlayer;
	GameObject playerObject;

	public void InitPlayer(int index) {
		playerIndex = index;
	}

	public void SpawnPlayer() {
		GameObject playerPrefab = (GameObject)Resources.Load("PlayerCube");
		playerObject = (GameObject)Object.Instantiate(playerPrefab,
			new Vector3(-1 + playerIndex * 2, 0, 0), Quaternion.identity);
		playerObject.GetComponent<PlayerCube>().Init(playerIndex);
	}

	public void DestroyPlayer() {
		GameObject playerObject = GameObject.Find(GetPlayerName());
		if (playerObject) {
			Object.Destroy(playerObject);
		}
	}

	public string GetPlayerName() {
		return playerIndex.ToString();
	}

	public int GetPlayerIndex() {
		return playerIndex;
	}
}
