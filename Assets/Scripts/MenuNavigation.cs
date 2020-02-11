using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuNavigation : MonoBehaviour
{
    public GameObject[] pages;
    public float transitionTime = 0.3f;
    private Image overlay;
    public GameObject popup;
    public Image popupOverlay;
    public Text popupText;
    public Text popupTitle;

    private void Start()
    {
        overlay = GameObject.Find("TransitionOverlay").GetComponent<Image>();
    }   
    public void ChangePage(string toChangePage)
    {
        StartCoroutine(Transiton(toChangePage));
    }

    IEnumerator Transiton(string toChangePage)
    {
        float t = 0;
        while(t <= 1)
        {
            t += Time.deltaTime / transitionTime;
            overlay.color = new Color32(0, 0, 0, (byte)Mathf.Lerp(0, 255, t));       
            yield return null;
        }
        foreach (GameObject page in pages) //PAGE CHANGE
        {
            if (page.name == toChangePage) page.SetActive(true);
            else page.SetActive(false);
        }   
        while (t >= 0)
        {
            t -= Time.deltaTime / transitionTime;
            overlay.color = new Color32(0, 0, 0, (byte)Mathf.Lerp(0, 255, t));        
            yield return null;
        }
        yield break;
    }

    public void PopUp(string title, string text, bool showUp)
    {
        popupOverlay.enabled = showUp;
        if (showUp) StartCoroutine(Popup(title, text, showUp));
        else popup.SetActive(showUp);
    }

    IEnumerator Popup(string title, string text, bool showUp)
    {
        float t = 0;
        while(t <= 1)
        {
            t += Time.deltaTime / 0.3f;
            popupOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0, 0.7f, t));
            yield return null;
        }
        popup.SetActive(showUp);
        popupTitle.text = title;
        popupText.text = text;
        yield break;
    }
}