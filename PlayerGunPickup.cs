using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling gun pickup interactions
public class PlayerGunPickup : MonoBehaviour {

	public string weaponName; //name of the weapon being picked up (used to identify the GameObjects in the PlayerController class)

	/*
	 * if the player moves into the pickup, give them the gun, then 'consume' the pickup
	 */
	void OnTriggerEnter(Collider other){
		if (other.tag == "Player" && other.isTrigger) {
			other.gameObject.GetComponent<PlayerController> ().PickUpWeapon(weaponName); 
			Destroy (gameObject);
		}
	}
}
