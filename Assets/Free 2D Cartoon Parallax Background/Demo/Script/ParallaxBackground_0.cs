using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground_0 : MonoBehaviour
{
    public float Level_MoveSpeed = 1.5f; // Speed at which the level scrolls to the left

    [Header("Layer Setting")]
    public float[] Layer_Speed = new float[7]; // Parallax speed for each layer
    public GameObject[] Layer_Objects = new GameObject[7]; // Background layers

    private float[] startPos = new float[7]; // Starting positions of each layer
    private float boundSizeX;               // Width of a single sprite (world units)
    private float sizeX;                    // Scale of the layer object

    void Start()
    {
        // Initialize starting positions and bounds
        sizeX = Layer_Objects[0].transform.localScale.x;
        boundSizeX = Layer_Objects[0].GetComponent<SpriteRenderer>().sprite.bounds.size.x;

        for (int i = 0; i < Layer_Objects.Length; i++)
        {
            startPos[i] = Layer_Objects[i].transform.localPosition.x;
        }
    }

    void Update()
    {
        Level_MoveSpeed = LevelGenerator.Instance.speed.Value;

        for (int i = 0; i < Layer_Objects.Length; i++)
        {
            // Calculate the parallax effect for each layer
            float distance = Time.deltaTime * Level_MoveSpeed * Layer_Speed[i];
            startPos[i] -= distance; // Move layers to the left

            // Update the position of the layer
            Layer_Objects[i].transform.localPosition = new Vector2(startPos[i], Layer_Objects[i].transform.localPosition.y);

            // Repeat the layer to create an infinite scrolling effect
            if (startPos[i] <= -boundSizeX * sizeX)
            {
                startPos[i] += boundSizeX * sizeX;
            }
        }
    }
}
