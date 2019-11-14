using UnityEngine;

public class Chest : MonoBehaviour
{
    public enum Items { RedKey, GreenKey, BlueKey, Decoy, Trap, Knife, Tracker, Gun, Backpack, Empty };
    public Items chestContent;
    private Transform player;
    private bool isRendered = true;

    private void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        if (Vector3.Distance(player.transform.position, transform.position) > 10f && isRendered) Render(false);
        else if (Vector3.Distance(player.transform.position, transform.position) <= 10f && !isRendered) Render(true);
    }

    private void Render(bool render)
    {
        foreach (MeshRenderer rend in transform.GetComponentsInChildren<MeshRenderer>()) rend.enabled = render;
        isRendered = render;
    }
}
