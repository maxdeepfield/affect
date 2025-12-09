using UnityEngine;

public enum CellType
{
    Empty,
    Wall,
    Corridor,
    Room,
    Hall,
    Entrance,
    Door
}

public class GridCell
{
    public int X { get; set; }
    public int Z { get; set; }
    public CellType Type { get; set; }
    public bool NorthWall { get; set; }
    public bool SouthWall { get; set; }
    public bool EastWall { get; set; }
    public bool WestWall { get; set; }
    public bool NorthDoor { get; set; }
    public bool SouthDoor { get; set; }
    public bool EastDoor { get; set; }
    public bool WestDoor { get; set; }
    public int RoomId { get; set; }

    public GridCell(int x, int z)
    {
        X = x;
        Z = z;
        Type = CellType.Empty;
        NorthWall = false;
        SouthWall = false;
        EastWall = false;
        WestWall = false;
        NorthDoor = false;
        SouthDoor = false;
        EastDoor = false;
        WestDoor = false;
        RoomId = -1;
    }

    public Vector3 GetWorldPosition(float cellSize = 3f)
    {
        return new Vector3(X * cellSize, 0, Z * cellSize);
    }
}
