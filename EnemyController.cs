using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling enemy AI movement and interactions
public class EnemyController : MonoBehaviour {

	//non behaviour specific variables
	public float hitPoints; //HP of the agent
	public int scoreReward; //score rewarded to the player for killing this Agent
	public float maxSpeed; //fastest speed the agent can move at
	public float maxForce; //limits the potential total steering force, lower values mean slower turnrate
	public float maxBehaviourForce; //limits the potential behaviour forces magnitude (seek, pursue, wander)
	public float maxAvoidanceForce; //limits the maximum collision avoidance forces magnitude (forward and side)
	public float detectionRange; //the range at which the agent will 'detect' its target
	public float closeRange; //the range at which the agent will switch from pursuing to seeking its target
	public Transform agent; //transform of the agent
	public GameObject onDeathSound; //GameObject which plays a sound upon this agents death
	public GameObject deathSplatter; //GameObject which plays a particle affect upon this agents death

	private Rigidbody agentRigidbody; //reference to the agents Rigidbody component
	private Rigidbody targetRigidbody; //reference to the targets rigidbody
	private GameObject target; //gameobject of the target (the player)
	private Vector3 desiredVelocity; //velocity to take the agent directly towards the target
	private Vector3 steer; //steering force to slowly turn the agent towards it's desiredVelocity
	private Director director; //reference to my director class, allowing me to find out KPM
	private List<int>[] nodes; //list of int arrays to hold the nodes
	private float targetDistance; //distance to the target
	private float allInThreshold; //20% of the agents starting HP
	private bool allIn = false; //when true, agent will be stuck in seek mode
	private bool inFear = false; //when true, agent will flee its target
	private int mapWidth; //width of the map (and height since it's square) 
	private bool dead = false; //controls when the agent is dead or not

	//variables for pursue behaviour
	public float lead; //amount of seconds ahead to predict targets position
	private Vector3 targetPrediction; //prediction of the targets location in 'lead' seconds

	//variables for wandering behaviour
	public float circleDistance; //distance from the agent to the centre of the abstract circle
	public float circleRadius; //radius of the abstract circle
	public float wanderAngle; //maximum angle change per frame
	private float angle; //angle of the displacement vector inside the circle

	//variables for collision avoidance
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
		mapWidth = director.mapWidth;
		nodes = director.nodes; 
		target = GameObject.FindGameObjectWithTag ("Player");
		targetRigidbody = target.GetComponent<Rigidbody> ();
		allInThreshold = hitPoints / 5f;
	}

	/*
	 * controls the FSM of the agent, and it's rotation, each frame
	 */ 
	void Update () {
		//finding absolute distance to the target
		targetDistance = Mathf.Abs(Vector3.Magnitude(target.transform.position - agent.position));

		//sets the agent to fear the player if they're nearby with the fear aura buff
		if (targetDistance < target.gameObject.GetComponent<PlayerController> ().auraDistance && target.gameObject.GetComponent<PlayerController> ().fearAura == true && inFear == false)
			StartCoroutine (Fear (target.gameObject.GetComponent<PlayerController> ().buffDuration));
		
		//finite state machine control statements
		if (allIn) {
			steer = getSeekForce();
		
		} else if (inFear) {
			steer = getFleeForce();

		} else if (targetDistance > detectionRange) {
			steer = getWanderForce();
		
		} else if (targetDistance <= closeRange) {
			if (hitPoints <= allInThreshold) {
				allIn = true;
				maxSpeed *= 1.5f;
			}
			steer = getSeekForce();

		} else if (targetDistance <= detectionRange) {
			steer = getSearchForce();
		}

		//if the agent is moving, slerp it's rotation to smooth the turning
		if (agentRigidbody.velocity.magnitude > 0.1) {
			transform.rotation = Quaternion.Slerp (transform.rotation, Quaternion.LookRotation (agentRigidbody.velocity), Time.deltaTime * maxSpeed * 2.0f);
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
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + getForwardAvoidanceForce() + getSidewaysAvoidanceForce(), maxForce);  
	}

	/*
	 * returns the Flee force for this agent
	 */ 
	Vector3 getFleeForce(){
		
		//sets desiredVelocity to a vector of length maxSpeed in the opposite direction than the target
		desiredVelocity = (new Vector3(agent.position.x, 0.0f, agent.position.z) - new Vector3 (target.transform.position.x, 0.0f, target.transform.position.z)); 
		desiredVelocity.Normalize();
		desiredVelocity *= maxSpeed; 

		//returns a Vector in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + getForwardAvoidanceForce() + getSidewaysAvoidanceForce(), maxForce);  
	}

	/*
	 * returns the Pursue force for this agent
	 */ 
	Vector3 getPursueForce(){
		
		//predicting the targets location
		targetPrediction = (new Vector3 (target.transform.position.x, 0.0f, target.transform.position.z) + new Vector3 (targetRigidbody.velocity.x, 0.0f, targetRigidbody.velocity.z) * lead * targetDistance); 

		//sets desiredVelocity to a vector of length maxSpeed in the direction of the targets predicted location
		desiredVelocity = (targetPrediction - new Vector3(agent.position.x, transform.position.y, agent.position.z)); 
		desiredVelocity.Normalize(); 
		desiredVelocity *= maxSpeed; 

		//sets steer to a Vector of length up to maxForce, in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce), maxForce); 
	}

	/*
	 * returns the 'Seek with Pathfinding' force for this agent
	 */ 
	Vector3 getSearchForce(){

		//uses my Closest First Search to determine which way to move next to reach the player
		Vector3 routeToTarget = (ClosestFirstSearch(transform.position, target.transform.position));

		//sets desiredVelocity to a vector of length maxSpeed in the direction of the first node in the path towards the player
		desiredVelocity = (routeToTarget - new Vector3(agent.position.x, transform.position.y, agent.position.z)); 
		desiredVelocity.Normalize(); 
		desiredVelocity *= maxSpeed; 

		//returns a Vector in the direction from the agents current velocity to the desired velocity
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + getForwardAvoidanceForce() *0.25f + getSidewaysAvoidanceForce() * 0.25f, maxForce); 
	}

	/*
	 * returns the wander force for this agent
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
		return Vector3.ClampMagnitude (Vector3.ClampMagnitude((desiredVelocity - agentRigidbody.velocity), maxBehaviourForce) + getForwardAvoidanceForce() + getSidewaysAvoidanceForce(), maxForce);  
	}
		
	/*
	 * returns the ForwardAvoidanceForce for this agent, turning it away from obstacles infront of it
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
	 * returns the sidewaysAvoidanceForce for this agent, allwing it to move around walls or avoid obstacles to its sides
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
	 * Closest First Search Pathfinding Algorithm
	 * Searches through a node graph to find a path to the target using 'closest' node priority for node exploration
	 */ 
	Vector3 ClosestFirstSearch(Vector3 start, Vector3 goal){
		int startX = Mathf.RoundToInt (start.x); //x location of the agent
		int startZ = Mathf.RoundToInt (start.z - 0.5f); //z location of the agent
		int goalX = Mathf.RoundToInt (goal.x); //x location of the player
		int goalZ = Mathf.RoundToInt (goal.z - 0.5f); //z location of the player
		int currentNode = startX * mapWidth + startZ; //turning the node into a form suitable for an array
		bool pathFound = false; //when this is true, the path has been found! No need to search anymore

		Dictionary<int, List<int>> frontier = new Dictionary<int, List<int>> (); //dictionary to hold nodes yet to explore
		Dictionary<int, List<int>> explored = new Dictionary<int, List<int>> (); //dictionary to hold nodes already explored

		//adds the first node to the frontier lsit, along with it's distance to the goal and a special value to differentiate it as the start node
		frontier.Add (currentNode, new List<int> ());
		frontier [currentNode].Add (GetDistance(currentNode, goalX, goalZ));
		frontier [currentNode].Add (-1);

		//until a path has been found, keep exploring nodes in the frontier list
		while (!pathFound) {
			currentNode = -1;
			//chooses which node to explore next based on proximity to the goal node
			foreach (KeyValuePair<int, List<int>> item in frontier) {
				if (currentNode == -1 || item.Value [0] < frontier [currentNode][0])
					currentNode = item.Key;
			}

			//adds the node to be explored to the explored dictionary, and removes it from the frontier dictionary
			explored.Add (currentNode, new List<int> ());
			explored [currentNode].Add (frontier [currentNode] [0]);
			explored [currentNode].Add (frontier [currentNode] [1]);
			frontier.Remove (currentNode);

			//checks to see if the node is the goal node or not
			if (currentNode == goalX * mapWidth + goalZ)
				pathFound = true;
			
			//if not, adds all connected nodes (that haven't already been discovered) to the frontier dictionary
			else {
				for (int i = 0; i < nodes[currentNode].Count; i++){
					if (!explored.ContainsKey(nodes[currentNode][i]) && !frontier.ContainsKey(nodes[currentNode][i])){
						frontier.Add(nodes[currentNode][i], new List<int> ()); //adds the node
						frontier [nodes[currentNode][i]].Add (GetDistance (nodes [currentNode] [i], goalX, goalZ)); //adds it's distance to the goal
						frontier [nodes[currentNode][i]].Add (currentNode); //adds the current node as the connection node
					}
				}
			}
		}

		//when a path has been found, we need to go back through it to find the first node
		List<int> path = new List<int> ();
		while (explored[currentNode][1] != startX * mapWidth + startZ) {
			path.Add (currentNode);
			currentNode = explored [currentNode] [1];
		}

		//returns the vector3 position of the first node in the path towards the target
		return new Vector3 (((currentNode - (currentNode % mapWidth)) / mapWidth), transform.position.y, currentNode % mapWidth + 0.5f);
	}

	/*
	 * Gets (and returns) the manhattan distance between a node, and the goal node
	 */ 
	int GetDistance(int node, int goalX, int goalZ){
		int startZ = node % mapWidth;
		int startX = (node - startZ) / mapWidth;

		return (Mathf.Abs (goalX - startX) + Mathf.Abs (goalZ - startZ));
	}
		
	/*
	 * Damages the agent when hit by the players bullets, if it's health drops below 0 this calls the Die() method
	 * additionally, this adds damage to the counter in the director, and flashes the agents material to show it's been hit
	 */ 
	public void TakeDamage (int damage){
		hitPoints -= damage;
		director.addDamage (damage);
		StartCoroutine (HitFlash ());
		if (hitPoints <= 0 && !dead) 
			Die ();
	}

	/*
	 * Damages the agent when hit by the players bullets, if it's health drops below 0 this calls the Die() method
	 * additionally, this adds damage to the counter in the director, and flashes the agents material to show it's been hit
	 * The beserker version also buffs the move speed, and damage of the Beserker, making it strong with each hit!
	 */ 
	public void BeserkerTakeDamage(int damage){
		hitPoints -= damage;
		StartCoroutine (HitFlash ());
		if (hitPoints <= 0 && !dead)
			Die ();
		maxSpeed += 1;
		GetComponentInChildren<EnemyAttack> ().damage += 2;
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
	 * Coroutine to render the agent in fear if it comes near the player whilst they are buffed with a fear Aura
	 */ 
	IEnumerator Fear(int duration){
		inFear = true;
		yield return new WaitForSeconds (duration);
		inFear = false;
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
