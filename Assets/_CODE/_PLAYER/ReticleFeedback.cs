using System.Collections;
using UnityEngine;

public class ReticleFeedback : MonoBehaviour
{
    [SerializeField] private GameObject reticleHit;
    [SerializeField] private GameObject reticleKill;
    [SerializeField] private float flashDuration = 0.15f;

    private Coroutine flashRoutine;

    public void RegisterHit(bool killed)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(Flash(killed));
    }

    private IEnumerator Flash(bool killed)
    {
        if (reticleHit != null) reticleHit.SetActive(true);
        if (reticleKill != null) reticleKill.SetActive(killed);

        yield return new WaitForSeconds(flashDuration);

        if (reticleHit != null) reticleHit.SetActive(false);
        if (reticleKill != null) reticleKill.SetActive(false);
        flashRoutine = null;
    }
}
