using UnityEngine;
using System.Collections;

public class PlayerCube : MonoBehaviour {

	public Vector3 targetPosition;
	bool _moving = false;
	float _speed = 0.1f;
	// int _playerID;

	// Use this for initialization
	void Start () {
	}

	public void Init(int playerID) {
		// _playerID = playerID;
		// Debug.Log("PlayerCube " + playerID + " Created");
		gameObject.name = playerID.ToString();
		switch (playerID) {
			case 0:
				gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(1,0,0));
				break;
			case 1:
				gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0,1,0));
				break;
			case 2:
				gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0,1,1));
				break;
			case 3:
				gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(1,1,0));
				break;
		}
	}

	public void GotoPosition(Vector3 pos) {
		targetPosition = pos;
		_moving = true;
	}

	// Update is called once per frame
	void Update () {
		if (_moving) {
			gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, targetPosition, _speed);
			Vector3 direction = targetPosition - gameObject.transform.position;
			if (direction.magnitude < 0.4) {
				_moving = false;
			}
			// direction.Normalize();
			// gameObject.transform.position += direction * Time.deltaTime * _speed;
		}
	}
}
