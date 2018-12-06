using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class to generate the Perlin Noise used in my map generation
public static class NoiseGeneration{

	/*
	 * Generates and returns a 2D array to act as a noise map (used to make my heightmap and moisturemap)
	 */ 
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float amplitudeModifier, float frequencyModifier){
		float[,] noiseMap = new float[mapWidth, mapHeight]; //creating a 2D array for holding my noiseMap

		//System.Random pseudoRandom = new System.Random (seed); //seeds the Random Function, allowing me to reuse maps if I like them
		System.Random pseudoRandom = new System.Random (Random.Range(0, 1000)); //Creates a random seed, will generate a random map each time

		//sets a random offset x and y value for each octave, meaning they'll sample a different part of Unitys Noise and generate more random maps
		Vector2[] octaveOffsets = new Vector2[octaves]; 
		//loop to give each octave a different offset, so they use a different section of the noise
		for (int i = 0; i < octaves; i++) {
			float xOffset = pseudoRandom.Next(-10000, 10000); //giving xOffset for octave i a random value
			float yOffset = pseudoRandom.Next(-10000, 10000); //giving yOffset for octave i a random value
			octaveOffsets [i] = new Vector2 (xOffset, yOffset);
		}
			
		//ensures scale is greater than 0, to prevent division by 0 errors
		if (scale <= 0) {
			scale = 0.0001f;
		}

		//floats to keep track of heighest and lowest noise values, used to normalize the noise back to a 0 to 1 range
		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;

		//loop to fill my noiseMap array with a section of Perlin Noise from each octave
		for (int x = 0; x < mapWidth; x++) {
			for (int y = 0; y < mapHeight; y++) {

				float amplitude = 1; //each new octave has a lower amplitude, meaning it has less affect on the noiseHeight
				float frequency = 1; //each new octave has a higher frequency, meaning its sample points are further apart (resulting in more peaks/dips)
				float noiseHeight = 0; //the overall value of the noise after adding the affect from each octave

				//loop to add noise from each octave to the noiseMap
				for (int i = 0; i < octaves; i++){
					//picking the x and y values of the point in the Noise to sample the noise value from 
					float xSample = x / scale * frequency + octaveOffsets[i].x; 
					float ySample = y / scale * frequency + octaveOffsets[i].y;

					//gets a perlinValue from -1 to 1 so additional octaves can either decrease or increase the noiseHeight
					float perlinValue = Mathf.PerlinNoise (xSample, ySample) * 2 - 1; //gets the perlin noise value for point (xSample, ySample) in the generated Perlin Noise
					noiseHeight += perlinValue * amplitude; //adds (or subtracts) the current octaves perlinValue * amplitude to noiseHeight
					noiseMap [x, y] = perlinValue; //sets the corresponsing point in noiseMap to the perlinValue

					amplitude *= amplitudeModifier; //reduces the amplitude for the next octave by multiplying it by persistance
					frequency *= frequencyModifier; //increases the frequency of the next octave by multiplying it by lacunarity
				}

				//updates maxNoiseHeight and minNoiseHeight if the current noiseHeight is higher/lower than them respectively
				if (noiseHeight > maxNoiseHeight)
					maxNoiseHeight = noiseHeight;
				else if (noiseHeight < minNoiseHeight)
					minNoiseHeight = noiseHeight;

				noiseMap [x, y] = noiseHeight; //setting the points noiseHeight in the noiseMap array
			}
		}

		//normalized the array to be in range 0 to 1 again instead of -1 to 1
		for (int x = 0; x < mapWidth; x++) {
			for (int y = 0; y < mapHeight; y++) {
				noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap [x, y]); //gets the fraction that the point noiseMap[x,y] is in the range minNoiseHeight to maxNoiseHeight
			}
		}

		return noiseMap;
	}
}
