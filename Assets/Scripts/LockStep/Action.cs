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
	private int playerID;
	private float x,z;
	public MovementAction(int id, float xFactor, float zFactor) {playerID = id; x = xFactor; z = zFactor;}
	public override void ProcessAction() {
		GameObject playerObject = GameObject.Find(playerID.ToString());
		// Debug.Log("MovementAction!!!" + playerID.ToString());
		if (playerObject != null) {
			playerObject.GetComponent<PlayerCube>().GotoPosition(new Vector3(x, 0, z));
			// Vector3 position = cubeGameObject.transform.position;
			// cubeGameObject.transform.position = new Vector3(position.x + x, position.y, position.z + z);
			// Debug.Log("Cube has been moved!!!");
		}
	}
}
