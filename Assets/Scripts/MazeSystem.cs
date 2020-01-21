using System.Collections.Generic;
using UnityEngine;

public class MazeSystem : MonoBehaviour
{
    [SerializeField]
    public GameObject[,] mazeMatrix;
    [SerializeField]
    public bool[,] obstacleMatrix; //FALSE == obstacle
    public struct hide
    {
        public Vector3 checkingRot;
        public Transform transform;
        public hide(Vector3 CheckingRot, Transform Transform)
        {
            checkingRot = CheckingRot;
            transform = Transform;
        }
    }
    public List<hide>[,] hideMatrix;
    public GameObject wallBlock;
    public GameObject floor;
    public GameObject AIPrefab;
    public Transform[] AIs;
    public GameObject ChestObject;
    public bool debugMazePath;

    void Start()
    {
        SetupMaze();
    }

    private void SetupMaze()
    {
        if (GameParameters.size % 2 == 1) GameParameters.size += 1;
        mazeMatrix = new GameObject[GameParameters.size + 1, GameParameters.size + 1];
        obstacleMatrix = new bool[GameParameters.size + 1, GameParameters.size + 1];
        hideMatrix = new List<hide>[GameParameters.size + 1, GameParameters.size + 1];
        FillMaze(GameParameters.size);
        List<Vector2Int> walls = new List<Vector2Int>() { new Vector2Int(1, 3), new Vector2Int(3, 1) };
        UpdateMaze(1, 1);
        while (walls.Count != 0)
        {
            int randomIndex = Random.Range(0, walls.Count);
            walls = UpdateMazePrim(walls[randomIndex].x, walls[randomIndex].y, walls);
            walls.RemoveAt(randomIndex);
        }
        for (int x = 0; x <= GameParameters.size; x++) for (int y = 0; y <= GameParameters.size; y++) if (x == 0 || x == GameParameters.size || y == 0 || y == GameParameters.size) obstacleMatrix[x,y] = false;
        UpdateMaze(GameParameters.size, GameParameters.size - 1);
        for (int y = 0; y <= GameParameters.size; y++) for (int x = 0; x <= GameParameters.size; x++)
            {
                if (!obstacleMatrix[x, y]) mazeMatrix[x, y].GetComponent<MazeBlock>().Build(obstacleMatrix, debugMazePath);
                else mazeMatrix[x, y].GetComponent<Collider>().enabled = true;
            }
        SpawnChests(FindDeadEnds(obstacleMatrix));
        AIs = new Transform[GameParameters.AICount];
        for (int x = 0; x < GameParameters.AICount; x++) AIs[x] = Instantiate(AIPrefab, randomPos(GameParameters.size), Quaternion.identity).transform;
    }

    private void FillMaze(int size)
    {
        GameObject Floor = Instantiate(floor, new Vector3((size - 1) / 2, 0, (size - 1) / 2), Quaternion.identity);
        Floor.transform.localScale = new Vector3(size / 1f, 1, size / 1f);
        Floor.GetComponent<Renderer>().material.mainTextureScale = new Vector2(size * 1.2f, size * 1.2f);

        for (int y = 0; y <= size; y++)
        {
            for(int x = 0; x <= size; x++)
            {
                mazeMatrix[x,y] = Instantiate(wallBlock, new Vector3(x, 0, y), Quaternion.identity, transform);
                mazeMatrix[x, y].GetComponent<MazeBlock>().maze = this;
                if (x == 0 || x == size || y == 0 || y == size) UpdateMaze(x, y);
            }
        }
    }

    private List<Vector2Int> CheckPossibleDirections(int x, int y)
    {
        List<Vector2Int> allowedDirections = new List<Vector2Int>();
        if (obstacleMatrix[x + 1, y] == true) allowedDirections.Add(new Vector2Int(1, 0));
        if (obstacleMatrix[x - 1, y] == true) allowedDirections.Add(new Vector2Int(-1, 0));
        if (obstacleMatrix[x, y + 1] == true) allowedDirections.Add(new Vector2Int(0, 1));
        if (obstacleMatrix[x, y - 1] == true) allowedDirections.Add(new Vector2Int(0, -1));
        return allowedDirections;
    }

    private void UpdateMaze(int x, int y)
    {
        obstacleMatrix[x, y] = true;
    }

    private List<Vector2Int> UpdateMazePrim(int x, int y, List<Vector2Int> walls)
    {
        UpdateMaze(x, y);
        List<Vector2Int> allowedDirections = new List<Vector2Int>();
        if (!obstacleMatrix[x + 1, y])
        {
            if (obstacleMatrix[x + 2, y]) allowedDirections.Add(new Vector2Int(1, 0));
            else if (!walls.Contains(new Vector2Int(x + 2, y))) walls.Add(new Vector2Int(x + 2, y));
        }
        if (!obstacleMatrix[x - 1, y])
        {
            if (obstacleMatrix[x - 2, y]) allowedDirections.Add(new Vector2Int(-1, 0));
            else if(!walls.Contains(new Vector2Int(x - 2, y))) walls.Add(new Vector2Int(x - 2, y));
        }
        if (!obstacleMatrix[x, y + 1])
        {
            if (obstacleMatrix[x, y + 2]) allowedDirections.Add(new Vector2Int(0, 1));
            else if (!walls.Contains(new Vector2Int(x, y + 2))) walls.Add(new Vector2Int(x, y + 2));
        }
        if (!obstacleMatrix[x, y - 1])
        {
            if (obstacleMatrix[x, y - 2]) allowedDirections.Add(new Vector2Int(0, -1));
            else if (!walls.Contains(new Vector2Int(x, y - 2))) walls.Add(new Vector2Int(x, y - 2));        
        }
        Vector2Int direction = allowedDirections[Random.Range(0, allowedDirections.Count)];

        UpdateMaze(x + direction.x, y + direction.y);
        return walls;
    }

    private List<Vector2Int> FindDeadEnds(bool[,] obstacleMemory)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        for (int y = 1; y < GameParameters.size; y++)
        {
            for (int x = 1; x < GameParameters.size; x++)
            {
                if (obstacleMemory[x, y] && CheckPossibleDirections(x, y).Count == 1 && new Vector2Int(x,y) != Vector2Int.one) deadEnds.Add(new Vector2Int(x, y));
            }
        }
        Debug.Log(deadEnds.Count);
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
        for (int chestCnt = GameParameters.chestCount; chestCnt != 0; chestCnt--)
        {
            int randomDeadEndIndex = Random.Range(0, deadEnds.Count);
            CreateChest(deadEnds[randomDeadEndIndex], (Chest.Items)Random.Range(3,9));
            deadEnds.RemoveAt(randomDeadEndIndex);
        }
    }

    private void CreateChest(Vector2Int pos, Chest.Items item)
    {
        Vector2Int dir = CheckPossibleDirections(pos.x, pos.y)[0];
        Instantiate(ChestObject, new Vector3(pos.x, 0, pos.y), Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y)) * Quaternion.Euler(0,180,0), transform).GetComponent<Chest>().chestContent = item;
    }

    Vector3 randomPos(int mazeSize)
    {
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(Random.Range(1, mazeSize), Random.Range(1, mazeSize));
        } while (!obstacleMatrix[pos.x, pos.y]);
        return new Vector3(pos.x, 0, pos.y);
    }
}
