using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling bullet movement and interactions
public class BulletController : MonoBehaviour {

	public float lifetime; //after this time the gameObject will be destroyed
	public float speed; //multiplier to control the speed of the bullets
	public int bulletDamage; //how much damage the bullet will do

	public GameObject hitMarkerSoundObject; //GameObject to play a hitMarker Sound if the bullet hits an enemy AI

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
	 * Allows bullets to damage enemies they collide with, and destroys them if they hit obstacles
	 * Upon hitting an enemy, it will also play a HitMarker Sound
	 */ 
	void OnTriggerEnter(Collider other){
		if (other.tag == "Tree") {
			Destroy (gameObject);
		}

		if (other.tag == "Enemy") {
			other.gameObject.GetComponent<EnemyController> ().TakeDamage (bulletDamage);
			Instantiate (hitMarkerSoundObject);
			Destroy (gameObject);
		}

		if (other.tag == "Beserker") {
			other.gameObject.GetComponent<EnemyController> ().BeserkerTakeDamage (bulletDamage);
			Instantiate (hitMarkerSoundObject);
			Destroy (gameObject);
		}

		if (other.tag == "RangedEnemy") {
			other.gameObject.GetComponent<RangedEnemyController> ().TakeDamage (bulletDamage);
			Instantiate (hitMarkerSoundObject);
			Destroy (gameObject);
		}
	}
}
