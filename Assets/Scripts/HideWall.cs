using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideWall : MonoBehaviour
{
    public Vector3 camLockPos;
    private void Start()
    {
        camLockPos = transform.position + transform.rotation * camLockPos;
    }
}
