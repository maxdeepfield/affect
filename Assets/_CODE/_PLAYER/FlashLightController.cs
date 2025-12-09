using UnityEngine;
using System.Collections;
//wadwaq
public class FlashLightController : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.05f; // Duration the light stays on

    void OnEnable()
    {
        // Start the coroutine to turn off the light after flashDuration
        StartCoroutine(TurnOffLight());
    }

    private IEnumerator TurnOffLight()
    {
        yield return new WaitForSeconds(flashDuration);
        // Deactivate the GameObject, which effectively turns off the light
        // Optionally, you could destroy it if you don't plan to pool them
        Light lightComponent = GetComponent<Light>();
        if (lightComponent != null)
        {
            lightComponent.enabled = false;
        }
        Destroy(gameObject);
    }
}
