using System.Collections.Generic;
using UnityEngine;

namespace _SCRIPTS
{
    /// Summary:
    /// Spatial Grid система для быстрой проверки коллизий в процедурной генерации
    /// Делит карту на сектора для ускорения поиска соседних объектов
    ///
    public class SpatialGrid
    {
        public int SectorWidth { get; private set; }
        public int SectorHeight { get; private set; }
        private int sectorSize;
        private Dictionary<Vector2Int, List<RoomNode>> grid;
        
        /// Summary:
        /// Создать spatial grid
        ///
        /// Param gridWidth: Ширина всей карты
        /// Param gridHeight: Высота всей карты
        /// Param sectorSize: Размер сектора (рекомендуется 5-10)
        public SpatialGrid(int gridWidth, int gridHeight, int sectorSize = 5)
        {
            this.sectorSize = sectorSize;
            this.SectorWidth = Mathf.CeilToInt((float)gridWidth / sectorSize);
            this.SectorHeight = Mathf.CeilToInt((float)gridHeight / sectorSize);
            this.grid = new Dictionary<Vector2Int, List<RoomNode>>();
            
            Debug.Log($"🌐 Создана Spatial Grid: {this.SectorWidth}x{this.SectorHeight} секторов (размер сектора: {sectorSize})");
        }

        /// Summary:
        /// Добавить объект в сектор
        ///
        public void AddObject(Vector2Int position, RoomNode room)
        {
            var sector = GetSector(position);
            
            if (!grid.ContainsKey(sector))
            {
                grid[sector] = new List<RoomNode>();
            }
            
            grid[sector].Add(room);
        }

        /// Summary:
        /// Удалить объект из сектора
        ///
        public bool RemoveObject(Vector2Int position, RoomNode room)
        {
            var sector = GetSector(position);
            
            if (grid.ContainsKey(sector))
            {
                for (int i = grid[sector].Count - 1; i >= 0; i--)
                {
                    if (grid[sector][i].position == room.position && 
                        grid[sector][i].size == room.size)
                    {
                        grid[sector].RemoveAt(i);
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// Summary:
        /// Проверить наличие объектов в секторе
        ///
        public bool HasObjectsInSector(int sectorX, int sectorY)
        {
            var sector = new Vector2Int(sectorX, sectorY);
            return grid.ContainsKey(sector) && grid[sector].Count > 0;
        }

        /// Summary:
        /// Получить все объекты в секторе
        ///
        public List<RoomNode> GetObjectsInSector(int sectorX, int sectorY)
        {
            var sector = new Vector2Int(sectorX, sectorY);
            
            if (grid.ContainsKey(sector))
            {
                return grid[sector];
            }
            
            return new List<RoomNode>();
        }

        /// Summary:
        /// Получить объекты в радиусе секторов
        ///
        public List<RoomNode> GetObjectsInRadius(Vector2Int position, int radiusSectors = 1)
        {
            var nearbyObjects = new List<RoomNode>();
            var centerSector = GetSector(position);
            
            for (int x = -radiusSectors; x <= radiusSectors; x++)
            {
                for (int y = -radiusSectors; y <= radiusSectors; y++)
                {
                    var sector = new Vector2Int(centerSector.x + x, centerSector.y + y);
                    
                    if (grid.ContainsKey(sector))
                    {
                        nearbyObjects.AddRange(grid[sector]);
                    }
                }
            }
            
            return nearbyObjects;
        }

        /// Summary:
        /// Проверить коллизии в радиусе
        ///
        public bool HasCollisionsInRange(Vector2Int position, Vector2Int size, int radiusSectors = 1)
        {
            var nearbyObjects = GetObjectsInRadius(position, radiusSectors);
            
            foreach (var nearbyRoom in nearbyObjects)
            {
                if (RoomsOverlap(position, size, nearbyRoom.position, nearbyRoom.size))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// Summary:
        /// Получить все объекты в grid
        ///
        public List<RoomNode> GetAllObjects()
        {
            var allObjects = new List<RoomNode>();
            
            foreach (var sectorObjects in grid.Values)
            {
                allObjects.AddRange(sectorObjects);
            }
            
            return allObjects;
        }

        /// Summary:
        /// Очистить grid
        ///
        public void Clear()
        {
            grid.Clear();
        }

        /// Summary:
        /// Получить статистику по заполненности секторов
        ///
        public string GetStatistics()
        {
            int totalSectors = SectorWidth * SectorHeight;
            int occupiedSectors = grid.Count;
            int totalObjects = 0;
            
            foreach (var sectorObjects in grid.Values)
            {
                totalObjects += sectorObjects.Count;
            }
            
            float occupancyRate = (float)occupiedSectors / totalSectors * 100f;
            float avgObjectsPerSector = occupiedSectors > 0 ? (float)totalObjects / occupiedSectors : 0;
            
            return $"📊 Spatial Grid Статистика:\n" +
                   $"  • Секторов: {occupiedSectors}/{totalSectors} ({occupancyRate:F1}%)\n" +
                   $"  • Объектов: {totalObjects}\n" +
                   $"  • Среднее на сектор: {avgObjectsPerSector:F1}";
        }

        /// Summary:
        /// Получить сектор по позиции
        ///
        private Vector2Int GetSector(Vector2Int position)
        {
            return new Vector2Int(position.x / sectorSize, position.y / sectorSize);
        }

        /// Summary:
        /// Проверка пересечения комнат
        ///
        private bool RoomsOverlap(Vector2Int pos1, Vector2Int size1, Vector2Int pos2, Vector2Int size2)
        {
            return pos1.x < pos2.x + size2.x &&
                   pos1.x + size1.x > pos2.x &&
                   pos1.y < pos2.y + size2.y &&
                   pos1.y + size1.y > pos2.y;
        }
    }
}
