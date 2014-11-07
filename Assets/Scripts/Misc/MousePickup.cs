using UnityEngine;
using System.Collections;

public class MousePickup : MonoBehaviour {

	public bool IsUsingLayerObject = false;
	public GameObject PickUpLayerObject;
	public Vector3 MousePosition;

	private bool IsSelected = false;
	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonUp(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit))
			{
				IsSelected = false;
				if (IsUsingLayerObject) {
					if (hit.collider.gameObject == PickUpLayerObject) {
						MousePosition = hit.point;
						IsSelected = true;
					}
				} else {
					if (hit.collider.gameObject != null) {
						MousePosition = hit.point;
						IsSelected = true;
					}
				}
				// print(hit.collider.gameObject.name);
			}
		}
	}

	public bool GetPickUpPosition(out Vector3 pickUpPosition) {
		pickUpPosition = MousePosition;
		return IsSelected;
	}
}
