using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// WEEKLY STAGE MODE - Separate competitive mode with shared seeds.
/// 
/// NOT a gate for main progression!
/// Everyone plays the same building each week, competes on leaderboard.
/// 
/// Main Game (AFFECT/RUN): Normal keycard progression, no time gates
/// Weekly Stage: Fixed seed, fixed rules, online leaderboard
/// </summary>
public class WeeklyStageSystem : MonoBehaviour
{
    public static WeeklyStageSystem Instance { get; private set; }

    [Header("Weekly Seed")]
    [Tooltip("Use interesting hand-picked seeds instead of auto-generated")]
    [SerializeField] private bool useHandPickedSeeds = true;
    [SerializeField] private int[] interestingSeeds = new int[]
    {
        48291573,  // Week 1 - Classic layout
        73629184,  // Week 2 - Maze-heavy
        19384756,  // Week 3 - Open halls
        82947361,  // Week 4 - Vertical focus
        56281934,  // Week 5 - Cramped corridors
        37492816,  // Week 6 - Symmetrical
        91827364,  // Week 7 - Chaotic
        64738291,  // Week 8 - Long corridors
    };

    [Header("Weekly Rules")]
    [Tooltip("Fixed keycard level for fair competition")]
    [SerializeField] private int fixedKeycardLevel = 2;
    [SerializeField] private WeeklyModifier[] weeklyModifiers;

    [Header("Scoring")]
    [SerializeField] private int pointsPerFloor = 1000;
    [SerializeField] private int pointsPerKill = 50;
    [SerializeField] private int pointsPerSecret = 200;
    [SerializeField] private int penaltyPerMinute = 20;
    [SerializeField] private int penaltyPerDamage = 5;

    [Header("Events")]
    public UnityEvent<int, int> OnWeekChanged = new UnityEvent<int, int>(); // year, week
    public UnityEvent<int> OnRunCompleted = new UnityEvent<int>(); // score
    public UnityEvent<WeeklyRunData> OnRunDataReady = new UnityEvent<WeeklyRunData>();

    // Current week data
    private int currentYear;
    private int currentWeek;
    private int currentSeed;
    private bool isWeeklyRun;
    private WeeklyRunData currentRun;

    public int CurrentSeed => currentSeed;
    public int CurrentWeek => currentWeek;
    public int CurrentYear => currentYear;
    public string WeeklyId => $"{currentYear}-W{currentWeek:D2}";
    public int FixedKeycardLevel => fixedKeycardLevel;
    public bool IsWeeklyRun => isWeeklyRun;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CalculateCurrentWeek();
    }

    private void Start()
    {
        // Check for week change every minute
        InvokeRepeating(nameof(CheckWeekChange), 60f, 60f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Calculates current week number and generates seed.
    /// </summary>
    private void CalculateCurrentWeek()
    {
        DateTime now = DateTime.UtcNow;
        currentYear = now.Year;
        currentWeek = GetISOWeek(now);
        currentSeed = GenerateSeedForWeek(currentYear, currentWeek);
    }

    /// <summary>
    /// Gets ISO week number (Monday-based weeks).
    /// </summary>
    private int GetISOWeek(DateTime date)
    {
        // ISO 8601 week calculation
        DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
    }

    /// <summary>
    /// Generates deterministic seed for a specific week.
    /// </summary>
    private int GenerateSeedForWeek(int year, int week)
    {
        if (useHandPickedSeeds && interestingSeeds.Length > 0)
        {
            // Cycle through interesting seeds
            int index = (year * 52 + week) % interestingSeeds.Length;
            return interestingSeeds[index];
        }

        // Deterministic hash from year + week
        string weekId = $"{year}-W{week}";
        return GetStableHash(weekId);
    }

    private int GetStableHash(string str)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in str)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash);
        }
    }

    private void CheckWeekChange()
    {
        int prevWeek = currentWeek;
        int prevYear = currentYear;
        
        CalculateCurrentWeek();

        if (currentWeek != prevWeek || currentYear != prevYear)
        {
            OnWeekChanged.Invoke(currentYear, currentWeek);
            Debug.Log($"[WeeklyStage] New week! {WeeklyId} - Seed: {currentSeed}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // WEEKLY RUN MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Starts a new weekly run. Call this when entering Weekly Stage mode.
    /// </summary>
    public void StartWeeklyRun()
    {
        isWeeklyRun = true;
        currentRun = new WeeklyRunData
        {
            weeklyId = WeeklyId,
            seed = currentSeed,
            startTime = DateTime.UtcNow,
            floorsReached = 0,
            enemiesKilled = 0,
            secretsFound = 0,
            damageTaken = 0
        };

        // Apply fixed keycard level
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.UpgradeKeycard(fixedKeycardLevel);
        }

        // Seed the random for building generation
        UnityEngine.Random.InitState(currentSeed);

        Debug.Log($"[WeeklyStage] Run started - {WeeklyId} - Seed: {currentSeed}");
    }

    /// <summary>
    /// Ends the weekly run and calculates score.
    /// </summary>
    public void EndWeeklyRun()
    {
        if (!isWeeklyRun || currentRun == null) return;

        currentRun.endTime = DateTime.UtcNow;
        currentRun.score = CalculateScore(currentRun);

        isWeeklyRun = false;

        OnRunCompleted.Invoke(currentRun.score);
        OnRunDataReady.Invoke(currentRun);

        Debug.Log($"[WeeklyStage] Run complete! Score: {currentRun.score}");
    }

    /// <summary>
    /// Calculates final score from run data.
    /// </summary>
    public int CalculateScore(WeeklyRunData run)
    {
        float minutes = (float)(run.endTime - run.startTime).TotalMinutes;

        int score = 0;
        score += run.floorsReached * pointsPerFloor;
        score += run.enemiesKilled * pointsPerKill;
        score += run.secretsFound * pointsPerSecret;
        score -= (int)(minutes * penaltyPerMinute);
        score -= run.damageTaken * penaltyPerDamage;

        return Mathf.Max(0, score);
    }

    // ═══════════════════════════════════════════════════════════════
    // RUN TRACKING (call these during gameplay)
    // ═══════════════════════════════════════════════════════════════

    public void RecordFloorReached(int floor)
    {
        if (currentRun != null)
            currentRun.floorsReached = Mathf.Max(currentRun.floorsReached, floor);
    }

    public void RecordKill()
    {
        if (currentRun != null)
            currentRun.enemiesKilled++;
    }

    public void RecordSecret()
    {
        if (currentRun != null)
            currentRun.secretsFound++;
    }

    public void RecordDamage(int amount)
    {
        if (currentRun != null)
            currentRun.damageTaken += amount;
    }

    // ═══════════════════════════════════════════════════════════════
    // TIME DISPLAY
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns time until next week's seed changes.
    /// </summary>
    public TimeSpan TimeUntilNextWeek()
    {
        DateTime now = DateTime.UtcNow;
        // Next Monday 00:00 UTC
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        
        DateTime nextMonday = now.Date.AddDays(daysUntilMonday);
        return nextMonday - now;
    }

    public string FormatTimeUntilNextWeek()
    {
        TimeSpan remaining = TimeUntilNextWeek();

        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
        
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        
        return $"{remaining.Minutes}m";
    }

    /// <summary>
    /// Gets the weekly modifier for current week (if any).
    /// </summary>
    public WeeklyModifier GetCurrentModifier()
    {
        if (weeklyModifiers == null || weeklyModifiers.Length == 0)
            return null;

        int index = (currentYear * 52 + currentWeek) % weeklyModifiers.Length;
        return weeklyModifiers[index];
    }
}

/// <summary>
/// Data for a single weekly run attempt.
/// </summary>
[System.Serializable]
public class WeeklyRunData
{
    public string weeklyId;
    public int seed;
    public DateTime startTime;
    public DateTime endTime;
    public int floorsReached;
    public int enemiesKilled;
    public int secretsFound;
    public int damageTaken;
    public int score;

    /// <summary>
    /// Converts to JSON for leaderboard submission.
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}

/// <summary>
/// Optional weekly modifiers for variety.
/// </summary>
[System.Serializable]
public class WeeklyModifier
{
    public string name = "Standard";
    public string description = "No modifiers";
    
    [Header("Enemy Modifiers")]
    public float enemyHealthMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public int extraSpiders = 0;

    [Header("Player Modifiers")]
    public float playerDamageMultiplier = 1f;
    public int startingAmmo = 90;
    public int startingHealth = 100;

    [Header("Building Modifiers")]
    public int extraFloors = 0;
    public float roomSizeMultiplier = 1f;
    public bool darknessMode = false;
    public bool noMinimap = false;
}
