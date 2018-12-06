using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling my Duck AI
public class DuckController : MonoBehaviour {

	public float maxSpeed; //fastest speed the agent can move at
	public float maxForce; //limits the potential steering force, lower values mean slower turnrate
	public Transform agent; //transform of the agent

	//Wander behaviour variables
	public float maxWander; //maximum wander force that can be applied
	public float circleDistance; //distance from the agent to the centre of the abstract circle
	public float circleRadius; //radius of the abstract circle
	public float wanderAngle; //maximum angle change per frame
	private float angle; //angle of the displacement vector inside the circle

	//Obstacle avoidance variables
	public float maxForwardAvoidance; //maximum avoidance force that can be applied
	public float scanRange; //range that the agent will look for obstacles in
	public Transform leftScan; //start point of the left front scanner
	public Transform rightScan; //start point of the right front scanner

	//Separate behaviour variables
	public float maxSeparate; //maximum seperate Force that can be applied
	private List<GameObject> separateList; //List containing references to all GameObjects the Duck needs to seperate from

	private Rigidbody agentRigidbody; //reference to the agents Rigidbody component
	private Vector3 desiredVelocity; //velocity to take the agent directly towards the target
	private Vector3 wanderForce; //wander force applied to the agent each frame 
	private Vector3 forwardAvoidanceForce; //frontal avoidance force applied to the agent each frame theres an obstacle ahead
	private Vector3 separateForce; //separate force to applied to the agent if it's too close to others
	private Vector3 steer; //steering force to slowly turn the agent towards it's desiredVelocity

	void Start () {
		agentRigidbody = GetComponent<Rigidbody> (); 
		separateList = new List<GameObject> ();
	}
		
	/*
	 * gets all forces to be applied to the Duck each frame and uses them to find the steer force
	 * also slerps the Ducks rotation, giving it a gradual turn rate
	 */
	void Update () {
		wanderForce = Vector3.ClampMagnitude (getWanderForce (), maxWander);
		forwardAvoidanceForce = Vector3.ClampMagnitude (getForwardAvoidanceForce (), maxForwardAvoidance);
		separateForce = Vector3.ClampMagnitude (getSeparateForce (), maxSeparate);

		steer = Vector3.ClampMagnitude ((wanderForce + forwardAvoidanceForce + separateForce), maxForce); 

		//if the Duck is moving, slerp it's rotation to smooth the turning
		if (agentRigidbody.velocity.magnitude > 0.1)
			transform.rotation = Quaternion.Slerp (transform.rotation, Quaternion.LookRotation (agentRigidbody.velocity), Time.deltaTime * 2.5f);
	}
		
	/*
	 * applies the steer force to the Ducks rigidbody
	 */
	void FixedUpdate(){
		agentRigidbody.velocity += steer;
	}
		
	/*
	 * Returns the random wander force for this agent, causing it to wander randomly, but realistically
	 */
	Vector3 getWanderForce(){
		Vector3 circleCentre; //vector from the agent to the centre of the abstract circle
		Vector3 wanderTarget; //point on the circle the Duck will seek
		float xDisplacement; //float to hold the x value of the displacement inside the abstact circle
		float zDisplacement; //float to hold the z value of the displacement inside the abstract circle

		//setting up position of the abstract circle
		circleCentre = agentRigidbody.velocity;
		circleCentre.Normalize();
		circleCentre *= circleDistance;

		//generating a random angle for my displacement point
		angle += Random.Range(-wanderAngle, wanderAngle); 
		xDisplacement = Mathf.Cos (angle) * circleRadius;
		zDisplacement = Mathf.Sin (angle) * circleRadius;

		//setting up target vector by adding circleCentre and Displacement point
		wanderTarget = new Vector3 ((agent.position.x + circleCentre.x + xDisplacement), agent.position.y , (agent.position.z + circleCentre.z + zDisplacement));

		//sets desiredVelocity to a vector of length maxSpeed in the direction of the target
		desiredVelocity = (wanderTarget - agent.position); 
		desiredVelocity.Normalize(); 
		desiredVelocity *= maxSpeed; 

		//returns a Vector in the direction from the Ducks current velocity to the desired velocity
		return (desiredVelocity - agentRigidbody.velocity); 
	}
		
	/*
	 * returns the ForwardAvoidanceForce for the Duck, allowing it to turn away from obstacles in it's path
	 */
	Vector3 getForwardAvoidanceForce(){
		RaycastHit LeftHit; //left side Raycast, causes the agent to turn right if it senses the closest collision
		RaycastHit RightHit; //right side Raycast, causes the agent to turn left if it senses the closets collision
		desiredVelocity = new Vector3 (0.0f, 0.0f, 0.0f); //setting up desiredVelocity variable

		//statements to determine which raycast detects the closest collision, then apply the closest rays force
		if (Physics.Raycast (leftScan.position, leftScan.forward, out LeftHit, scanRange) && Physics.Raycast (rightScan.position, rightScan.forward, out RightHit, scanRange)){

			if (LeftHit.distance < RightHit.distance) 
				desiredVelocity = (agent.right * maxSpeed) / LeftHit.distance; 

			if (RightHit.distance < LeftHit.distance)
				desiredVelocity = (-agent.right * maxSpeed) / RightHit.distance;

			if (LeftHit.distance == RightHit.distance) {
				int randomDirection = Random.Range (0, 2);

				if (randomDirection == 0)
					desiredVelocity = (agent.right * maxSpeed) / LeftHit.distance;

				if (randomDirection == 1)
					desiredVelocity = (-agent.right * maxSpeed) / RightHit.distance;
			}
		}
		else {

			if (Physics.Raycast (leftScan.position, leftScan.forward, out LeftHit, scanRange))
				desiredVelocity = (agent.right * maxSpeed) / LeftHit.distance;
			else {	

				if (Physics.Raycast (rightScan.position, rightScan.forward, out RightHit, scanRange))
					desiredVelocity = (-agent.right * maxSpeed) / RightHit.distance;
			}
		}
		//if a collision is detected, return an avoidance steering force, if not, return a default vector3
		if (desiredVelocity != new Vector3 (0.0f, 0.0f, 0.0f))
			return (desiredVelocity - agentRigidbody.velocity);
		else
			return desiredVelocity;

	}
		
	/*
	 * returns the Separate force for the Duck, making it separate from other nearby Ducks
	 */
	Vector3 getSeparateForce(){
		Vector3 desiredVelocitySum = new Vector3 (0, 0, 0); //Vector sum of all avoidance vectors away from nearby Ducks

		//if there are Ducks nearby, add up flee vectors from them, and average them to create a Separate vector
		if (separateList.Count > 0) {
			for (int i = 0; i < separateList.Count; i++) {
				desiredVelocity = (new Vector3 (agent.position.x, 0.0f, agent.position.z) - new Vector3 (separateList[i].transform.position.x, 0.0f, separateList[i].transform.position.z)); 
				desiredVelocity.Normalize ();
				desiredVelocitySum += desiredVelocity;
			}
			desiredVelocitySum /= separateList.Count;
			desiredVelocitySum.Normalize ();
			desiredVelocitySum *= maxSpeed;
		}

		return (desiredVelocitySum - agentRigidbody.velocity); 
	}

	/*
	 * If another Duck is too close, add it to the separate list, so this Duck can move away!
	 */
	void OnTriggerEnter(Collider other){
		if (!separateList.Contains (other.gameObject) && other.gameObject.tag == "Duck" && other.gameObject != agent.gameObject) {
			separateList.Add (other.gameObject);
		}
	}

	/*
	 * Once another Duck isn't too close anymore, remove it from the separate list so it won't affect this Ducks actions
	 */
	void OnTriggerExit(Collider other){
		if (separateList.Contains (other.gameObject)) {
			separateList.Remove (other.gameObject);
		}
	}
}
