using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class to control the movement, HP, weapons and pickup interactions of the player
public class PlayerController : MonoBehaviour {

	public float moveSpeed; //multiplier to control player movespeed
	public float invulnerableLength; //how long the player is invulnerable for after being hit
	public int auraDistance; //how far auras should extend from the player
	public int buffDuration; //how long buffs should affect the player
	public int maxHitPoints; //maximum HP of the player
	public bool fearAura = false; //whether the player currently causes enemies to flee or not
	public GameObject dualPistols; //Dual Pistols weapon
	public GameObject shotgun; //Shotgun weapon
	public GameObject SMG; //SMG weapon
	public GameObject AR; //AR weapon
	public GameObject enemiesList; //list of all enemies alive (used for the feat aura)
	public GameObject director; //the director (used to update game control variables)
	public AudioSource onPickupSound; //sound to be played when an object is picked up by the player

	private float invulnerableTime; //time the player is invulnerable until after being hit
	private int currentHitPoints; //current HP of the player
	private GUIText playerHP; //UI HP element reference
	private GUIText MagSize; //UI to show mag size, ammo, and bullets in the clip of the current weapon
	private Rigidbody playerRigidbody; //allows us to apply physics forces to the player
	private Vector3 moveInput; //used to get directional movement input from keyboard / controller
	private Vector3 moveVelocity; //used to apply movement to the player
	private int currentWeapon = 0; //int to reference the current weapon
	private int totalWeapons = 1; //int to show current number of weapons
	private List<GameObject> weaponsList; //list of weapons
	private GameObject gun; //current weapon of the player

	public MeshRenderer body; //meshrenderer of the player
	public Material skinColour; //normal material of the player
	public Material invulnColour; //material to show when the player is invulerable

	/*
	 * setting up variables and references
	 */ 
	void Start () {
		playerRigidbody = GetComponent<Rigidbody> ();
		playerHP = GameObject.FindGameObjectWithTag ("HPUI").GetComponent<GUIText> ();
		MagSize = GameObject.FindGameObjectWithTag ("Mag").GetComponent<GUIText> ();
		currentHitPoints = maxHitPoints;
		weaponsList = new List<GameObject>();
		weaponsList.Add (dualPistols);
	}

	/*
	 * controls player movement and rotation
	 */ 
	void Update () {
		//gets input from controller and multiplies it by moveSpeed to get a movement velocity for the player
		moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
		moveVelocity = moveInput * moveSpeed;

		//gets input from controller to determine player facing
		Vector3 playerDirection = Vector3.right * Input.GetAxisRaw ("ControllerRightHorizontal") + Vector3.forward * -Input.GetAxisRaw ("ControllerRightVertical");
		//if there is input, set player facing
		if (playerDirection.sqrMagnitude > 0.0f) { 
			transform.rotation = Quaternion.LookRotation (playerDirection, Vector3.up);
		}			
	}

	/*
	 * moves the player
	 */ 
	void FixedUpdate (){
		//moves player based on moveVelocity
		playerRigidbody.velocity = moveVelocity;
	}
		
	/*
	 * deals damage to the player, reducing their HP. Called by enemies on attack
	 */ 
	public void TakeDamage (int damage){
		//only deals damage if the player isn't in their invulnerable frames
		if (Time.time > invulnerableTime) {
			//if they aren't invulnerable: deal damage, update HP UI and set the player to invulnerable
			StartCoroutine(InvulnerableReColour());
			currentHitPoints -= damage;
			GetHP ();
			invulnerableTime = Time.time + invulnerableLength;
			if (currentHitPoints <= 0)
				GameOver ();
		}
	}
		
	/*
	 * heals the player, healing up to their maxHP. Called when they pick up health packs
	 */ 
	public void HealDamage (int heal){
		currentHitPoints += heal;
		onPickupSound.Play ();
		//only heals up to the players max HP
		if (currentHitPoints > maxHitPoints)
			currentHitPoints = maxHitPoints;

		//updates the UI to show correct Player HP
		GetHP ();
	}
		
	/*
	 * updates the players HP UI element 
	 */ 
	public float GetHP(){
		playerHP.text = "HP: " + currentHitPoints;
		return (float) currentHitPoints;
	}

	/*
	 * switches weapons forwards one, and updates the MagSize UI element
	 */ 
	public void switchWeaponForwards(){
		weaponsList[currentWeapon].SetActive (false);
		if (currentWeapon + 1 < weaponsList.Count)
			currentWeapon++;
		else
			currentWeapon = 0;
		weaponsList [currentWeapon].SetActive (true);
		if (weaponsList [currentWeapon].Equals (dualPistols))
			weaponsList [currentWeapon].GetComponent<DualGunController> ().UpdateUI ();
		else if (weaponsList [currentWeapon].Equals (SMG) || weaponsList [currentWeapon].Equals (AR))
			weaponsList [currentWeapon].GetComponent<GunController> ().UpdateUI ();
		else if (weaponsList [currentWeapon].Equals (shotgun))
			weaponsList [currentWeapon].GetComponent<SpreadGunController> ().UpdateUI ();
	}

	/*
	 * switches weapons backwards one, and updates the MagSize UI element
	 */ 
	public void switchWeaponBackwards(){
		weaponsList[currentWeapon].SetActive (false);
		if (currentWeapon - 1 >= 0)
			currentWeapon--;
		else
			currentWeapon = weaponsList.Count - 1;
		weaponsList [currentWeapon].SetActive (true);
		if (weaponsList [currentWeapon].Equals (dualPistols))
			weaponsList [currentWeapon].GetComponent<DualGunController> ().UpdateUI ();
		else if (weaponsList [currentWeapon].Equals (SMG) || weaponsList [currentWeapon].Equals (AR))
			weaponsList [currentWeapon].GetComponent<GunController> ().UpdateUI ();
		else if (weaponsList [currentWeapon].Equals (shotgun))
			weaponsList [currentWeapon].GetComponent<SpreadGunController> ().UpdateUI ();
	}

	/*
	 * updates the MagSize UI element (called by the individual guns)
	 */ 
	public void setMagUI(int bulletsLeft, int magSize, int ammo){
		MagSize.text = "Mag: " + bulletsLeft + " / " + magSize + " (" + ammo + ")";
	}
		
	/*
	 * destroys the player, all enemies, and displays the GameOver UI
	 */ 
	void GameOver(){
		playerHP.text = "HP: Dead";
		director.GetComponent<Director> ().gameOverUI ();
		Destroy (enemiesList);
		Destroy (gameObject);
	}

	/*
	 * plays the pickup sound and starts the buff coroutine (called by the pickup)
	 */
	public void StartSpeedBuff(float buffAmount){
		onPickupSound.Play ();
		StartCoroutine (SpeedBuff (buffAmount));
	}

	/*
	 * temporarily buffs player speed
	 */
	public IEnumerator SpeedBuff(float buffAmount){
		moveSpeed += buffAmount;
		yield return new WaitForSeconds (buffDuration);
		moveSpeed -= buffAmount;
	}

	/*
	 * plays the pickup sound and starts the buff coroutine (called by the pickup)
	 */
	public void StartFearBuff(float buffDuration){
		onPickupSound.Play ();
		StartCoroutine (FearBuff (buffDuration));
	}

	/*
	 * temporarily makes nearby enemies flee
	 */
	public IEnumerator FearBuff(float buffDuration){
		fearAura = true;
		yield return new WaitForSeconds (buffDuration);
		fearAura = false;
	}

	/*
	 * plays the pickups sound and adds a new weapon to the list!
	 */
	public void PickUpWeapon(string name){
		if (name.Equals ("Shotgun")) {
			weaponsList.Add(shotgun);
			totalWeapons++;
		} else if (name.Equals ("SMG")) {
			weaponsList.Add(SMG);
			totalWeapons++;
		} else if (name.Equals ("AR")) {
			weaponsList.Add(AR);
			totalWeapons++;
		}
		onPickupSound.Play ();
	}

	/*
	 * plays the pickup sound and adds ammo to the gun specified by the pickup
	 */
	public void PickUpAmmo(string type, int amount){
		if (type.Equals ("Shotgun")) {
			if (weaponsList [currentWeapon].Equals (shotgun))
				shotgun.GetComponent<SpreadGunController> ().addAmmo (amount, true);
			else
				shotgun.GetComponent<SpreadGunController> ().addAmmo (amount, false);
		} else if (type.Equals ("SMG")) {
			if (weaponsList [currentWeapon].Equals (SMG))
				SMG.GetComponent<GunController> ().addAmmo (amount, true);
			else
				SMG.GetComponent<GunController> ().addAmmo (amount, false);
		} else if (type.Equals ("AR")) {
			if (weaponsList [currentWeapon].Equals (AR))
				AR.GetComponent<GunController> ().addAmmo (amount, true);
			else
				AR.GetComponent<GunController> ().addAmmo (amount, false);
		}
		onPickupSound.Play ();
	}

	/*
	 * temporarily changes player model colour to indicate invulnerability
	 */
	IEnumerator InvulnerableReColour(){
		body.material = invulnColour;
		yield return new WaitForSeconds (invulnerableLength);
		body.material = skinColour;
	}
}