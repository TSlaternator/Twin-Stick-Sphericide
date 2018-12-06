using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling enemy bullet movement and interactions
public class EnemyBulletController : MonoBehaviour {

	public float lifetime; //after this time the gameObject will be destroyed
	public float speed; //multiplier to control the speed of the bullets
	public int bulletDamage; //how much damage the bullet will do

	private Rigidbody bulletRigidbody; //holds a reference to a Rigidbody component

	/*
	 * Assigns References, and sets the bullets lifetime
	 */ 
	void Start () {
		bulletRigidbody = GetComponent<Rigidbody> (); 
		bulletRigidbody.velocity = transform.forward * speed; 

		Destroy (gameObject, lifetime);
	}
		
	/*
	 * Allows bullets to damage the Player if they make contact, and destroys them if they hit obstacles
	 */ 
	void OnTriggerEnter(Collider other){
		if (other.tag == "Tree") 
			Destroy (gameObject);

		if (other.tag == "Player") {
			other.gameObject.GetComponent<PlayerController> ().TakeDamage (bulletDamage);
			Destroy (gameObject);
		}
	}
}
