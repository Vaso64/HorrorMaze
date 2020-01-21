using UnityEngine;
static public class GameParameters
{
    //STRUCTS 
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
        public roaming(float PatrolHideCheckProbability, float PatrolLookAroundProbability, float AlertedHideCheckProbability, float AlertedLookAroundProbability,
            int AlertedMaxPathRecursion, float SeekingHideCheckProbability, float SeekingLookAroundProbability, int SeekingMaxPathRecursion)
        {
            patrolHideCheckProbability = PatrolHideCheckProbability;
            patrolLookAroundProbability = PatrolLookAroundProbability;
            alertedHideCheckProbability = AlertedHideCheckProbability;
            alertedLookAroundProbability = AlertedLookAroundProbability;
            alertedMaxPathRecursion = AlertedMaxPathRecursion;
            seekingHideCheckProbability = SeekingHideCheckProbability;
            seekingLookAroundProbability = SeekingLookAroundProbability;
            seekingMaxPathRecursion = SeekingMaxPathRecursion;
        }
        public float patrolHideCheckProbability;
        public float patrolLookAroundProbability;
        public float alertedHideCheckProbability;
        public float alertedLookAroundProbability;
        public int alertedMaxPathRecursion;
        public float seekingHideCheckProbability;
        public float seekingLookAroundProbability;
        public int seekingMaxPathRecursion;
    }
    //ENVIROMENT
    public static Color fogColor = new Color32(20, 20, 20, 255);
    public static float visibility = 3.5f;
    public static float torchVisibility = 6;
    //MAZE
    public static int size = 18;
    public static int AICount = 1;
    public static int hideDensity = 25;
    public static int chestCount = 10;
    //AI PRESETS 
    //TODO
    public static hearing[] hearingDifficulties;
    public static sight[] sightDifficulties;
    public static speed[] speedsDifficulties;
    public static roaming[] roamingDifficulties;
    //AI MAIN STRUCT PARAMETER
    public struct AI
    {
        public AI(hearing Hearing, sight Sight, lightSense LightSense, speed Speed, roaming Roaming)
        {
            hearing = Hearing;
            sight = Sight;
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
}