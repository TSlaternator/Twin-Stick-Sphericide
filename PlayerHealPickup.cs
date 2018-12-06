using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling health pickup interactions
public class PlayerHealPickup : MonoBehaviour {

	public int healAmount; //amount of healing to give

	/*
	 * if the player moves into the pickup, heal them, then 'consume' the pickup
	 */
	void OnTriggerEnter(Collider other){
		if (other.tag == "Player") {
			other.gameObject.GetComponent<PlayerController> ().HealDamage (healAmount); 
			Destroy (gameObject);
		}
	}
}
