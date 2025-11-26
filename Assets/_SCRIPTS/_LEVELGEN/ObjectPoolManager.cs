using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Summary:
/// Умный Object Pool Manager с автоматическим управлением пулами для разных типов объектов
/// Оптимизирован для процедурной генерации зданий и разрушаемых элементов
///
[DisallowMultipleComponent]
public class ObjectPoolManager : MonoBehaviour
{
    [Header("🔧 Настройки Пулов")]
    [Tooltip("Начальный размер пула для каждого типа объектов")]
    [Range(10, 200)] public int defaultPoolSize = 50;
    [Tooltip("Максимальный размер пула (защита от перерасхода памяти)")]
    [Range(100, 1000)] public int maxPoolSize = 200;
    [Tooltip("Авто-расширение пула при нехватке объектов")]
    public bool autoExpand = true;
    [Tooltip("Удалять неиспользуемые объекты через время")]
    public bool enableAutoCleanup = true;
    [Tooltip("Время жизни неиспользуемого объекта в секундах")]
    [Range(5f, 300f)] public float objectLifetime = 60f;

    private Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();
    private Dictionary<GameObject, string> prefabToPoolName = new Dictionary<GameObject, string>();

    private void Update()
    {
        if (enableAutoCleanup)
        {
            foreach (var pool in pools.Values)
            {
                pool.Update();
            }
        }
    }

    /// Summary:
    /// Получить объект из пула или создать новый
    ///
    public GameObject GetObject(string prefabName)
    {
        if (pools.TryGetValue(prefabName, out var pool))
        {
            return pool.GetObject();
        }
        
        Debug.LogWarning($"❌ Пул для {prefabName} не найден");
        return null;
    }

    /// Summary:
    /// Получить объект из пула по префабу
    ///
    public GameObject GetObject(GameObject prefab)
    {
        if (prefabToPoolName.TryGetValue(prefab, out var poolName))
        {
            return GetObject(poolName);
        }
        
        // Создаем новый пул если его нет
        AddObject(prefab.name, prefab);
        return GetObject(prefab.name);
    }

    /// Summary:
    /// Добавить префаб в пул
    ///
    public void AddObject(string poolName, GameObject prefab)
    {
        if (!pools.ContainsKey(poolName))
        {
            pools[poolName] = new ObjectPool(poolName, prefab, defaultPoolSize, maxPoolSize, autoExpand, objectLifetime);
            prefabToPoolName[prefab] = poolName;
            
            Debug.Log($"🏗️ Создан пул: {poolName} (размер: {defaultPoolSize})");
        }
    }

    /// Summary:
    /// Вернуть все объекты во все пулы
    ///
    public void ReturnAllObjects()
    {
        foreach (var pool in pools.Values)
        {
            pool.ReturnAllObjects();
        }
        
        Debug.Log($"🔄 Все объекты возвращены в пулы ({pools.Count} типов)");
    }

    /// Summary:
    /// Вернуть объект в пул по имени
    ///
    public void ReturnObject(string poolName, GameObject obj)
    {
        if (pools.TryGetValue(poolName, out var pool))
        {
            pool.ReturnObject(obj);
        }
    }

    /// Summary:
    /// Вернуть объект в пул по префабу
    ///
    public void ReturnObject(GameObject prefab, GameObject obj)
    {
        if (prefabToPoolName.TryGetValue(prefab, out var poolName))
        {
            ReturnObject(poolName, obj);
        }
    }

    /// Summary:
    /// Получить статистику по всем пулам
    ///
    public string GetPoolStatistics()
    {
        var stats = $"📊 Object Pool Статистика ({pools.Count} типов):\n";
        
        foreach (var pool in pools.Values)
        {
            stats += $"  • {pool.poolName}: {pool.ActiveCount}/{pool.TotalCount} активно\n";
        }
        
        return stats;
    }

    /// Summary:
    /// Очистить все пулы (полная очистка)
    ///
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }
        pools.Clear();
        prefabToPoolName.Clear();
        
        Debug.Log("🗑️ Все пулы очищены");
    }

    /// Summary:
    /// Увеличить размер пула
    ///
    public void ExpandPool(string poolName, int additionalObjects)
    {
        if (pools.TryGetValue(poolName, out var pool))
        {
            pool.ExpandPool(additionalObjects);
        }
    }

    /// Summary:
    /// Проверить существует ли пул
    ///
    public bool HasPool(string poolName)
    {
        return pools.ContainsKey(poolName);
    }

    /// Summary:
    /// Получить количество активных объектов в пуле
    ///
    public int GetActiveCount(string poolName)
    {
        return pools.TryGetValue(poolName, out var pool) ? pool.ActiveCount : 0;
    }

    /// Summary:
    /// Получить общее количество объектов в пуле
    ///
    public int GetTotalCount(string poolName)
    {
        return pools.TryGetValue(poolName, out var pool) ? pool.TotalCount : 0;
    }
}

/// Summary:
/// Один пул объектов для конкретного типа
///
public class ObjectPool
{
    public string poolName { get; private set; }
    private GameObject prefab;
    private int defaultSize;
    private int maxSize;
    private bool autoExpand;
    private float lifetime;
    
    private Stack<GameObject> pooledObjects = new Stack<GameObject>();
    private List<PooledObject> activeObjects = new List<PooledObject>();
    
    private float lastCleanupTime;

    public int ActiveCount => activeObjects.Count;
    public int TotalCount => pooledObjects.Count + activeObjects.Count;

    public ObjectPool(string name, GameObject prefabObj, int defaultPoolSize, int maximumSize, bool autoExpandPools, float objectLifeTime)
    {
        poolName = name;
        prefab = prefabObj;
        defaultSize = defaultPoolSize;
        maxSize = maximumSize;
        autoExpand = autoExpandPools;
        lifetime = objectLifeTime;
        
        // Предварительное создание объектов
        for (int i = 0; i < defaultSize; i++)
        {
            CreatePooledObject();
        }
    }

    /// Summary:
    /// Получить объект из пула
    ///
    public GameObject GetObject()
    {
        GameObject obj;
        
        if (pooledObjects.Count > 0)
        {
            obj = pooledObjects.Pop();
            obj.SetActive(true);
        }
        else if (autoExpand && TotalCount < maxSize)
        {
            // Авто-расширение
            ExpandPool(10);
            obj = pooledObjects.Pop();
            obj.SetActive(true);
        }
        else
        {
            // Создаем новый объект если пул заполнен
            obj = GameObject.Instantiate(prefab);
            Debug.LogWarning($"⚠️ Создан новый объект {poolName} (пул заполнен: {TotalCount}/{maxSize})");
        }
        
        var pooledObj = new PooledObject(obj, Time.time);
        activeObjects.Add(pooledObj);
        
        return obj;
    }

    /// Summary:
    /// Вернуть объект в пул
    ///
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;
        
        var pooledObj = activeObjects.FirstOrDefault(po => po.gameObject == obj);
        if (pooledObj.gameObject != null)
        {
            activeObjects.Remove(pooledObj);
            
            obj.SetActive(false);
            obj.transform.SetParent(null);
            pooledObjects.Push(obj);
        }
        else
        {
            Debug.LogWarning($"❌ Объект {obj.name} не найден в активных объектах пула {poolName}");
        }
    }

    /// Summary:
    /// Вернуть все активные объекты
    ///
    public void ReturnAllObjects()
    {
        foreach (var activeObj in activeObjects.ToList())
        {
            ReturnObject(activeObj.gameObject);
        }
    }

    /// Summary:
    /// Обновление пула (авто-очистка)
    ///
    public void Update()
    {
        if (Time.time - lastCleanupTime > 1f) // Проверяем раз в секунду
        {
            CleanupExpiredObjects();
            lastCleanupTime = Time.time;
        }
    }

    /// Summary:
    /// Очистка просроченных объектов
    ///
    private void CleanupExpiredObjects()
    {
        var expiredObjects = activeObjects.Where(obj => Time.time - obj.spawnTime > lifetime).ToList();
        
        foreach (var expiredObj in expiredObjects)
        {
            ReturnObject(expiredObj.gameObject);
        }
        
        if (expiredObjects.Count > 0)
        {
            Debug.Log($"🧹 Очищено {expiredObjects.Count} просроченных объектов из {poolName}");
        }
    }

    /// Summary:
    /// Расширить пул
    ///
    public void ExpandPool(int additionalObjects)
    {
        if (TotalCount + additionalObjects <= maxSize)
        {
            for (int i = 0; i < additionalObjects; i++)
            {
                CreatePooledObject();
            }
            Debug.Log($"📈 Пул {poolName} расширен на {additionalObjects} объектов");
        }
        else
        {
            Debug.LogWarning($"❌ Невозможно расширить пул {poolName} (достигнут лимит: {maxSize})");
        }
    }

    /// Summary:
    /// Создать новый объект для пула
    ///
    private void CreatePooledObject()
    {
        var obj = GameObject.Instantiate(prefab);
        obj.SetActive(false);
        obj.transform.SetParent(null);
        pooledObjects.Push(obj);
    }

    /// Summary:
    /// Полная очистка пула
    ///
    public void Clear()
    {
        // Удаляем все неактивные объекты
        foreach (var obj in pooledObjects)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                GameObject.DestroyImmediate(obj);
#else
                GameObject.Destroy(obj);
#endif
            }
        }

        // Удаляем активные объекты
        foreach (var activeObj in activeObjects)
        {
            if (activeObj.gameObject != null)
            {
#if UNITY_EDITOR
                GameObject.DestroyImmediate(activeObj.gameObject);
#else
                GameObject.Destroy(activeObj.gameObject);
#endif
            }
        }

        pooledObjects.Clear();
        activeObjects.Clear();
    }
}

/// Summary:
/// Объект в пуле с временем создания
///
public struct PooledObject
{
    public GameObject gameObject;
    public float spawnTime;
    
    public PooledObject(GameObject obj, float time)
    {
        gameObject = obj;
        spawnTime = time;
    }
}


