using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeSystem : MonoBehaviour
{
    [SerializeField]
    public GameObject[,] mazeMatrix = new GameObject[200, 200];
    [SerializeField]
    public bool[,] obstacleMemory = new bool[200, 200];
    public GameObject wall;
    public GameObject floor;
    public int size;
    public int AiCount;
    public GameObject AI;
    public GameObject ChestObject;
    private bool dynamicOcculusion = false;
    public int chestDensity = 300;
    public bool debugMazePath;

    void Start()
    {
        SetupMaze();
    }

    private void FillMaze(int size)
    {
        GameObject Floor = Instantiate(floor, new Vector3((size - 1) / 2, 0, (size - 1) / 2), Quaternion.identity);
        Floor.transform.localScale = new Vector3(size / 1f, 1, size / 1f);
        Floor.GetComponent<Renderer>().material.mainTextureScale = new Vector2(size * 1.2f, size * 1.2f);
        for (int y = 0; y < size; y++)
        {
            for(int x = 0; x < size; x++)
            {
                mazeMatrix[x,y] = Instantiate(wall, new Vector3(x, 0, y), Quaternion.identity);
                mazeMatrix[x, y].transform.parent = gameObject.transform;
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1) obstacleMemory[x, y] = true;
            }
        }
    }

    private void SetupMaze()
    {
        if (size % 2 == 0) size += 1;
        FillMaze(size);
        List<Vector2Int> walls = new List<Vector2Int>() { new Vector2Int(1, 3), new Vector2Int(3, 1) };
        UpdateMaze(1, 1);
        while (walls.Count != 0)
        {
            int randomIndex = Random.Range(0, walls.Count);
            walls = UpdateMazePrim(walls[randomIndex].x, walls[randomIndex].y, walls);
            walls.RemoveAt(randomIndex);
        }
        UpdateMaze(size - 1, size - 2);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) if (x == 0 || x == size - 1 || y == 0 || y == size - 1) obstacleMemory[x, y] = false;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) mazeMatrix[x, y].GetComponent<MazeBlock>().Build(obstacleMemory, debugMazePath);
        SpawnChests(FindDeadEnds(obstacleMemory));
        for (int x = AiCount; AiCount > 0; AiCount--) Instantiate(AI);
    }

    private List<Vector2Int> CheckPossibleDirections(int x, int y)
    {
        List<Vector2Int> allowedDirections = new List<Vector2Int>();
        if (obstacleMemory[x + 1, y] == true) allowedDirections.Add(new Vector2Int(1, 0));
        if (obstacleMemory[x - 1, y] == true) allowedDirections.Add(new Vector2Int(-1, 0));
        if (obstacleMemory[x, y + 1] == true) allowedDirections.Add(new Vector2Int(0, 1));
        if (obstacleMemory[x, y - 1] == true) allowedDirections.Add(new Vector2Int(0, -1));
        return allowedDirections;
    }

    private void UpdateMaze(int x, int y)
    {
        Destroy(mazeMatrix[x, y]);
        obstacleMemory[x, y] = true;
    }

    private List<Vector2Int> UpdateMazePrim(int x, int y, List<Vector2Int> walls)
    {
        UpdateMaze(x, y);
        List<Vector2Int> allowedDirections = new List<Vector2Int>();
        if (obstacleMemory[x + 1, y] != true)
        {
            if (obstacleMemory[x + 2, y] == true) allowedDirections.Add(new Vector2Int(1, 0));
            else if (!walls.Contains(new Vector2Int(x + 2, y))) walls.Add(new Vector2Int(x + 2, y));
        }
        if (obstacleMemory[x - 1, y] != true)
        {
            if (obstacleMemory[x - 2, y] == true) allowedDirections.Add(new Vector2Int(-1, 0));
            else if(!walls.Contains(new Vector2Int(x - 2, y))) walls.Add(new Vector2Int(x - 2, y));
        }
        if (obstacleMemory[x, y + 1] != true)
        {
            if (obstacleMemory[x, y + 2] == true) allowedDirections.Add(new Vector2Int(0, 1));
            else if (!walls.Contains(new Vector2Int(x, y + 2))) walls.Add(new Vector2Int(x, y + 2));
        }
        if (obstacleMemory[x, y - 1] != true)
        {
            if (obstacleMemory[x, y - 2] == true) allowedDirections.Add(new Vector2Int(0, -1));
            else if (!walls.Contains(new Vector2Int(x, y - 2))) walls.Add(new Vector2Int(x, y - 2));
            
        }
        Vector2Int direction = allowedDirections[Random.Range(0, allowedDirections.Count)];

        UpdateMaze(x + direction.x, y + direction.y);
        return walls;
    }

    private List<Vector2Int> FindDeadEnds(bool[,] obstacleMemory)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        for (int y = 1; y < size - 1; y++)
        {
            for (int x = 1; x < size - 1; x++)
            {
                bool inArea = (x > 5 || y > 5) && (x < size - 5 || y < size - 5);
                if (obstacleMemory[x, y] && CheckPossibleDirections(x, y).Count == 1 && inArea) deadEnds.Add(new Vector2Int(x, y));
            }
        }
        return deadEnds;
    }

    private void SpawnChests(List<Vector2Int> deadEnds)
    {
        for (int x = 0; x < 3; x++)
        {
            int randomDeadEndIndex = Random.Range(0, deadEnds.Count);
            CreateChest(deadEnds[randomDeadEndIndex], (Chest.Items)x);
            deadEnds.RemoveAt(randomDeadEndIndex);
        }
        int chestCnt = 1 + (int)Mathf.Pow(size, 2) / chestDensity;
        for (; chestCnt != 0; chestCnt--)
        {
            int randomDeadEndIndex = Random.Range(0, deadEnds.Count);
            CreateChest(deadEnds[randomDeadEndIndex], (Chest.Items)Random.Range(3, 9));
            deadEnds.RemoveAt(randomDeadEndIndex);
        }
    }

    private void CreateChest(Vector2Int pos, Chest.Items item)
    {
        Vector2Int dir = CheckPossibleDirections(pos.x, pos.y)[0];
        Instantiate(ChestObject, new Vector3(pos.x, 0, pos.y), Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y)) * Quaternion.Euler(0,180,0)).GetComponent<Chest>().chestContent = item;
    }
}
