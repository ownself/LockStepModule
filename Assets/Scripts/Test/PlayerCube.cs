using UnityEngine;
using System.Collections;

public class PlayerCube : MonoBehaviour {

	public Vector3 targetPosition;
	private bool moving = false;
	private float speed = 0.1f;
	private int playerID;

	// Use this for initialization
	void Start () {
	}

	public void Init(int playerID) {
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
		moving = true;
	}

	// Update is called once per frame
	void Update () {
		if (moving) {
			gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, targetPosition, speed);
			Vector3 direction = targetPosition - gameObject.transform.position;
			if (direction.magnitude < 0.4) {
				moving = false;
			}
			// direction.Normalize();
			// gameObject.transform.position += direction * Time.deltaTime * speed;
		}
	}
}
