using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public enum Items { GreenKey, BlueKey, YellowKey, Decoy, Trap, Knife, Tracker, Empty };
    public Items chestContent;
    private Renderer rend;
    private Transform player;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        player = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        if (Vector3.Distance(player.transform.position, transform.position) > 12f && rend.enabled) rend.enabled = false;
        else if (Vector3.Distance(player.transform.position, transform.position) <= 12f && !rend.enabled) rend.enabled = true;
    }
}
