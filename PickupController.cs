using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class to control the movement of my Pickups
public class PickupController : MonoBehaviour {

	public float tumble; //the maximum angular velocity of my pickups
	public float bobOffset; //maximum height above / below spawnHeight the pickup will bob

	private float spawnHeight; //holds the initial height of the pickup
	private float bobPerFrame = 0.0002f;
	private bool rising; //controls whether the pickup is rising or falling
	private Rigidbody targetRigidbody; //will hold a reference to the rigidbody component

	void Start (){
		targetRigidbody = GetComponent<Rigidbody> ();
		targetRigidbody.angularVelocity = Random.insideUnitSphere * tumble; //sets a random angular velocity to the pickups!
		spawnHeight = transform.position.y;
		rising = true;
	}
		
	void Update (){
		bob();
	}

	/* 
	 * bobs the pickup up and down 
	 */
	void bob (){
		if (transform.position.y > spawnHeight + bobOffset) rising = false;
		else if (transform.position.y < spawnHeight - bobOffset) rising = true;

		if (rising) transform.position += new Vector3(0f, bobPerFrame, 0f);
		else transform.position -= new Vector3(0f, bobPerFrame, 0f);
	} 
}
