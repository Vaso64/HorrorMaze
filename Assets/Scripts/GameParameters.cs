using UnityEngine;
static public class GameParameters
{
    //STRUCTS 
    public struct sight
    {
        public sight(float SightBase, float SightRange, float SightRangeBonus, int Fov)
        {
            sightBase = SightBase;
            sightRange = SightRange;
            sightRangeBonus = SightRangeBonus;
            fov = Fov;
        }
        public float sightBase;
        public float sightRange;
        public float sightRangeBonus;
        public float fov;
    }
    public struct hearing
    {
        public hearing(float HearWalkBase, float HearWalkRange, float HearWalkRangeBonus, float HearRunBase, float HearRunRange, float HearRunRangeBonus)
        {
            hearWalkBase = HearWalkBase;
            hearWalkRange = HearWalkRange;
            hearWalkRangeBonus = HearWalkRangeBonus;
            hearRunBase = HearRunBase;
            hearRunRange = HearRunRange;
            hearRunRangeBonus = HearRunRangeBonus;
        }
        public float hearWalkBase;
        public float hearWalkRange;
        public float hearWalkRangeBonus;
        public float hearRunBase;
        public float hearRunRange;
        public float hearRunRangeBonus;
    }
    public struct lightSense
    {
        public lightSense(float LightBase, float LightRange, float LightRangeBonus)
        {
            lightBase = LightBase;
            lightRange = LightRange;
            lightRangeBonus = LightRangeBonus;
        }
        public float lightBase;
        public float lightRange;
        public float lightRangeBonus;
    }
    public struct speed
    {
        public speed(float Patrol, float Suspicous, float Alerted, float Hunting, float Seeking)
        {
            patrolSpeed = Patrol;
            suspiciousSpeed = Suspicous;
            alertedSpeed = Alerted;
            huntingSpeed = Hunting;
            seekingSpeed = Seeking;
        }
        public float patrolSpeed;
        public float suspiciousSpeed;
        public float alertedSpeed;
        public float huntingSpeed;
        public float seekingSpeed;
    }
    public struct roaming
    {
        public roaming(float PatrolHideCheckProbability, float PatrolLookAroundProbability, float AlertedHideCheckProbability, float AlertedPathChance, 
            int AlertedMaxPathRecursion, float SeekingHideCheckProbability, float SeekingPathChance, int SeekingMaxPathRecursion)
        {
            patrolHideCheckProbability = PatrolHideCheckProbability;
            patrolLookAroundProbability = PatrolLookAroundProbability;
            alertedHideCheckProbability = AlertedHideCheckProbability;
            alertedPathChance = AlertedPathChance;
            alertedMaxPathRecursion = AlertedMaxPathRecursion;
            seekingHideCheckProbability = SeekingHideCheckProbability;
            seekingPathChance = SeekingPathChance;
            seekingMaxPathRecursion = SeekingMaxPathRecursion;
        }
        public float patrolHideCheckProbability;
        public float patrolLookAroundProbability;
        public float alertedHideCheckProbability;
        public float alertedPathChance;
        public int alertedMaxPathRecursion;
        public float seekingHideCheckProbability;
        public float seekingPathChance;
        public int seekingMaxPathRecursion;
    }
    public struct AIStruct
    {
        public AIStruct(sight Sight, hearing Hearing, lightSense LightSense, speed Speed, roaming Roaming)
        {
            sight = Sight;
            hearing = Hearing;
            lightSense = LightSense;
            speed = Speed;
            roaming = Roaming;
        }
        public hearing hearing;
        public sight sight;
        public lightSense lightSense;
        public speed speed;
        public roaming roaming;
    }
    public struct MazeStrcut
    {
        public MazeStrcut(int MazeSize, int AICount, float HideDensity, int ChestCount, Color FogColor, float Visibility)
        {
            mazeSize = MazeSize;
            aiCount = AICount;
            hideDensity = HideDensity;
            chestCount = ChestCount;
            fogColor = FogColor;
            visibility = Visibility;
        }
        public int mazeSize;
        public int aiCount;
        public float hideDensity;
        public int chestCount;
        public Color fogColor;
        public float visibility;
    }
    public struct Settings
    {
        public Settings(bool DynamicCulling, bool PathFindingUseCaching)
        {
            dynamicCulling = DynamicCulling;
            pathFindingUseCaching = PathFindingUseCaching;
        }
        public bool dynamicCulling;
        public bool pathFindingUseCaching;
    }

    //AI PRESETS
    public static sight[] sightDifficulties = { new sight(0, 0, 0, 1), new sight(20, 3.5f, 20, 40), new sight(20, 4, 30, 45), new sight(25, 5, 35, 65), new sight(30, 6.5f, 40, 100), new sight(30, 9, 55, 120) };
    public static hearing[] hearingDifficulties = { new hearing(0, 0, 0, 0, 0, 0), new hearing(12.5f, 1, 10,15,4,12.5f), new hearing(17.5f, 1, 12.5f,17.5f,7,15), new hearing(17.5f,2,15,20,9,20), new hearing(17.5f,3,20,22.5f,12,25), new hearing(20,4,35,25,16,45) } ; //TODO HERE
    public static lightSense[] lightSenseDifficulties = { new lightSense(0, 0, 0), new lightSense(10, 1.75f, 7.5f), new lightSense(15, 3f, 12.5f), new lightSense(20, 5, 17.5f), new lightSense(25, 8, 22.5f), new lightSense(30, 12, 50) };
    public static speed[] speedsDifficulties = { new speed(0.3f, 0.4f, 0.5f, 0.7f, 0.4f) ,new speed(0.7f, 0.9f, 1.25f, 1.5f, 0.8f), new speed(0.9f, 1.2f, 1.4f, 1.9f, 1f), new speed(1.1f, 1.4f, 1.8f, 2.3f, 1.2f), new speed(1.4f, 1.7f, 2.3f, 2.5f, 1.5f), new speed(2, 2.2f, 2.5f, 3, 2.1f) };
    public static roaming[] roamingDifficulties = { new roaming(0, 0, 0, 0, 0, 0, 25, 1), new roaming(5, 15, 20, 20, 1, 35, 60, 1), new roaming(10, 30, 35, 40, 1, 60, 80, 2), new roaming(15, 50, 50, 70, 2, 80, 75, 2), new roaming(30, 80, 75, 85, 2, 100, 90, 3) };


    //PARAMETERS
    public static AIStruct AI = new AIStruct(sightDifficulties[3], hearingDifficulties[3], lightSenseDifficulties[3], speedsDifficulties[3], roamingDifficulties[3]);
    public static MazeStrcut maze = new MazeStrcut(25, 1, 100, 15, new Color32(10,0,0,25), 8);
    public static Settings settings = new Settings(true, false);
}