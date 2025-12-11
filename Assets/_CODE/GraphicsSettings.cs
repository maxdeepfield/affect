using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages graphics settings for the game.
/// Handles quality, resolution, vsync, shadows, and more.
/// </summary>
public class GraphicsSettings : MonoBehaviour
{
    [System.Serializable]
    public class GraphicsData
    {
        public int qualityLevel = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        public int resolutionIndex = 0;
        public bool vsyncEnabled = true;
        public int targetFramerate = 60;
        public bool shadowsEnabled = true;
        public int shadowDistance = 100;
        public float masterVolume = 1f;
    }

    private static GraphicsSettings instance;
    private GraphicsData data = new GraphicsData();
    private const string SaveKey = "GraphicsSettingsData";

    // Quality presets
    private string[] qualityNames = { "Low", "Medium", "High", "Ultra" };
    private int[] shadowDistances = { 30, 60, 100, 150 };

    public static GraphicsSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GraphicsSettings>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GraphicsSettings");
                    instance = go.AddComponent<GraphicsSettings>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    private void Start()
    {
        ApplyAllSettings();
    }

    /// <summary>
    /// Load settings from PlayerPrefs
    /// </summary>
    public void LoadSettings()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                data = JsonUtility.FromJson<GraphicsData>(json);
            }
            catch
            {
                data = new GraphicsData();
            }
        }
    }

    /// <summary>
    /// Save settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Apply all current settings to the game
    /// </summary>
    public void ApplyAllSettings()
    {
        SetQualityLevel(data.qualityLevel);
        SetVSync(data.vsyncEnabled);
        SetTargetFramerate(data.targetFramerate);
        SetShadowDistance(data.shadowDistance);
    }

    #region Quality Level

    public void SetQualityLevel(int level)
    {
        level = Mathf.Clamp(level, 0, qualityNames.Length - 1);
        data.qualityLevel = level;
        QualitySettings.SetQualityLevel(level, true);
        SetShadowDistance(shadowDistances[level]);
        Debug.Log($"[Graphics] Quality set to: {qualityNames[level]}");
    }

    public int GetQualityLevel() => data.qualityLevel;
    public string[] GetQualityNames() => qualityNames;

    #endregion

    #region Resolution

    public void SetResolution(int width, int height, bool fullscreen)
    {
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        Debug.Log($"[Graphics] Resolution set to: {width}x{height} ({(fullscreen ? "Fullscreen" : "Windowed")})");
    }

    public (int, int, bool) GetResolution()
    {
        return (Screen.width, Screen.height, Screen.fullScreen);
    }

    public (int width, int height)[] GetAvailableResolutions()
    {
        List<(int, int)> resolutions = new List<(int, int)>();
        foreach (Resolution res in Screen.resolutions)
        {
            resolutions.Add((res.width, res.height));
        }
        return resolutions.ToArray();
    }

    #endregion

    #region V-Sync

    public void SetVSync(bool enabled)
    {
        data.vsyncEnabled = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        Debug.Log($"[Graphics] VSync: {(enabled ? "Enabled" : "Disabled")}");
    }

    public bool GetVSync() => data.vsyncEnabled;

    #endregion

    #region Target Framerate

    public void SetTargetFramerate(int fps)
    {
        fps = Mathf.Max(30, fps); // Minimum 30 FPS
        data.targetFramerate = fps;
        Application.targetFrameRate = fps;
        Debug.Log($"[Graphics] Target framerate set to: {fps} FPS");
    }

    public int GetTargetFramerate() => data.targetFramerate;

    #endregion

    #region Shadows

    public void SetShadowDistance(int distance)
    {
        data.shadowDistance = distance;
        QualitySettings.shadowDistance = distance;
        Debug.Log($"[Graphics] Shadow distance: {distance}");
    }

    public int GetShadowDistance() => data.shadowDistance;

    public void SetShadowsEnabled(bool enabled)
    {
        data.shadowsEnabled = enabled;
        QualitySettings.shadows = enabled ? ShadowQuality.All : ShadowQuality.Disable;
        Debug.Log($"[Graphics] Shadows: {(enabled ? "Enabled" : "Disabled")}");
    }

    public bool GetShadowsEnabled() => data.shadowsEnabled;

    #endregion

    #region Audio

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        data.masterVolume = volume;
        AudioListener.volume = volume;
        Debug.Log($"[Graphics] Master Volume: {volume:P0}");
    }

    public float GetMasterVolume() => data.masterVolume;

    #endregion

    #region Getters

    public GraphicsData GetCurrentData() => data;

    #endregion
}
