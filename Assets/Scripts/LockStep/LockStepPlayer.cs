using UnityEngine;
using System.Collections;

public class LockStepPlayer {

	public NetworkPlayer networkPlayer;
	GameObject _playerObject;
	int _playerIndex; // Index of current game

	public void InitPlayer(int index) {
		_playerIndex = index;
	}

	public void SpawnPlayer() {
		GameObject playerPrefab = (GameObject)Resources.Load("PlayerCube");
		_playerObject = (GameObject)Object.Instantiate(playerPrefab,
			new Vector3(-1 + _playerIndex * 2, 0, 0), Quaternion.identity);
		_playerObject.GetComponent<PlayerCube>().Init(_playerIndex);
	}

	public void DestroyPlayer() {
		GameObject _playerObject = GameObject.Find(GetPlayerName());
		if (_playerObject) {
			Object.Destroy(_playerObject);
		}
	}

	public string GetPlayerName() {
		return _playerIndex.ToString();
	}

	public int GetPlayerIndex() {
		return _playerIndex;
	}
}
