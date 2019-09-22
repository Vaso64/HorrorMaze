using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour
{
    [HideInInspector]
    public Vector3 cameraDifferentialInput;
    public float cameraDifferential = 50;
    Animator anim;
    void Start()
    {
        anim = gameObject.GetComponent<Animator>();
        StartCoroutine(paranormal());
    }

    private void Update()
    {
        cameraDifferentialInput /= cameraDifferential;
        gameObject.transform.localRotation = Quaternion.Euler(cameraDifferentialInput);
    }

    IEnumerator paranormal()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(8, 20));
            anim.SetInteger("Paranormal", Random.Range(1, 4));
            yield return new WaitForSeconds(0.25f);
            anim.SetInteger("Paranormal", 0);
        }
    }
}
