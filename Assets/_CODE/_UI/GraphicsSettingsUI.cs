using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI panel for graphics settings.
/// Allows players to adjust quality, resolution, vsync, etc.
/// </summary>
public class GraphicsSettingsUI : MonoBehaviour
{
    [SerializeField] private Dropdown qualityDropdown;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider framerateSlider;
    [SerializeField] private TextMeshProUGUI framerateText;
    [SerializeField] private Toggle shadowsToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeText;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;

    private GraphicsSettings graphicsSettings;

    private void Start()
    {
        graphicsSettings = GraphicsSettings.Instance;
        
        if (graphicsSettings == null)
        {
            Debug.LogError("[GraphicsSettingsUI] GraphicsSettings not found!");
            return;
        }

        InitializeUI();
        RegisterListeners();
    }

    private void InitializeUI()
    {
        // Quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(graphicsSettings.GetQualityNames()));
            qualityDropdown.value = graphicsSettings.GetQualityLevel();
        }

        // Resolution dropdown
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var resolutions = graphicsSettings.GetAvailableResolutions();
            System.Collections.Generic.List<string> resolutionOptions = new System.Collections.Generic.List<string>();
            
            foreach (var res in resolutions)
            {
                resolutionOptions.Add($"{res.width}x{res.height}");
            }
            
            resolutionDropdown.AddOptions(resolutionOptions);
        }

        // V-Sync toggle
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = graphicsSettings.GetVSync();
        }

        // Framerate slider
        if (framerateSlider != null)
        {
            framerateSlider.minValue = 30;
            framerateSlider.maxValue = 240;
            framerateSlider.value = graphicsSettings.GetTargetFramerate();
            UpdateFramerateText();
        }

        // Shadows toggle
        if (shadowsToggle != null)
        {
            shadowsToggle.isOn = graphicsSettings.GetShadowsEnabled();
        }

        // Volume slider
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = graphicsSettings.GetMasterVolume();
            UpdateVolumeText();
        }
    }

    private void RegisterListeners()
    {
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        if (framerateSlider != null)
            framerateSlider.onValueChanged.AddListener(OnFramerateChanged);

        if (shadowsToggle != null)
            shadowsToggle.onValueChanged.AddListener(OnShadowsChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnQualityChanged(int index)
    {
        graphicsSettings.SetQualityLevel(index);
    }

    private void OnResolutionChanged(int index)
    {
        var resolutions = graphicsSettings.GetAvailableResolutions();
        if (index >= 0 && index < resolutions.Length)
        {
            graphicsSettings.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreen);
        }
    }

    private void OnVSyncChanged(bool enabled)
    {
        graphicsSettings.SetVSync(enabled);
    }

    private void OnFramerateChanged(float value)
    {
        graphicsSettings.SetTargetFramerate((int)value);
        UpdateFramerateText();
    }

    private void OnShadowsChanged(bool enabled)
    {
        graphicsSettings.SetShadowsEnabled(enabled);
    }

    private void OnVolumeChanged(float value)
    {
        graphicsSettings.SetMasterVolume(value);
        UpdateVolumeText();
    }

    private void UpdateFramerateText()
    {
        if (framerateText != null && framerateSlider != null)
        {
            framerateText.text = $"{(int)framerateSlider.value} FPS";
        }
    }

    private void UpdateVolumeText()
    {
        if (volumeText != null && volumeSlider != null)
        {
            volumeText.text = $"{(volumeSlider.value * 100):F0}%";
        }
    }

    private void OnApplyClicked()
    {
        graphicsSettings.SaveSettings();
        Debug.Log("[GraphicsSettingsUI] Settings applied and saved!");
    }

    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }
}
