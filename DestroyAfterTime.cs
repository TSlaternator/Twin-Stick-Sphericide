using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class to destroy a GameObject the specified amount of time after it's instantiated
public class DestroyAfterTime : MonoBehaviour {

	public float timeTillDespawn; //lifetime of the GameObject

	/*
	 * Destroys the object once it's lifetime is up
	 */
	void Start () {
		Destroy (gameObject, timeTillDespawn);
	}
}
