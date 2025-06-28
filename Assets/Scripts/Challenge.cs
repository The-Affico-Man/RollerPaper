using UnityEngine;

public enum ChallengeType
{
    ScrollTotalDistance, ScrollInOneRun, ScrollTotalTime, ScrollTimeInOneRun,
    UseMultipleFingers, ScrollWithoutLift, ChangePawSkin, ChangePaperSkin,
    UseDifferentPawSkins, UseDifferentPaperSkins, UseAnyBoost, UseSpecificBoost,
    EarnCoins, Login, VisitShop, VisitMilestones, WatchRewardedAd,
    CompleteDailies, PlayOnDifferentDays,
}

[CreateAssetMenu(fileName = "NewChallenge", menuName = "Challenges/Create New Challenge")]
public class Challenge : ScriptableObject
{
    [Header("Core Info")]
    public string challengeID;
    public ChallengeType type;
    [TextArea(2, 4)]
    public string description;

    [Header("Goal & Reward")]
    public float goal = 1;
    public int coinReward = 100;

    [Header("Categorization")]
    public bool isDaily = true;
    public bool isWeekly = false;

    [Header("Type-Specific Data")]
    public string stringParameter;
}