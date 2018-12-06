using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class to create falloff noise to ensure the map is an island
public static class FalloffGeneration {

	/*
	 * Generates and returns grid falloffMap with 1s towards the edges and 0s in the middle 
	 */ 
	public static float[,] GenerateFalloffMap(int size, float falloffPower, float falloffOffset){
		float power = falloffPower; //how 'steep' the falloff will be
		float offset = falloffOffset; //how far from the centre to start the falloff
		float[,] falloffMap = new float[size, size]; //create a 2D array to hold the falloff values

		for (int x = 0; x < size; x++) {
			for (int y = 0; y < size; y++) {
				//setting the x and y points to a value in range -1 to 1 
				float pointX = x / (float)size * 2 - 1; //set x based on the distance from the left side of the map
				float pointY = y / (float)size * 2 - 1; //set y based on the distance from the top side of the map

				//sets closestToEdge to either pointX or pointY, whichever is closest to the edge of the array
				float closestToEdge = Mathf.Max(Mathf.Abs (pointX), Mathf.Abs (pointY)); 
				falloffMap [x, y] = FalloffCurve(closestToEdge, power, offset);
			}
		}

		return falloffMap;
	}
		
	/*
	 * formula to change the severity and offset of the falloff map
	 */ 
	static float FalloffCurve(float value, float power, float offset){
		float a = power;
		float b = offset; 
		float val = value;

		return (Mathf.Pow (val, a) / (Mathf.Pow (val, a) + Mathf.Pow ((b - b * val), a)));
	}
}
