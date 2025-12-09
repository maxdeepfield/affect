using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject windowPrefab;
    [SerializeField] private GameObject elevatorPrefab;

    [Header("Building Size")]
    [SerializeField] private int buildingWidth = 20;
    [SerializeField] private int buildingDepth = 20;
    [SerializeField] private int floorsCount = 1;

    [Header("Generation Parameters")]
    [SerializeField] private int mainCorridorWidth = 2;
    [SerializeField] private int minRoomSize = 2;
    [SerializeField] private int maxRoomSize = 5;
    [SerializeField] private int corridorBranches = 3;
    [SerializeField] private int hallSize = 3;
    [SerializeField] private float backRoomDoorChance = 0.2f; // kept for tuning but corridor doors now prioritized
    [SerializeField] private int perimeterWindowSpacing = 2;
    [SerializeField] private float corridorRoomWindowChance = 0.05f;

    [Header("Settings")]
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private bool showDebugGizmos = false;

    private GridCell[,] grid;
    private System.Random random;
    private Transform buildingParent;

    private struct DoorCandidate
    {
        public GridCell Cell;
        public float Rotation;
        public bool IsCorridor;
        public int NeighborRoomId;
        public int X;
        public int Z;

        public DoorCandidate(GridCell cell, float rotation, bool isCorridor, int x, int z, int neighborRoomId = -1)
        {
            Cell = cell;
            Rotation = rotation;
            IsCorridor = isCorridor;
            NeighborRoomId = neighborRoomId;
            X = x;
            Z = z;
        }
    }

    private enum WallDir
    {
        North,
        South,
        East,
        West
    }

    [ContextMenu("Generate Building")]
    public void GenerateBuilding()
    {
        ClearBuilding();
        InitializeGrid();
        GenerateLayout();
        MarkWalls();
        List<DoorCandidate> chosenDoors = SelectRoomDoors();
        // Pre-mark door flags so windows/walls know not to cover door openings
        foreach (var d in chosenDoors)
        {
            SetDoorFlagBidirectional(d);
        }
        chosenDoors = EnsureReachability(chosenDoors);

        int floors = Mathf.Max(1, floorsCount);
        for (int i = 0; i < floors; i++)
        {
            Vector3 floorOffset = Vector3.up * (cellSize * i);
            Transform floorRoot = new GameObject($"Floor_{i}").transform;
            floorRoot.SetParent(buildingParent);

            Transform wallsParent = new GameObject("Walls").transform;
            wallsParent.SetParent(floorRoot);

            Transform doorsParent = new GameObject("Doors").transform;
            doorsParent.SetParent(floorRoot);

            Transform floorsParent = new GameObject("Floors").transform;
            floorsParent.SetParent(floorRoot);

            PlaceWalls(floorOffset, wallsParent);
            PlaceDoors(floorOffset, doorsParent, chosenDoors);
            PlaceFloors(floorOffset, floorsParent);
        }
    }

    [ContextMenu("Clear Building")]
    public void ClearBuilding()
    {
        if (buildingParent != null)
        {
            DestroyImmediate(buildingParent.gameObject);
        }
        buildingParent = new GameObject("Building").transform;
        buildingParent.SetParent(transform);
    }

    private void InitializeGrid()
    {
        if (useRandomSeed)
            seed = Random.Range(0, 999999);
        
        random = new System.Random(seed);
        grid = new GridCell[buildingWidth, buildingDepth];

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                grid[x, z] = new GridCell(x, z);
            }
        }
    }

    private void GenerateLayout()
    {
        int centerX = buildingWidth / 2;
        int hallZ = Mathf.Clamp(
            buildingDepth / 2 + random.Next(-buildingDepth / 8, buildingDepth / 8),
            mainCorridorWidth,
            buildingDepth - mainCorridorWidth - 1);

        GenerateMainCorridor(centerX);
        GenerateCrossCorridor(hallZ);
        GenerateHalls(centerX, hallZ);
        GenerateCorridorBranches(centerX);
        GenerateRooms();
    }

    private void GenerateMainCorridor(int centerX)
    {
        int halfWidth = Mathf.Max(0, mainCorridorWidth / 2);
        for (int z = 0; z < buildingDepth; z++)
        {
            for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
            {
                if (!IsInBounds(x, z)) continue;
                grid[x, z].Type = CellType.Corridor;
            }
        }

        if (IsInBounds(centerX, 0))
        {
            grid[centerX, 0].Type = CellType.Entrance;
        }
    }

    private void GenerateCrossCorridor(int hallZ)
    {
        int halfWidth = Mathf.Max(0, mainCorridorWidth / 2);
        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = hallZ - halfWidth; z <= hallZ + halfWidth; z++)
            {
                if (!IsInBounds(x, z)) continue;
                if (grid[x, z].Type == CellType.Empty)
                {
                    grid[x, z].Type = CellType.Corridor;
                }
            }
        }
    }

    private void GenerateHalls(int centerX, int hallZ)
    {
        int halfHall = Mathf.Max(1, hallSize) / 2;
        for (int dx = -halfHall; dx <= halfHall; dx++)
        {
            for (int dz = -halfHall; dz <= halfHall; dz++)
            {
                int nx = centerX + dx;
                int nz = hallZ + dz;
                if (!IsInBounds(nx, nz)) continue;
                grid[nx, nz].Type = CellType.Hall;
            }
        }
    }

    private void GenerateCorridorBranches(int centerX)
    {
        int attempts = Mathf.Max(1, corridorBranches);
        for (int i = 0; i < attempts; i++)
        {
            int startZ = random.Next(1, buildingDepth - 1);
            if (!IsCorridorLike(grid[centerX, startZ].Type))
                continue;

            int direction = random.Next(0, 2) == 0 ? -1 : 1;
            int length = random.Next(Mathf.Max(4, minRoomSize + 1), Mathf.Max(6, buildingWidth / 2));
            CarveHorizontalBranch(centerX, startZ, direction, length);
        }
    }

    private void GenerateRooms()
    {
        int roomId = 0;

        for (int z = 0; z < buildingDepth; z++)
        {
            for (int x = 0; x < buildingWidth; x++)
            {
                if (grid[x, z].Type != CellType.Empty)
                    continue;

                if (!HasCorridorNeighbor(x, z))
                    continue;

                TryPlaceRoom(x, z, ref roomId);
            }
        }
    }

    private void CarveHorizontalBranch(int startX, int startZ, int direction, int length)
    {
        for (int step = 0; step < length; step++)
        {
            int x = startX + direction * step;
            if (!IsInBounds(x, startZ))
                break;

            if (grid[x, startZ].Type == CellType.Empty)
            {
                grid[x, startZ].Type = CellType.Corridor;
            }
        }
    }

    private bool TryPlaceRoom(int startX, int startZ, ref int roomId)
    {
        int maxWidth = Mathf.Min(maxRoomSize, buildingWidth - startX);
        int maxDepth = Mathf.Min(maxRoomSize, buildingDepth - startZ);

        if (maxWidth < minRoomSize || maxDepth < minRoomSize)
            return false;

        int chosenWidth = 0;
        int chosenDepth = 0;

        for (int attempt = 0; attempt < 4 && chosenWidth == 0; attempt++)
        {
            int width = random.Next(minRoomSize, maxWidth + 1);
            int depth = random.Next(minRoomSize, maxDepth + 1);

            if (!AreaIsEmpty(startX, startZ, width, depth))
                continue;

            if (!RectTouchesCorridor(startX, startZ, width, depth))
                continue;

            chosenWidth = width;
            chosenDepth = depth;
        }

        if (chosenWidth == 0)
        {
            for (int width = maxWidth; width >= minRoomSize && chosenWidth == 0; width--)
            {
                for (int depth = maxDepth; depth >= minRoomSize && chosenDepth == 0; depth--)
                {
                    if (!AreaIsEmpty(startX, startZ, width, depth)) continue;
                    if (!RectTouchesCorridor(startX, startZ, width, depth)) continue;

                    chosenWidth = width;
                    chosenDepth = depth;
                }
            }
        }

        if (chosenWidth == 0)
            return false;

        for (int x = startX; x < startX + chosenWidth; x++)
        {
            for (int z = startZ; z < startZ + chosenDepth; z++)
            {
                grid[x, z].Type = CellType.Room;
                grid[x, z].RoomId = roomId;
            }
        }

        roomId++;
        return true;
    }

    private bool AreaIsEmpty(int startX, int startZ, int width, int depth)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + depth; z++)
            {
                if (!IsInBounds(x, z))
                    return false;

                if (grid[x, z].Type != CellType.Empty)
                    return false;
            }
        }

        return true;
    }

    private bool RectTouchesCorridor(int startX, int startZ, int width, int depth)
    {
        for (int x = startX; x < startX + width; x++)
        {
            int top = startZ - 1;
            int bottom = startZ + depth;
            if (IsInBounds(x, top) && IsCorridorLike(grid[x, top].Type)) return true;
            if (IsInBounds(x, bottom) && IsCorridorLike(grid[x, bottom].Type)) return true;
        }

        for (int z = startZ; z < startZ + depth; z++)
        {
            int left = startX - 1;
            int right = startX + width;
            if (IsInBounds(left, z) && IsCorridorLike(grid[left, z].Type)) return true;
            if (IsInBounds(right, z) && IsCorridorLike(grid[right, z].Type)) return true;
        }

        return false;
    }

    private bool HasCorridorNeighbor(int x, int z)
    {
        return (IsInBounds(x + 1, z) && IsCorridorLike(grid[x + 1, z].Type)) ||
               (IsInBounds(x - 1, z) && IsCorridorLike(grid[x - 1, z].Type)) ||
               (IsInBounds(x, z + 1) && IsCorridorLike(grid[x, z + 1].Type)) ||
               (IsInBounds(x, z - 1) && IsCorridorLike(grid[x, z - 1].Type));
    }

    private bool IsCorridorLike(CellType type)
    {
        return type == CellType.Corridor || type == CellType.Hall || type == CellType.Entrance;
    }

    private bool IsInBounds(int x, int z)
    {
        return x >= 0 && x < buildingWidth && z >= 0 && z < buildingDepth;
    }

    private void MarkWalls()
    {
        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];

                if (cell.Type == CellType.Empty || cell.Type == CellType.Wall)
                    continue;

                if (z + 1 >= buildingDepth || ShouldPlaceWall(x, z, x, z + 1))
                {
                    cell.NorthWall = true;
                }

                if (z - 1 < 0 || ShouldPlaceWall(x, z, x, z - 1))
                {
                    cell.SouthWall = true;
                }

                if (x + 1 >= buildingWidth || ShouldPlaceWall(x, z, x + 1, z))
                {
                    cell.EastWall = true;
                }

                if (x - 1 < 0 || ShouldPlaceWall(x, z, x - 1, z))
                {
                    cell.WestWall = true;
                }
            }
        }
    }

    private List<DoorCandidate> SelectRoomDoors()
    {
        Dictionary<int, DoorCandidate> chosenDoors = new Dictionary<int, DoorCandidate>();

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];
                if (cell.Type != CellType.Room || cell.RoomId < 0)
                    continue;

                List<DoorCandidate> corridorCandidates = new List<DoorCandidate>();
                List<DoorCandidate> roomCandidates = new List<DoorCandidate>();

                // Corridor-like neighbors (prefer these)
                if (z + 1 < buildingDepth && IsCorridorLike(grid[x, z + 1].Type))
                    corridorCandidates.Add(new DoorCandidate(cell, 0, true, x, z));
                if (z - 1 >= 0 && IsCorridorLike(grid[x, z - 1].Type))
                    corridorCandidates.Add(new DoorCandidate(cell, 180, true, x, z));
                if (x + 1 < buildingWidth && IsCorridorLike(grid[x + 1, z].Type))
                    corridorCandidates.Add(new DoorCandidate(cell, 90, true, x, z));
                if (x - 1 >= 0 && IsCorridorLike(grid[x - 1, z].Type))
                    corridorCandidates.Add(new DoorCandidate(cell, 270, true, x, z));

                // Adjacent rooms (fallback, back-office)
                if (z + 1 < buildingDepth && grid[x, z + 1].Type == CellType.Room &&
                    grid[x, z + 1].RoomId != cell.RoomId)
                    roomCandidates.Add(new DoorCandidate(cell, 0, false, x, z, grid[x, z + 1].RoomId));
                if (z - 1 >= 0 && grid[x, z - 1].Type == CellType.Room &&
                    grid[x, z - 1].RoomId != cell.RoomId)
                    roomCandidates.Add(new DoorCandidate(cell, 180, false, x, z, grid[x, z - 1].RoomId));
                if (x + 1 < buildingWidth && grid[x + 1, z].Type == CellType.Room &&
                    grid[x + 1, z].RoomId != cell.RoomId)
                    roomCandidates.Add(new DoorCandidate(cell, 90, false, x, z, grid[x + 1, z].RoomId));
                if (x - 1 >= 0 && grid[x - 1, z].Type == CellType.Room &&
                    grid[x - 1, z].RoomId != cell.RoomId)
                    roomCandidates.Add(new DoorCandidate(cell, 270, false, x, z, grid[x - 1, z].RoomId));

                DoorCandidate? chosen = null;

                // Guarantee corridor access when possible
                if (corridorCandidates.Count > 0)
                {
                    chosen = corridorCandidates[random.Next(corridorCandidates.Count)];
                }
                else if (roomCandidates.Count > 0)
                {
                    // only if no corridor option at all
                    chosen = roomCandidates[random.Next(roomCandidates.Count)];
                }

                if (chosen.HasValue)
                {
                    chosenDoors[cell.RoomId] = chosen.Value;
                }
            }
        }

        return new List<DoorCandidate>(chosenDoors.Values);
    }
    private void PlaceWalls(Vector3 floorOffset, Transform wallsParent)
    {
        HashSet<string> wallKeys = new HashSet<string>();

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];
                
                if (cell.Type == CellType.Empty || cell.Type == CellType.Wall)
                    continue;

                if (z + 1 >= buildingDepth || ShouldPlaceWall(x, z, x, z + 1))
                {
                    cell.NorthWall = true;
                    if (!HasDoorOnEdge(x, z, WallDir.North))
                    {
                        bool asWindow = ShouldPlaceWindow(cell.Type, z + 1 >= buildingDepth ? CellType.Empty : grid[x, z + 1].Type, z + 1 >= buildingDepth, WallDir.North, x, z);
                        PlaceUniqueWallSegment(cell.GetWorldPosition(cellSize) + floorOffset, 0, wallsParent, wallKeys, asWindow);
                    }
                }

                if (z - 1 < 0 || ShouldPlaceWall(x, z, x, z - 1))
                {
                    cell.SouthWall = true;
                    if (!HasDoorOnEdge(x, z, WallDir.South))
                    {
                        bool asWindow = ShouldPlaceWindow(cell.Type, z - 1 < 0 ? CellType.Empty : grid[x, z - 1].Type, z - 1 < 0, WallDir.South, x, z);
                        PlaceUniqueWallSegment(cell.GetWorldPosition(cellSize) + floorOffset, 180, wallsParent, wallKeys, asWindow);
                    }
                }

                if (x + 1 >= buildingWidth || ShouldPlaceWall(x, z, x + 1, z))
                {
                    cell.EastWall = true;
                    if (!HasDoorOnEdge(x, z, WallDir.East))
                    {
                        bool asWindow = ShouldPlaceWindow(cell.Type, x + 1 >= buildingWidth ? CellType.Empty : grid[x + 1, z].Type, x + 1 >= buildingWidth, WallDir.East, x, z);
                        PlaceUniqueWallSegment(cell.GetWorldPosition(cellSize) + floorOffset, 90, wallsParent, wallKeys, asWindow);
                    }
                }

                if (x - 1 < 0 || ShouldPlaceWall(x, z, x - 1, z))
                {
                    cell.WestWall = true;
                    if (!HasDoorOnEdge(x, z, WallDir.West))
                    {
                        bool asWindow = ShouldPlaceWindow(cell.Type, x - 1 < 0 ? CellType.Empty : grid[x - 1, z].Type, x - 1 < 0, WallDir.West, x, z);
                        PlaceUniqueWallSegment(cell.GetWorldPosition(cellSize) + floorOffset, 270, wallsParent, wallKeys, asWindow);
                    }
                }
            }
        }
    }

    private bool ShouldPlaceWall(int x1, int z1, int x2, int z2)
    {
        CellType type1 = grid[x1, z1].Type;
        CellType type2 = grid[x2, z2].Type;

        if (type2 == CellType.Empty || type2 == CellType.Wall)
            return true;

        if ((type1 == CellType.Room || type1 == CellType.Corridor || type1 == CellType.Hall || type1 == CellType.Entrance) &&
            (type2 == CellType.Room || type2 == CellType.Corridor || type2 == CellType.Hall || type2 == CellType.Entrance))
        {
            if (type1 == CellType.Room && type2 == CellType.Room)
            {
                return grid[x1, z1].RoomId != grid[x2, z2].RoomId;
            }

            if ((type1 == CellType.Room && type2 != CellType.Room) ||
                (type1 != CellType.Room && type2 == CellType.Room))
            {
                return true;
            }
        }

        return false;
    }

    private void PlaceDoors(Vector3 floorOffset, Transform doorsParent, List<DoorCandidate> chosenDoors)
    {
        foreach (var chosen in chosenDoors)
        {
            SetDoorFlagBidirectional(chosen);
            PlaceDoorSegment(chosen.Cell.GetWorldPosition(cellSize) + floorOffset, chosen.Rotation, doorsParent);
        }
    }

    private void SetDoorFlag(GridCell cell, float rotation)
    {
        switch ((int)rotation)
        {
            case 0:
                cell.NorthDoor = true;
                break;
            case 180:
                cell.SouthDoor = true;
                break;
            case 90:
                cell.EastDoor = true;
                break;
            case 270:
                cell.WestDoor = true;
                break;
        }
    }

    private void SetDoorFlagBidirectional(DoorCandidate candidate)
    {
        SetDoorFlag(candidate.Cell, candidate.Rotation);

        int nx = candidate.X;
        int nz = candidate.Z;
        switch ((int)candidate.Rotation)
        {
            case 0:
                nz += 1;
                break;
            case 180:
                nz -= 1;
                break;
            case 90:
                nx += 1;
                break;
            case 270:
                nx -= 1;
                break;
        }

        if (IsInBounds(nx, nz))
        {
            GridCell neighbor = grid[nx, nz];
            switch ((int)candidate.Rotation)
            {
                case 0:
                    neighbor.SouthDoor = true;
                    break;
                case 180:
                    neighbor.NorthDoor = true;
                    break;
                case 90:
                    neighbor.WestDoor = true;
                    break;
                case 270:
                    neighbor.EastDoor = true;
                    break;
            }
        }
    }

    private bool CanTraverse(int x, int z, int nx, int nz)
    {
        if (!IsInBounds(nx, nz)) return false;

        GridCell a = grid[x, z];
        GridCell b = grid[nx, nz];
        if (a.Type == CellType.Empty || a.Type == CellType.Wall) return false;
        if (b.Type == CellType.Empty || b.Type == CellType.Wall) return false;

        // disallow leaving building bounds; already checked

        if (nx == x && nz == z + 1)
        {
            if (a.NorthWall && !a.NorthDoor) return false;
            if (b.SouthWall && !b.SouthDoor) return false;
            return true;
        }
        if (nx == x && nz == z - 1)
        {
            if (a.SouthWall && !a.SouthDoor) return false;
            if (b.NorthWall && !b.NorthDoor) return false;
            return true;
        }
        if (nx == x + 1 && nz == z)
        {
            if (a.EastWall && !a.EastDoor) return false;
            if (b.WestWall && !b.WestDoor) return false;
            return true;
        }
        if (nx == x - 1 && nz == z)
        {
            if (a.WestWall && !a.WestDoor) return false;
            if (b.EastWall && !b.EastDoor) return false;
            return true;
        }

        return false;
    }

    private List<DoorCandidate> EnsureReachability(List<DoorCandidate> doors)
    {
        bool[,] visited = new bool[buildingWidth, buildingDepth];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<int, DoorCandidate> doorByRoom = new Dictionary<int, DoorCandidate>();
        foreach (var d in doors)
        {
            if (!doorByRoom.ContainsKey(d.Cell.RoomId))
                doorByRoom[d.Cell.RoomId] = d;
        }

        Vector2Int start = new Vector2Int(buildingWidth / 2, buildingDepth / 2);
        if (!IsWalkable(start.x, start.y))
        {
            int centerX = buildingWidth / 2;
            start = new Vector2Int(centerX, 0);
            if (!IsWalkable(start.x, start.y))
            {
                bool found = false;
                for (int z = 0; z < buildingDepth && !found; z++)
                {
                    for (int x = 0; x < buildingWidth && !found; x++)
                    {
                        if (IsCorridorLike(grid[x, z].Type))
                        {
                            start = new Vector2Int(x, z);
                            found = true;
                        }
                    }
                }
                if (!found) return doors; // nothing to do
            }
        }

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i];
                int nz = p.y + dz[i];
                if (!IsInBounds(nx, nz) || visited[nx, nz]) continue;
                if (CanTraverse(p.x, p.y, nx, nz))
                {
                    visited[nx, nz] = true;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }

        List<DoorCandidate> updated = new List<DoorCandidate>(doors);

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];
                if (cell.Type != CellType.Room || visited[x, z])
                    continue;

                // Try to punch a door to any adjacent corridor-like cell
                if (z + 1 < buildingDepth && IsCorridorLike(grid[x, z + 1].Type))
                {
                    AddDoorIfMissing(doorByRoom, updated, cell, 0, x, z);
                    continue;
                }
                if (z - 1 >= 0 && IsCorridorLike(grid[x, z - 1].Type))
                {
                    AddDoorIfMissing(doorByRoom, updated, cell, 180, x, z);
                    continue;
                }
                if (x + 1 < buildingWidth && IsCorridorLike(grid[x + 1, z].Type))
                {
                    AddDoorIfMissing(doorByRoom, updated, cell, 90, x, z);
                    continue;
                }
                if (x - 1 >= 0 && IsCorridorLike(grid[x - 1, z].Type))
                {
                    AddDoorIfMissing(doorByRoom, updated, cell, 270, x, z);
                    continue;
                }
            }
        }

        // Recompute reachability once after fixes (optional but cheap)
        visited = new bool[buildingWidth, buildingDepth];
        queue.Clear();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i];
                int nz = p.y + dz[i];
                if (!IsInBounds(nx, nz) || visited[nx, nz]) continue;
                if (CanTraverse(p.x, p.y, nx, nz))
                {
                    visited[nx, nz] = true;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }

        // Remove any rooms still unreachable by forcing a door to corridor if present
        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];
                if (cell.Type != CellType.Room || visited[x, z])
                    continue;

                // As last resort, if corridor-like neighbor exists, add door
                if (z + 1 < buildingDepth && IsCorridorLike(grid[x, z + 1].Type))
                    AddDoorIfMissing(doorByRoom, updated, cell, 0, x, z);
                else if (z - 1 >= 0 && IsCorridorLike(grid[x, z - 1].Type))
                    AddDoorIfMissing(doorByRoom, updated, cell, 180, x, z);
                else if (x + 1 < buildingWidth && IsCorridorLike(grid[x + 1, z].Type))
                    AddDoorIfMissing(doorByRoom, updated, cell, 90, x, z);
                else if (x - 1 >= 0 && IsCorridorLike(grid[x - 1, z].Type))
                    AddDoorIfMissing(doorByRoom, updated, cell, 270, x, z);
            }
        }

        return new List<DoorCandidate>(doorByRoom.Values);
    }

    private void AddDoorIfMissing(Dictionary<int, DoorCandidate> doorByRoom, List<DoorCandidate> list, GridCell cell, float rotation, int x, int z)
    {
        if (HasDoorOnEdge(x, z, RotationToDir(rotation)))
            return;

        if (doorByRoom.ContainsKey(cell.RoomId))
            return;

        DoorCandidate candidate = new DoorCandidate(cell, rotation, true, x, z);
        doorByRoom[cell.RoomId] = candidate;
        list.Add(candidate);
        SetDoorFlagBidirectional(candidate);
    }

    private WallDir RotationToDir(float rotation)
    {
        switch ((int)rotation)
        {
            case 0: return WallDir.North;
            case 180: return WallDir.South;
            case 90: return WallDir.East;
            case 270: return WallDir.West;
        }
        return WallDir.North;
    }

    private bool IsWalkable(int x, int z)
    {
        if (!IsInBounds(x, z)) return false;
        CellType t = grid[x, z].Type;
        return t != CellType.Empty && t != CellType.Wall;
    }

    private void PlaceFloors(Vector3 floorOffset, Transform floorsParent)
    {
        Vector2Int liftCell = new Vector2Int(buildingWidth / 2, buildingDepth / 2);

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];
                
                if (x == liftCell.x && z == liftCell.y)
                {
                    Vector3 holePos = cell.GetWorldPosition(cellSize) + floorOffset + Vector3.down * 1.5f + Vector3.back * 1.5f;

                    if (Mathf.Approximately(floorOffset.y, 0f))
                    {
                        // Place the actual elevator only on the first floor
                        if (elevatorPrefab != null)
                        {
                            GameObject lift = Instantiate(elevatorPrefab, holePos, Quaternion.identity, floorsParent);
                            ElevatorController controller = lift.GetComponentInChildren<ElevatorController>();
                            if (controller != null)
                            {
                                controller.SetFloors(floorsCount);
                                controller.SetFloorHeight(cellSize);
                            }
                        }
                        else
                        {
                            GameObject marker = new GameObject("LiftPlace");
                            marker.transform.SetPositionAndRotation(holePos, Quaternion.identity);
                            marker.transform.SetParent(floorsParent);
                        }
                    }

                    // Upper floors keep the hole, no prefab needed
                    continue;
                }

                if (cell.Type != CellType.Empty && cell.Type != CellType.Wall)
                {
                    Vector3 position = cell.GetWorldPosition(cellSize) + floorOffset + Vector3.down * 1.5f;
                    GameObject floor = Instantiate(floorPrefab, position, Quaternion.identity, floorsParent);
                    floor.name = $"Floor_{x}_{z}";
                }
            }
        }
    }

    private void PlaceWallSegment(Vector3 position, float rotation, Transform parent, bool asWindow)
    {
        GameObject prefabToUse = asWindow && windowPrefab != null ? windowPrefab : wallPrefab;
        if (prefabToUse == null) return;

        Quaternion rot = Quaternion.Euler(0, rotation, 0);
        Vector3 spawnPos = GetWallSpawnPosition(position, rotation);

        GameObject wall = Instantiate(prefabToUse, spawnPos, rot, parent);
        wall.name = $"{(asWindow ? "Window" : "Wall")}_{rotation}";
    }

    private void PlaceUniqueWallSegment(Vector3 position, float rotation, Transform parent, HashSet<string> wallKeys, bool asWindow)
    {
        Vector3 spawnPos = GetWallSpawnPosition(position, rotation);
        string key = GetWallKey(spawnPos, rotation);
        if (wallKeys.Contains(key))
            return;

        wallKeys.Add(key);
        PlaceWallSegment(position, rotation, parent, asWindow);
    }

    private string GetWallKey(Vector3 position, float rotation)
    {
        int px = Mathf.RoundToInt(position.x * 1000f);
        int py = Mathf.RoundToInt(position.y * 1000f);
        int pz = Mathf.RoundToInt(position.z * 1000f);
        int rot = Mathf.Abs(Mathf.RoundToInt(rotation) % 180); // 0==180, 90==270
        return $"{px}_{py}_{pz}_{rot}";
    }

    private Vector3 GetWallSpawnPosition(Vector3 position, float rotation)
    {
        Vector3 offset = Vector3.zero;

        if (rotation == 0)
            offset = new Vector3(0, 0, cellSize / 2f);
        else if (rotation == 180)
            offset = new Vector3(0, 0, -cellSize / 2f);
        else if (rotation == 90)
            offset = new Vector3(cellSize / 2f, 0, 0);
        else if (rotation == 270)
            offset = new Vector3(-cellSize / 2f, 0, 0);

        return position + offset;
    }

    private bool ShouldPlaceWindow(CellType current, CellType neighbor, bool neighborOutside, WallDir direction, int x, int z)
    {
        if (windowPrefab == null)
            return false;

        if (current == CellType.Empty || current == CellType.Wall)
            return false;

        if (HasDoorOnEdge(x, z, direction))
            return false;

        int spacing = Mathf.Max(1, perimeterWindowSpacing);

        if (neighborOutside)
        {
            bool aligned = (direction == WallDir.North || direction == WallDir.South)
                ? (x % spacing == 0)
                : (z % spacing == 0);
            return aligned;
        }

        bool corridorToRoom = (IsCorridorLike(current) && neighbor == CellType.Room) ||
                              (current == CellType.Room && IsCorridorLike(neighbor));
        if (corridorToRoom)
        {
            return random.NextDouble() < corridorRoomWindowChance;
        }

        return false;
    }

    private bool HasDoorOnEdge(int x, int z, WallDir dir)
    {
        if (!IsInBounds(x, z)) return false;

        GridCell cell = grid[x, z];
        switch (dir)
        {
            case WallDir.North:
                if (cell.NorthDoor) return true;
                if (IsInBounds(x, z + 1) && grid[x, z + 1].SouthDoor) return true;
                break;
            case WallDir.South:
                if (cell.SouthDoor) return true;
                if (IsInBounds(x, z - 1) && grid[x, z - 1].NorthDoor) return true;
                break;
            case WallDir.East:
                if (cell.EastDoor) return true;
                if (IsInBounds(x + 1, z) && grid[x + 1, z].WestDoor) return true;
                break;
            case WallDir.West:
                if (cell.WestDoor) return true;
                if (IsInBounds(x - 1, z) && grid[x - 1, z].EastDoor) return true;
                break;
        }

        return false;
    }

    private void PlaceDoorSegment(Vector3 position, float rotation, Transform parent)
    {
        if (doorPrefab == null) return;

        Quaternion rot = Quaternion.Euler(0, rotation, 0);
        Vector3 offset = Vector3.zero;

        if (rotation == 0)
            offset = new Vector3(0, 0, cellSize / 2);
        else if (rotation == 180)
            offset = new Vector3(0, 0, -cellSize / 2);
        else if (rotation == 90)
            offset = new Vector3(cellSize / 2, 0, 0);
        else if (rotation == 270)
            offset = new Vector3(-cellSize / 2, 0, 0);

        GameObject door = Instantiate(doorPrefab, position + offset, rot, parent);
        door.name = $"Door_{rotation}";
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || grid == null) return;

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                GridCell cell = grid[x, z];
                Vector3 pos = cell.GetWorldPosition(cellSize);

                switch (cell.Type)
                {
                    case CellType.Empty:
                        Gizmos.color = Color.black;
                        break;
                    case CellType.Corridor:
                        Gizmos.color = Color.yellow;
                        break;
                    case CellType.Room:
                        Gizmos.color = Color.blue;
                        break;
                    case CellType.Hall:
                        Gizmos.color = Color.green;
                        break;
                    case CellType.Entrance:
                        Gizmos.color = Color.red;
                        break;
                }

                Gizmos.DrawCube(pos + Vector3.up * 0.1f, new Vector3(cellSize * 0.8f, 0.1f, cellSize * 0.8f));
            }
        }
    }
}
