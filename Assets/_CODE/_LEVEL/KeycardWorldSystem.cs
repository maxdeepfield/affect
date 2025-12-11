using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Core system: Single upgradable keycard that transforms the world.
/// When keycard upgrades, a NEW building assembles from the sky.
/// 
/// Flow:
/// 1. Player completes building
/// 2. Collects keycard upgrade at terminal
/// 3. Keycard level increases
/// 4. NEW building slowly descends from sky, assembling piece by piece
/// 5. Player has never seen this building before
/// </summary>
public class KeycardWorldSystem : MonoBehaviour
{
    public static KeycardWorldSystem Instance { get; private set; }

    [Header("Building Prefabs by Keycard Level")]
    [Tooltip("Index 0 = starting building (level 0), Index 1 = building after first upgrade, etc.")]
    [SerializeField] private GameObject[] buildingPrefabs;

    [Header("Building Assembly Animation")]
    [SerializeField] private float buildingDropHeight = 50f;
    [SerializeField] private float buildingDescentSpeed = 2f;
    [SerializeField] private float floorAssemblyDelay = 0.8f;
    [SerializeField] private AnimationCurve descentCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Spawn Position")]
    [SerializeField] private Transform nextBuildingSpawnPoint;
    [SerializeField] private Vector3 spawnOffset = new Vector3(30f, 0f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioClip buildingDescentSound;
    [SerializeField] private AudioClip floorLandSound;

    [Header("Events")]
    public UnityEvent<int> OnKeycardUpgraded = new UnityEvent<int>();
    public UnityEvent<GameObject> OnNewBuildingSpawned = new UnityEvent<GameObject>();
    public UnityEvent OnBuildingAssemblyComplete = new UnityEvent();

    private GameObject currentBuilding;
    private GameObject nextBuilding;
    private bool isAssembling;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to keycard upgrades
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnKeycardLevelChanged.AddListener(OnKeycardLevelChanged);
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnKeycardLevelChanged.RemoveListener(OnKeycardLevelChanged);
        }

        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Called when player upgrades their keycard.
    /// Triggers the next building to descend from the sky.
    /// </summary>
    private void OnKeycardLevelChanged(int newLevel)
    {
        if (isAssembling) return;

        OnKeycardUpgraded.Invoke(newLevel);

        // Spawn next building based on new keycard level
        if (newLevel < buildingPrefabs.Length && buildingPrefabs[newLevel] != null)
        {
            StartCoroutine(AssembleNewBuilding(newLevel));
        }
    }

    /// <summary>
    /// The magic moment: building slowly descends from the sky, assembling floor by floor.
    /// Player watches through window as a building they've NEVER SEEN materializes.
    /// </summary>
    private IEnumerator AssembleNewBuilding(int level)
    {
        isAssembling = true;

        // Calculate spawn position
        Vector3 spawnPos = nextBuildingSpawnPoint != null 
            ? nextBuildingSpawnPoint.position 
            : (currentBuilding != null ? currentBuilding.transform.position + spawnOffset : spawnOffset);

        Vector3 targetPos = spawnPos;
        Vector3 startPos = spawnPos + Vector3.up * buildingDropHeight;

        // Instantiate building at sky position
        nextBuilding = Instantiate(buildingPrefabs[level], startPos, Quaternion.identity);
        nextBuilding.name = $"Building_Level_{level}";

        OnNewBuildingSpawned.Invoke(nextBuilding);

        // Play descent sound
        if (buildingDescentSound != null)
        {
            AudioSource.PlayClipAtPoint(buildingDescentSound, startPos, 0.8f);
        }

        // Get all floors for sequential assembly
        Transform[] floors = GetBuildingFloors(nextBuilding);

        if (floors.Length > 0)
        {
            // Hide all floors initially
            foreach (var floor in floors)
            {
                floor.gameObject.SetActive(false);
            }

            // Descend and reveal floor by floor
            yield return StartCoroutine(DescentWithFloorAssembly(nextBuilding.transform, startPos, targetPos, floors));
        }
        else
        {
            // Simple descent if no floor structure
            yield return StartCoroutine(SimpleDescend(nextBuilding.transform, startPos, targetPos));
        }

        // Assembly complete
        currentBuilding = nextBuilding;
        nextBuilding = null;
        isAssembling = false;

        OnBuildingAssemblyComplete.Invoke();
    }

    /// <summary>
    /// Building descends while floors activate one by one from bottom to top.
    /// </summary>
    private IEnumerator DescentWithFloorAssembly(Transform building, Vector3 start, Vector3 target, Transform[] floors)
    {
        float totalDistance = Vector3.Distance(start, target);
        float distancePerFloor = totalDistance / floors.Length;
        int currentFloorIndex = 0;

        float elapsed = 0f;
        float duration = totalDistance / buildingDescentSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = descentCurve.Evaluate(t);

            building.position = Vector3.Lerp(start, target, curvedT);

            // Check if we should reveal next floor
            float currentDistance = totalDistance * curvedT;
            int floorsToReveal = Mathf.FloorToInt(currentDistance / distancePerFloor);

            while (currentFloorIndex < floorsToReveal && currentFloorIndex < floors.Length)
            {
                // Reveal floor with effect
                floors[currentFloorIndex].gameObject.SetActive(true);
                
                if (floorLandSound != null)
                {
                    AudioSource.PlayClipAtPoint(floorLandSound, floors[currentFloorIndex].position, 0.5f);
                }

                currentFloorIndex++;
                yield return new WaitForSeconds(floorAssemblyDelay);
            }

            yield return null;
        }

        // Ensure final position and all floors visible
        building.position = target;
        foreach (var floor in floors)
        {
            floor.gameObject.SetActive(true);
        }
    }

    private IEnumerator SimpleDescend(Transform building, Vector3 start, Vector3 target)
    {
        float duration = Vector3.Distance(start, target) / buildingDescentSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = descentCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            building.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        building.position = target;
    }

    /// <summary>
    /// Finds floor transforms in building hierarchy.
    /// Expects children named "Floor_0", "Floor_1", etc.
    /// </summary>
    private Transform[] GetBuildingFloors(GameObject building)
    {
        var floors = new System.Collections.Generic.List<Transform>();

        foreach (Transform child in building.transform)
        {
            if (child.name.StartsWith("Floor_") || child.name.StartsWith("Floor "))
            {
                floors.Add(child);
            }
        }

        // Sort by name to ensure correct order
        floors.Sort((a, b) => a.name.CompareTo(b.name));
        return floors.ToArray();
    }

    /// <summary>
    /// Sets the current building reference (for initial scene setup).
    /// </summary>
    public void SetCurrentBuilding(GameObject building)
    {
        currentBuilding = building;
    }

    /// <summary>
    /// Gets building rules for current keycard level.
    /// </summary>
    public BuildingRules GetCurrentBuildingRules()
    {
        int level = PlayerInventory.Instance != null ? PlayerInventory.Instance.KeycardLevel : 0;
        return BuildingRules.ForLevel(level);
    }
}
