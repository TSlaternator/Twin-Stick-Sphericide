using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling fast enemies suicide ability
public class EnemySuicide : MonoBehaviour {

	public GameObject agent; //GameObject of the agent
	public GameObject onSuicideSound; //GameObject which holds a sound clip to play on suicide
	public int damage; //damage the agents suicide will deal (to the player)

	/*
	 * if the player is inside the agents suicide hitbox, suicide, and damage them!
	 */
	public void OnTriggerStay(Collider other){
		if (other.gameObject.tag == "Player" && other.isTrigger) {
			other.gameObject.GetComponent<PlayerController> ().TakeDamage (damage); 
			Instantiate (onSuicideSound); 
			DestroyObject(agent);
		}
	}
}
