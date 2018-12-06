using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling fearBuff pickup interactions
public class PlayerFearPickup : MonoBehaviour {

	public float buffDuration; //how long to buff the player for

	/*
	 * if the player moves into the pickup, give them the aura, then 'consume' the pickup
	 */
	void OnTriggerEnter(Collider other){
		if (other.tag == "Player" && other.isTrigger) {
			other.gameObject.GetComponent<PlayerController> ().StartFearBuff(buffDuration);
			Destroy (gameObject);
		}
	}
}
