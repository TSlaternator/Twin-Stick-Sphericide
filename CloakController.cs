using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class to allow the agent to temporarily turn invisible!
public class CloakController : MonoBehaviour {

	public int visibleTime; //how long the agent will be visible for each cycle
	public int invisibleTime; //how long the agent will be invisible for each cycle
	public AudioSource cloakSound; //Sound Clip to play upon turning invisible / visible

	public MeshRenderer[] meshes; //Array to hold the meshes of every GameObject that makes up the AI (so we can turn them invisble)
	public GameObject smokeCloud; //GameObject which plays a smoke cloud particle affect where / when the agent turns invisible

	/*
	 * Starts the invis / visible cycle when the agent is instantiated
	 */
	void Start(){		
		meshes = GetComponentsInChildren<MeshRenderer> ();
		StartCoroutine (turnVisible());
	}
		
	/*
	 * Temporarily turns the agent visible
	 */
	private IEnumerator turnVisible(){
		//enables all the agents meshes, turning it visible
		for (int i = 0; i < meshes.Length; i++)
			meshes [i].enabled = true;

		cloakSound.Play ();

		yield return new WaitForSeconds (visibleTime);

		StartCoroutine (turnInvisible());
	}

	/*
	 * Temporarily turns the agent invisible
	 */
	private IEnumerator turnInvisible(){
		//disables all the agents meshes, turning it invisible
		for (int i = 0; i < meshes.Length; i++)
			meshes [i].enabled = false;

		Instantiate (smokeCloud, transform.position, transform.rotation);
		cloakSound.Play ();

		yield return new WaitForSeconds (invisibleTime);

		StartCoroutine (turnVisible());
	}
}
