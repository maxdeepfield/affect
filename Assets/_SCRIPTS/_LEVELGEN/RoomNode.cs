using UnityEngine;

[System.Serializable]
public struct RoomNode
{
    public Vector2Int position;  // Bottom-left corner of the room
    public Vector2Int size;      // Size of the room (width, height)
    public Vector2Int center;    // Center of the room
    public RoomTheme theme;      // The theme of this room

    public RoomNode(int x, int y, int width, int height)
    {
        position = new Vector2Int(x, y);
        size = new Vector2Int(width, height);
        center = new Vector2Int(x + width / 2, y + height / 2);
        theme = RoomTheme.Office; // Default theme
    }

    public bool Contains(Vector2Int point)
    {
        return point.x >= position.x && point.x < position.x + size.x &&
               point.y >= position.y && point.y < position.y + size.y;
    }

    public float Area()
    {
        return size.x * size.y;
    }
}