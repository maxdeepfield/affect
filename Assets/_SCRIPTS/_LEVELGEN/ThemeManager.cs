using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomTheme
{
    Office,
    Residential,
    Industrial,
    Laboratory,
    Medical,
    Storage,
    Recreation,
    Security,
    Hall
}

[Serializable]
public struct ThemeAsset
{
    public string name;
    public GameObject prefab;
    public int weight; // For weighted random selection
    public Vector3 offset; // Position offset from center
    public Vector3 scale; // Scale to apply
}

public class ThemeManager : MonoBehaviour
{
    [Header("Theme Configuration")]
    public List<RoomThemeData> themes = new List<RoomThemeData>();

    [Header("General Assets")]
    public List<GameObject> commonAssets = new List<GameObject>(); // Generic assets that can appear anywhere

    [Header("🏗️ Hall & Stairs")]
    [Tooltip("Assets специфичные для залов с лестницами")]
    public List<GameObject> hallAssets = new List<GameObject>();
    [Tooltip("Префабы лестниц для разных этажей")]
    public List<GameObject> stairsAssets = new List<GameObject>();
    [Tooltip("Высотные лестницы (между этажами)")]
    public List<GameObject> multiFloorStairsAssets = new List<GameObject>();

    private static ThemeManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static ThemeManager Instance
    {
        get
        {
            if (instance == null)
            {
                #if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<ThemeManager>();
                #else
                #pragma warning disable 618
                instance = FindObjectOfType<ThemeManager>();
                #pragma warning restore 618
                #endif
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("ThemeManager");
                    instance = managerObj.AddComponent<ThemeManager>();
                }
            }
            return instance;
        }
    }

    public List<GameObject> GetAssetsForTheme(RoomTheme theme)
    {
        for (int i = 0; i < themes.Count; i++)
        {
            if (themes[i].theme == theme)
            {
                List<GameObject> result = new List<GameObject>();
                foreach (var asset in themes[i].assets)
                {
                    for (int j = 0; j < asset.weight; j++)
                    {
                        result.Add(asset.prefab);
                    }
                }
                return result;
            }
        }
        return new List<GameObject>();
    }

    public GameObject GetRandomAssetForTheme(RoomTheme theme)
    {
        var assets = GetAssetsForTheme(theme);
        if (assets.Count == 0) return null;
        
        return assets[UnityEngine.Random.Range(0, assets.Count)];
    }

    public GameObject GetRandomHallAsset()
    {
        if (hallAssets.Count == 0) return null;
        return hallAssets[UnityEngine.Random.Range(0, hallAssets.Count)];
    }

    public GameObject GetRandomStairsAsset(int floorIndex = 0)
    {
        var stairsList = stairsAssets;
        
        // Выбираем разные лестницы для разных этажей
        if (floorIndex > 0 && multiFloorStairsAssets.Count > 0)
        {
            stairsList = multiFloorStairsAssets;
        }
        
        if (stairsList.Count == 0) return null;
        return stairsList[UnityEngine.Random.Range(0, stairsList.Count)];
    }

    public void PlaceAssetsInRoom(Transform parent, RoomTheme theme, int roomSize, Vector3 roomCenter, float density = 0.3f)
    {
        var themeAssets = GetAssetsForTheme(theme);
        if (themeAssets.Count == 0) return;

        int maxAssets = Mathf.RoundToInt(roomSize * density);
        
        // Add common assets to the mix
        var allAssets = new List<GameObject>(themeAssets);
        foreach (var commonAsset in commonAssets)
        {
            allAssets.Add(commonAsset);
        }

        // Special handling for hall rooms
        if (theme == RoomTheme.Hall)
        {
            // Добавляем hall-специфичные активы
            foreach (var hallAsset in hallAssets)
            {
                if (hallAsset != null) allAssets.Add(hallAsset);
            }
            
            // Лестничные активы имеют особую обработку
            maxAssets = Mathf.RoundToInt(roomSize * density * 1.5f); // Больше активов в залах
        }

        // Place a random number of assets in the room
        int numAssets = UnityEngine.Random.Range(Mathf.Max(1, maxAssets / 3), maxAssets + 1);

        for (int i = 0; i < numAssets; i++)
        {
            if (allAssets.Count == 0) continue;
            
            GameObject assetPrefab = allAssets[UnityEngine.Random.Range(0, allAssets.Count)];
            if (assetPrefab == null) continue;

            // Calculate random position within the room bounds
            float halfRoomSize = Mathf.Sqrt(roomSize) * 1.5f; // Approximate half size in Unity units
            Vector3 position = roomCenter + new Vector3(
                UnityEngine.Random.Range(-halfRoomSize, halfRoomSize),
                0, // Y position depends on the asset and floor height
                UnityEngine.Random.Range(-halfRoomSize, halfRoomSize)
            );

            // Avoid placing assets too close to walls or other assets
            bool validPosition = true;
            foreach (Transform child in parent)
            {
                if (Vector3.Distance(child.position, position) < 1.5f)
                {
                    validPosition = false;
                    break;
                }
            }

            if (validPosition)
            {
                GameObject assetInstance = Instantiate(assetPrefab, position, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), parent);
                
                // Apply random scale variation
                float scaleVariation = UnityEngine.Random.Range(0.8f, 1.2f);
                assetInstance.transform.localScale *= scaleVariation;
                
                // If the asset has special placement requirements, handle them
                ApplySpecialPlacement(assetInstance, theme);
            }
        }
    }

    private void ApplySpecialPlacement(GameObject asset, RoomTheme theme)
    {
        // Some themes may need special placement logic
        switch (theme)
        {
            case RoomTheme.Office:
                // Office assets might be aligned to walls
                break;
            case RoomTheme.Industrial:
                // Industrial assets might be placed lower or with specific alignment
                break;
            case RoomTheme.Medical:
                // Medical assets might be centered or aligned in specific ways
                break;
            case RoomTheme.Hall:
                // Hall assets - special logic for stairs placement
                HandleHallAssetPlacement(asset);
                break;
        }
    }

    private void HandleHallAssetPlacement(GameObject asset)
    {
        // Специальная логика размещения для залов
        // Можно добавить здесь дополнительную логику для лестниц и других hall-элементов
        
        // Пример: проверяем, является ли объект лестницей
        if (asset.name.ToLower().Contains("stairs") || 
            asset.name.ToLower().Contains("stair") ||
            asset.name.ToLower().Contains("лестниц"))
        {
            // Специальное размещение лестниц - обычно у стен
            Vector3 currentPos = asset.transform.position;
            asset.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
            
            // Поворачиваем лестницу так, чтобы она была направлена вверх
            asset.transform.rotation = Quaternion.Euler(0, 0, 0);
            
            Debug.Log($"🏃‍♀️ Лестница размещена в зале: {asset.name} в позиции {asset.transform.position}");
        }
    }

    /// <summary>
    /// Создать лестничный объект в заданной позиции
    /// </summary>
    public GameObject CreateStairsAtPosition(Vector3 position, RoomTheme theme = RoomTheme.Hall, Transform parent = null)
    {
        GameObject stairsPrefab = GetRandomStairsAsset();
        if (stairsPrefab == null)
        {
            Debug.LogWarning("⚠️ Не найден префаб лестницы!");
            return null;
        }

        GameObject stairs = Instantiate(stairsPrefab, position, Quaternion.identity, parent);
        stairs.name = $"Stairs_{theme}";
        
        // Note: StairTrigger component setup removed as the class is not defined.
        // To use stairs with triggers, define StairTrigger class or add trigger setup here.
        
        Debug.Log($"🏃‍♀️ Лестница создана в позиции {position}");
        
        return stairs;
    }
}

[System.Serializable]
public class RoomThemeData
{
    public RoomTheme theme;
    public List<ThemeAsset> assets = new List<ThemeAsset>();
    [Range(0f, 1f)] public float probability = 0.5f; // How likely this theme is to be chosen
}
