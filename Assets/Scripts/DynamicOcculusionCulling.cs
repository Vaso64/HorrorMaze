using System.Collections.Generic;
using UnityEngine;
using System;

public class DynamicOcculusionCulling : MonoBehaviour
{
    public int rayDensity = 30;
    public float _castingRate = 64;
    private float castingRate
    {
        set
        {
            _castingRate = value;
            CancelInvoke("Raycast");
            InvokeRepeating("Raycast", 0, 1 / _castingRate);
        }
        get { return _castingRate; }
    }
    public bool overrideCameraFOV;
    public float FOV;
    public float castingDistance = 10;
    private float hFOV;
    private float eulerDifferential;
    private Vector3 direction;
    private Camera camera;
    private MazeSystem mazeSystem;
    private List<MeshRenderer>[,,] rendMatrix;
    bool[,,] renderedWalls;

    private void Start()
    {
        hFOV = 2 * Mathf.Atan(Mathf.Tan(FOV * Mathf.Deg2Rad / 2) * Camera.main.aspect) * Mathf.Rad2Deg;
        mazeSystem = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        camera = gameObject.GetComponent<Camera>();
        if (!overrideCameraFOV) FOV = camera.fieldOfView;
        rendMatrix = new List<MeshRenderer>[GameParameters.size + 1, GameParameters.size + 1, 4];
        for (int x = 0; x <= GameParameters.size; x++)
        {
            for (int y = 0; y <= GameParameters.size; y++)
            {
                for (int w = 0; w < 4; w++)
                {
                    rendMatrix[x, y, w] = new List<MeshRenderer>();
                    if(!mazeSystem.obstacleMatrix[x,y] && mazeSystem.mazeMatrix[x, y].GetComponent<MazeBlock>().walls[w] != null)
                    {
                        foreach (MeshRenderer rend in mazeSystem.mazeMatrix[x, y].GetComponent<MazeBlock>().walls[w].GetComponentsInChildren<MeshRenderer>())
                        {
                            rendMatrix[x, y, w].Add(rend);
                        }
                    }
                }
            }
        }
    }

    private void Raycast()
    {  
        renderedWalls = new bool[GameParameters.size + 1, GameParameters.size + 1, 4];
        eulerDifferential =  hFOV / (rayDensity - 1);
        for (int rayIndex = 0; rayIndex < rayDensity; rayIndex++)
        {
            direction = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0) - new Vector3(0, hFOV / 2 - eulerDifferential * rayIndex, 0)) * Vector3.forward * castingDistance;
            //Debug.DrawRay(transform.position, direction, Color.red, 1/castingRate);
            if(Physics.Raycast(transform.position, direction, out RaycastHit hit, castingDistance) && (hit.transform.tag == "Wall" || hit.transform.tag == "HideWall"))
            {
                Vector2Int pos = Vector2Int.RoundToInt(new Vector2(hit.transform.position.x, hit.transform.position.z));
                int wallIndex = hit.transform.parent.GetComponent<MazeBlock>().walls.IndexOf(hit.transform.gameObject);
                foreach (Vector3Int wall in MapSurroundings(new Vector3Int(pos.x, pos.y, wallIndex)))
                {
                    try
                    {
                        renderedWalls[wall.x, wall.y, wall.z] = true;
                    }
                    catch (System.Exception)
                    {
                        Debug.LogError("Error rendering " + wall);
                    }         
                }     
            }
        }
        for (int x = 0; x <= GameParameters.size; x++)
        {
            for (int y = 0; y <= GameParameters.size; y++)
            {
                for (int w = 0; w < 4; w++)
                {
                    try
                    {
                        foreach (MeshRenderer rend in rendMatrix[x, y, w])
                        {
                            rend.enabled = renderedWalls[x, y, w];
                        }
                    }
                    catch (System.Exception)
                    {
                        Debug.LogError("error rendering " + new Vector3(x, y, w));
                        throw;
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
        for (int y = 0; y < GameParameters.size; y++)
        {
            for (int x = 0; x < GameParameters.size; x++)
            {
                if(mazeSystem.mazeMatrix[x, y] != null)
                {
                    foreach (MeshRenderer rend in mazeSystem.mazeMatrix[x, y].GetComponentsInChildren<MeshRenderer>()) rend.enabled = true;
                }
            }
        }
    }

    private void OnEnable()
    {
        InvokeRepeating("Raycast", 0, 1 / castingRate);
    }

    
    private List<Vector3Int> MapSurroundings(Vector3Int pos)
    {
        //LEFT 0, RIGHT 1, DOWN 2, UP 3
        List<Vector3Int> toRender = new List<Vector3Int>();
        toRender.Add(pos);
        //Occulude surroundings
        switch (pos.z)
        {         
            case 0:
                if (InRange(pos + new Vector3Int(0, 1, 0))) toRender.Add(pos + new Vector3Int(0, 1, 0));
                if (InRange(pos + new Vector3Int(0, -1, 0))) toRender.Add(pos + new Vector3Int(0, -1, 0));
                if (InRange(pos + new Vector3Int(0, 2, 0))) toRender.Add(pos + new Vector3Int(0, 2, 0));
                if (InRange(pos + new Vector3Int(0, -2, 0))) toRender.Add(pos + new Vector3Int(0, -2, 0));
                if (InRange(pos + new Vector3Int(-2, 0, 1))) toRender.Add(pos + new Vector3Int(-2, 0, 1));
                if (InRange(pos + new Vector3Int(-1, 1, 2))) toRender.Add(pos + new Vector3Int(-1, 1, 2));
                if (InRange(pos + new Vector3Int(-1, -1, 3))) toRender.Add(pos + new Vector3Int(-1, -1, 3));
                if (InRange(pos + new Vector3Int(0, 0, 2))) toRender.Add(pos + new Vector3Int(0, 0, 2));
                if (InRange(pos + new Vector3Int(0, 0, 3))) toRender.Add(pos + new Vector3Int(0, 0, 3));
                if (InRange(pos + new Vector3Int(0, 2, 2))) toRender.Add(pos + new Vector3Int(0, 2, 2));
                if (InRange(pos + new Vector3Int(0, -2, 3))) toRender.Add(pos + new Vector3Int(0, -2, 3));
                if (InRange(pos + new Vector3Int(0, 3, 2))) toRender.Add(pos + new Vector3Int(0, 3, 2));
                if (InRange(pos + new Vector3Int(0, -3, 3))) toRender.Add(pos + new Vector3Int(0, -3, 3));
                break;
            case 1:
                if (InRange(pos + new Vector3Int(0, 1, 0))) toRender.Add(pos + new Vector3Int(0, 1, 0));
                if (InRange(pos + new Vector3Int(0, -1, 0))) toRender.Add(pos + new Vector3Int(0, -1, 0));
                if (InRange(pos + new Vector3Int(0, 2, 0))) toRender.Add(pos + new Vector3Int(0, 2, 0));
                if (InRange(pos + new Vector3Int(0, -2, 0))) toRender.Add(pos + new Vector3Int(0, -2, 0));
                if (InRange(pos + new Vector3Int(2, 0, -1))) toRender.Add(pos + new Vector3Int(2, 0, -1));
                if (InRange(pos + new Vector3Int(1, 1, 1))) toRender.Add(pos + new Vector3Int(1, 1, 1));
                if (InRange(pos + new Vector3Int(1, -1, 2))) toRender.Add(pos + new Vector3Int(1, -1, 2));
                if (InRange(pos + new Vector3Int(0, 0, 1))) toRender.Add(pos + new Vector3Int(0, 0, 1));
                if (InRange(pos + new Vector3Int(0, 0, 2))) toRender.Add(pos + new Vector3Int(0, 0, 2));
                if (InRange(pos + new Vector3Int(0, 2, 1))) toRender.Add(pos + new Vector3Int(0, 2, 1));
                if (InRange(pos + new Vector3Int(0, -2, 2))) toRender.Add(pos + new Vector3Int(0, -2, 2));
                if (InRange(pos + new Vector3Int(0, 3, 1))) toRender.Add(pos + new Vector3Int(0, 3, 1));
                if (InRange(pos + new Vector3Int(0, -3, 2))) toRender.Add(pos + new Vector3Int(0, -3, 2));
                break;
            case 2:
                if (InRange(pos + new Vector3Int(1, 0, 0))) toRender.Add(pos + new Vector3Int(1, 0, 0));
                if (InRange(pos + new Vector3Int(-1, 0, 0))) toRender.Add(pos + new Vector3Int(-1, 0, 0));
                if (InRange(pos + new Vector3Int(2, 0, 0))) toRender.Add(pos + new Vector3Int(2, 0, 0));
                if (InRange(pos + new Vector3Int(-2, 0, 0))) toRender.Add(pos + new Vector3Int(-2, 0, 0));
                if (InRange(pos + new Vector3Int(0, -2, +1))) toRender.Add(pos + new Vector3Int(0, -2, +1));
                if (InRange(pos + new Vector3Int(1, -1, -2))) toRender.Add(pos + new Vector3Int(1, -1, -2));
                if (InRange(pos + new Vector3Int(-1, -1, -1))) toRender.Add(pos + new Vector3Int(-1, -1, -1));
                if (InRange(pos + new Vector3Int(0, 0, -1))) toRender.Add(pos + new Vector3Int(0, 0, -1));
                if (InRange(pos + new Vector3Int(0, 0, -2))) toRender.Add(pos + new Vector3Int(0, 0, -2));
                if (InRange(pos + new Vector3Int(2, 0, -2))) toRender.Add(pos + new Vector3Int(2, 0, -2));
                if (InRange(pos + new Vector3Int(-2, 0, -1))) toRender.Add(pos + new Vector3Int(-2, 0, -1));
                if (InRange(pos + new Vector3Int(3, 0, -2))) toRender.Add(pos + new Vector3Int(3, 0, -2));
                if (InRange(pos + new Vector3Int(-3, 0, -1))) toRender.Add(pos + new Vector3Int(-3, 0, -1));
                break;
            case 3:
                if (InRange(pos + new Vector3Int(1, 0, 0))) toRender.Add(pos + new Vector3Int(1, 0, 0));
                if (InRange(pos + new Vector3Int(-1, 0, 0))) toRender.Add(pos + new Vector3Int(-1, 0, 0));
                if (InRange(pos + new Vector3Int(2, 0, 0))) toRender.Add(pos + new Vector3Int(2, 0, 0));
                if (InRange(pos + new Vector3Int(-2, 0, 0))) toRender.Add(pos + new Vector3Int(-2, 0, 0));
                if (InRange(pos + new Vector3Int(0, 2, -1))) toRender.Add(pos + new Vector3Int(0, 2, -1));
                if (InRange(pos + new Vector3Int(-1, 1, -2))) toRender.Add(pos + new Vector3Int(-1, 1, -2));
                if (InRange(pos + new Vector3Int(1, 1, -3))) toRender.Add(pos + new Vector3Int(1, 1, -3));
                if (InRange(pos + new Vector3Int(0, 0, -2))) toRender.Add(pos + new Vector3Int(0, 0, -2));
                if (InRange(pos + new Vector3Int(0, 0, -3))) toRender.Add(pos + new Vector3Int(0, 0, -3));
                if (InRange(pos + new Vector3Int(2, 0, -3))) toRender.Add(pos + new Vector3Int(2, 0, -3));
                if (InRange(pos + new Vector3Int(-2, 0, -2))) toRender.Add(pos + new Vector3Int(-2, 0, -2));
                if (InRange(pos + new Vector3Int(3, 0, -3))) toRender.Add(pos + new Vector3Int(3, 0, -3));
                if (InRange(pos + new Vector3Int(-3, 0, -2))) toRender.Add(pos + new Vector3Int(-3, 0, -2));
                break;
        }
        return toRender;
    }

    private bool InRange(Vector3Int pos)
    {
        if (pos.x <= GameParameters.size && pos.x >= 0 && pos.y <= GameParameters.size && pos.y >= 0 && pos.z <= 3 && pos.z >= 0) return true;
        else return false;
    }
}
