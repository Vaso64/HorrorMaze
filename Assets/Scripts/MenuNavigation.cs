using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    //PageChange
    public GameObject[] pages;
    public float transitionTime = 0.3f;
    private Image overlay;
    //PopUp
    public GameObject popup;
    private Image popupOverlay;
    public int popUpOutcome = -1;
    //Hover
    public enum hoverColorsEnum { red, grey }
    public Color[] hoverColors;
    public float hoverTime;
    //Level
    private Level currentLoadedLevel;
    //Slider
    private IEnumerator sliderCoroutine;
    [System.Serializable]
    public struct MazeColor
    {
        public Color color;
        public string name;
    }
    public MazeColor[] mazeColors;


    public enum Properties { sight, hearing, light, speed, roaming, aiCount, mazeSize, hideDensity, chestCount, visibility, fogColor }

    private void Start()
    {
        overlay = GameObject.Find("TransitionOverlay").GetComponent<Image>();
        popupOverlay = GameObject.Find("PopupOverlay").GetComponent<Image>();
    }   

    public void ChangePage(string toChangePage)
    {
        StartCoroutine(ChangePageCoroutine(toChangePage));
    }

    private IEnumerator ChangePageCoroutine(string toChangePage)
    {
        float t = 0;
        while(t < 1)
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
        while (t > 0)
        {          
            t -= Time.deltaTime / transitionTime;
            overlay.color = new Color32(0, 0, 0, (byte)Mathf.Lerp(0, 255, t));
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    public IEnumerator Popup(string title, string text)
    {
        popUpOutcome = -1;
        float t = 0;
        while(t <= 1)
        {
            t += Time.deltaTime / 0.3f;
            popupOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0, 0.7f, t));
            yield return null;
        }
        popup.SetActive(true);
        popup.transform.Find("Title").GetComponent<Text>().text = title;
        popup.transform.Find("TextBox").GetComponent<Text>().text = text;
        while (popUpOutcome == -1) yield return null;
        yield return null;
        popupOverlay.color = Color.clear;
        popUpOutcome = -1;
        popup.SetActive(false);
    }
    public string SetLevelProperty(Properties property, float value)
    {     
        switch (property)
        {
            case Properties.sight:
                currentLoadedLevel.sightDifficult = (Level.sightDifficulties)(int)value;
                return ((Level.sightDifficulties)(int)value).ToString();
            case Properties.hearing:
                currentLoadedLevel.hearDifficulty = (Level.hearingDifficulties)(int)value;
                return ((Level.hearingDifficulties)(int)value).ToString();
            case Properties.light:
                currentLoadedLevel.lightDifficulty = (Level.lightSenseDifficulties)(int)value;
                return ((Level.lightSenseDifficulties)(int)value).ToString();
            case Properties.speed:
                currentLoadedLevel.speed = (Level.speeds)(int)value;
                return ((Level.speeds)(int)value).ToString();
            case Properties.roaming:
                currentLoadedLevel.roamingIntensity = (Level.RoamingIntensities)(int)value;
                return ((Level.RoamingIntensities)(int)value).ToString();
            case Properties.aiCount:
                currentLoadedLevel.AICount = (int)value;
                return Mathf.RoundToInt(value).ToString();
            case Properties.mazeSize:
                currentLoadedLevel.mazeSize = (int)value;
                return Mathf.RoundToInt(value).ToString();
            case Properties.hideDensity:
                currentLoadedLevel.hideDensity = value;
                return value.ToString();
            case Properties.chestCount:
                currentLoadedLevel.chestCount = (int)value;
                return Mathf.RoundToInt(value).ToString();
            case Properties.visibility:
                currentLoadedLevel.visibility = value;
                return value.ToString();
            case Properties.fogColor:
                //TODO HERE
                Debug.LogError("How the fuck am I supposed to cast a float to a fucking color?");
                //level.fogColor = (Color)value;
                return "Undefinied Color";
            default:
                return "Undefinied property";
        }
    }

    public float GetLevelProperty(Properties property)
    {
        switch (property)
        {
            case Properties.sight: return (float)currentLoadedLevel.sightDifficult;
            case Properties.hearing: return (float)currentLoadedLevel.hearDifficulty;
            case Properties.light: return (float)currentLoadedLevel.lightDifficulty;
            case Properties.speed: return (float)currentLoadedLevel.speed;
            case Properties.roaming: return (float)currentLoadedLevel.roamingIntensity;
            case Properties.aiCount: return currentLoadedLevel.AICount;
            case Properties.mazeSize: return currentLoadedLevel.mazeSize;
            case Properties.hideDensity: return currentLoadedLevel.hideDensity;
            case Properties.chestCount: return currentLoadedLevel.chestCount;
            case Properties.visibility: return currentLoadedLevel.visibility;
            case Properties.fogColor:
            default: return 0;
        }
    }

    public void SliderDown(Transform slider, Properties property, float value, float minValue, float maxValue, float maxChangesPerSecond, bool numericOutput)
    {
        SliderStop();
        sliderCoroutine = SliderDownCoroutine(slider, property, value, minValue, maxValue, maxChangesPerSecond, numericOutput);
        StartCoroutine(sliderCoroutine);
    }
    public void SliderStop()
    {
        if(sliderCoroutine != null) StopCoroutine(sliderCoroutine);
    }
    IEnumerator SliderDownCoroutine(Transform slider, Properties property, float addUpValue, float minValue, float maxValue, float maxChangesPerSecond, bool numericOutput)
    {
        Text currentSliderText = slider.Find("CurrentValue").GetComponent<Text>();
        float waitTime = 0.5f;
        float currentValue;
        while (true)
        {
            currentValue = GetLevelProperty(property);
            currentValue += addUpValue;
            if (currentValue > maxValue || currentValue < minValue) currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
            if (numericOutput) currentSliderText.text = SetLevelProperty(property, currentValue).Replace(',', '.');
            else currentSliderText.text = CamelToWord(SetLevelProperty(property, currentValue));
            if (maxChangesPerSecond == 0 || currentValue == maxValue || currentValue == minValue) yield break;
            yield return new WaitForSeconds(waitTime);
            if (1 / (waitTime * 0.5f) < maxChangesPerSecond) waitTime *= 0.5f;
        }
    }

    public void Load(Level level)
    {
        StartCoroutine(LoadCoroutine(level));
    }
    private IEnumerator LoadCoroutine(Level level)
    {
        currentLoadedLevel = level;
        while (!pages[2].activeSelf) yield return null;
        Transform main = pages[2].transform.Find("Body/Configuration");
        //Dropdowns
        DropDownSet(main.Find("Enemy/Sight/DropDown"), CamelToWord(level.sightDifficult.ToString()), level.isEditable);
        DropDownSet(main.Find("Enemy/Hearing/DropDown"), CamelToWord(level.hearDifficulty.ToString()), level.isEditable);
        DropDownSet(main.Find("Enemy/Light/DropDown"), CamelToWord(level.lightDifficulty.ToString()), level.isEditable);
        DropDownSet(main.Find("Enemy/Speed/DropDown"), CamelToWord(level.speed.ToString()), level.isEditable);
        DropDownSet(main.Find("Enemy/Roaming/DropDown"), CamelToWord(level.roamingIntensity.ToString()), level.isEditable);
        //Sliders
        SliderSet(main.Find("Enemy/Count/Slider"), level.AICount.ToString(), level.isEditable);
        SliderSet(main.Find("Maze/Size/Slider"), level.mazeSize.ToString(), level.isEditable);
        SliderSet(main.Find("Maze/HideDensity/Slider"), level.hideDensity.ToString(), level.isEditable);
        SliderSet(main.Find("Maze/ChestCount/Slider"), level.chestCount.ToString(), level.isEditable);
        SliderSet(main.Find("Maze/Visibility/Slider"), level.visibility.ToString(), level.isEditable);
    }

    public void DropDownSet(Transform dropDown, bool open)
    {
        dropDown.Find("SubItems").gameObject.SetActive(open);
    }
    public void DropDownSet(Transform dropDown, string textValue, bool status)
    {
        dropDown.Find("MainItem/CurrentValue").GetComponent<Text>().text = textValue;
        dropDown.Find("MainItem").GetComponent<MenuElement>().enabled = status;
        dropDown.Find("MainItem/Icon").gameObject.SetActive(status);
    }
    public void DropDownSet(Transform dropDown, Properties property, float value, string textValue)
    {
        dropDown.Find("MainItem/CurrentValue").GetComponent<Text>().text = textValue;
        SetLevelProperty(property, value);
        DropDownSet(dropDown, false);
    }

    private void SliderSet(Transform slider, string textValue, bool status)
    {
        slider.Find("CurrentValue").GetComponent<Text>().text = textValue;
        slider.Find("Minus").gameObject.SetActive(status);
        slider.Find("Plus").gameObject.SetActive(status);
    }

    public void Play()
    {
        Level level = currentLoadedLevel.GetComponent<Level>();

        GameParameters.AI = new GameParameters.AIStruct(GameParameters.sightDifficulties[(int)level.sightDifficult],
                                                        GameParameters.hearingDifficulties[(int)level.hearDifficulty],
                                                        GameParameters.lightSenseDifficulties[(int)level.lightDifficulty],
                                                        GameParameters.speedsDifficulties[(int)level.speed],
                                                        GameParameters.roamingDifficulties[(int)level.roamingIntensity]);
        GameParameters.AI.sight.sightBase = Mathf.Clamp(GameParameters.AI.sight.sightBase, 0, GameParameters.maze.visibility * 0.9f);
        GameParameters.maze = new GameParameters.MazeStrcut(level.mazeSize + level.mazeSize % 2, level.AICount, level.hideDensity, level.chestCount, level.fogColor, level.visibility);
        SceneManager.LoadScene("Game");
    }

    public string CamelToWord(string camelString)
    {
        List<char> returnString = new List<char>();
        foreach (char key in camelString.ToCharArray())
        {
            if (returnString.Count == 0) returnString.Add(char.ToUpper(key));
            else if (char.IsUpper(key))
            {
                returnString.Add(' ');
                returnString.Add(char.ToLower(key));
            }
            else returnString.Add(key);
        }
        return new string(returnString.ToArray());
    }
}