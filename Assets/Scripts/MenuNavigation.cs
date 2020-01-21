using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuNavigation : MonoBehaviour
{
    public GameObject[] pages;
    public float transitionTime = 0.3f;
    private Image overlay;
    private void Start()
    {
        overlay = GameObject.Find("Overlay").GetComponent<Image>();
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
}