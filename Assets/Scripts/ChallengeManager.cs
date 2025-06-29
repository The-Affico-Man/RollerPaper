using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChallengeState
{
    public Challenge challenge;
    public float progress;
    public bool IsComplete() { return progress >= challenge.goal - 0.001f; }
}

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance { get; private set; }

    [Header("Challenge Data")]
    public List<Challenge> allDailyChallenges;
    public List<Challenge> allWeeklyChallenges;

    [Header("Challenge Slots")]
    public int numDailySlots = 3;
    public int numWeeklySlots = 3;

    public List<ChallengeState> ActiveDailies { get; private set; }
    public List<ChallengeState> ActiveWeeklies { get; private set; }

    private float scrollDistanceThisRun = 0;
    private float continuousScrollTime = 0;
    private HashSet<string> usedPawSkinsThisWeek;
    private HashSet<string> usedPaperSkinsThisWeek;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
        usedPawSkinsThisWeek = new HashSet<string>();
        usedPaperSkinsThisWeek = new HashSet<string>();
    }

    // REMOVED Start(). Loading is now initiated by GameDataManager.

    // This is the NEW public load method called by GameDataManager
    [ContextMenu("Load Challenge Data")]
    public void LoadProgress()
    {
        InitializeChallenges();
    }

    // This is the NEW public save method called by GameDataManager
    [ContextMenu("Save Challenge Data")]
    public void SaveProgress()
    {
        SaveChallengeProgress();
    }

    private void InitializeChallenges()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogWarning("TimeManager not found. Loading challenges without checking for reset.");
            LoadAllChallengeData();
            return;
        }

        DateTime currentTime = TimeManager.Instance.GetCurrentTime();
        string anchorDateString = PlayerPrefs.GetString(PlayerPrefsKeys.ChallengeAnchorDate, "");
        DateTime anchorDate;

        if (string.IsNullOrEmpty(anchorDateString))
        {
            anchorDate = currentTime;
            PlayerPrefs.SetString(PlayerPrefsKeys.ChallengeAnchorDate, anchorDate.ToBinary().ToString());
        }
        else
        {
            anchorDate = DateTime.FromBinary(Convert.ToInt64(anchorDateString));
        }

        int lastDailyReset = PlayerPrefs.GetInt(PlayerPrefsKeys.LastDailyResetDay, -1);
        int lastWeeklyReset = PlayerPrefs.GetInt(PlayerPrefsKeys.LastWeeklyResetWeek, -1);
        int currentDayNumber = (int)(currentTime - anchorDate).TotalDays;
        int currentWeekNumber = (int)(currentDayNumber / 7);

        bool needsDailyReset = currentDayNumber > lastDailyReset;
        bool needsWeeklyReset = currentWeekNumber > lastWeeklyReset;

        if (!TimeManager.Instance.HasSecureTime())
        {
            Debug.LogWarning("No secure time. Skipping reset check for development.");
            needsDailyReset = false;
            needsWeeklyReset = false;
        }

        if (needsWeeklyReset)
        {
            Debug.Log("New week detected! Resetting weekly challenges.");
            PlayerPrefs.SetInt(PlayerPrefsKeys.LastWeeklyResetWeek, currentWeekNumber);
            ResetWeeklyChallenges();
        }
        if (needsDailyReset)
        {
            Debug.Log("New day detected! Resetting daily challenges.");
            PlayerPrefs.SetInt(PlayerPrefsKeys.LastDailyResetDay, currentDayNumber);
            UpdateDaysPlayedThisWeek(currentDayNumber);
            ResetDailyChallenges();
        }

        LoadAllChallengeData();

        if (needsDailyReset)
        {
            UpdateChallengeProgress(ChallengeType.Login);
        }
    }

    #region --- Challenge Data Management ---

    public bool IsRewardClaimed(Challenge challenge)
    {
        if (challenge == null) return false;
        string claimedKey = challenge.isDaily ? "ClaimedDailies" : "ClaimedWeeklies";
        string claimedIDs = PlayerPrefs.GetString(claimedKey, "");

        if (string.IsNullOrEmpty(claimedIDs)) return false;

        // --- THIS IS THE ROBUST FIX ---
        // We create a list from the saved string and check if the list contains our ID.
        // This prevents partial matches (e.g., "ID1" accidentally matching "ID10").
        List<string> claimedList = new List<string>(claimedIDs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        return claimedList.Contains(challenge.challengeID);
        // --- END OF FIX ---
    }


    private void ResetDailyChallenges()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.ActiveDailyIDs);
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.ActiveDailyProgress);
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.ClaimedDailies);
        PickNewChallenges(true, numDailySlots);
    }

    private void ResetWeeklyChallenges()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.ActiveWeeklyIDs);
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.ActiveWeeklyProgress);
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.ClaimedWeeklies);
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.DaysPlayedThisWeek);
        PlayerPrefs.SetInt(PlayerPrefsKeys.CompletedDailiesThisWeek, 0);
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.UsedPawSkinsThisWeek);
        usedPawSkinsThisWeek.Clear();
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.UsedPaperSkinsThisWeek);
        usedPaperSkinsThisWeek.Clear();
        PickNewChallenges(false, numWeeklySlots);
    }

    private void PickNewChallenges(bool isDaily, int count)
    {
        List<Challenge> sourcePool = isDaily ? allDailyChallenges : allWeeklyChallenges;
        var filteredPool = sourcePool.Where(c => (isDaily && c.isDaily) || (!isDaily && c.isWeekly)).ToList();

        var available = new List<Challenge>(filteredPool);
        var chosenIDs = new List<string>();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, available.Count);
            chosenIDs.Add(available[randomIndex].challengeID);
            available.RemoveAt(randomIndex);
        }
        string key = isDaily ? PlayerPrefsKeys.ActiveDailyIDs : PlayerPrefsKeys.ActiveWeeklyIDs;
        PlayerPrefs.SetString(key, string.Join(",", chosenIDs));
        // PlayerPrefs.Save() is no longer called here; GameDataManager handles it.
    }

    private void LoadAllChallengeData()
    {
        ActiveDailies = LoadChallengeStates(true);
        ActiveWeeklies = LoadChallengeStates(false);

        string savedPawSkins = PlayerPrefs.GetString(PlayerPrefsKeys.UsedPawSkinsThisWeek, "");
        if (!string.IsNullOrEmpty(savedPawSkins)) { usedPawSkinsThisWeek = new HashSet<string>(savedPawSkins.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); }

        string savedPaperSkins = PlayerPrefs.GetString(PlayerPrefsKeys.UsedPaperSkinsThisWeek, "");
        if (!string.IsNullOrEmpty(savedPaperSkins)) { usedPaperSkinsThisWeek = new HashSet<string>(savedPaperSkins.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); }
    }

    private List<ChallengeState> LoadChallengeStates(bool isDaily)
    {
        var states = new List<ChallengeState>();
        string idsKey = isDaily ? PlayerPrefsKeys.ActiveDailyIDs : PlayerPrefsKeys.ActiveWeeklyIDs;
        string progressKey = isDaily ? PlayerPrefsKeys.ActiveDailyProgress : PlayerPrefsKeys.ActiveWeeklyProgress;
        string idsString = PlayerPrefs.GetString(idsKey, "");
        if (string.IsNullOrEmpty(idsString)) return states;
        List<string> ids = new List<string>(idsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        var sourcePool = isDaily ? allDailyChallenges : allWeeklyChallenges;
        Dictionary<string, float> progressDict = LoadProgressFromString(PlayerPrefs.GetString(progressKey, ""));
        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;
            Challenge challengeAsset = sourcePool.FirstOrDefault(c => c.challengeID == id);
            if (challengeAsset != null)
            {
                var state = new ChallengeState { challenge = challengeAsset, progress = 0 };
                if (progressDict.ContainsKey(id)) { state.progress = progressDict[id]; }
                states.Add(state);
            }
        }
        return states;
    }

    private void SaveChallengeProgress()
    {
        if (ActiveDailies != null)
        {
            var dailyProgress = new Dictionary<string, float>();
            foreach (var state in ActiveDailies) { dailyProgress[state.challenge.challengeID] = state.progress; }
            PlayerPrefs.SetString(PlayerPrefsKeys.ActiveDailyProgress, ConvertProgressToString(dailyProgress));
        }
        if (ActiveWeeklies != null)
        {
            var weeklyProgress = new Dictionary<string, float>();
            foreach (var state in ActiveWeeklies) { weeklyProgress[state.challenge.challengeID] = state.progress; }
            PlayerPrefs.SetString(PlayerPrefsKeys.ActiveWeeklyProgress, ConvertProgressToString(weeklyProgress));
        }
    }

    private string ConvertProgressToString(Dictionary<string, float> dict) { return string.Join(";", dict.Select(p => p.Key + ":" + p.Value)); }
    private Dictionary<string, float> LoadProgressFromString(string s) { var dict = new Dictionary<string, float>(); if (string.IsNullOrEmpty(s)) return dict; foreach (string part in s.Split(';')) { string[] keyValue = part.Split(':'); if (keyValue.Length == 2 && float.TryParse(keyValue[1], out float value)) { dict[keyValue[0]] = value; } } return dict; }

    private void UpdateDaysPlayedThisWeek(int currentDayNumber)
    {
        string daysPlayedString = PlayerPrefs.GetString(PlayerPrefsKeys.DaysPlayedThisWeek, "");
        var daysPlayed = new HashSet<string>(daysPlayedString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        if (daysPlayed.Add(currentDayNumber.ToString()))
        {
            PlayerPrefs.SetString(PlayerPrefsKeys.DaysPlayedThisWeek, string.Join(",", daysPlayed));
            UpdateChallengeProgress(ChallengeType.PlayOnDifferentDays, daysPlayed.Count);
        }
    }
    #endregion

    #region --- Public Progress Update Methods ---

    public void UpdateChallengeProgress(ChallengeType type, float amount = 1, string stringParam = "")
    {
        List<Challenge> newlyCompleted = new List<Challenge>();
        ProcessChallengeList(ActiveDailies, type, amount, stringParam, newlyCompleted);
        ProcessChallengeList(ActiveWeeklies, type, amount, stringParam, newlyCompleted);

        foreach (var challenge in newlyCompleted)
        {
            HandleCompletion(challenge);
        }

        // We no longer save immediately here. GameDataManager decides when to save.
        // SaveChallengeProgress(); 
    }

    // ... (ProcessChallengeList is unchanged) ...
    private void ProcessChallengeList(List<ChallengeState> states, ChallengeType type, float amount, string stringParam, List<Challenge> newlyCompleted) { if (states == null) return; foreach (var state in states) { if (state.challenge.type == type && !IsRewardClaimed(state.challenge) && !state.IsComplete()) { if (type == ChallengeType.UseSpecificBoost && !string.IsNullOrEmpty(state.challenge.stringParameter) && state.challenge.stringParameter != stringParam) { continue; } bool isSetType = type == ChallengeType.UseDifferentPawSkins || type == ChallengeType.UseDifferentPaperSkins || type == ChallengeType.ScrollInOneRun || type == ChallengeType.ScrollTimeInOneRun || type == ChallengeType.PlayOnDifferentDays || type == ChallengeType.CompleteDailies; if (isSetType) { state.progress = amount; } else { state.progress += amount; } if (state.IsComplete()) { state.progress = state.challenge.goal; newlyCompleted.Add(state.challenge); } } } }

    private void HandleCompletion(Challenge challenge)
    {
        Debug.Log($"CHALLENGE COMPLETE: {challenge.challengeID}");
        if (challenge.isDaily)
        {
            int completed = PlayerPrefs.GetInt(PlayerPrefsKeys.CompletedDailiesThisWeek, 0) + 1;
            PlayerPrefs.SetInt(PlayerPrefsKeys.CompletedDailiesThisWeek, completed);
            UpdateChallengeProgress(ChallengeType.CompleteDailies, completed);
        }
    }

    public void ClaimReward(Challenge challenge, Vector3 rewardSourcePosition)
    {
        // --- THIS IS THE ROBUST FIX ---
        // We check if it is *already* claimed BEFORE doing anything else.
        if (challenge == null || IsRewardClaimed(challenge)) return;
        // ----------------------------

        ChallengeState stateToClaim = challenge.isDaily
            ? ActiveDailies?.FirstOrDefault(s => s.challenge == challenge)
            : ActiveWeeklies?.FirstOrDefault(s => s.challenge == challenge);

        if (stateToClaim != null && stateToClaim.IsComplete())
        {
            // Give the reward with animation.
            CurrencyManager.Instance?.AddCoinsFromWorldPosition(challenge.coinReward, rewardSourcePosition);

            // Mark as claimed.
            string claimedKey = challenge.isDaily ? "ClaimedDailies" : "ClaimedWeeklies";
            string claimedIDs = PlayerPrefs.GetString(claimedKey, "");
            // We append the ID followed by a comma to ensure clean separation.
            PlayerPrefs.SetString(claimedKey, claimedIDs + challenge.challengeID + ",");
            PlayerPrefs.Save();
        }
    }

    public void UpdateScrollInOneRun(float distance, float time) { scrollDistanceThisRun += distance; continuousScrollTime += time; UpdateChallengeProgress(ChallengeType.ScrollInOneRun, scrollDistanceThisRun); UpdateChallengeProgress(ChallengeType.ScrollTimeInOneRun, continuousScrollTime); }
    public void OnScrollStopped() { continuousScrollTime = 0; }
    public void OnPawSkinChanged(string skinName) { if (usedPawSkinsThisWeek.Add(skinName)) { UpdateChallengeProgress(ChallengeType.UseDifferentPawSkins, usedPawSkinsThisWeek.Count); PlayerPrefs.SetString(PlayerPrefsKeys.UsedPawSkinsThisWeek, string.Join(",", usedPawSkinsThisWeek)); } }
    public void OnPaperSkinChanged(string skinName) { if (usedPaperSkinsThisWeek.Add(skinName)) { UpdateChallengeProgress(ChallengeType.UseDifferentPaperSkins, usedPaperSkinsThisWeek.Count); PlayerPrefs.SetString(PlayerPrefsKeys.UsedPaperSkinsThisWeek, string.Join(",", usedPaperSkinsThisWeek)); } }
    #endregion
}