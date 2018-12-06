using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling enemies melee attacks
public class EnemyAttack : MonoBehaviour {

	public int damage; //damage the agent can deal per 'hit'

	/*
	 * if the player is inside the agents attack hitbox, damage them!
	 */
	public void OnTriggerStay(Collider other){
		if (other.gameObject.tag == "Player" && other.isTrigger) {
			other.gameObject.GetComponent<PlayerController> ().TakeDamage (damage); 
		}
	}
}
