using System;
using UnityEngine;
using System.Collections;

[Serializable]
public abstract class Action {
	public int networkLagTime { get; set; }
	public int gameLagTime { get; set; }
	public abstract void ProcessAction();
}

[Serializable]
public class NoAction : Action {
	public override void ProcessAction() {}
}

[Serializable]
public class MovementAction : Action {
	int _playerID;
	float _x,_z;
	public MovementAction(int id, float xFactor, float zFactor) {_playerID = id; _x = xFactor; _z = zFactor;}
	public override void ProcessAction() {
		GameObject playerObject = GameObject.Find(_playerID.ToString());
		// Debug.Log("MovementAction!!!" + _playerID.ToString());
		if (playerObject != null) {
			playerObject.GetComponent<PlayerCube>().GotoPosition(new Vector3(_x, 0, _z));
			// Vector3 position = cubeGameObject.transform.position;
			// cubeGameObject.transform.position = new Vector3(position._x + _x, position.y, position._z + _z);
			// Debug.Log("Cube has been moved!!!");
		}
	}
}
