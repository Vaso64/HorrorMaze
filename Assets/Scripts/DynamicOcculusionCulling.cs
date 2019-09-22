using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        get
        {
            return _castingRate;
        }
    }
    public bool overrideCameraFOV;
    public float FOV;
    public int hitRange = 3;
    private float hFOV;
    private float eulerDifferential;
    private Vector3 direction;
    public float castingDistance = 10;
    private Camera camera;
    private MazeSystem mazeSystem;

    private void Start()
    {
        mazeSystem = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        camera = gameObject.GetComponent<Camera>();
        if (!overrideCameraFOV) FOV = camera.fieldOfView;
        InvokeRepeating("Raycast", 0, 1 / castingRate);
    }

    private void Raycast()
    {
        hFOV = 2 * Mathf.Atan(Mathf.Tan(FOV * Mathf.Deg2Rad / 2) * Camera.main.aspect) * Mathf.Rad2Deg;
        bool[,] renderedWalls = new bool[200, 200];
        eulerDifferential =  hFOV / (rayDensity - 1);
        for (int rayIndex = 0; rayIndex < rayDensity; rayIndex++)
        {
            direction = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0) - new Vector3(0, hFOV / 2 - eulerDifferential * rayIndex, 0)) * Vector3.forward * castingDistance;
            Debug.DrawRay(transform.position, direction, Color.red, 1/castingRate);
            if(Physics.Raycast(transform.position, direction, out RaycastHit hit, Mathf.Infinity))
            {
                for (int x = (int)Mathf.Clamp(hit.transform.position.x - hitRange + 1, 0, Mathf.Infinity); x < hit.transform.position.x + hitRange; x++)
                {
                    for (int y = (int)Mathf.Clamp(hit.transform.position.z - hitRange + 1, 0, Mathf.Infinity); y < hit.transform.position.z + hitRange; y++)
                    {
                        if (mazeSystem.mazeMatrix[x, y] != null && !renderedWalls[x, y])
                        {
                            renderedWalls[x, y] = true;
                            mazeSystem.mazeMatrix[x, y].GetComponent<MeshRenderer>().enabled = true;
                        }
                    }
                }
            }
        }

       for (int x = 0; x < mazeSystem.size; x++)
       {
            for (int y = 0; y < mazeSystem.size; y++)
            {
                if (mazeSystem.mazeMatrix[x, y] != null && (!renderedWalls[x,y] ||
                    Vector3.Distance(transform.position, mazeSystem.mazeMatrix[x,y].transform.position) > castingDistance))
                    mazeSystem.mazeMatrix[x, y].GetComponent<MeshRenderer>().enabled = false;
            }
       }
    }

    private void OnDisable()
    {
        CancelInvoke();
        for (int y = 0; y < mazeSystem.size; y++)
        {
            for (int x = 0; x < mazeSystem.size; x++)
            {
                if (mazeSystem.mazeMatrix[x, y] != null) mazeSystem.mazeMatrix[x, y].GetComponent<MeshRenderer>().enabled = true;
            }
        }
    }

    private void OnEnable()
    {
        InvokeRepeating("Raycast", 0, 1 / castingRate);
    }
}
