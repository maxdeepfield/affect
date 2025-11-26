using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using _SCRIPTS;

/// <summary>
/// Оптимизированный генератор зданий с улучшенной производительностью и естественными алгоритмами
/// Вдохновлено: процедурной генерацией Sci-Fi хорроров и кооперативными механиками
/// 
/// МНОГОЭТАЖНАЯ СИСТЕМА:
/// - Генерация лестниц в залах (hall rooms)
/// - Потолки аналогично полу
/// - При подъеме по лестнице создается новый этаж выше
/// - Поэтапная генерация башни
/// </summary>
[RequireComponent(typeof(ObjectPoolManager))]
public class OptimizedBuildingGenerator : MonoBehaviour
{
    [Header("🔧 Основные Префабы")]
    [Tooltip("Базовый сегмент стены 3x3x0.2")]
    public GameObject wallPrefab;
    [Tooltip("Сегмент пола 3x3")]
    public GameObject floorPrefab;
    [Tooltip("Сегмент окна 3x3x0.2")]
    public GameObject windowPrefab;
    [Tooltip("Входная дверь (наружная) 3x3x0.2")]
    public GameObject entranceDoorPrefab;
    [Tooltip("Внутренняя дверь (между комнатами) 3x3x0.2")]
    public GameObject interiorDoorPrefab;
    
    [Header("🏗️ Многоэтажность")]
    [Tooltip("Префаб лестницы для перехода между этажами")]
    public GameObject stairsPrefab;
    [Tooltip("Префаб потолка (аналогично полу)")]
    public GameObject ceilingPrefab;
    [Tooltip("Высота одного этажа в Unity units")]
    public float floorHeight = 4f;

    [Header("⚙️ Параметры Генерации")]
    [Range(10, 50)] public int gridWidth = 20;
    [Range(10, 50)] public int gridHeight = 20;
    [Range(3, 15)] public int minRooms = 5;
    [Range(5, 20)] public int maxRooms = 8;
    [Range(3, 8)] public int minRoomSize = 3;
    [Range(5, 12)] public int maxRoomSize = 6;
    [Range(0f, 1f)] public float windowChance = 0.2f;
    [Range(1f, 10f)] public float corridorSmoothness = 3f;

    [Header("🎯 Производительность")]
    [Tooltip("Использовать Object Pooling для оптимизации")]
    public bool useObjectPooling = true;
    [Tooltip("Включить spatial partitioning для быстрых коллизий")]
    public bool enableSpatialPartitioning = true;
    [Tooltip("Максимальное время генерации в миллисекундах (для плавности)")]
    public int maxGenerationTimeMs = 16;

    [Header("🎨 Тематика Помещений")]
    public bool assignRoomThemes = true;
    [Range(0f, 1f)] public float themeVariety = 0.7f; // How varied should room themes be

    [Header("🏗️ Многоэтажность - Настройки")]
    [Tooltip("Вероятность генерации зала с лестницей")]
    [Range(0f, 1f)] public float hallChance = 0.3f;
    [Tooltip("Ленивая генерация этажей (только при достижении лестницы)")]
    public bool lazyFloorGeneration = true;

    [Tooltip("Количество этажей для генерации здания")]
    [Range(1, 20)] public int targetFloorCount = 3;

    // Многоэтажная система
    private List<FloorData> floors = new List<FloorData>();
    private int currentFloorIndex;
    private FloorData CurrentFloor => floors[currentFloorIndex];
    
    // Старые переменные для совместимости (устаревшие)
    private bool[,] floorPlan;
    private Dictionary<Vector2Int, RoomNode> roomNodes = new Dictionary<Vector2Int, RoomNode>();
    private SpatialGrid spatialGrid;
    private ObjectPoolManager poolManager;
    private HashSet<Vector2Int> boundaryCache = new HashSet<Vector2Int>();

    // Статистика
    private int totalGenerationTime;
    private int roomCount;
    private int wallCount;

    // Публичные методы для доступа к этажам
    public List<FloorData> GetAllFloors() => floors;
    public FloorData GetCurrentFloor() => CurrentFloor;
    public int GetCurrentFloorIndex() => currentFloorIndex;
    public FloorData GetFloor(int index) => index >= 0 && index < floors.Count ? floors[index] : null;

    private InputAction devRegenerateAction;

    private void Awake()
    {
        devRegenerateAction = new InputAction("Regenerate", InputActionType.Button, "&lt;Keyboard&gt;/space");
        devRegenerateAction.Enable();

        InitializeComponents();
    }

    private void Start()
    {
        GenerateBuilding();
    }

    private void OnDestroy()
    {
        devRegenerateAction?.Disable();
    }

    [ContextMenu("🔄 Сгенерировать Здание")]
    public void GenerateBuilding()
    {
        // Validate required prefabs
        if (wallPrefab == null)
        {
            Debug.LogError("❌ wallPrefab не назначен! Пожалуйста, назначьте префаб стены в инспекторе.");
            return;
        }
        if (floorPrefab == null)
        {
            Debug.LogError("❌ floorPrefab не назначен! Пожалуйста, назначьте префаб пола в инспекторе.");
            return;
        }
        if (windowPrefab == null)
        {
            Debug.LogError("❌ windowPrefab не назначен! Пожалуйста, назначьте префаб окна в инспекторе.");
            return;
        }
        if (entranceDoorPrefab == null)
        {
            Debug.LogError("❌ entranceDoorPrefab не назначен! Пожалуйста, назначьте префаб входной двери в инспекторе.");
            return;
        }
        if (interiorDoorPrefab == null)
        {
            Debug.LogError("❌ interiorDoorPrefab не назначен! Пожалуйста, назначьте префаб внутренней двери в инспекторе.");
            return;
        }

        var startTime = Time.realtimeSinceStartup;

        // Ensure components are initialized (important for editor usage)
        InitializeComponents();
        ClearPreviousGeneration();
        InitializeDataStructures();

        try
        {
            GenerateRoomsOptimized();
            ConnectRoomsWithCorridors();
            PlaceFloorsOptimized();
            PlaceCeilingsOptimized();
            PlaceWallsOptimized();

            var generationTime = (Time.realtimeSinceStartup - startTime) * 1000;
            totalGenerationTime = (int)generationTime;

            // Place themed assets in rooms if enabled
            if (assignRoomThemes)
            {
                PlaceThemedAssets();
            }

            // Событие завершения генерации
            BuildingGeneratorEvents.OnGenerationCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Ошибка генерации: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    private void InitializeComponents()
    {
        poolManager = GetComponent<ObjectPoolManager>();
        if (!poolManager) poolManager = gameObject.AddComponent<ObjectPoolManager>();

        if (enableSpatialPartitioning)
        {
            spatialGrid = new SpatialGrid(gridWidth, gridHeight);
        }
    }

    private void InitializeDataStructures()
    {
        floors.Clear();
        
        // Создаем первый этаж
        FloorData firstFloor = new FloorData
        {
            floorPlan = new bool[gridWidth, gridHeight],
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            floorY = 0f,
            floorIndex = 0
        };
        floors.Add(firstFloor);
        
        // Совместимость со старым кодом
        floorPlan = firstFloor.floorPlan;
        roomNodes.Clear();
        boundaryCache.Clear();
        roomCount = 0;
        wallCount = 0;
    }

    // Очистка предыдущей генерации, доступна из Editor-скрипта
    public void ClearPreviousGeneration()
    {
        // Ensure poolManager is initialized
        if (poolManager == null)
        {
            poolManager = GetComponent<ObjectPoolManager>();
            if (poolManager == null && useObjectPooling)
            {
                poolManager = gameObject.AddComponent<ObjectPoolManager>();
            }
        }

        if (useObjectPooling && poolManager != null)
        {
            poolManager.ReturnAllObjects();
        }
        else
        {
            // Удаляем всех потомков генератора в редакторе и в рантайме корректно
#if UNITY_EDITOR
            // В редакторе используем DestroyImmediate, чтобы изменения были видны сразу
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
            }
#else
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
#endif
        }
    }

    [ContextMenu("🗑️ Очистить Пулы и Пересоздать")]
    public void ClearAndRebuildPools()
    {
        // First, clear the old building from the scene
#if UNITY_EDITOR
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
        }
#else
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
#endif

        // Then clear and destroy all pooled objects
        if (poolManager == null)
        {
            poolManager = GetComponent<ObjectPoolManager>();
        }

        if (poolManager != null)
        {
            poolManager.ClearAllPools();
            Debug.Log("✅ Здание удалено и все пулы очищены! Теперь будут использоваться обновленные префабы.");
        }
        else
        {
            Debug.LogWarning("⚠️ ObjectPoolManager не найден!");
        }
    }

    private void GenerateRoomsOptimized()
    {
        BuildingGeneratorEvents.OnGenerationStarted?.Invoke();

        // Генерация стартовой комнаты
        var startRoom = GenerateRandomRoom();
        AssignRoomTheme(startRoom);
        CarveRoom(startRoom);
        AddRoomToNodes(startRoom);

        int roomsToGenerate = Random.Range(minRooms, maxRooms + 1) - 1; // -1 так как стартовая уже есть
        int attempts = 0;
        const int maxAttempts = 1000;

        while (roomsToGenerate > 0 && attempts < maxAttempts)
        {
            var room = GenerateRandomRoom();

            if (enableSpatialPartitioning)
            {
                if (CanPlaceRoomWithSpatialPartitioning(room))
                {
                    AssignRoomTheme(room);
                    CarveRoom(room);
                    AddRoomToNodes(room);
                    roomsToGenerate--;
                }
            }
            else
            {
                if (CanPlaceRoomClassic(room))
                {
                    AssignRoomTheme(room);
                    CarveRoom(room);
                    AddRoomToNodes(room);
                    roomsToGenerate--;
                }
            }

            attempts++;
        }

        // Попытка создать зал с лестницей
        if (Random.value < hallChance && CurrentFloor.roomNodes.Count > 2)
        {
            CreateHallWithStairs();
        }

        if (roomsToGenerate > 0)
        {
            Debug.LogWarning($"⚠️ Не удалось разместить {roomsToGenerate} комнат после {attempts} попыток");
        }
    }

    private void CreateHallWithStairs()
    {
        // Создаем большой зал для лестниц
        int hallX = Random.Range(gridWidth / 4, gridWidth * 3 / 4);
        int hallY = Random.Range(gridHeight / 4, gridHeight * 3 / 4);
        int hallWidth = Random.Range(6, 10);
        int hallHeight = Random.Range(4, 8);

        RoomNode hall = new RoomNode(hallX, hallY, hallWidth, hallHeight);
        hall.theme = RoomTheme.Recreation; // Можно добавить Hall в enum, пока используем Recreation
        
        // Размещаем зал
        if (CanPlaceRoomClassic(hall))
        {
            AssignRoomTheme(hall);
            CarveRoom(hall);
            AddRoomToNodes(hall);
            Debug.Log("🏛️ Создан зал с лестницей");
        }
    }

    private RoomNode GenerateRandomRoom()
    {
        int x = Random.Range(1, gridWidth - maxRoomSize - 1);
        int y = Random.Range(1, gridHeight - maxRoomSize - 1);
        int width = Random.Range(minRoomSize, maxRoomSize + 1);
        int height = Random.Range(minRoomSize, maxRoomSize + 1);

        return new RoomNode(x, y, width, height);
    }

    private bool CanPlaceRoomWithSpatialPartitioning(RoomNode room)
    {
        // Проверяем только соседние сектора
        var sectorX = room.center.x / 5;
        var sectorY = room.center.y / 5;

        for (int sx = Mathf.Max(0, sectorX - 1); sx <= Mathf.Min(sectorX + 1, spatialGrid.SectorWidth - 1); sx++)
        {
            for (int sy = Mathf.Max(0, sectorY - 1); sy <= Mathf.Min(sectorY + 1, spatialGrid.SectorHeight - 1); sy++)
            {
                if (spatialGrid.HasObjectsInSector(sx, sy))
                {
                    var nearbyRooms = spatialGrid.GetObjectsInSector(sx, sy);
                    foreach (var nearbyRoom in nearbyRooms)
                    {
                        if (RoomsOverlap(room, nearbyRoom))
                            return false;
                    }
                }
            }
        }

        return true;
    }

    private bool CanPlaceRoomClassic(RoomNode room)
    {
        for (int x = room.position.x; x < room.position.x + room.size.x; x++)
        {
            for (int y = room.position.y; y < room.position.y + room.size.y; y++)
            {
                if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight || CurrentFloor.floorPlan[x, y])
                    return false;
            }
        }
        return true;
    }

    private bool RoomsOverlap(RoomNode room1, RoomNode room2)
    {
        return room1.position.x < room2.position.x + room2.size.x &&
               room1.position.x + room1.size.x > room2.position.x &&
               room1.position.y < room2.position.y + room2.size.y &&
               room1.position.y + room1.size.y > room2.position.y;
    }

    private void AddRoomToNodes(RoomNode room)
    {
        roomNodes[room.center] = room;
        CurrentFloor.roomNodes[room.center] = room;
        roomCount++;

        if (enableSpatialPartitioning)
        {
            spatialGrid.AddObject(room.center, room);
        }

        BuildingGeneratorEvents.OnRoomGenerated?.Invoke(room);
    }

    private void AssignRoomTheme(RoomNode room)
    {
        if (!assignRoomThemes) return;

        // Create a list of possible themes with their weights/probabilities
        var possibleThemes = new List<(RoomTheme theme, float weight)>
        {
            (RoomTheme.Office, 1.0f),
            (RoomTheme.Residential, 1.0f),
            (RoomTheme.Industrial, 1.0f),
            (RoomTheme.Laboratory, 0.8f),
            (RoomTheme.Medical, 0.6f),
            (RoomTheme.Storage, 1.0f),
            (RoomTheme.Recreation, 0.7f),
            (RoomTheme.Security, 0.5f)
        };

        // Select a theme based on weights
        RoomTheme selectedTheme = SelectWeightedRandomTheme(possibleThemes);

        // Update the room with the selected theme
        var updatedRoom = room;
        updatedRoom.theme = selectedTheme;

        // Update the room in the dictionary if it already exists
        if (roomNodes.ContainsKey(updatedRoom.center))
        {
            roomNodes[updatedRoom.center] = updatedRoom;
        }
        
        if (CurrentFloor.roomNodes.ContainsKey(updatedRoom.center))
        {
            CurrentFloor.roomNodes[updatedRoom.center] = updatedRoom;
        }
    }

    private RoomTheme SelectWeightedRandomTheme(List<(RoomTheme theme, float weight)> themes)
    {
        float totalWeight = themes.Sum(t => t.weight);
        float randomValue = Random.Range(0f, totalWeight);

        float cumulativeWeight = 0f;
        foreach (var (theme, weight) in themes)
        {
            cumulativeWeight += weight;
            if (randomValue <= cumulativeWeight)
            {
                return theme;
            }
        }

        // Fallback to the first theme if something goes wrong
        return themes[0].theme;
    }

    private void CarveRoom(RoomNode room)
    {
        for (int x = room.position.x; x < room.position.x + room.size.x; x++)
        {
            for (int y = room.position.y; y < room.position.y + room.size.y; y++)
            {
                if (IsInBounds(x, y))
                {
                    CurrentFloor.floorPlan[x, y] = true;
                    floorPlan[x, y] = true; // Совместимость
                }
            }
        }
    }

    private void ConnectRoomsWithCorridors()
    {
        var rooms = CurrentFloor.roomNodes.Values.ToList();

        for (int i = 1; i < rooms.Count; i++)
        {
            var path = FindPathAStar(rooms[i - 1].center, rooms[i].center);

            foreach (var point in path)
            {
                if (IsInBounds(point.x, point.y))
                    CurrentFloor.floorPlan[point.x, point.y] = true;
            }
        }
    }

    private List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int end)
    {
        var openSet = new List<Vector2Int> { start };
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { { start, 0 } };
        var fScore = new Dictionary<Vector2Int, float> { { start, HeuristicCost(start, end) } };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(pos => fScore.GetValueOrDefault(pos, float.MaxValue)).First();

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetCardinalNeighbors(current))
            {
                if (closedSet.Contains(neighbor) || !IsInBounds(neighbor))
                    continue;

                float tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCost(neighbor, end);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return new List<Vector2Int>(); // Путь не найден
    }

    private float HeuristicCost(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var totalPath = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }

    private Vector2Int[] GetCardinalNeighbors(Vector2Int position)
    {
        return new[] {
        new Vector2Int(position.x + 1, position.y),
        new Vector2Int(position.x - 1, position.y),
        new Vector2Int(position.x, position.y + 1),
        new Vector2Int(position.x, position.y - 1)
    };
    }

    private void PlaceFloorsOptimized()
    {
        float tileSize = 3f;
        int floorCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!CurrentFloor.floorPlan[x, y]) continue;

                // Floor position - align with walls (Z offset +3)
                Vector3 position = new Vector3(x * tileSize, CurrentFloor.floorY, y * tileSize + tileSize);
                GameObject floorObject;

                if (useObjectPooling && poolManager != null)
                {
                    floorObject = poolManager.GetObject(floorPrefab.name);
                    if (floorObject == null)
                    {
                        floorObject = Instantiate(floorPrefab, position, Quaternion.identity, transform);
                        poolManager.AddObject(floorPrefab.name, floorObject);
                    }
                    else
                    {
                        floorObject.transform.position = position;
                        floorObject.transform.rotation = Quaternion.identity;
                        floorObject.transform.parent = transform;
                        floorObject.SetActive(true);
                    }
                }
                else
                {
                    Instantiate(floorPrefab, position, Quaternion.identity, transform);
                }

                floorCount++;
            }
        }

        Debug.Log($"🟫 Размещено полов на этаже {currentFloorIndex}: {floorCount}");
    }

    private void PlaceCeilingsOptimized()
    {
        if (ceilingPrefab == null)
        {
            Debug.LogWarning("⚠️ ceilingPrefab не назначен! Пропускаем генерацию потолков.");
            return;
        }
        
        float tileSize = 3f;
        int ceilingCount = 0;
        float ceilingY = CurrentFloor.floorY + floorHeight - 1f; // Ceiling at top of floor height

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!CurrentFloor.floorPlan[x, y]) continue;

                Vector3 position = new Vector3(x * tileSize, ceilingY, y * tileSize + tileSize);
                GameObject ceilingObject;

                if (useObjectPooling && poolManager != null)
                {
                    ceilingObject = poolManager.GetObject(ceilingPrefab.name);
                    if (ceilingObject == null)
                    {
                        ceilingObject = Instantiate(ceilingPrefab, position, Quaternion.identity, transform);
                        poolManager.AddObject(ceilingPrefab.name, ceilingObject);
                    }
                    else
                    {
                        ceilingObject.transform.position = position;
                        ceilingObject.transform.rotation = Quaternion.identity;
                        ceilingObject.transform.parent = transform;
                        ceilingObject.SetActive(true);
                    }
                }
                else
                {
                    Instantiate(ceilingPrefab, position, Quaternion.identity, transform);
                }

                ceilingCount++;
            }
        }

        Debug.Log($"⬆️ Размещено потолков на этаже {currentFloorIndex}: {ceilingCount}");
    }

    private void PlaceWallsOptimized()
    {
        boundaryCache.Clear();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!CurrentFloor.floorPlan[x, y]) continue;

                foreach (var neighbor in GetCardinalNeighbors(new Vector2Int(x, y)))
                {
                    if (!IsInBounds(neighbor) || !CurrentFloor.floorPlan[neighbor.x, neighbor.y])
                    {
                        boundaryCache.Add(new Vector2Int(x, y));
                        break;
                    }
                }
            }
        }

        PlaceWallSegmentsWithPooling();
    }

    private void PlaceWallSegmentsWithPooling()
    {
        var wallPositions = new List<WallInfo>();
        var interiorDoorwayPositions = new List<WallInfo>();

        // First pass: collect exterior walls (where adjacent cell has no floor)
        foreach (var boundary in boundaryCache)
        {
            // North wall
            if (boundary.y + 1 >= gridHeight || !CurrentFloor.floorPlan[boundary.x, boundary.y + 1])
                wallPositions.Add(new WallInfo { cellX = boundary.x, cellY = boundary.y, dir = "north" });

            // South wall
            if (boundary.y - 1 < 0 || !CurrentFloor.floorPlan[boundary.x, boundary.y - 1])
                wallPositions.Add(new WallInfo { cellX = boundary.x, cellY = boundary.y, dir = "south" });

            // East wall
            if (boundary.x + 1 >= gridWidth || !CurrentFloor.floorPlan[boundary.x + 1, boundary.y])
                wallPositions.Add(new WallInfo { cellX = boundary.x, cellY = boundary.y, dir = "east" });

            // West wall
            if (boundary.x - 1 < 0 || !CurrentFloor.floorPlan[boundary.x - 1, boundary.y])
                wallPositions.Add(new WallInfo { cellX = boundary.x, cellY = boundary.y, dir = "west" });
        }

        // Second pass: find interior doorway positions (room edges that connect to corridors)
        foreach (var room in CurrentFloor.roomNodes.Values)
        {
            // Check all edges of the room
            for (int x = room.position.x; x < room.position.x + room.size.x; x++)
            {
                // North edge
                int northY = room.position.y + room.size.y;
                if (IsInBounds(x, northY) && CurrentFloor.floorPlan[x, northY])
                {
                    interiorDoorwayPositions.Add(new WallInfo { cellX = x, cellY = room.position.y + room.size.y - 1, dir = "north" });
                }

                // South edge
                int southY = room.position.y - 1;
                if (IsInBounds(x, southY) && CurrentFloor.floorPlan[x, southY])
                {
                    interiorDoorwayPositions.Add(new WallInfo { cellX = x, cellY = room.position.y, dir = "south" });
                }
            }

            for (int y = room.position.y; y < room.position.y + room.size.y; y++)
            {
                // East edge
                int eastX = room.position.x + room.size.x;
                if (IsInBounds(eastX, y) && CurrentFloor.floorPlan[eastX, y])
                {
                    interiorDoorwayPositions.Add(new WallInfo { cellX = room.position.x + room.size.x - 1, cellY = y, dir = "east" });
                }

                // West edge
                int westX = room.position.x - 1;
                if (IsInBounds(westX, y) && CurrentFloor.floorPlan[westX, y])
                {
                    interiorDoorwayPositions.Add(new WallInfo { cellX = room.position.x, cellY = y, dir = "west" });
                }
            }
        }

        // Размещение стен
        if (wallPositions.Count == 0)
        {
            Debug.LogWarning("⚠️ Нет позиций для стен!");
            return;
        }

        int entranceIdx = Random.Range(0, wallPositions.Count);
        wallCount = wallPositions.Count + interiorDoorwayPositions.Count;

        int entranceDoorCount = 0;
        int interiorDoorCount = 0;
        int windowCount = 0;
        int wallOnlyCount = 0;

        // Place exterior walls
        for (int i = 0; i < wallPositions.Count; i++)
        {
            var wallInfo = wallPositions[i];
            GameObject prefab = wallPrefab;

            if (i == entranceIdx)
            {
                // Entrance door (exterior)
                prefab = entranceDoorPrefab;
                entranceDoorCount++;
            }
            else if (Random.value < windowChance)
            {
                prefab = windowPrefab;
                windowCount++;
            }
            else
            {
                wallOnlyCount++;
            }

            PlaceWallSegment(wallInfo, prefab);
        }

        // Place interior doors at room-corridor connections
        foreach (var doorwayInfo in interiorDoorwayPositions)
        {
            PlaceWallSegment(doorwayInfo, interiorDoorPrefab);
            interiorDoorCount++;
        }

        Debug.Log($"🚪 Размещено на этаже {currentFloorIndex}: {entranceDoorCount} входных дверей, {interiorDoorCount} внутренних дверей, {windowCount} окон, {wallOnlyCount} стен");
    }

    private void PlaceWallSegment(WallInfo wallInfo, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"⚠️ Prefab is null for wall at ({wallInfo.cellX}, {wallInfo.cellY})");
            return;
        }

        var position = CalculateWallPosition(wallInfo);
        var rotation = CalculateWallRotation(wallInfo.dir);

        // Windows and doors need 180 degree rotation around pivot
        if (prefab == windowPrefab || prefab == entranceDoorPrefab || prefab == interiorDoorPrefab)
        {
            rotation *= Quaternion.Euler(0, 180, 0);
        }

        GameObject wallObject;

        if (useObjectPooling && poolManager != null)
        {
            wallObject = poolManager.GetObject(prefab.name);
            if (wallObject == null)
            {
                wallObject = Instantiate(prefab, position, rotation, transform);
                poolManager.AddObject(prefab.name, wallObject);
            }
            else
            {
                wallObject.transform.position = position;
                wallObject.transform.rotation = rotation;
                wallObject.transform.parent = transform;
                wallObject.SetActive(true);
            }
        }
        else
        {
            Instantiate(prefab, position, rotation, transform);
        }

        BuildingGeneratorEvents.OnWallPlaced?.Invoke(wallInfo);
    }

    private Vector3 CalculateWallPosition(WallInfo wallInfo)
    {
        float tileSize = 3f;
        float currentFloorY = CurrentFloor.floorY + 1f; // Wall height offset

        // Wall is 3 units wide (along Right axis) and 1 unit deep (along Forward axis)
        // Pivot is at the right edge when Forward points along the wall face
        // From top view: pivot is at top-right corner of the 1x3 rectangle

        switch (wallInfo.dir)
        {
            case "north":
                // Wall faces +Z. Right axis is +X. Pivot is at right edge (+X side)
                // Wall should be at Z = (cellY + 1) * tileSize
                return new Vector3((wallInfo.cellX + 1) * tileSize, currentFloorY, (wallInfo.cellY + 1) * tileSize);
            case "south":
                // Wall faces -Z (rotated 180°). Right axis is -X. Pivot is at right edge (-X side)
                // Wall should be at Z = cellY * tileSize
                return new Vector3(wallInfo.cellX * tileSize, currentFloorY, wallInfo.cellY * tileSize);
            case "east":
                // Wall faces +X (rotated 90°). Right axis is -Z. Pivot is at right edge (-Z side)
                // Wall should be at X = (cellX + 1) * tileSize
                return new Vector3((wallInfo.cellX + 1) * tileSize, currentFloorY, wallInfo.cellY * tileSize);
            case "west":
                // Wall faces -X (rotated -90°). Right axis is +Z. Pivot is at right edge (+Z side)
                // Wall should be at X = cellX * tileSize
                return new Vector3(wallInfo.cellX * tileSize, currentFloorY, (wallInfo.cellY + 1) * tileSize);
            default:
                return Vector3.zero;
        }
    }

    private Quaternion CalculateWallRotation(string direction)
    {
        switch (direction)
        {
            case "north": return Quaternion.Euler(0, 0, 0);
            case "south": return Quaternion.Euler(0, 180, 0);
            case "east": return Quaternion.Euler(0, 90, 0);
            case "west": return Quaternion.Euler(0, -90, 0);
            default: return Quaternion.identity;
        }
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < gridWidth && y < gridHeight;
    }

    private bool IsInBounds(Vector2Int position) => IsInBounds(position.x, position.y);

    // Создание нового этажа
    public FloorData CreateNewFloor(int floorIndex)
    {
        FloorData newFloor = new FloorData
        {
            floorPlan = new bool[gridWidth, gridHeight],
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            floorY = floorIndex * floorHeight,
            floorIndex = floorIndex
        };
        
        floors.Add(newFloor);
        Debug.Log($"🏗️ Создан этаж {floorIndex} на высоте {newFloor.floorY}");
        
        return newFloor;
    }

    // Генерация этажа при подходе к лестнице
    public void GenerateFloorOnDemand(int nextFloorIndex)
    {
        if (nextFloorIndex >= floors.Count)
        {
            CreateNewFloor(nextFloorIndex);
            // Здесь можно вызвать генерацию содержимого нового этажа
        }
        
        currentFloorIndex = nextFloorIndex;
    }

    // Получение статистики
    public int GetRoomCount() => roomCount;
    public int GetWallCount() => wallCount;
    public int GetGenerationTime() => totalGenerationTime;
    public int GetTotalFloors() => floors.Count;

    private void Update()
    {
        // Режим разработчика: перегенерация по Space
        if (devRegenerateAction.WasPressedThisFrame())
        {
            GenerateBuilding();
        }
    }

    private void PlaceThemedAssets()
    {
        // Create a parent object for all placed assets
        GameObject assetsParent = new GameObject("Room Assets");
        assetsParent.transform.SetParent(transform);

        // For each room, place themed assets
        foreach (var roomEntry in CurrentFloor.roomNodes)
        {
            RoomNode room = roomEntry.Value;

            // Calculate room center in world space
            float tileSize = 3f;
            Vector3 roomCenter = new Vector3(
                (room.position.x + room.size.x / 2f) * tileSize,
                CurrentFloor.floorY + 1f, // Y position at floor level
                (room.position.y + room.size.y / 2f) * tileSize + tileSize // Align with wall placement
            );

            // Use ThemeManager to place assets in the room
            if (ThemeManager.Instance != null)
            {
                int roomSizeInTiles = room.size.x * room.size.y;
                ThemeManager.Instance.PlaceAssetsInRoom(assetsParent.transform, room.theme, roomSizeInTiles, roomCenter);
            }
        }
    }

    private void PlaceStairsOptimized()
    {
        if (stairsPrefab == null)
        {
            Debug.LogWarning("⚠️ stairsPrefab не назначен! Пропускаем лестницы.");
            return;
        }

        float tileSize = 3f;
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        foreach (FloorData floor in floors)
        {
            // Проверяем, что центр - это пол (открытое пространство)
            if (centerX >= 0 && centerX < gridWidth && centerY >= 0 && centerY < gridHeight && floor.floorPlan[centerX, centerY])
            {
                Vector3 pos = new Vector3(
                    (centerX * tileSize) + tileSize * 0.5f,
                    floor.floorY + floorHeight * 0.4f,
                    (centerY * tileSize) + tileSize * 0.5f
                );

                GameObject stairsObj;
                if (useObjectPooling && poolManager != null)
                {
                    stairsObj = poolManager.GetObject(stairsPrefab);
                }
                else
                {
                    stairsObj = Instantiate(stairsPrefab, pos, Quaternion.identity, transform);
                }

                if (stairsObj != null)
                {
                    stairsObj.transform.position = pos;
                    stairsObj.transform.rotation = Quaternion.identity;
                    stairsObj.transform.SetParent(transform);
                    stairsObj.SetActive(true);
                    Debug.Log($"🪜 Лестница размещена на этаже {floor.floorIndex} в центре ({centerX},{centerY})");
                }
            }
        }
    }
}

// Вспомогательные структуры
public struct WallInfo
{
    public int cellX, cellY;
    public string dir;
}


// События генерации
public static class BuildingGeneratorEvents
{
    public static Action OnGenerationStarted;
    public static Action<RoomNode> OnRoomGenerated;
    public static Action<WallInfo> OnWallPlaced;
    public static Action OnGenerationCompleted;
}