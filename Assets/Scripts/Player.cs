using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    AudioSource audioSource;
    [SerializeField]
    Transform camera;
    [SerializeField]
    AudioClip[] footsteps = new AudioClip[6];
    [SerializeField]
    float speed = 50;
    public enum States{Idle, Walking, Running, Hiding };
    public States playerState;
    private Torch torch;
    [SerializeField]
    private Chest.Items[] inventory = { Chest.Items.Empty, Chest.Items.Empty, Chest.Items.Empty };
    [SerializeField]
    private bool[] keys = new bool[3];
    // Start is called before the first frame update
    void Start()
    {
        torch = GameObject.Find("Torch").GetComponent<Torch>();
        playerState = States.Idle;
        audioSource = gameObject.GetComponent<AudioSource>();
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isEditor)
        {
            Navigation(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")),
           new Vector3(Input.GetAxis("Mouse Y") * -4, Input.GetAxis("Mouse X") * 4, 0),
           Input.GetKey(KeyCode.LeftShift));
            if (Input.GetKeyDown(KeyCode.LeftShift)) audioSource.clip = footsteps[1];
            if (Input.GetKeyUp(KeyCode.LeftShift)) audioSource.clip = footsteps[0];
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)) Interact();
        }
    }

    public void Navigation(Vector3 move, Vector3 view, bool sprint)
    {
        torch.cameraDifferentialInput -= view;
        if (move == Vector3.zero) playerState = States.Idle;
        else
        {
            if (sprint) playerState = States.Running;
            else playerState = States.Walking;
        }
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            camera.Rotate(new Vector3(view.x, 0, 0));
            transform.Rotate(new Vector3(0, view.y, 0));
        }
        if (sprint) speed = 100;
        else speed = 50;
        rb.AddRelativeForce(move * Time.deltaTime * speed, ForceMode.Impulse);

        if (!audioSource.isPlaying && move != Vector3.zero) audioSource.Play();
        else if (audioSource.isPlaying && move == Vector3.zero)
        {
            audioSource.Pause();
        }
    }

    private void Interact()
    {
        if(Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 2))
        {
            switch (hit.transform.tag)
            {
                case "Chest":
                    Chest targetChest = hit.transform.GetComponent<Chest>();
                    if(targetChest.chestContent != Chest.Items.Empty) switch (targetChest.chestContent)
                    {
                            case Chest.Items.BlueKey: keys[0] = true; break;
                            case Chest.Items.GreenKey: keys[1] = true; break;
                            case Chest.Items.YellowKey: keys[2] = true; break;
                            default:
                                for(int inventoryIndex = 0; inventoryIndex < inventory.Length; inventoryIndex++)
                                {
                                    if(inventory[inventoryIndex] == Chest.Items.Empty)
                                    {
                                        inventory[inventoryIndex] = targetChest.chestContent;
                                        targetChest.chestContent = Chest.Items.Empty;
                                    }
                                }
                                break;
                    } 
                    break;
                case "HideWall":
                    break;
            }
        }
    }
}
