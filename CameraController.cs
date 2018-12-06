using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class to control camera movement
public class CameraController : MonoBehaviour {

	public float heightDistance; //y distance from the target to the camera
	public float followDistance; //z distance from the target to the camera

	public Transform target; //gets the transform properties of the target GameObject (the player in this case)

	/*
	 * Sets the camera position each frame based on the position of the player and height/followDistance variabels
	 */
	void Update () {
		transform.position = new Vector3 (target.position.x, target.position.y + heightDistance, target.position.z - followDistance);
	}
}
