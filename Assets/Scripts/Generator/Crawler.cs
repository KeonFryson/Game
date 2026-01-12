using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Crawler : Maze
{
    public override void GenerateMazeData()
    {
        // First loop: Fill everything with walls
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < depth; k++)
                {
                    mazeData[i, j, k] = 1; // All walls
                }
            }
        }

        // Second loop: Set the floor at y = 0
        for (int i = 0; i < width; i++)
        {
            for (int k = 0; k < depth; k++)
            {
                mazeData[i, 0, k] = 0; // Floor at bottom
            }
        }

        bool done = false;
        Vector3Int[] direction = new Vector3Int[]
        {
            new Vector3Int(1,0,0),  //right
            new Vector3Int(-1,0,0), //left
            new Vector3Int(0,1,0),  //up
            new Vector3Int(0,-1,0), //down
            new Vector3Int(0,0,1),  //forward
            new Vector3Int(0,0,-1)  //back
        };
        // Initialize starting position at the center, just above the floor
        int x = width / 2;
        int y = height-1; // Start at y=1 (just above the floor at y=0)
        int z = depth / 2;

        Debug.Log($"Starting crawler at position: ({x}, {y}, {z})");

        while (!done)
        {

            mazeData[x, y, z] = 2; // Mark current position as path

            Debug.Log($"Crawler at: ({x}, {y}, {z})");  // Add this line
            Vector3Int move = direction[Random.Range(0, direction.Length)];

            // Update position
            int newX = x + move.x;
            // int newY = y + move.y;
            int newZ = z + move.z;

            Debug.Log($"New position would be: ({newX}, {0}, {newZ})");

            //complete when we hit a edge or are out of bounds  not the floor
            //done = (newX <= 0 || newX >= width - 1 || newY < 1 || newY >= height || newZ <= 0 || newZ >= depth - 1);
            done = (newX <= 0 || newX >= width - 1 || newZ <= 0 || newZ >= depth - 1);

            // Only move if within bounds
            if (!done)
            {
                x = newX;
                // y = newY;
                z = newZ;
            }
        }
        Debug.Log("Crawler finished = " + done);
    }
}
