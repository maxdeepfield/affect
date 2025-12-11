using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI for Weekly Stage mode.
/// Shows current week, seed, countdown to next week, and leaderboard.
/// </summary>
public class WeeklyStageUI : MonoBehaviour
{
    [Header("Week Info")]
    [SerializeField] private TMP_Text weekTitleText;
    [SerializeField] private TMP_Text seedText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text modifierText;

    [Header("Score Display")]
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text rankText;

    [Header("Run Stats")]
    [SerializeField] private TMP_Text floorsText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text timeText;

    [Header("Leaderboard")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private int maxLeaderboardEntries = 10;

    [Header("End Run Screen")]
    [SerializeField] private GameObject endRunPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text scoreBreakdownText;
    [SerializeField] private TMP_Text newBestText;

    [Header("Formatting")]
    [SerializeField] private string weekFormat = "WEEKLY STAGE — {0}";
    [SerializeField] private string seedFormat = "SEED: {0}";
    [SerializeField] private string countdownFormat = "NEXT SEED IN {0}";
    [SerializeField] private string scoreFormat = "SCORE: {0:N0}";
    [SerializeField] private string bestFormat = "BEST: {0:N0}";

    private int localBestScore;
    private float runStartTime;

    private void Start()
    {
        if (WeeklyStageSystem.Instance != null)
        {
            WeeklyStageSystem.Instance.OnRunCompleted.AddListener(OnRunCompleted);
            WeeklyStageSystem.Instance.OnRunDataReady.AddListener(OnRunDataReady);
        }

        LoadLocalBest();
        UpdateWeekInfo();
        
        if (endRunPanel != null)
            endRunPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (WeeklyStageSystem.Instance != null)
        {
            WeeklyStageSystem.Instance.OnRunCompleted.RemoveListener(OnRunCompleted);
            WeeklyStageSystem.Instance.OnRunDataReady.RemoveListener(OnRunDataReady);
        }
    }

    private void Update()
    {
        UpdateCountdown();
        UpdateRunStats();
    }

    private void UpdateWeekInfo()
    {
        if (WeeklyStageSystem.Instance == null) return;

        var weekly = WeeklyStageSystem.Instance;

        if (weekTitleText != null)
            weekTitleText.text = string.Format(weekFormat, weekly.WeeklyId);

        if (seedText != null)
            seedText.text = string.Format(seedFormat, weekly.CurrentSeed);

        if (bestScoreText != null)
            bestScoreText.text = string.Format(bestFormat, localBestScore);

        // Show modifier if any
        var modifier = weekly.GetCurrentModifier();
        if (modifierText != null)
        {
            if (modifier != null && modifier.name != "Standard")
            {
                modifierText.text = $"MODIFIER: {modifier.name}";
                modifierText.gameObject.SetActive(true);
            }
            else
            {
                modifierText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateCountdown()
    {
        if (countdownText == null || WeeklyStageSystem.Instance == null) return;

        string timeStr = WeeklyStageSystem.Instance.FormatTimeUntilNextWeek();
        countdownText.text = string.Format(countdownFormat, timeStr);
    }

    private void UpdateRunStats()
    {
        if (WeeklyStageSystem.Instance == null || !WeeklyStageSystem.Instance.IsWeeklyRun)
            return;

        // Update time
        if (timeText != null && runStartTime > 0)
        {
            float elapsed = Time.time - runStartTime;
            TimeSpan ts = TimeSpan.FromSeconds(elapsed);
            timeText.text = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    /// <summary>
    /// Call when weekly run starts.
    /// </summary>
    public void OnRunStarted()
    {
        runStartTime = Time.time;
        
        if (scorePanel != null)
            scorePanel.SetActive(true);
        
        if (endRunPanel != null)
            endRunPanel.SetActive(false);

        if (currentScoreText != null)
            currentScoreText.text = string.Format(scoreFormat, 0);
    }

    /// <summary>
    /// Updates live score during run.
    /// </summary>
    public void UpdateLiveScore(int floors, int kills, int secrets)
    {
        if (floorsText != null)
            floorsText.text = $"FLOOR {floors}";

        if (killsText != null)
            killsText.text = $"KILLS {kills}";

        // Rough live score estimate
        int estimate = floors * 1000 + kills * 50 + secrets * 200;
        if (currentScoreText != null)
            currentScoreText.text = string.Format(scoreFormat, estimate);
    }

    private void OnRunCompleted(int score)
    {
        // Check for new best
        bool isNewBest = score > localBestScore;
        if (isNewBest)
        {
            localBestScore = score;
            SaveLocalBest();
        }

        if (newBestText != null)
        {
            newBestText.gameObject.SetActive(isNewBest);
            if (isNewBest)
                newBestText.text = "★ NEW BEST! ★";
        }

        if (finalScoreText != null)
            finalScoreText.text = string.Format(scoreFormat, score);

        if (endRunPanel != null)
            endRunPanel.SetActive(true);
    }

    private void OnRunDataReady(WeeklyRunData data)
    {
        if (scoreBreakdownText == null) return;

        float minutes = (float)(data.endTime - data.startTime).TotalMinutes;

        string breakdown = $"Floors: {data.floorsReached} × 1000 = {data.floorsReached * 1000}\n" +
                          $"Kills: {data.enemiesKilled} × 50 = {data.enemiesKilled * 50}\n" +
                          $"Secrets: {data.secretsFound} × 200 = {data.secretsFound * 200}\n" +
                          $"Time: -{(int)(minutes * 20)}\n" +
                          $"Damage: -{data.damageTaken * 5}";

        scoreBreakdownText.text = breakdown;
    }

    // ═══════════════════════════════════════════════════════════════
    // LOCAL STORAGE
    // ═══════════════════════════════════════════════════════════════

    private void LoadLocalBest()
    {
        if (WeeklyStageSystem.Instance == null) return;
        
        string key = $"WeeklyBest_{WeeklyStageSystem.Instance.WeeklyId}";
        localBestScore = PlayerPrefs.GetInt(key, 0);
    }

    private void SaveLocalBest()
    {
        if (WeeklyStageSystem.Instance == null) return;
        
        string key = $"WeeklyBest_{WeeklyStageSystem.Instance.WeeklyId}";
        PlayerPrefs.SetInt(key, localBestScore);
        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════════
    // LEADERBOARD (placeholder for online integration)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Populates leaderboard with entries.
    /// Call this after fetching from server.
    /// </summary>
    public void PopulateLeaderboard(LeaderboardEntry[] entries)
    {
        if (leaderboardContent == null || leaderboardEntryPrefab == null) return;

        // Clear existing
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }

        // Add entries
        int count = Mathf.Min(entries.Length, maxLeaderboardEntries);
        for (int i = 0; i < count; i++)
        {
            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            
            // Assuming entry has TMP_Text children for rank, name, score
            var texts = entry.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = $"#{i + 1}";
                texts[1].text = entries[i].playerName;
                texts[2].text = entries[i].score.ToString("N0");
            }
        }
    }

    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }

    public void HideLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }
}

/// <summary>
/// Single leaderboard entry.
/// </summary>
[System.Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string playerName;
    public int score;
    public string weeklyId;
}
