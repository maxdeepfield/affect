using UnityEngine;

/// <summary>
/// Defines how buildings change based on keycard level.
/// Each level unlocks new architectural rules, enemies, and mechanics.
/// 
/// Level 0: Single floor, basic corridors, no enemies
/// Level 1: 2 floors, elevator, basic spiders
/// Level 2: 3 floors, locked doors, aggressive spiders
/// Level 3: 4+ floors, complex layout, boss spider
/// ...and so on
/// </summary>
[System.Serializable]
public class BuildingRules
{
    [Header("Structure")]
    public int minFloors = 1;
    public int maxFloors = 1;
    public int minRoomSize = 2;
    public int maxRoomSize = 4;
    public int buildingWidth = 15;
    public int buildingDepth = 15;

    [Header("Corridors")]
    public int mainCorridorWidth = 2;
    public int corridorBranches = 2;
    public bool hasCrossCorridor = false;

    [Header("Features")]
    public bool hasElevator = false;
    public bool hasLockedDoors = false;
    public bool hasWindows = true;
    public float windowChance = 0.3f;
    public float backRoomDoorChance = 0.1f;

    [Header("Enemies")]
    public bool hasEnemies = false;
    public int minSpiders = 0;
    public int maxSpiders = 0;
    public float spiderAggressionMultiplier = 1f;
    public bool hasBossSpider = false;

    [Header("Pickups")]
    public int medkitCount = 1;
    public int ammoCount = 2;
    public bool hasKeycardUpgrade = true;

    [Header("Atmosphere")]
    public float lightIntensity = 1f;
    public Color ambientColor = Color.white;
    public bool hasFlickeringLights = false;
    public float fogDensity = 0f;

    /// <summary>
    /// Returns building rules for a specific keycard level.
    /// This is where the game's progression is defined.
    /// </summary>
    public static BuildingRules ForLevel(int level)
    {
        return level switch
        {
            0 => Level0_Tutorial(),
            1 => Level1_FirstReal(),
            2 => Level2_GettingSerious(),
            3 => Level3_MultiFloor(),
            4 => Level4_Labyrinth(),
            5 => Level5_Nightmare(),
            _ => LevelN_Endless(level)
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // LEVEL DEFINITIONS - Each upgrade transforms the world
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Level 0: Tutorial building. Safe, simple, teaches basics.
    /// </summary>
    private static BuildingRules Level0_Tutorial()
    {
        return new BuildingRules
        {
            // Small single floor
            minFloors = 1,
            maxFloors = 1,
            buildingWidth = 12,
            buildingDepth = 12,
            minRoomSize = 3,
            maxRoomSize = 4,

            // Simple layout
            mainCorridorWidth = 2,
            corridorBranches = 1,
            hasCrossCorridor = false,

            // No threats
            hasElevator = false,
            hasLockedDoors = false,
            hasEnemies = false,
            minSpiders = 0,
            maxSpiders = 0,

            // Bright and safe
            lightIntensity = 1.2f,
            ambientColor = new Color(1f, 0.98f, 0.95f),
            hasFlickeringLights = false,
            fogDensity = 0f,

            // Generous pickups
            medkitCount = 2,
            ammoCount = 3,
            hasKeycardUpgrade = true
        };
    }

    /// <summary>
    /// Level 1: First real building. 2 floors, elevator, first spiders.
    /// Player sees: "Oh shit, it has TWO floors now"
    /// </summary>
    private static BuildingRules Level1_FirstReal()
    {
        return new BuildingRules
        {
            // Two floors!
            minFloors = 2,
            maxFloors = 2,
            buildingWidth = 15,
            buildingDepth = 15,
            minRoomSize = 2,
            maxRoomSize = 5,

            // More complex
            mainCorridorWidth = 2,
            corridorBranches = 2,
            hasCrossCorridor = true,

            // Elevator introduced
            hasElevator = true,
            hasLockedDoors = false,
            hasWindows = true,
            windowChance = 0.25f,

            // First enemies
            hasEnemies = true,
            minSpiders = 1,
            maxSpiders = 2,
            spiderAggressionMultiplier = 0.7f, // Slower, easier

            // Slightly darker
            lightIntensity = 1f,
            ambientColor = new Color(0.95f, 0.95f, 1f),
            hasFlickeringLights = false,
            fogDensity = 0.01f,

            medkitCount = 2,
            ammoCount = 3,
            hasKeycardUpgrade = true
        };
    }

    /// <summary>
    /// Level 2: Getting serious. 3 floors, locked doors, aggressive spiders.
    /// </summary>
    private static BuildingRules Level2_GettingSerious()
    {
        return new BuildingRules
        {
            minFloors = 2,
            maxFloors = 3,
            buildingWidth = 18,
            buildingDepth = 18,
            minRoomSize = 2,
            maxRoomSize = 6,

            mainCorridorWidth = 2,
            corridorBranches = 3,
            hasCrossCorridor = true,

            hasElevator = true,
            hasLockedDoors = true, // Need to find keys within building
            hasWindows = true,
            windowChance = 0.2f,
            backRoomDoorChance = 0.15f,

            hasEnemies = true,
            minSpiders = 2,
            maxSpiders = 4,
            spiderAggressionMultiplier = 1f,

            lightIntensity = 0.9f,
            ambientColor = new Color(0.9f, 0.92f, 1f),
            hasFlickeringLights = true,
            fogDensity = 0.02f,

            medkitCount = 3,
            ammoCount = 4,
            hasKeycardUpgrade = true
        };
    }

    /// <summary>
    /// Level 3: Multi-floor maze. 4 floors, complex layout.
    /// </summary>
    private static BuildingRules Level3_MultiFloor()
    {
        return new BuildingRules
        {
            minFloors = 3,
            maxFloors = 4,
            buildingWidth = 20,
            buildingDepth = 20,
            minRoomSize = 2,
            maxRoomSize = 5,

            mainCorridorWidth = 2,
            corridorBranches = 4,
            hasCrossCorridor = true,

            hasElevator = true,
            hasLockedDoors = true,
            hasWindows = true,
            windowChance = 0.15f,
            backRoomDoorChance = 0.2f,

            hasEnemies = true,
            minSpiders = 3,
            maxSpiders = 6,
            spiderAggressionMultiplier = 1.2f,

            lightIntensity = 0.8f,
            ambientColor = new Color(0.85f, 0.88f, 1f),
            hasFlickeringLights = true,
            fogDensity = 0.03f,

            medkitCount = 4,
            ammoCount = 5,
            hasKeycardUpgrade = true
        };
    }

    /// <summary>
    /// Level 4: Labyrinth. Confusing layout, many dead ends.
    /// </summary>
    private static BuildingRules Level4_Labyrinth()
    {
        return new BuildingRules
        {
            minFloors = 4,
            maxFloors = 5,
            buildingWidth = 22,
            buildingDepth = 22,
            minRoomSize = 2,
            maxRoomSize = 4, // Smaller rooms = more maze-like

            mainCorridorWidth = 2,
            corridorBranches = 5,
            hasCrossCorridor = true,

            hasElevator = true,
            hasLockedDoors = true,
            hasWindows = true,
            windowChance = 0.1f,
            backRoomDoorChance = 0.25f,

            hasEnemies = true,
            minSpiders = 4,
            maxSpiders = 8,
            spiderAggressionMultiplier = 1.3f,

            lightIntensity = 0.7f,
            ambientColor = new Color(0.8f, 0.85f, 1f),
            hasFlickeringLights = true,
            fogDensity = 0.04f,

            medkitCount = 5,
            ammoCount = 6,
            hasKeycardUpgrade = true
        };
    }

    /// <summary>
    /// Level 5: Nightmare. Boss spider, minimal light, maximum fear.
    /// </summary>
    private static BuildingRules Level5_Nightmare()
    {
        return new BuildingRules
        {
            minFloors = 5,
            maxFloors = 6,
            buildingWidth = 25,
            buildingDepth = 25,
            minRoomSize = 2,
            maxRoomSize = 6,

            mainCorridorWidth = 3,
            corridorBranches = 5,
            hasCrossCorridor = true,

            hasElevator = true,
            hasLockedDoors = true,
            hasWindows = true,
            windowChance = 0.05f,
            backRoomDoorChance = 0.3f,

            hasEnemies = true,
            minSpiders = 5,
            maxSpiders = 10,
            spiderAggressionMultiplier = 1.5f,
            hasBossSpider = true, // THE BIG ONE

            lightIntensity = 0.5f,
            ambientColor = new Color(0.7f, 0.75f, 0.9f),
            hasFlickeringLights = true,
            fogDensity = 0.06f,

            medkitCount = 6,
            ammoCount = 8,
            hasKeycardUpgrade = true
        };
    }

    /// <summary>
    /// Level N: Endless scaling for post-game.
    /// </summary>
    private static BuildingRules LevelN_Endless(int level)
    {
        int extraFloors = level - 5;
        return new BuildingRules
        {
            minFloors = 5 + extraFloors,
            maxFloors = 6 + extraFloors,
            buildingWidth = Mathf.Min(30, 25 + extraFloors),
            buildingDepth = Mathf.Min(30, 25 + extraFloors),
            minRoomSize = 2,
            maxRoomSize = 6,

            mainCorridorWidth = 3,
            corridorBranches = 5 + extraFloors / 2,
            hasCrossCorridor = true,

            hasElevator = true,
            hasLockedDoors = true,
            hasWindows = true,
            windowChance = 0.05f,
            backRoomDoorChance = 0.3f,

            hasEnemies = true,
            minSpiders = 5 + extraFloors,
            maxSpiders = 10 + extraFloors * 2,
            spiderAggressionMultiplier = 1.5f + extraFloors * 0.1f,
            hasBossSpider = level % 3 == 0, // Boss every 3 levels

            lightIntensity = Mathf.Max(0.3f, 0.5f - extraFloors * 0.05f),
            ambientColor = new Color(0.6f, 0.65f, 0.85f),
            hasFlickeringLights = true,
            fogDensity = Mathf.Min(0.1f, 0.06f + extraFloors * 0.01f),

            medkitCount = 6 + extraFloors,
            ammoCount = 8 + extraFloors,
            hasKeycardUpgrade = true
        };
    }
}
