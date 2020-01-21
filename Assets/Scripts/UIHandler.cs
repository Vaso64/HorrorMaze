﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField]
    GameObject FPS;
    Vector2 movementStartPos;
    private int currentFPS;
    public GameObject[] keysUI;
    public Sprite[] Sprites;
    public GameObject[] InvenotryUI;
    public GameObject ItemPopUp;
    private RectTransform innerJoystick;
    private RectTransform outerJoystick;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        RenderSettings.fogColor = GameParameters.fogColor;
        innerJoystick = GameObject.Find("innerJoystick").GetComponent<RectTransform>();
        outerJoystick = GameObject.Find("outerJoystick").GetComponent<RectTransform>();
        StartCoroutine(FPSCounter());
    }

    void Update()
    {
        currentFPS = (int)(1f / Time.unscaledDeltaTime);
    }

    public void MoveUI(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                outerJoystick.GetComponent<RectTransform>().anchoredPosition = touch.position;
                movementStartPos = touch.position;
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                Vector2 direction = Vector2.ClampMagnitude(new Vector2((movementStartPos.x - touch.position.x) * -1, (movementStartPos.y - touch.position.y) * -1), 100);
                innerJoystick.anchoredPosition = direction;
                break;
            case TouchPhase.Ended:
                outerJoystick.anchoredPosition = new Vector2(-300, -300);
                break;
        }
    }

    IEnumerator FPSCounter()
    {
        while(true)
        {
            FPS.GetComponent<Text>().text = currentFPS.ToString();
            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateUI(bool[] keys, Chest.Items[] inventory, Chest.Items pickedItem, int selectedItem)
    {

        transform.Find("Inventory").GetComponent<Animator>().SetTrigger("FadeIn");
        transform.Find("Inventory").GetComponent<Animator>().SetInteger("Selected", selectedItem);
        if (pickedItem != Chest.Items.Empty)
        {
            ItemPopUp.transform.Find("Text").GetComponent<Text>().text = pickedItem.ToString();
            ItemPopUp.transform.Find("Image").GetComponent<Image>().sprite = Sprites[(int)pickedItem];
            ItemPopUp.GetComponent<Animator>().SetTrigger("Display");
        }
        for (int x = 0; x < 3; x ++)
        {
            if (keys[x]) keysUI[x].GetComponent<Image>().sprite = Sprites[x];
            InvenotryUI[x].GetComponent<Image>().sprite = Sprites[(int)inventory[x]];
        }
    }
}
