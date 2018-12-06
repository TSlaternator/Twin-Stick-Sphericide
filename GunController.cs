using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class for controlling weapon mechanics for single guns 
public class GunController : MonoBehaviour {

	public Director director; //reference to my director class, allowing me to find out BPM
	public GameObject bullet; //bullet objects to be fires at enemies
	public GameObject player; //GameObject of the Player
	public Transform bulletSpawn; //transform of the bulletSpawn gameObject
	public float fireRate; //variable to set fireRate of the players weapon
	public int clipSize; //how many bullets the gun can fire before reloading
	public int ammo; //how much ammunition the gun has
	public int ammoSpentModifier; //how much to modify the ammoSpent variable in the director
	public float reloadTime; //how long the gun takes to reload
	public ParticleSystem gunParticles; //Paricle system to play when the gun fires
	public AudioSource gunshotSound; //sound played when the gun shots
	public AudioSource reloadSound; //sound played when the gun reloads
	public AudioSource emptyClipSound; //sound played when the player tries to shoot / reload without sufficient bullets

	private float nextFire; //holds the soonest time the player can fire again after firing
	private int bulletsInClip; //how many bullets are left in the clip (decrements as the player shoots)
	private bool reloading = false; //booleon to denote when the gun is being reloaded

	void Start () {
		bulletsInClip = clipSize;
		player.GetComponent<PlayerController> ().setMagUI (bulletsInClip, clipSize, ammo);
	}

	/*
	 * Allows the player to switch to the next (or previous gun), shoot the gun, or reload the gun
	 */ 
	void Update () {

		//statements to allow the player to switch guns forwards / backwards in the cycle
		if (Input.GetButtonDown("RBuffer") && Time.time > nextFire){
			player.GetComponent<PlayerController> ().switchWeaponForwards ();
		}

		if (Input.GetButtonDown("LBuffer") && Time.time > nextFire){
			player.GetComponent<PlayerController> ().switchWeaponBackwards ();
		}

		//allows the player to reload their gun if they're not in the fireRate cooldown period and have enough ammo
		if ((Input.GetAxis ("XButton") > 0) && !reloading && bulletsInClip != clipSize && ammo != 0 && Time.time > nextFire) {
			reloading = true;
			reloadSound.Play ();
			nextFire = Time.time + reloadTime;
			StartCoroutine (reload());
		} else if ((Input.GetAxis ("XButton") > 0) && !reloading && bulletsInClip != clipSize && ammo == 0 && Time.time > nextFire) {
			emptyClipSound.Play();
			nextFire = Time.time + reloadTime;
		}

		//fires a bullet if the player is holding the trigger, aren't reloading and the time since last fire > fireRate 
		if ((Input.GetAxis ("ControllerFire1") > 0) && Time.time > nextFire && bulletsInClip > 0 && !reloading) {
			nextFire = Time.time + fireRate; //sets time of nextFire based on the current time, and fireRate
			Instantiate (bullet, bulletSpawn.position, bulletSpawn.rotation);
			gunParticles.Play ();
			gunshotSound.Play ();
			director.AddBullet ();  //adds a bullet to the List in the director class (used for calculating BPM)
			director.ammoSpent += ammoSpentModifier; //adds to the ammoSpent variable in the director, allowing it to spawn more ammo
			bulletsInClip--;
			ammo--;
			UpdateUI ();
		}
		else if ((Input.GetAxis ("ControllerFire1") > 0) && Time.time > nextFire && bulletsInClip == 0 && !reloading){
			emptyClipSound.Play();
			nextFire = Time.time + fireRate;
		}
	}

	/*
	 * Reloads the gun, increasing the ammo in the clip based on clip size and total ammo count
	 */ 
	public IEnumerator reload(){
		yield return new WaitForSeconds (reloadTime);
		if (ammo > clipSize)
			bulletsInClip = clipSize;
		else
			bulletsInClip = ammo;
		player.GetComponent<PlayerController> ().setMagUI (bulletsInClip, clipSize, ammo);
		reloading = false;
	}

	/*
	 * Updates the UI Display to show the player thei bulletsInClip, ClipSize and Ammo for the gun
	 */ 
	public void UpdateUI(){
		player.GetComponent<PlayerController> ().setMagUI (bulletsInClip, clipSize, ammo);
	}

	/*
	 * Adds to the total ammo count of the gun (called when the player picks up ammo)
	 */ 
	public void addAmmo(int amount, bool update){
		ammo += amount;
		if (update)
			player.GetComponent<PlayerController> ().setMagUI (bulletsInClip, clipSize, ammo);
	}
}
