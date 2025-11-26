
# 🔍 Door Placement Logic Analysis - OptimizedBuildingGenerator.cs

## Current Problem (Engine v1)

**Root Cause**: Two separate door placement passes create duplicate/overlapping doors:

```
1. EXTERIOR WALLS (PlaceWallSegmentsWithPooling()):
   - Places 1 entrance door randomly on ANY boundary wall
   - Places windows randomly on remaining exterior walls
   - ✅ CORRECT for outer building perimeter

2. INTERIOR DOORS (interiorDoorwayPositions):
   ❌ BUG: Places doors on ALL room edges touching ANY floor
   - Includes room-to-room connections (WRONG)
   - Includes corridor-corridor connections (WRONG)  
   - Includes room-to-corridor (CORRECT - but duplicates possible)
```

**Result**: 
```
🚪 Too many doors everywhere
🚪 Doors between same room cells
🚪 No intelligent room-corridor distinction
```

## Current Code Flow

```
GenerateRooms() → ConnectRoomsWithCorridors() → PlaceFloors() → PlaceWalls()
                                                              ↓
                                    PlaceWallSegmentsWithPooling()
                                    ├─ Exterior walls + 1 entrance
                                    └─ ALL room boundary edges ← BUG HERE
```

## Engine v2 Architecture

```
STEP 1: Generate Rooms + Corridors (unchanged)
STEP 2: Build ROOM BOUNDARY GRAPH
        ├─ Each room gets perimeter edges
        └─ Track corridor connections per edge
STEP 3: Place Exterior Walls (improved)
        ├─ 1 entrance door (random exterior)
        └─ Windows on remaining exterior  
STEP 4: Place Interior Doors (INTELLIGENT)
        └─ ONLY edges where room touches corridor
        └─ Max 2-4 doors per room (controlled)
```

## New Data Structures

```csharp
// Track corridor cells separately from rooms
private HashSet<Vector2Int> corridorCells = new HashSet<Vector2Int>();

// Room boundary edges with connection type
public struct RoomEdge 
{ 
    public Vector2Int position; 
    public string direction; 
    public bool touchesCorridor;  // NEW
    public bool isExterior;       // NEW  
}

// Doors only where room-perimeter touches corridor
private List<RoomEdge> validDoorPositions = new List<RoomEdge>();
```

## Algorithm Flow (Pseudocode)

```
1. After corridor carving:
   corridorCells.AddAll(corridor path cells)

2. For each room:
   for each perimeter edge:
       adjacentCell = edge + direction
       if adjacentCell in corridorCells:
           validDoorPositions.Add(edge)
           break // 1 door per connection max

3. Place doors:
   foreach validDoorPosition:
       PlaceInteriorDoor()
```

## Benefits
```
✅ Exactly 1 door per room-corridor connection
✅ No intra-room doors  
✅ No corridor-corridor doors
✅ Controlled door count per room (max 4)
✅ Maintains performance optimizations
```

**Next**: Implement in Code mode as `OptimizedBuildingGeneratorV2.cs`