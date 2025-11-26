using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Расширенный редактор для OptimizedBuildingGenerator с визуализацией и профилированием
/// </summary>
[CustomEditor(typeof(OptimizedBuildingGenerator))]
[CanEditMultipleObjects]
public class OptimizedBuildingGeneratorEditor : Editor
{
    private OptimizedBuildingGenerator generator;
    private ObjectPoolManager poolManager;
    
    private bool showGenerationSettings = true;
    private bool showPerformanceSettings = true;
    private bool showDebugInfo = false;
    private bool showPoolStats = false;
    
    private float lastGenerationTime = 0f;
    private int lastRoomCount = 0;
    private int lastWallCount = 0;

    private void OnEnable()
    {
        generator = (OptimizedBuildingGenerator)target;
        poolManager = generator.GetComponent<ObjectPoolManager>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawCustomHeader();
        DrawGenerationControls();
        DrawGenerationSettings();
        DrawPerformanceSettings();
        DrawDebugInfo();
        
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCustomHeader()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🏗️ Optimized Building Generator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Мощный генератор зданий с улучшенной производительностью", EditorStyles.miniLabel);
        EditorGUILayout.Space();
        
        // Красивая статистика
        if (lastGenerationTime > 0)
        {
            EditorGUILayout.LabelField($"⏱️ Последняя генерация: {lastGenerationTime:F1}ms", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"🏢 К室мы: {lastRoomCount} | 🧱 Стены: {lastWallCount}", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.Space();
    }

    private void DrawGenerationControls()
    {
        EditorGUILayout.LabelField("🎮 Управление Генерацией", EditorStyles.boldLabel);
        
        // Основная кнопка генерации
        if (GUILayout.Button("🔄 Сгенерировать Здание", GUILayout.Height(35)))
        {
            GenerateBuildingWithProfiling();
        }
        
        // Быстрые действия
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🧹 Очистить", EditorStyles.miniButton))
        {
            // Полная очистка: удаляем здание и очищаем пулы для подготовки к следующей генерации
            if (generator != null)
            {
                generator.ClearAndRebuildPools();
            }
        }
        
        if (GUILayout.Button("📊 Статистика", EditorStyles.miniButton))
        {
            ShowGenerationStatistics();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
    }

    private void DrawGenerationSettings()
    {
        showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings, "📐 Параметры Генерации", true);
        
        if (showGenerationSettings)
        {
            EditorGUI.indentLevel++;
            
            // Основные префабы
            EditorGUILayout.LabelField("🔧 Префабы", EditorStyles.boldLabel);
            DrawProperty("wallPrefab", "Стена");
            DrawProperty("floorPrefab", "Пол");
            DrawProperty("windowPrefab", "Окно");
            DrawProperty("entranceDoorPrefab", "Входная дверь");
            DrawProperty("interiorDoorPrefab", "Внутренняя дверь");
            
            // Размеры
            EditorGUILayout.LabelField("📐 Размеры", EditorStyles.boldLabel);
            DrawProperty("gridWidth", "Ширина сетки");
            DrawProperty("gridHeight", "Высота сетки");
            
            // К室мы
            EditorGUILayout.LabelField("🚪 К室мы", EditorStyles.boldLabel);
            DrawProperty("minRooms", "Минимум комнат");
            DrawProperty("maxRooms", "Максимум комнат");
            DrawProperty("minRoomSize", "Мин. размер комнаты");
            DrawProperty("maxRoomSize", "Макс. размер комнаты");
            
            // Шансы
            EditorGUILayout.LabelField("🎲 Вероятности", EditorStyles.boldLabel);
            DrawProperty("windowChance", "Шанс окна");
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawPerformanceSettings()
    {
        showPerformanceSettings = EditorGUILayout.Foldout(showPerformanceSettings, "⚡ Производительность", true);
        
        if (showPerformanceSettings)
        {
            EditorGUI.indentLevel++;
            
            DrawProperty("useObjectPooling", "Object Pooling");
            DrawProperty("enableSpatialPartitioning", "Spatial Partitioning");
            DrawProperty("maxGenerationTimeMs", "Лимит времени (ms)");
            DrawProperty("enableIncrementalGeneration", "Incremental Генерация");
            
            if (poolManager)
            {
                EditorGUILayout.LabelField($"📦 Object Pools: {poolManager.GetPoolStatistics()}", EditorStyles.miniLabel);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawDebugInfo()
    {
        showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "🛠️ Отладка", true);
        
        if (showDebugInfo)
        {
            EditorGUI.indentLevel++;
            
            DrawProperty("enableIncrementalGeneration", "Incremental Режим");
            
            if (GUILayout.Button("🔍 Проверить Целостность"))
            {
                ValidateGeneration();
            }
            
            if (GUILayout.Button("📈 Профилировать Производительность"))
            {
                ProfilePerformance();
            }
            
            // Статистика пулов
            if (poolManager && poolManager.HasPool("Wall"))
            {
                showPoolStats = EditorGUILayout.Foldout(showPoolStats, "📊 Статистика Пулов", true);
                
                if (showPoolStats)
                {
                    EditorGUILayout.LabelField($"Активно: {poolManager.GetActiveCount("Wall")}");
                    EditorGUILayout.LabelField($"Всего: {poolManager.GetTotalCount("Wall")}");
                    
                    if (GUILayout.Button("🧹 Очистить Пулы"))
                    {
                        generator.ClearAndRebuildPools();
                    }
                }
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    private void DrawProperty(string propertyName, string label = null)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label ?? propertyName));
        }
    }

    private void GenerateBuildingWithProfiling()
    {
        var startTime = Time.realtimeSinceStartup;
        
        generator.GenerateBuilding();
        
        var generationTime = (Time.realtimeSinceStartup - startTime) * 1000;
        lastGenerationTime = (float)generationTime;
        lastRoomCount = generator.GetRoomCount();
        lastWallCount = generator.GetWallCount();
        
        EditorUtility.DisplayDialog("Генерация завершена", 
            $"Время: {generationTime:F1}ms\nК室мы: {lastRoomCount}\nСтены: {lastWallCount}", "ОК");
        
        SceneView.RepaintAll();
    }

    private void ShowGenerationStatistics()
    {
        var stats = $"📊 Статистика Генерации:\n" +
                   $"• Время: {lastGenerationTime:F1}ms\n" +
                   $"• К室мы: {lastRoomCount}\n" +
                   $"• Стены: {lastWallCount}\n" +
                   $"• Размер: {generator.gridWidth}x{generator.gridHeight}\n" +
                   $"• Плотность: {(float)(lastRoomCount * lastWallCount) / (generator.gridWidth * generator.gridHeight):F2} об/ед";
        
        Debug.Log(stats);
        EditorUtility.DisplayDialog("Статистика", stats, "Закрыть");
    }



    private void ValidateGeneration()
    {
        var issues = new System.Text.StringBuilder();
        issues.AppendLine("🔍 Проверка Целостности Генерации:");
        
        // Проверка размеров
        if (generator.gridWidth < 5 || generator.gridHeight < 5)
        {
            issues.AppendLine("⚠️ Слишком маленькие размеры сетки");
        }
        
        // Проверка префабов
        if (!generator.wallPrefab) issues.AppendLine("❌ Не задан префаб стены");
        if (!generator.entranceDoorPrefab) issues.AppendLine("❌ Не задан префаб входной двери");
        if (!generator.interiorDoorPrefab) issues.AppendLine("❌ Не задан префаб внутренней двери");
        
        // Проверка пулов
        if (poolManager && !poolManager.HasPool("Wall"))
        {
            issues.AppendLine("⚠️ Object Pool для стен не инициализирован");
        }
        
        if (issues.ToString().Split('\n').Length <= 2)
        {
            issues.AppendLine("✅ Все проверки пройдены!");
        }
        
        Debug.Log(issues.ToString());
        EditorUtility.DisplayDialog("Проверка", issues.ToString(), "Закрыть");
    }

    private void ProfilePerformance()
    {
        var profileResult = new System.Text.StringBuilder();
        profileResult.AppendLine("📈 Профилирование Производительности:");
        
        // Тест генерации
        var times = new float[5];
        for (int i = 0; i < 5; i++)
        {
            generator.ClearPreviousGeneration();
            var startTime = Time.realtimeSinceStartup;
            generator.GenerateBuilding();
            times[i] = (Time.realtimeSinceStartup - startTime) * 1000;
        }
        
        var avgTime = times.Average();
        var maxTime = times.Max();
        var minTime = times.Min();
        
        profileResult.AppendLine($"Среднее время: {avgTime:F1}ms");
        profileResult.AppendLine($"Максимальное: {maxTime:F1}ms");
        profileResult.AppendLine($"Минимальное: {minTime:F1}ms");
        profileResult.AppendLine($"Разброс: {maxTime - minTime:F1}ms");
        
        if (avgTime > generator.maxGenerationTimeMs)
        {
            profileResult.AppendLine($"⚠️ Среднее время превышает лимит {generator.maxGenerationTimeMs}ms");
        }
        else
        {
            profileResult.AppendLine("✅ Производительность в норме");
        }
        
        Debug.Log(profileResult.ToString());
        EditorUtility.DisplayDialog("Профилирование", profileResult.ToString(), "Закрыть");
    }

    // Добавляем кнопку в сцену для быстрой генерации
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGeneratorGizmo(OptimizedBuildingGenerator gen, GizmoType gizmoType)
    {
        if (gen == null) return;
        
        // Рисуем границы генерации
        var bounds = new Bounds(
            new Vector3(gen.gridWidth * 1.5f, 1.5f, gen.gridHeight * 1.5f),
            new Vector3(gen.gridWidth * 3f, 3f, gen.gridHeight * 3f)
        );
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        // Стрелка вверх для обозначения направления Y
        Gizmos.color = Color.green;
        Gizmos.DrawRay(bounds.center, Vector3.up * 3f);
    }
}
