using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows the current usable prompt on a UI Text. Assign this to a Text element in your HUD.
/// </summary>
public class UsePromptUI : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string idleText = "";
    [SerializeField] private string format = "{0}";

    private void Awake()
    {
        if (promptText == null)
        {
            promptText = GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (promptText == null) return;

        // Avoid overwriting the keycard inventory display if someone wired the same TMP to both.
        if (PlayerHUD.Instance != null && PlayerHUD.Instance.KeycardText == promptText)
            return;

        Usable usable = Usable.Current;
        if (usable != null)
        {
            promptText.text = string.Format(format, usable.Prompt);
        }
        else
        {
            promptText.text = idleText;
        }
    }
}
