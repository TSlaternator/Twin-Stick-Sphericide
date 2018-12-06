using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling ammo pickup interactions
public class PlayerAmmoPickup : MonoBehaviour {

	public string ammoType; //type of ammo (each gun has a different ammo type)
	public int ammoAmount; //amount of ammo to pickup

	/*
	 * if the player moves into the pickup, resupply their ammo, then 'consume' the pickup
	 */
	void OnTriggerEnter(Collider other){
		if (other.tag == "Player" && other.isTrigger) {
			other.gameObject.GetComponent<PlayerController> ().PickUpAmmo(ammoType, ammoAmount);
			Destroy (gameObject);
		}
	}
}
