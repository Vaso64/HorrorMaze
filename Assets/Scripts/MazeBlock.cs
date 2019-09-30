using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeBlock : MonoBehaviour
{
    public GameObject[] wallPrefabs;
    public List<GameObject> walls = new List<GameObject>();

    public void Build(bool[,] obstacleMemory, bool debugMazePath)
    {
        Vector2Int pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        int index;
        bool hideSpawned = false;
        foreach (Vector3 direction in AllowedDirection(obstacleMemory, pos))
        {
            if (direction != Vector3.zero)
            {
                if (Random.Range(0f, 1f) > 0.15f || hideSpawned) index = 0;
                else index = Random.Range(1, 8);
                walls.Add(Instantiate(wallPrefabs[index], transform.position + direction / 2, Quaternion.LookRotation(direction), transform));
            }
            else walls.Add(null);
        }
        if (Application.isEditor && debugMazePath)
        {
            Instantiate(wallPrefabs[0], transform.position + new Vector3(0, +5.01f, 0), Quaternion.Euler(0,0,0), transform).transform.localScale = new Vector3(0.1f, 1f, 0.1f);
        }
    }

    List<Vector3> AllowedDirection(bool[,] obstacleMemory, Vector2Int pos)
    {
        List<Vector3> allowedDirection = new List<Vector3>();
        if (pos.x - 1 > 0 && obstacleMemory[pos.x - 1, pos.y]) allowedDirection.Add(Vector3.left);
        else allowedDirection.Add(Vector3.zero);       
        if (pos.x + 1 < obstacleMemory.GetLength(0) && obstacleMemory[pos.x + 1, pos.y]) allowedDirection.Add(Vector3.right);
        else allowedDirection.Add(Vector3.zero);
        if (pos.y - 1 > 0 && obstacleMemory[pos.x, pos.y - 1]) allowedDirection.Add(Vector3.back);
        else allowedDirection.Add(Vector3.zero);
        if (pos.y + 1 < obstacleMemory.GetLength(1) && obstacleMemory[pos.x, pos.y + 1]) allowedDirection.Add(Vector3.forward);
        else allowedDirection.Add(Vector3.zero);
        return allowedDirection;
    }
}
