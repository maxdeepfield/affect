using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles online leaderboard for Weekly Stage mode.
/// Abstracted interface - can plug in Steam, PlayFab, custom backend, etc.
/// </summary>
public class WeeklyLeaderboard : MonoBehaviour
{
    public static WeeklyLeaderboard Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private string backendUrl = "https://your-backend.com/api/weekly";
    [SerializeField] private float requestTimeout = 10f;
    [SerializeField] private bool offlineMode = true; // Start offline, enable when backend ready

    [Header("Events")]
    public UnityEvent<LeaderboardEntry[]> OnLeaderboardLoaded = new UnityEvent<LeaderboardEntry[]>();
    public UnityEvent<int> OnScoreSubmitted = new UnityEvent<int>(); // returns rank
    public UnityEvent<string> OnError = new UnityEvent<string>();

    // Local cache
    private Dictionary<string, List<LeaderboardEntry>> localLeaderboards = new Dictionary<string, List<LeaderboardEntry>>();
    private Dictionary<string, int> localBestScores = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLocalData();
    }

    private void Start()
    {
        // Subscribe to weekly run completion
        if (WeeklyStageSystem.Instance != null)
        {
            WeeklyStageSystem.Instance.OnRunDataReady.AddListener(OnRunCompleted);
        }
    }

    private void OnDestroy()
    {
        if (WeeklyStageSystem.Instance != null)
        {
            WeeklyStageSystem.Instance.OnRunDataReady.RemoveListener(OnRunCompleted);
        }

        if (Instance == this)
            Instance = null;
    }

    // ═══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches leaderboard for current week.
    /// </summary>
    public void FetchLeaderboard()
    {
        if (WeeklyStageSystem.Instance == null) return;
        FetchLeaderboard(WeeklyStageSystem.Instance.WeeklyId);
    }

    /// <summary>
    /// Fetches leaderboard for specific week.
    /// </summary>
    public void FetchLeaderboard(string weeklyId)
    {
        if (offlineMode)
        {
            // Return local data
            var entries = GetLocalLeaderboard(weeklyId);
            OnLeaderboardLoaded.Invoke(entries);
            return;
        }

        StartCoroutine(FetchLeaderboardCoroutine(weeklyId));
    }

    /// <summary>
    /// Submits score to leaderboard.
    /// </summary>
    public void SubmitScore(WeeklyRunData runData, string playerName)
    {
        if (offlineMode)
        {
            // Store locally
            SaveLocalScore(runData.weeklyId, playerName, runData.score);
            int rank = GetLocalRank(runData.weeklyId, runData.score);
            OnScoreSubmitted.Invoke(rank);
            return;
        }

        StartCoroutine(SubmitScoreCoroutine(runData, playerName));
    }

    /// <summary>
    /// Gets player's best score for a week.
    /// </summary>
    public int GetBestScore(string weeklyId)
    {
        if (localBestScores.TryGetValue(weeklyId, out int score))
            return score;
        return 0;
    }

    /// <summary>
    /// Gets player's rank for current best score.
    /// </summary>
    public int GetLocalRank(string weeklyId, int score)
    {
        var entries = GetLocalLeaderboard(weeklyId);
        int rank = 1;
        foreach (var entry in entries)
        {
            if (entry.score > score)
                rank++;
        }
        return rank;
    }

    // ═══════════════════════════════════════════════════════════════
    // LOCAL STORAGE
    // ═══════════════════════════════════════════════════════════════

    private void LoadLocalData()
    {
        // Load best scores from PlayerPrefs
        // Format: WeeklyBest_2025-W03 = 12500
        // This is a simple implementation - could use JSON file for full leaderboard
    }

    private LeaderboardEntry[] GetLocalLeaderboard(string weeklyId)
    {
        if (localLeaderboards.TryGetValue(weeklyId, out var list))
        {
            list.Sort((a, b) => b.score.CompareTo(a.score));
            return list.ToArray();
        }
        return new LeaderboardEntry[0];
    }

    private void SaveLocalScore(string weeklyId, string playerName, int score)
    {
        // Update best score
        if (!localBestScores.ContainsKey(weeklyId) || score > localBestScores[weeklyId])
        {
            localBestScores[weeklyId] = score;
            PlayerPrefs.SetInt($"WeeklyBest_{weeklyId}", score);
            PlayerPrefs.Save();
        }

        // Add to local leaderboard
        if (!localLeaderboards.ContainsKey(weeklyId))
        {
            localLeaderboards[weeklyId] = new List<LeaderboardEntry>();
        }

        localLeaderboards[weeklyId].Add(new LeaderboardEntry
        {
            playerName = playerName,
            score = score,
            weeklyId = weeklyId
        });

        // Keep only top 100
        localLeaderboards[weeklyId].Sort((a, b) => b.score.CompareTo(a.score));
        if (localLeaderboards[weeklyId].Count > 100)
        {
            localLeaderboards[weeklyId].RemoveRange(100, localLeaderboards[weeklyId].Count - 100);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // NETWORK (placeholder for real backend)
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator FetchLeaderboardCoroutine(string weeklyId)
    {
        string url = $"{backendUrl}/leaderboard?week={weeklyId}";

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            request.timeout = (int)requestTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Parse JSON response
                // LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
                // OnLeaderboardLoaded.Invoke(response.entries);
                
                // For now, return empty
                OnLeaderboardLoaded.Invoke(new LeaderboardEntry[0]);
            }
            else
            {
                Debug.LogWarning($"[WeeklyLeaderboard] Fetch failed: {request.error}");
                OnError.Invoke(request.error);
                
                // Fallback to local
                var entries = GetLocalLeaderboard(weeklyId);
                OnLeaderboardLoaded.Invoke(entries);
            }
        }
    }

    private IEnumerator SubmitScoreCoroutine(WeeklyRunData runData, string playerName)
    {
        string url = $"{backendUrl}/submit";

        var submitData = new ScoreSubmission
        {
            weeklyId = runData.weeklyId,
            seed = runData.seed,
            playerName = playerName,
            score = runData.score,
            floorsReached = runData.floorsReached,
            enemiesKilled = runData.enemiesKilled,
            secretsFound = runData.secretsFound,
            timeSeconds = (int)(runData.endTime - runData.startTime).TotalSeconds
        };

        string json = JsonUtility.ToJson(submitData);

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Post(url, json, "application/json"))
        {
            request.timeout = (int)requestTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Parse rank from response
                // SubmitResponse response = JsonUtility.FromJson<SubmitResponse>(request.downloadHandler.text);
                // OnScoreSubmitted.Invoke(response.rank);
                
                OnScoreSubmitted.Invoke(1); // Placeholder
            }
            else
            {
                Debug.LogWarning($"[WeeklyLeaderboard] Submit failed: {request.error}");
                OnError.Invoke(request.error);
                
                // Save locally anyway
                SaveLocalScore(runData.weeklyId, playerName, runData.score);
                int rank = GetLocalRank(runData.weeklyId, runData.score);
                OnScoreSubmitted.Invoke(rank);
            }
        }
    }

    private void OnRunCompleted(WeeklyRunData data)
    {
        // Auto-submit with default name (could prompt for name)
        string playerName = PlayerPrefs.GetString("PlayerName", "Anonymous");
        SubmitScore(data, playerName);
    }

    [System.Serializable]
    private class ScoreSubmission
    {
        public string weeklyId;
        public int seed;
        public string playerName;
        public int score;
        public int floorsReached;
        public int enemiesKilled;
        public int secretsFound;
        public int timeSeconds;
    }
}
