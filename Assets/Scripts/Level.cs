using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public enum sightDifficulties { blind, veryEasy, easy, medium, hard, nightmare }
    public enum hearingDifficulties { deaf, veryEasy, easy, medium, hard, nightmare }
    public enum lightSenseDifficulties { noSense, veryEasy, easy, medium, hard, nightmare }
    public enum speeds { slug, superSlow, slow, normal, fast, hyperFast }
    public enum RoamingIntensities { noAwarenes, easy, medium, hard, nightmare }
    public sightDifficulties sightDifficult;
    public hearingDifficulties hearDifficulty;
    public lightSenseDifficulties lightDifficulty;
    public speeds speed;
    public RoamingIntensities roamingIntensity;
    public int mazeSize;
    public int AICount;
    public Color fogColor;
    public float visibility;
    public int chestCount;
    public float hideDensity;
    public bool isEditable = false;


}
