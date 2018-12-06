using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling speedBuff pickup interactions
public class PlayerSpeedPickup : MonoBehaviour {

	public float buffAmount; //amount to buff the players speed by 

	/*
	 * if the player moves into the pickup, buff them, then 'consume' the pickup
	 */
	void OnTriggerEnter(Collider other){
		if (other.tag == "Player" && other.isTrigger) {
			other.gameObject.GetComponent<PlayerController> ().StartSpeedBuff(buffAmount);
			Destroy (gameObject);
		}
	}
}
