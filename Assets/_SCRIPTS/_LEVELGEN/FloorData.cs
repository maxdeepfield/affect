using System;
using System.Collections.Generic;
using UnityEngine;
using _SCRIPTS;

[System.Serializable]
public class FloorData
{
    public bool[,] floorPlan;
    public Dictionary<Vector2Int, RoomNode> roomNodes = new Dictionary<Vector2Int, RoomNode>();
    public List<Vector2Int> stairUpPositions = new List<Vector2Int>();   // Where stairs go UP to next floor
    public List<Vector2Int> stairDownPositions = new List<Vector2Int>(); // Where stairs go DOWN to previous floor
    public int gridWidth;
    public int gridHeight;
    public float floorY; // World Y position of this floor
    public int floorIndex;
    
    public FloorData Clone()
    {
        FloorData clone = new FloorData
        {
            floorPlan = (bool[,])this.floorPlan.Clone(),
            roomNodes = new Dictionary<Vector2Int, RoomNode>(this.roomNodes),
            stairUpPositions = new List<Vector2Int>(this.stairUpPositions),
            stairDownPositions = new List<Vector2Int>(this.stairDownPositions),
            gridWidth = this.gridWidth,
            gridHeight = this.gridHeight,
            floorY = this.floorY,
            floorIndex = this.floorIndex
        };
        return clone;
    }
}

public enum StairType
{
    None,
    Up,
    Down
}