using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Director class to control my Game
public class Director : MonoBehaviour {

	public GUIText BPMText; //displays BPM
	public GUIText KPMText; //displays KPM
	public GUIText enemyCountText; //displays enemy Count
	public GUIText avgEnemyDistanceText; //displays enemy Average Distance to Player
	public GUIText timeAliveText; //displays Player's Time Alive
	public GUIText playerScoreText; //displays Player's Score
	public GUIText skillText; //displays Player's Skill
	public GUIText stressText; //displays Player's Stress
	public GUIText spawnFactorText; //displays the current spawnFactor
	public GUIText phaseText; //displays the current Phase
	public GUIText spawnRateText; //displays the current SpawnRate of enemies
	public GameObject enemies; //empty GameObject to parent all enemy GameObjects
	public GameObject player; //Player GameObject
	public GameObject mapGen; //mapGen empty GameObject, used to reference MapGeneration Script
	public int spawnDistance; //Manhattan distance to spawn enemies at (should be outside Player view)
	public int pickupSpawnDistance; //Manhattan distance to spawn pickups at (should be inside Player view)
	public float difficulty; //difficulty multiplier of the game (around 1 should be a good difficulty)

	public EnemyType [] enemyTypes; //array of enemyType structs to hold my varies enemy gameobjects
	public List<int> [] nodes; //array of lists to act as my graph

	private List<float> bullets; //List to hold number of bullets fired by the player in the last minute
	private List<float> kills; //list to hold number of enemies killed by the player in the last minute
	private bool[,] spawnable; //array to denote which map tiles are acceptable spawn locations
	private float[,] spawnHeight; //array to hold the height of each tile, for spawning purposes
	private float spawnRate = 2.5f; //spawns one enemy per 'spawnRate' seconds
	private float nextSpawn = 10; //holds the next time an enemy will be spawned
	public int mapWidth; //width of my map (used to declare 'nodes' size)
	public int mapHeight; //height of my map (used to declare 'nodes' size)
	private int totalKills; //total kills the player has achieved
	private int totalDamage; //total damage the player has dealt

	private int BPM; //bullets fired by the player in the last minute
	private int KPM; //enemies killed by the player in the last minute
	private float KPB; //KPM / BPM
	private int avgEnemyDistance; //average enemy distance to player
	private int timeAlive; //how long the player has survived for
	private float playerHP; //HP of the player
	private int enemyCount; //how many enemies are currently active
	private float skill; //skill of the player
	private float stress; //stress of the player
	private float spawnFactor; //spawnFactor of the enemies
	private float phase; //current phase of the game
	private int score = 0; //score of the player

	private List<GameObject> guns; //weapons the player can pick up
	public GameObject shotgunPickup; //shotgun weapon
	public GameObject SMGPickup; //SMG weapon
	public GameObject ARPickup; //AR weapon
	private int gunsToSpawn = 3; //how many guns to spawn
	public int firstGunThreshold; //how many kills before the first weapon spawns
	public int secondGunThreshold; //how many kills before the second weapon spawns
	public int thirdGunThreshold; //how many kills before the third weapon spawns

	private List<GameObject> ammo; //ammo types the player can pick up
	public GameObject shotgunAmmo; //ammo for the shotgun
	public GameObject SMGAmmo; //ammo for the SMG
	public GameObject ARAmmo; //ammo for the AR
	public int ammoSpent; //incremented each time the player shoots, when > threshold, ammo will be spawned
	public int ammoThreshold; //threshold at which to spawn ammo

	private List<GameObject> pickups; //pickups the player can pick up
	public GameObject healPickup; //heal pickup (heals the player)
	public GameObject fearPickup; //fear pickup (temporarily causes enemies to flee)
	public GameObject speedPickup; //speed pickup (temporarily increases player speed)
	private int pickupScore; //incremented over time (based on stress/skill), when > threshold, a (random) pickup will be spawned
	public int pickupThreshold; //threshold at which to spawn a pickup

	public GameObject gameOverText; //says 'GAME OVER' when the player dies
	public GameObject gameOverScore; //displays the score when the player dies
	public GameObject gameOverKills; //displays total kills when the player dies
	public GameObject gameOverDamage; //displays damage dealt when the player dies
	public GameObject gameOverTime; //displays time alive when the player dies

	/*
	 * Sets up variables, generates the map and randomizes the player spawn
	 */ 
	void Start () {
		mapHeight = mapGen.GetComponent<MapGeneration> ().mapHeight;
		mapWidth = mapGen.GetComponent<MapGeneration> ().mapWidth;
		spawnable = new bool[mapWidth, mapHeight];
		spawnHeight = new float[mapWidth, mapHeight];
		nodes = new List<int>[mapHeight * mapWidth];
		bullets = new List<float> ();
		kills = new List<float> ();
		guns = new List<GameObject> ();
		ammo = new List<GameObject> ();
		pickups = new List<GameObject> ();

		//defaults every tile to be spawnable, until stated otherwise
		for (int x = 0; x < spawnable.GetLength(0); x++){
			for (int y = 0; y < spawnable.GetLength (1); y++) {
				spawnable [x, y] = true;
			}
		}

		//procedurally generates my map! (This will also modify 'spawnable' to hold 'false' values for water or tree tiles)
		mapGen.GetComponent<MapGeneration> ().GenerateMap();

		//creates a graph representation of my map! (used for Closest First Search)
		FillNodeGraph ();

		//selects a random 'spawnable' location to position player at game start
		while (true) {
			int startX = Random.Range (0, mapWidth);
			int startZ = Random.Range (0, mapHeight);
			if (spawnable [startX, startZ] == true) {
				player.transform.position = new Vector3(startX, spawnHeight[startX, startZ], startZ + 0.5f);
				break;
			}
		}

		//adding the GameObjects to the pickup lists
		guns.Add (ARPickup);
		guns.Add (SMGPickup);
		guns.Add (shotgunPickup);

		ammo.Add (ARAmmo);
		ammo.Add (SMGAmmo);
		ammo.Add (shotgunAmmo);

		pickups.Add (healPickup);
		pickups.Add (speedPickup);
		pickups.Add (fearPickup);

		//starts the pickup spawning system
		StartCoroutine (PickupScoreMod());
	}
		
	/*
	 * continuously updates spawn conditions, and spawns enemies and pickups based on these
	 */ 
	void Update () {
		//removing bullets / kills that happened over 60 seconds ago
		RemoveBullets (); 
		RemoveKills ();
		//updating UI elements and variables
		GetBPM ();
		GetKPM ();
		GetCount ();
		GetAvgDistance ();
		GetTimeAlive ();
		GetSpawnFactor ();
		GetPhase ();
		GetSpawnRate ();
		//spawning enemies (if conditions are met)
		if (Time.time > nextSpawn && enemyCount < (timeAlive / 2)) {
			nextSpawn = Time.time + spawnRate;
			SpawnEnemy ();
		} 
		//spawning guns (if conditions are met)
		if (gunsToSpawn == 3 && totalKills >= firstGunThreshold)
			SpawnGun ();
		if (gunsToSpawn == 2 && totalKills >= secondGunThreshold)
			SpawnGun ();
		if (gunsToSpawn == 1 && totalKills >= thirdGunThreshold)
			SpawnGun ();
		//spawning ammo (if conditions are met)
		if (ammoSpent >= ammoThreshold)
			SpawnAmmo ();
		//spawning pickups (if conditions are met)
		if (pickupScore >= pickupThreshold)
			SpawnPickup ();
	} 
		
	/*
	 * adds the time a bullet was fired to 'bullets' List
	 */ 
	public void AddBullet(){
		bullets.Add (Time.time);
	}
		
	/*
	 * removes any bullets fired over 60 seconds ago
	 */ 
	void RemoveBullets(){
		while (bullets.Count > 0) {
			if (bullets [0] + 60 < Time.time) {
				bullets.RemoveAt(0);
			} else {
				break;
			}
		}
	}
		
	/*
	 * updates the BPM UI Text to the number of items in the 'bullets' List
	 */ 
	void GetBPM (){
		BPM = bullets.Count;
		BPMText.text = "BPM: " + BPM;
	}
		
	/*
	 * adds the time of a kill to the 'kills' List
	 */ 
	public void AddKill(){
		kills.Add (Time.time);
		totalKills++;
	}
		
	/*
	 * removes any kills achieved over 60 seconds ago
	 */ 
	void RemoveKills (){
		while (kills.Count > 0) {
			if (kills [0] + 60 < Time.time) {
				kills.RemoveAt(0);
			} else {
				break;
			}
		}
	}
		
	/*
	 * updates the KPM UI Text to number of items in the 'kills' List
	 */ 
	void GetKPM (){
		KPM = kills.Count;
		KPMText.text = "KPM: " + KPM;
	}
		
	/*
	 * updates the Count UI Text to number of child objects in 'enemies'
	 */ 
	void GetCount (){
		enemyCount = enemies.transform.childCount;
		enemyCountText.text = "Enemy Count: " + enemyCount;
	}
		
	/*
	 * Gets the average distance of every object in 'enemies' from the Player, then updates the UI element
	 */ 
	void GetAvgDistance (){
		//only work out distance if an enemy exists!
		if (enemies.transform.childCount > 0) {
			float totalDistance = 0f; //total distance of all enemies
			float avgDistance; //average distance of all enemies

			//adding each enemies Manhattan Distance (rounded to nearest Int) to totalDistance
			for (int i = 0; i < enemies.transform.childCount; i++) {
				totalDistance += Mathf.Abs (enemies.transform.GetChild (i).transform.position.x - player.transform.position.x);
				totalDistance += Mathf.Abs (enemies.transform.GetChild (i).transform.position.z - player.transform.position.z);
			}
			//calculating the average distance, and rounding it to nearest Int
			avgDistance = totalDistance / enemies.transform.childCount;
			avgEnemyDistance = Mathf.RoundToInt(avgDistance);
			avgEnemyDistanceText.text = "Avg Enemy Distance: " + avgEnemyDistance;
		} else{
			//default value for when no enemies exist
			avgEnemyDistanceText.text = "Avg Enemy Distance: -"; 
			avgEnemyDistance = 20;
		}
		
	}
		
	/*
	 * Updates the timeAlive UI to Time.time
	 */ 
	void GetTimeAlive (){
		timeAlive = Mathf.RoundToInt (Time.time);
		timeAliveText.text = "Time Alive: " + timeAlive;
	}
		
	/*
	 * Adds to the player Score, called by enemies on death by player
	 */ 
	public void AddScore(int points) {
		score += points;
		playerScoreText.text = "Score: " + score;
	}
		
	/*
	 * sets a location in 'spawnable' to false, called by the MapGerneration class when a water tile/tree is created
	 */ 
	public void NotSpawnable (int x, int y){
		spawnable [x, y] = false;
	}
		
	/*
	 * Get method to return if a tile is spawnable or not, used for my node graph
	 */ 
	public bool GetSpawnable (int x, int y){
		return spawnable [x, y];
	}
		
	/*
	 * sets the height of each tile in the map, called by the MapGeneration class
	 */ 
	public void SetSpawnHeight (int x, int y, float height){
		spawnHeight [x, y] = height;
	}
		
	/*
	 * spawns an enemy at a random location spawnDistance units from the player
	 */ 
	void SpawnEnemy (){
		bool spawnComplete = false; //sets to true once a spawnable position has been found
		int playerX; //x position of the player
		int playerZ; //z position of the player
		int spawnX; //x offset of the spawnlocation (from the player)
		int spawnZ; //z offset of the spawnlocatin (from the player)

		//rounding positions to Ints to allow spawning in the centre of each tile
		playerX = Mathf.RoundToInt (player.transform.position.x);
		playerZ = Mathf.RoundToInt (player.transform.position.z);

		float enemyChance = Random.Range (0f, 1f);
		for (int i = 0; i < enemyTypes.Length; i++) {
			if (enemyChance <= enemyTypes [i].chance) {
				//continue generating random locations until a spawnable location is found, then spawn the enemy
				while (spawnComplete == false) {
					//generates a random X point up from -spawnDistance to spawnDistance, then sets the Z point so total absolute distance = spawnDistance
					spawnX = Random.Range (-spawnDistance, spawnDistance + 1);
					spawnZ = spawnDistance - Mathf.Abs (spawnX);
					if (Random.Range (0, 2) == 0)
						spawnZ *= -1;
					spawnX += playerX;
					spawnZ += playerZ;
					//checks if the generated position is spawnable, if so, spawns the enemy
					if (spawnX > 0 && spawnX < mapWidth && spawnZ > 0 && spawnZ < mapHeight) {
						if (spawnable [spawnX, spawnZ] == true) {
							Instantiate (enemyTypes [i].type, new Vector3 (spawnX, spawnHeight [spawnX, spawnZ], spawnZ + 0.5f), Quaternion.identity, enemies.transform);
							spawnComplete = true;
						}
					}
				}
				break;
			}
		}
	}

	/*
	 * spawns a gun at a random location pickupSpawnDistance units from the player
	 */ 
	void SpawnGun (){
		bool spawnComplete = false; //sets to true once a spawnable position has been found
		int playerX; //x position of the player
		int playerZ; //z position of the player
		int spawnX; //x offset of the spawnlocation (from the player)
		int spawnZ; //z offset of the spawnlocatin (from the player)

		//rounding positions to Ints to allow spawning in the centre of each tile
		playerX = Mathf.RoundToInt (player.transform.position.x);
		playerZ = Mathf.RoundToInt (player.transform.position.z);

		int gunChoice = Random.Range (0, guns.Count);

		//continue generating random locations until a spawnable location is found, then spawn the gun
		while (spawnComplete == false) {
			//generates a random X point up from -spawnDistance to spawnDistance, then sets the Z point so total absolute distance = spawnDistance
			spawnX = Random.Range (-pickupSpawnDistance, pickupSpawnDistance + 1);
			spawnZ = pickupSpawnDistance - Mathf.Abs (spawnX);
			if (Random.Range (0, 2) == 0) 
				spawnZ *= -1;
			spawnX += playerX;
			spawnZ += playerZ;
			//checks if the generated position is spawnable, if so, spawns the gun
			if (spawnX > 0 && spawnX < mapWidth && spawnZ > 0 && spawnZ < mapHeight) {
				if (spawnable [spawnX, spawnZ] == true) {
					Instantiate (guns [gunChoice], new Vector3 (spawnX, spawnHeight [spawnX, spawnZ], spawnZ + 0.5f), Quaternion.identity);
					gunsToSpawn--;
					guns.Remove (guns[gunChoice]);
					spawnComplete = true;
				}
			}
		}
	}

	/*
	 * spawns ammo at a random location pickupSpawnDistance units from the player
	 */ 
	void SpawnAmmo (){
		bool spawnComplete = false; //sets to true once a spawnable position has been found
		int playerX; //x position of the player
		int playerZ; //z position of the player
		int spawnX; //x offset of the spawnlocation (from the player)
		int spawnZ; //z offset of the spawnlocatin (from the player)

		//rounding positions to Ints to allow spawning in the centre of each tile
		playerX = Mathf.RoundToInt (player.transform.position.x);
		playerZ = Mathf.RoundToInt (player.transform.position.z);

		int ammoChoice = Random.Range (0, ammo.Count);

		//continue generating random locations until a spawnable location is found, then spawn the ammo
		while (spawnComplete == false) {
			//generates a random X point up from -spawnDistance to spawnDistance, then sets the Z point so total absolute distance = spawnDistance
			spawnX = Random.Range (-pickupSpawnDistance, pickupSpawnDistance + 1);
			spawnZ = pickupSpawnDistance - Mathf.Abs (spawnX);
			if (Random.Range (0, 2) == 0)
				spawnZ *= -1;
			spawnX += playerX;
			spawnZ += playerZ;
			//checks if the generated position is spawnable, if so, spawns the ammo
			if (spawnX > 0 && spawnX < mapWidth && spawnZ > 0 && spawnZ < mapHeight) {
				if (spawnable [spawnX, spawnZ] == true) {
					Instantiate (ammo [ammoChoice], new Vector3 (spawnX, spawnHeight [spawnX, spawnZ], spawnZ + 0.5f), Quaternion.identity);
					ammoSpent = 0;
					spawnComplete = true;
				}
			}
		}
	}

	/*
	 * spawns a pickup at a random location pickupSpawnDistance units from the player
	 */ 
	void SpawnPickup (){
		bool spawnComplete = false; //sets to true once a spawnable position has been found
		int playerX; //x position of the player
		int playerZ; //z position of the player
		int spawnX; //x offset of the spawnlocation (from the player)
		int spawnZ; //z offset of the spawnlocatin (from the player)

		//rounding positions to Ints to allow spawning in the centre of each tile
		playerX = Mathf.RoundToInt (player.transform.position.x);
		playerZ = Mathf.RoundToInt (player.transform.position.z);

		int pickupChoice = Random.Range (0, pickups.Count);

		//continue generating random locations until a spawnable location is found, then spawn the pickup
		while (spawnComplete == false) {
			//generates a random X point up from -spawnDistance to spawnDistance, then sets the Z point so total absolute distance = spawnDistance
			spawnX = Random.Range (-pickupSpawnDistance, pickupSpawnDistance + 1);
			spawnZ = pickupSpawnDistance - Mathf.Abs (spawnX);
			if (Random.Range (0, 2) == 0)
				spawnZ *= -1;
			spawnX += playerX;
			spawnZ += playerZ;
			//checks if the generated position is spawnable, if so, spawns the pickup
			if (spawnX > 0 && spawnX < mapWidth && spawnZ > 0 && spawnZ < mapHeight) {
				if (spawnable [spawnX, spawnZ] == true) {
					Instantiate (pickups [pickupChoice], new Vector3 (spawnX, spawnHeight [spawnX, spawnZ], spawnZ + 0.5f), Quaternion.identity);
					pickupScore = 0;
					spawnComplete = true;
				}
			}
		}
	}
		
	/*
	 * runs a formula to find the 'skill' of the player, then updates the UI element
	 */ 
	float GetSkill(){
		if (BPM > 0)
			KPB = (float) KPM / (float) BPM;
		skill = Mathf.Clamp(score * 0.02f, 0, 40) + Mathf.Clamp(timeAlive * 0.03f, 0, 30) + Mathf.Clamp(KPB * 100, 0, 20);
		skillText.text = "Skill: " + skill;
		return skill;
	}

	/*
	 * runs a formula to find the 'stress' of the player, then updates the UI element
	 */ 
	float GetStress(){
		playerHP = player.GetComponent<PlayerController> ().GetHP ();
		stress = Mathf.Clamp(KPM * 0.25f , 0, 10) + Mathf.Clamp(BPM * 0.06f, 0, 10) + Mathf.Clamp(1 / avgEnemyDistance * 15, 0, 10) + Mathf.Clamp((1 - playerHP / 100) * 20, 0, 20) + Mathf.Clamp(enemyCount * 0.02f, 0, 50);
		stressText.text = "Stress: " + stress;
		return stress;
	}

	/*
	 * gets the SpawnFactor (skill / stress) and updates the UI element
	 */ 
	void GetSpawnFactor(){
		spawnFactor = GetSkill () / GetStress ();
		spawnFactorText.text = "Spawn Factor: " + spawnFactor;
	}

	/*
	 * Gets the Phase using a formula and updates the UI element
	 */ 
	void GetPhase(){
		int timeRemainder = (timeAlive + 180) % 180;
		phase = (Mathf.Pow (timeRemainder, 2) / (Mathf.Pow (timeRemainder, 2) + Mathf.Pow ((180 - timeRemainder), 2))) * 5;
		phaseText.text = "Phase: " + phase + ", Remainder: " + timeRemainder;
	}

	/*
	 * gets the spawnRate and updates the UI element
	 */ 
	void GetSpawnRate(){
		if (timeAlive < 30 && timeAlive > 10) spawnRate = 30 / timeAlive;
		else spawnRate = (6 - phase) / (((skill / stress) + 1) * difficulty);
		spawnRateText.text = "SpawnRate: " + spawnRate;
	}

	/*
	 * fills the node graph with connection nodes
	 */ 
	void FillNodeGraph(){
		for (int i = 0; i < (mapWidth * mapHeight); i++) {
			int y = i % mapWidth;
			int x = (i - y) / mapWidth;
			//if the current tile is a spawnable tile
			if (GetSpawnable (x, y)) {
				//resetting directional bools to false (non-spawnable)
				bool north = false; 
				bool east = false;
				bool south = false;
				bool west = false;
				nodes [i] = new List<int> ();
				//checking if the node to the north is spawnable
				if (GetSpawnable (x, y + 1)) {
					nodes [i].Add (x * mapWidth + y + 1);
					north = true;
				}
				//checking if the node to the east is spawnable
				if (GetSpawnable (x + 1, y)) {
					nodes [i].Add ((x + 1) * mapWidth + y);
					east = true;
				}
				//checking if the node to the south is spawnable
				if (GetSpawnable (x, y - 1)) {
					nodes [i].Add (x * mapWidth + y - 1);
					south = true;
				}
				//checking if the node to the west is spawnable
				if (GetSpawnable (x - 1, y)) {
					nodes [i].Add ((x - 1) * mapWidth + y);
					west = true;
				}
				//if the diagonal isn't blocked, checking if the node to the north-east is spawnable
				if (north == true && east == true) {
					if (GetSpawnable (x + 1, y + 1)){
						nodes [i].Add ((x + 1) * mapWidth + y + 1);
					}
				}
				//if the diagonal isn't blocked, checking if the node to the south-east is spawnable
				if (south == true && east == true) {
					if (GetSpawnable (x + 1, y - 1)){
						nodes [i].Add ((x + 1) * mapWidth + y - 1);
					}
				}
				//if the diagonal isn't blocked, checking if the node to the south-west is spawnable
				if (south == true && west == true) {
					if (GetSpawnable (x - 1, y - 1)){
						nodes [i].Add ((x - 1) * mapWidth + y - 1);
					}
				}
				//if the diagonal isn't blocked, checking if the node to the north-west is spawnable
				if (north == true && west == true) {
					if (GetSpawnable (x - 1, y + 1)){
						nodes [i].Add ((x - 1) * mapWidth + y + 1);
					}
				}

			}
		}
	}

	/*
	 * Revursive Coroutine to control spawning of pickups, based on time and skill / stress
	 */ 
	IEnumerator PickupScoreMod(){
		if (stress >= skill)
			pickupScore += Mathf.RoundToInt (stress);
		else
			pickupScore += Mathf.RoundToInt (skill);

		yield return new WaitForSeconds (1);
		StartCoroutine (PickupScoreMod());
	}

	/*
	 * adds damage to the totalDamage variable (called by my enemies when they take damage)
	 */ 
	public void addDamage(int damage){
		totalDamage += damage;
	}

	/*
	 * updates (and activates) all the UI elements for the GameOver display
	 */ 
	public void gameOverUI(){
		gameOverText.SetActive (true);
		gameOverScore.SetActive (true);
		gameOverScore.GetComponent<GUIText> ().text = "Final Score : " + score + " Points";
		gameOverKills.SetActive (true);
		gameOverKills.GetComponent<GUIText> ().text = "Total Kills : " + totalKills + " Kills";
		gameOverDamage.SetActive (true);
		gameOverDamage.GetComponent<GUIText> ().text = "Total Damage : " + totalDamage + " Damage";
		gameOverTime.SetActive (true);
		gameOverTime.GetComponent<GUIText> ().text = "Time Survived : " + timeAlive + " Seconds";

	}
}

/*
* Struct to hold my enemy types, and their spawn chance
*/ 
[System.Serializable]
public struct EnemyType{
	public string name; //name of the enemy to be spawned
	public float chance; //minimum random value (from 0 to 1) this enemy will start spawning at
	public GameObject type; //prefab of the enemy to be spawned
}
