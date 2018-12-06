using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling ranged enemy AI movement and interactions
public class RangedEnemyController : MonoBehaviour {

	//non behaviour specific variables
	public int hitPoints; //HP of the agent
	public int scoreReward; //score rewarded to the player for killing this Agent
	public float maxSpeed; //fastest speed the agent can move at
	public float maxForce; //limits the potential total steering force, lower values mean slower turnrate
	public float maxBehaviourForce; //limits the potential behaviour forces magnitude (seek, pursue, wander)
	public float maxAvoidanceForce; //limits the maximum collision avoidance forces magnitude (forward and side)
	public float detectionRange; //the range at which the agent will 'detect' its target
	public float nearShootRange; //the range at which the agent will start shooting it's target
	public float farShootRange; //the range at which the agent will stop shooting it's target
	public float fireRate; //variable to set fireRate of the agents weapon
	public float strafeTime; //how long the agent will strafe when hit by a bullet from the player
	public Transform agent; //transform of the agent
	public Transform bulletSpawn; //transform of the bulletSpawn gameObject
	public GameObject bullet; //bullet objects to be fires at the player
	public GameObject onDeathSound; //GameObject to play a sound upon the agents death
	public GameObject deathSplatter; //GameObject to play a particle effect upon the agents death
	public AudioSource gunshotSound; //reference to gunshot audio 

	private Rigidbody agentRigidbody; //reference to the agents Rigidbody component
	private GameObject target; //gameobject of the target (the player)
	private Vector3 desiredVelocity; //velocity to take the agent directly towards the target
	private Vector3 steer; //steering force to slowly turn the agent towards it's desiredVelocity
	private Director director; //reference to my director class, allowing me to find out KPM
	private float targetDistance; //distance to the target
	private float nextFire; //holds the soonest time the agent can fire again after firing
	private bool dead = false; //boolean to show when the agent is dead
	private bool lookAtTarget; //boolean to show whether the agent is looking at its target or its velocity
	private bool strafing = false; //booleon to show whether the agent is strafing or not
	private string strafeDirection; //string to show which direction the agent is strafing

	//variables for wandering behaviour
	public float circleDistance; //distance from the agent to the centre of the abstract circle
	public float circleRadius; //radius of the abstract circle
	public float wanderAngle; //maximum angle change per frame
	private float angle; //angle of the displacement vector inside the circle

	// variables for collision avoidance
	public float scanRange; //range that the agent will look for obstacles in
	public Transform leftScan; //start point of the left front scanner
	public Transform rightScan; //start point of the right front scanner

	//variables for displaying hits to the player
	public MeshRenderer body; //Meshrenderer of the agents body
	public MeshRenderer lHand; //Meshrenderer of the agents left hand
	public MeshRenderer rHand; //Meshrenderer of the agents right hand
	public Material skinColour; //Original material of the agent
	public Material flashColour; //material to be temporarily displayed when the agent takes damage
	private float flashTime = 0.05f; //how long to display the above material for before reverting to the original

	void Start () {
		agentRigidbody = GetComponent<Rigidbody> (); 
		director = GameObject.FindGameObjectWithTag("GameController").GetComponent<Director> ();
		target = GameObject.FindGameObjectWithTag ("Player");
		nextFire = 0f;
	}

	/*
	 * controls the FSM of the agent, and it's rotation, each frame
	 */ 
	void Update () {
		//finding absolute distance to the target
		targetDistance = Mathf.Abs(Vector3.Magnitude(target.transform.position - agent.position));

		//finite state machine control statements
		if (strafing) {
			steer = getStrafeForce ();
			lookAtTarget = true;

		} else if (targetDistance >= detectionRange) {
			steer = getWanderForce ();
			lookAtTarget = false;

		} else if (targetDistance < nearShootRange) {
			steer = getBackOffForce ();
			lookAtTarget = true;

		} else if (targetDistance < farShootRange && Time.time > nextFire){
			steer = new Vector3 (0, 0, 0);
			lookAtTarget = true;
			shoot ();

		} else if (targetDistance < farShootRange) {
			steer = new Vector3 (0, 0, 0);
			lookAtTarget = true;

		} else if (targetDistance < detectionRange) {
			steer = getSeekForce ();
			lookAtTarget = false;
		}

		//if the agent is moving, slerp it's rotation to smooth the turning, rotation will be towards the agents velocity, or towards the player based on the LookAtTarget boolean
		if (lookAtTarget == false && agentRigidbody.velocity.magnitude > 0.1) {
			transform.rotation = Quaternion.Slerp (transform.rotation, Quaternion.LookRotation (agentRigidbody.velocity), Time.deltaTime * 5.0f);

		} else if (lookAtTarget == true) {
			transform.rotation = Quaternion.Slerp (transform.rotation, Quaternion.LookRotation(target.transform.position - transform.position), Time.deltaTime * 20.0f);
		}
	}

	/*
	 * applies the steer force to the agents Rigidbody
	 */ 
	void FixedUpdate (){
		agentRigidbody.velocity += steer;
	}

	/*
	 * returns the Seek force for this agent
	 */ 
	Vector3 getSeekForce(){

		//sets desiredVelocity to a vector of length maxSpeed in the direction of the target
		desiredVelocity = (new Vector3 (target.transform.position.x, 0.0f, target.transform.position.z) - new Vector3(agent.position.x, 0.0f, agent.position.z)); 
		desiredVelocity.Normalize();
		desiredVelocity *= maxSpeed; 

		//returns a Vector in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + Vector3.ClampMagnitude(getForwardAvoidanceForce() + getSidewaysAvoidanceForce(), maxAvoidanceForce), maxForce);  
	}

	/*
	 * returns the Back off force for this agent, allowing it to back away from the player
	 */ 
	Vector3 getBackOffForce(){

		//sets desiredVelocity to a vector of length maxSpeed in the opposite direction than the target
		desiredVelocity = (new Vector3(agent.position.x, 0.0f, agent.position.z) - new Vector3 (target.transform.position.x, 0.0f, target.transform.position.z)); 
		desiredVelocity.Normalize();
		desiredVelocity *= maxSpeed; 

		//returns a Vector in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude ((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce);  
	}

	/*
	 * sets the agent to strafe, and which (random) direction to strafe upon being hit
	 */ 
	IEnumerator startStrafe(){
		int direction = Random.Range (0, 2);
		if (direction == 0) 
			strafeDirection = "Left";
		else if (direction == 1) 
			strafeDirection = "Right";

		strafing = true;
		yield return new WaitForSeconds (strafeTime);
		strafing = false;
	}

	/*
	 * returns the Strafe force for this agent, allowing it to back away from the player
	 */ 
	Vector3 getStrafeForce(){
		//sets desiredVelocity to a vector of length maxSpeed perpendicular to the target
		desiredVelocity = (new Vector3 (target.transform.position.x, 0.0f, target.transform.position.z) - new Vector3(agent.position.x, 0.0f, agent.position.z)); 
		//sets whether the perpendicular vector is right, or left, based on StrafeDirection
		if (strafeDirection == "Left") 
			desiredVelocity = Quaternion.Euler (0, -90, 0) * desiredVelocity;
		else if (strafeDirection == "Right") 
			desiredVelocity = Quaternion.Euler (0, 90, 0) * desiredVelocity;

		desiredVelocity.Normalize();
		desiredVelocity *= maxSpeed; 

		//returns a Vector in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + Vector3.ClampMagnitude(getSidewaysAvoidanceForce(), maxAvoidanceForce), maxForce);  
	}

	/*
	 * returns the Wander force for this agent, allowing it to wander randomly, but realistically
	 */ 
	Vector3 getWanderForce(){
		Vector3 circleCentre; //vector from the agent to the centre of the abstract circle
		Vector3 wanderTarget; //point on the circle the agent will seek
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

		//returns a Vector in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + Vector3.ClampMagnitude(getForwardAvoidanceForce() + getSidewaysAvoidanceForce(), maxAvoidanceForce), maxForce); 
	}

	/*
	 * returns the ForwardAvoidanceForce force for this agent, allowing it to avoid obstacles infront of it
	 */ 
	Vector3 getForwardAvoidanceForce(){
		RaycastHit LeftHit; //left side Raycast, causes the agent to turn right if it senses the closest collision
		RaycastHit RightHit; //right side Raycast, causes the agent to turn left if it senses the closets collision
		desiredVelocity = new Vector3 (0.0f, 0.0f, 0.0f); //setting up desiredVelocity variable

		//if statements to determine which raycast detects the closest collision, then apply the closest rays force
		if (Physics.Raycast (leftScan.position, leftScan.forward, out LeftHit, scanRange) && LeftHit.collider.gameObject.tag != "Player" && Physics.Raycast (rightScan.position, rightScan.forward, out RightHit, scanRange) && RightHit.collider.gameObject.tag != "Player"){

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

			if (Physics.Raycast (leftScan.position, leftScan.forward, out LeftHit, scanRange) && LeftHit.collider.gameObject.tag != "Player")
				desiredVelocity = (agent.right * maxSpeed) / LeftHit.distance;
			else {	

				if (Physics.Raycast (rightScan.position, rightScan.forward, out RightHit, scanRange) && RightHit.collider.gameObject.tag != "Player")
					desiredVelocity = (-agent.right * maxSpeed) / RightHit.distance;
			}
		}
		//if a collision is detected, return an avoidance steering force, if not, return a default vector3
		if (desiredVelocity != new Vector3 (0.0f, 0.0f, 0.0f))
			return Vector3.ClampMagnitude ((desiredVelocity - agentRigidbody.velocity), maxAvoidanceForce); 
		else
			return desiredVelocity;

	}
		
	/*
	 * returns the sideways avoidance force for this agent, allowing it to move around walls or avoid obstacles to its sides 
	 */ 
	Vector3 getSidewaysAvoidanceForce(){
		RaycastHit frontLeftHit; //raycast detecting collisions to the agents '10:30 o clock'
		RaycastHit midLeftHit; //raycast detecting collisions to the agents '9 o clock'
		RaycastHit backLeftHit; //raycast detecting collisions to the agents '7:30 o clock'
		RaycastHit frontRightHit; //raycast detecting collisions to the agents '1:30 o clock'
		RaycastHit midRightHit; //raycast detecting collisions to the agents '3 o clock'
		RaycastHit backRightHit; //raycast detecting collisions to the agents '4:30 o clock'
		desiredVelocity = new Vector3(0.0f, 0.0f, 0.0f); //sets a default value to use if no collisions are detected

		//adds a force for EACH raycast detecting a collision
		if (Physics.Raycast (agent.position, Vector3.Normalize (agent.forward - agent.right), out frontLeftHit, 1.5f)) {
			if (frontLeftHit.collider.gameObject.tag != "Player")
				desiredVelocity += (agent.right * maxSpeed) / frontLeftHit.distance;
		}

		if (Physics.Raycast (agent.position, -agent.right, out midLeftHit, 1.5f)) {
			if (midLeftHit.collider.gameObject.tag != "Player")
				desiredVelocity += (Vector3.Normalize (agent.forward + agent.right) * maxSpeed) / midLeftHit.distance;
		}

		if (Physics.Raycast (agent.position, Vector3.Normalize (-agent.forward - agent.right), out backLeftHit, 1.5f)) {
			if (backLeftHit.collider.gameObject.tag != "Player")
				desiredVelocity += (agent.forward * maxSpeed) / backLeftHit.distance;
		}

		if (Physics.Raycast (agent.position, Vector3.Normalize (agent.forward + agent.right), out frontRightHit, 1.5f)) {
			if (frontRightHit.collider.gameObject.tag != "Player")
				desiredVelocity += (-agent.right * maxSpeed) / frontRightHit.distance;
		}

		if (Physics.Raycast (agent.position, agent.right, out midRightHit, 1.5f)) {
			if (midRightHit.collider.gameObject.tag != "Player")
				desiredVelocity += (Vector3.Normalize (agent.forward - agent.right) * maxSpeed) / midRightHit.distance;
		}

		if (Physics.Raycast (agent.position, (Vector3.Normalize (-agent.forward + agent.right)), out backRightHit, 1.5f)) {
			if (backRightHit.collider.gameObject.tag != "Player") 
				desiredVelocity += (agent.forward * maxSpeed) / backRightHit.distance;
		}

		//if collisions are detected, return the total of the relevent avoidance forces, if not, return the default (0,0,0) Vector3
		if (desiredVelocity != new Vector3(0.0f, 0.0f, 0.0f))
			return Vector3.ClampMagnitude ((desiredVelocity - agentRigidbody.velocity), maxAvoidanceForce); 
		else
			return desiredVelocity;
	}

	/*
	 * Shoots at the player as long as they're in LOS (Line of Sight), and plays a gunshot noise
	 */ 
	public void shoot(){
		RaycastHit LOS;
		if (Physics.Raycast(bulletSpawn.position, bulletSpawn.forward, out LOS, farShootRange)){
			if (LOS.collider.gameObject.tag == "Player") {
				Instantiate (bullet, bulletSpawn.position, bulletSpawn.rotation);
				gunshotSound.Play ();
				nextFire = Time.time + fireRate; //sets time of nextFire based on the current time, and fireRate
			}
		}
	}

	/*
	 * Damages the agent when hit by the players bullets, if it's health drops below 0 this calls the Die() method
	 * If the agent isn't already strafing (or dead) then it will start the Strafing Coroutine too
	 * additionally, this adds damage to the counter in the director, and flashes the agents material to show it's been hit
	 */ 
	public void TakeDamage (int damage){
		hitPoints -= damage;
		director.addDamage (damage);
		StartCoroutine (HitFlash ());
		if (hitPoints <= 0 && !dead)
			Die ();
		if (strafing == false) {
			StartCoroutine(startStrafe ());
		}
	}

	/*
	 * Destroys the agent, and rewards the player with points! Also adds a referece of the kill to the directors 'kills' list
	 * and totalKills variables. Additionaly, instantiates GameObjects to play a death sound, and particle affect
	 */ 
	void Die (){
		dead = true;
		Instantiate (onDeathSound);
		Instantiate (deathSplatter, transform.position, transform.rotation);
		Destroy (gameObject);
		director.AddScore (scoreReward);
		director.AddKill ();
	}

	/*
	 * Coroutine to make the agents material flash upon hit, displaying to the player that they've been hit
	 */ 
	IEnumerator HitFlash(){
		body.material = flashColour;
		lHand.material = flashColour;
		rHand.material = flashColour;
		yield return new WaitForSeconds (flashTime);
		body.material = skinColour;
		lHand.material = skinColour;
		rHand.material = skinColour;
	}
}
