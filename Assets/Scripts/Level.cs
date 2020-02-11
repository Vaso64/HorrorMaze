using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    public enum sightDifficulties { blind, veryEasy, easy, medium, hard, nightmare }
    public enum hearingDifficulties { deaf, veryEasy, easy, medium, hard, nightmare }
    public enum lightSenseDifficulties { noSense, veryEasy, easy, medium, hard, nightmare }
    public enum speeds { slug, superSlow, slow, normal, fast, hyperFast }
    public enum RaomingIntensities { noAwarenes, easy, medium, hard, nightmare }
    public sightDifficulties sightDifficult;
    public hearingDifficulties hearDifficulty;
    public lightSenseDifficulties lightDifficulty;
    public speeds speed;
    public RaomingIntensities roamingIntensity;
    public int mazeSize;
    public int AICount;
    public Color fogColor;
    public float visibility;
    public int chestCount;
    public float hideDensity;

    public void Load()
    {
        GameParameters.AI.sight.sightBase = Mathf.Clamp(GameParameters.AI.sight.sightBase, 0, GameParameters.maze.visibility * 0.9f);
        GameParameters.AI = new GameParameters.AIStruct(GameParameters.sightDifficulties[(int)sightDifficult],
                                                        GameParameters.hearingDifficulties[(int)hearDifficulty],
                                                        GameParameters.lightSenseDifficulties[(int)lightDifficulty],
                                                        GameParameters.speedsDifficulties[(int)speed],
                                                        GameParameters.roamingDifficulties[(int)roamingIntensity]);
        GameParameters.maze = new GameParameters.MazeStrcut(mazeSize + mazeSize % 2, AICount, hideDensity, chestCount, fogColor, visibility);
        SceneManager.LoadScene("Game");
    }
}
