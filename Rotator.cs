using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for giving objects a random rotation
public class Rotator : MonoBehaviour {

	public float tumble; //maximum angular velocity of the object

	private Rigidbody targetRigidbody; //will hold a reference to the rigidbody component

	/*
	 * gives the GameObject a random angular velocity when it's instantiated
	 */
	void Start (){
		targetRigidbody = GetComponent<Rigidbody> ();
		targetRigidbody.angularVelocity = Random.insideUnitSphere * tumble;
	}
}
