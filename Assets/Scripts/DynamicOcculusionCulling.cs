using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class DynamicOcculusionCulling : MonoBehaviour
{
    public int rayDensity = 32;
    public float castingRate = 16;
    public float castingDistance = 12;
    public float fov = 120;
    private MazeSystem maze; 

    IEnumerator Culling()
    {
        List<Vector2Int> toRenderBlocks;
        List<Vector2Int> prevRenderedBlocks = new List<Vector2Int>();
        LayerMask wallLayer = LayerMask.GetMask("Wall");
        while (maze.rendMatrix == null) yield return null;
        for (int y = 0; y <= GameParameters.maze.mazeSize; y++) for (int x = 0; x <= GameParameters.maze.mazeSize; x++) Render(maze.rendMatrix[x, y], false);
        while (enabled)
        {
            toRenderBlocks = new List<Vector2Int>();
            for (int rayIndex = 0; rayIndex < rayDensity; rayIndex++)
            {
                if (Physics.Raycast(transform.position, Quaternion.Euler(new Vector3(0, fov / (rayDensity - 1) * rayIndex)) * (Quaternion.Euler(new Vector3(0, fov / -2, 0)) * Vector3.Scale(transform.forward, new Vector3(1, 0, 1)).normalized) * castingDistance, out RaycastHit hit, castingDistance, wallLayer))
                {
                    toRenderBlocks = IncludeSurround(Vector2Int.RoundToInt(new Vector2(hit.transform.position.x, hit.transform.position.z)), GameParameters.maze.mazeSize, toRenderBlocks);
                }
            }
            toRenderBlocks = toRenderBlocks.Distinct().ToList();
            foreach (Vector2Int vector in prevRenderedBlocks.Except(toRenderBlocks)) Render(maze.rendMatrix[vector.x, vector.y], false);
            foreach (Vector2Int vector in toRenderBlocks.Except(prevRenderedBlocks)) Render(maze.rendMatrix[vector.x, vector.y], true);
            prevRenderedBlocks = toRenderBlocks;
            yield return new WaitForSeconds(1 / castingRate);
        }
    }

    void Render(List<Renderer> rendList, bool render)
    {
        foreach (Renderer rend in rendList) rend.enabled = render;
    }

    List<Vector2Int> IncludeSurround(Vector2Int pos, int size, List<Vector2Int> returnList)
    {
        for(int x = Mathf.Clamp(pos.x - 2, 0, size); x <= Mathf.Clamp(pos.x + 2, 0, size); x++)
        {
            for (int y = Mathf.Clamp(pos.y - 2, 0, size); y <= Mathf.Clamp(pos.y + 2, 0, size); y++)
            {
                returnList.Add(new Vector2Int(x, y));
            }
        }
        return returnList;
    }

    void OnDisable()
    {
        StopCoroutine(Culling());
        if (maze != null) for (int y = 0; y <= GameParameters.maze.mazeSize; y++) for (int x = 0; x <= GameParameters.maze.mazeSize; x++) Render(maze.rendMatrix[x, y], true); 
    }

    void OnEnable()
    {
        maze = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        StartCoroutine(Culling());
    }
}
