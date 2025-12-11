using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual display for the single upgradable keycard.
/// Shows current level with visual feedback on upgrade.
/// </summary>
public class KeycardDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image keycardIcon;
    [SerializeField] private Image glowEffect;
    [SerializeField] private RectTransform cardTransform;

    [Header("Level Colors")]
    [SerializeField] private Color[] levelColors = new Color[]
    {
        new Color(0.5f, 0.5f, 0.5f),    // Level 0 - Gray (no card)
        new Color(0.2f, 0.8f, 0.2f),    // Level 1 - Green
        new Color(0.2f, 0.5f, 1f),      // Level 2 - Blue
        new Color(0.8f, 0.2f, 0.8f),    // Level 3 - Purple
        new Color(1f, 0.8f, 0.2f),      // Level 4 - Gold
        new Color(1f, 0.3f, 0.3f),      // Level 5 - Red
    };

    [Header("Animation")]
    [SerializeField] private float upgradePunchScale = 1.3f;
    [SerializeField] private float upgradePunchDuration = 0.3f;
    [SerializeField] private float glowPulseDuration = 0.5f;

    private int currentLevel = -1;
    private Coroutine upgradeAnimation;

    private void Start()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnKeycardLevelChanged.AddListener(OnLevelChanged);
            OnLevelChanged(PlayerInventory.Instance.KeycardLevel);
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnKeycardLevelChanged.RemoveListener(OnLevelChanged);
        }
    }

    private void OnLevelChanged(int newLevel)
    {
        bool isUpgrade = newLevel > currentLevel && currentLevel >= 0;
        currentLevel = newLevel;

        UpdateVisuals();

        if (isUpgrade)
        {
            PlayUpgradeAnimation();
        }
    }

    private void UpdateVisuals()
    {
        // Update level text
        if (levelText != null)
        {
            if (currentLevel <= 0)
            {
                levelText.text = "-";
            }
            else
            {
                levelText.text = currentLevel.ToString();
            }
        }

        // Update color
        Color targetColor = GetColorForLevel(currentLevel);
        
        if (keycardIcon != null)
        {
            keycardIcon.color = targetColor;
        }

        if (glowEffect != null)
        {
            Color glowColor = targetColor;
            glowColor.a = 0.5f;
            glowEffect.color = glowColor;
        }
    }

    private Color GetColorForLevel(int level)
    {
        if (level < 0) level = 0;
        if (level >= levelColors.Length)
        {
            // Cycle through colors for endless levels
            return levelColors[level % levelColors.Length];
        }
        return levelColors[level];
    }

    private void PlayUpgradeAnimation()
    {
        if (upgradeAnimation != null)
        {
            StopCoroutine(upgradeAnimation);
        }
        upgradeAnimation = StartCoroutine(UpgradeAnimationRoutine());
    }

    private System.Collections.IEnumerator UpgradeAnimationRoutine()
    {
        // Punch scale
        if (cardTransform != null)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = originalScale * upgradePunchScale;

            float elapsed = 0f;
            while (elapsed < upgradePunchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / upgradePunchDuration;
                
                // Punch out then back
                float scale = t < 0.5f 
                    ? Mathf.Lerp(1f, upgradePunchScale, t * 2f)
                    : Mathf.Lerp(upgradePunchScale, 1f, (t - 0.5f) * 2f);
                
                cardTransform.localScale = Vector3.one * scale;
                yield return null;
            }
            cardTransform.localScale = originalScale;
        }

        // Glow pulse
        if (glowEffect != null)
        {
            Color baseColor = glowEffect.color;
            float elapsed = 0f;
            while (elapsed < glowPulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / glowPulseDuration;
                float alpha = Mathf.Sin(t * Mathf.PI) * 0.8f;
                
                Color c = baseColor;
                c.a = alpha;
                glowEffect.color = c;
                yield return null;
            }
            
            Color finalColor = baseColor;
            finalColor.a = 0.3f;
            glowEffect.color = finalColor;
        }

        upgradeAnimation = null;
    }
}
