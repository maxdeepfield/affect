# 🏆 Weekly Stage Online System

## Overview

Weekly Stage is a **competitive mode** where all players play the same procedurally generated building each week, competing for the best score on a shared leaderboard.

```
┌──────────────────────────────────────────────────────┐
│           WEEKLY STAGE — 2025-W03                    │
│           SEED: 48291573                             │
│           NEXT SEED IN 2d 14h                        │
├──────────────────────────────────────────────────────┤
│  #1  SpiderSlayer99      15,420                      │
│  #2  NoobMaster          12,850                      │
│  #3  YOU →               11,200  ★ NEW BEST          │
│  #4  xXDarkLordXx         9,340                      │
│  ...                                                 │
└──────────────────────────────────────────────────────┘
```

---

## 🎮 How It Works

### For Players

1. Select **WEEKLY STAGE** from main menu
2. Everyone gets the **same building** (same seed)
3. Fixed keycard level (fair competition)
4. Complete the run, get scored
5. Compare on leaderboard!

### Scoring Formula

```
SCORE = (Floors × 1000)
      + (Kills × 50)
      + (Secrets × 200)
      - (Minutes × 20)
      - (Damage Taken × 5)
```

**Strategy:** Balance speed vs thoroughness. Rushing = less time penalty but fewer kills/secrets.

---

## 🔧 Setup Guide

### 1. Scene Setup

Add these to your Weekly Stage scene:

```
WeeklyStageManager (GameObject)
├── WeeklyStageSystem (component)
├── WeeklyLeaderboard (component)
└── WeeklyStageUI (component, on Canvas)
```

### 2. WeeklyStageSystem Configuration

```csharp
[Header("Weekly Seed")]
useHandPickedSeeds = true          // Use curated interesting seeds
interestingSeeds = [48291573, ...]  // Your hand-picked seeds

[Header("Weekly Rules")]
fixedKeycardLevel = 2              // Everyone plays at this level

[Header("Scoring")]
pointsPerFloor = 1000
pointsPerKill = 50
pointsPerSecret = 200
penaltyPerMinute = 20
penaltyPerDamage = 5
```

### 3. WeeklyLeaderboard Configuration

```csharp
backendUrl = "https://your-api.com/weekly"  // Your backend
requestTimeout = 10f
offlineMode = true  // Start with this, disable when backend ready
```

---

## 🌐 Backend Integration

### Option A: Offline Only (Default)

Works out of the box. Scores saved locally per week.

```csharp
// In WeeklyLeaderboard
offlineMode = true
```

### Option B: Custom Backend

#### API Endpoints Required

**GET** `/leaderboard?week={weeklyId}`
```json
{
  "entries": [
    { "rank": 1, "playerName": "SpiderSlayer99", "score": 15420 },
    { "rank": 2, "playerName": "NoobMaster", "score": 12850 }
  ]
}
```

**POST** `/submit`
```json
{
  "weeklyId": "2025-W03",
  "seed": 48291573,
  "playerName": "Player1",
  "score": 11200,
  "floorsReached": 4,
  "enemiesKilled": 23,
  "secretsFound": 2,
  "timeSeconds": 342
}
```

**Response:**
```json
{
  "rank": 3,
  "isNewBest": true
}
```

### Option C: Steam Leaderboards

Replace `WeeklyLeaderboard` network calls with Steamworks:

```csharp
// In SubmitScoreCoroutine, replace with:
SteamUserStats.UploadLeaderboardScore(
    leaderboardHandle,
    ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
    runData.score,
    null, 0
);
```

### Option D: PlayFab

```csharp
// Submit
PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest {
    Statistics = new List<StatisticUpdate> {
        new StatisticUpdate { StatisticName = weeklyId, Value = score }
    }
});

// Fetch
PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest {
    StatisticName = weeklyId,
    MaxResultsCount = 10
});
```

---

## 🎲 Seed System

### How Seeds Work

```csharp
// Deterministic: same week = same seed for everyone
int week = GetISOWeek(DateTime.UtcNow);  // Monday-based
int year = DateTime.UtcNow.Year;

if (useHandPickedSeeds)
    seed = interestingSeeds[(year * 52 + week) % interestingSeeds.Length];
else
    seed = Hash($"{year}-W{week}");
```

### Adding Interesting Seeds

Test seeds in editor, find good ones:

```csharp
interestingSeeds = new int[] {
    48291573,  // Week 1 - Classic layout, good flow
    73629184,  // Week 2 - Maze-heavy, challenging
    19384756,  // Week 3 - Open halls, speedrun friendly
    82947361,  // Week 4 - Vertical focus, elevator heavy
    // Add more as you find them!
};
```

---

## 🎭 Weekly Modifiers (Optional)

Add variety with rotating modifiers:

```csharp
[System.Serializable]
public class WeeklyModifier {
    public string name = "DARKNESS";
    public string description = "Reduced visibility";
    
    public float enemyHealthMultiplier = 1f;
    public float enemyDamageMultiplier = 1.2f;
    public bool darknessMode = true;
    public bool noMinimap = true;
}
```

Example rotation:
- Week 1: Standard
- Week 2: "SWARM" - 2x spiders
- Week 3: "DARKNESS" - Low visibility
- Week 4: "SPEEDRUN" - 2x time penalty
- Week 5: "TANK" - Enemies have 2x health

---

## 📊 Tracking Stats During Run

Call these from your game systems:

```csharp
// When player reaches new floor
WeeklyStageSystem.Instance.RecordFloorReached(floorNumber);

// When enemy dies
WeeklyStageSystem.Instance.RecordKill();

// When secret found
WeeklyStageSystem.Instance.RecordSecret();

// When player takes damage (in Health.cs)
WeeklyStageSystem.Instance.RecordDamage(damageAmount);
```

### Integration with Health.cs

```csharp
// In Health.ApplyDamage()
public bool ApplyDamage(float amount) {
    // ... existing code ...
    
    // Track for weekly scoring
    if (WeeklyStageSystem.Instance != null && 
        WeeklyStageSystem.Instance.IsWeeklyRun &&
        gameObject.CompareTag("Player"))
    {
        WeeklyStageSystem.Instance.RecordDamage((int)amount);
    }
}
```

---

## 🚀 Starting/Ending Runs

### Start Weekly Run

```csharp
public void StartWeeklyMode() {
    // Initialize weekly system
    WeeklyStageSystem.Instance.StartWeeklyRun();
    
    // Seed Unity's random for building generation
    // (Already done in StartWeeklyRun, but your BuildingGenerator should use it)
    
    // Load weekly scene or generate building
    BuildingGenerator.Instance.GenerateBuilding();
}
```

### End Weekly Run

```csharp
// When player dies or completes building
public void OnRunEnd() {
    if (WeeklyStageSystem.Instance.IsWeeklyRun) {
        WeeklyStageSystem.Instance.EndWeeklyRun();
        // UI will automatically show results via events
    }
}
```

---

## 🖥️ UI Setup

### WeeklyStageUI References

```
Canvas
└── WeeklyStagePanel
    ├── WeekTitle (TMP_Text) → "WEEKLY STAGE — 2025-W03"
    ├── SeedText (TMP_Text) → "SEED: 48291573"
    ├── CountdownText (TMP_Text) → "NEXT SEED IN 2d 14h"
    ├── ModifierText (TMP_Text) → "MODIFIER: DARKNESS"
    ├── ScorePanel
    │   ├── CurrentScore (TMP_Text)
    │   ├── BestScore (TMP_Text)
    │   └── RunStats (Floors, Kills, Time)
    ├── LeaderboardPanel
    │   └── LeaderboardContent (Scroll View)
    └── EndRunPanel (hidden until run ends)
        ├── FinalScore
        ├── ScoreBreakdown
        └── NewBestText
```

---

## ✅ Checklist

- [ ] Add `WeeklyStageSystem` to scene
- [ ] Add `WeeklyLeaderboard` to scene
- [ ] Set up `WeeklyStageUI` on canvas
- [ ] Configure interesting seeds
- [ ] Integrate stat tracking (kills, damage, floors)
- [ ] Add "Weekly Stage" button to main menu
- [ ] Test offline mode works
- [ ] (Optional) Set up backend API
- [ ] (Optional) Configure weekly modifiers

---

## 🐛 Debugging

### Editor Tools

```csharp
// In WeeklyStageSystem, use context menu:
[ContextMenu("Simulate Next Week")]  // Test week transition
[ContextMenu("Reset Override")]       // Back to real time
```

### Debug Info

```csharp
Debug.Log($"Weekly: {WeeklyStageSystem.Instance.WeeklyId}");
Debug.Log($"Seed: {WeeklyStageSystem.Instance.CurrentSeed}");
Debug.Log($"Is Weekly Run: {WeeklyStageSystem.Instance.IsWeeklyRun}");
```

---

## 📝 Example: Full Integration

```csharp
public class GameManager : MonoBehaviour {
    
    public void StartWeeklyStage() {
        // 1. Start the weekly run
        WeeklyStageSystem.Instance.StartWeeklyRun();
        
        // 2. Generate building with weekly seed
        // (Random.InitState already called in StartWeeklyRun)
        BuildingGenerator generator = FindObjectOfType<BuildingGenerator>();
        generator.GenerateBuilding();
        
        // 3. Spawn player at fixed keycard level
        // (Already handled in StartWeeklyRun)
        
        // 4. Show weekly UI
        WeeklyStageUI ui = FindObjectOfType<WeeklyStageUI>();
        ui.OnRunStarted();
    }
    
    public void OnPlayerDeath() {
        if (WeeklyStageSystem.Instance.IsWeeklyRun) {
            WeeklyStageSystem.Instance.EndWeeklyRun();
            // Results screen shown automatically via events
        }
    }
    
    public void OnBuildingComplete() {
        WeeklyStageSystem.Instance.RecordFloorReached(currentFloor);
        // Continue to next floor or end run
    }
}
```

---

**Questions?** Check `MEGA_WIKI.md` for full system documentation.
