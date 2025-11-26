using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerInputHandler))]
public class WeaponController : MonoBehaviour
{
    private enum FireMode
    {
        SemiAutomatic,
        FullAutomatic
    }

    [System.Serializable]
    private class RecoilSettings
    {
        [Tooltip("Rotation kick in degrees applied upward per shot.")]
        public float rotationKick = 15f;
        [Tooltip("Camera pitch recoil in degrees per shot.")]
        public float cameraRecoilAmount = 15f;
        [Tooltip("Weapon moves back on fire; higher = stronger kick.")]
        public float positionKick = 0.1f;
        [Tooltip("Seconds to return from recoil to neutral.")]
        public float recoveryDuration = 0.1f;
        [Tooltip("Random sideways recoil range (+/-) in degrees.")]
        public float sidewaysKick = 2f;
        [Tooltip("Enable camera pitch recoil")]
        public bool useCameraRecoil = true;
        [Tooltip("Maximum accumulated camera recoil in degrees")]
        public float maxAccumulatedRecoil = 45f;
    }

    [Header("Shooting Settings")]
    [SerializeField] private FireMode fireMode = FireMode.FullAutomatic;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float maxRange = 200f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float impactForce = 50f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private RecoilSettings recoil = new RecoilSettings();

    [Header("Transforms & FX")]
    [SerializeField] private Transform weaponTransform;
    [SerializeField] private Transform muzzlePosition;
    [SerializeField] private Transform shellEjectPort;
    [SerializeField] private ParticleSystem muzzleFlashPrefab;
    [SerializeField] private GameObject flashLightPrefab;
    [SerializeField] private float flashLightDuration = 0.05f;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private float shellEjectForce = 3f;
    [SerializeField] private float shellEjectTorque = 1f;
    [SerializeField] private float shellLifetime = 5f;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private ReticleFeedback reticleFeedback;

    [Header("Camera Kick")]
    [SerializeField] private float cameraShakeAmount = 0.1f;
    [SerializeField] private float cameraShakeDuration = 0.1f;

    [Header("Weapon Sway Settings")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySmoothness = 4f;

    [Header("Bobbing Settings")]
    [SerializeField] private float bobbingSpeed = 14f;
    [SerializeField] private float bobbingAmount = 0.05f;

    private float timer;
    private Vector3 targetWeaponPosition;
    private Vector3 recoilOffset;

    private float nextFireTime;
    private Quaternion originalWeaponRotation;
    private Vector3 originalWeaponPosition;
    private Vector3 originalCameraPosition;

    private PlayerInputHandler inputHandler;
    private CharacterController characterController;
    private Transform cameraTransform;
    private MouseLook mouseLook;

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>()?.transform;
        mouseLook = GetComponent<MouseLook>();

        if (weaponTransform == null && cameraTransform != null)
        {
            weaponTransform = cameraTransform.Find("Weapon");
        }

        if (weaponTransform != null)
        {
            originalWeaponRotation = weaponTransform.localRotation;
            originalWeaponPosition = weaponTransform.localPosition;
        }

        if (cameraTransform != null)
        {
            originalCameraPosition = cameraTransform.localPosition;
        }

        if (mouseLook != null)
        {
            mouseLook.recoilPitchOffset = 0f;
        }
    }

    private void Update()
    {
        HandleShooting();

        if (mouseLook != null)
        {
            mouseLook.recoilPitchOffset = 0f;
        }

        Vector3 swayPosition = HandleWeaponSway();
        Vector3 bobPosition = HandleBobbing();

        targetWeaponPosition = originalWeaponPosition + swayPosition + bobPosition + recoilOffset;

        if (weaponTransform != null)
        {
            weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, targetWeaponPosition, Time.deltaTime * swaySmoothness);
        }
    }

    private Vector3 HandleWeaponSway()
    {
        if (weaponTransform == null || inputHandler == null) return Vector3.zero;

        Vector2 mouseDelta = inputHandler.MouseLookInput;

        float moveX = -mouseDelta.x * swayAmount;
        float moveY = -mouseDelta.y * swayAmount;

        moveX = Mathf.Clamp(moveX, -swayAmount, swayAmount);
        moveY = Mathf.Clamp(moveY, -swayAmount, swayAmount);

        return new Vector3(moveX, moveY, 0f);
    }

    private Vector3 HandleBobbing()
    {
        if (characterController == null) return Vector3.zero;

        if (characterController.velocity.magnitude > 0.1f)
        {
            timer += Time.deltaTime * bobbingSpeed;
            float bobX = Mathf.Sin(timer) * bobbingAmount;
            float bobY = Mathf.Cos(timer * 2f) * bobbingAmount * 0.5f;
            return new Vector3(bobX, bobY, 0f);
        }

        timer = 0f;
        return Vector3.zero;
    }

    private void HandleShooting()
    {
        if (inputHandler == null) return;

        TryShoot(inputHandler.ShootInput, inputHandler.ShootInputPressedThisFrame);
    }

    public void TryShoot(bool isTriggerHeld, bool triggerPressedThisFrame)
    {
        if (!ShouldFireThisFrame(isTriggerHeld, triggerPressedThisFrame)) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    public void SetFireModeToFullAuto() => fireMode = FireMode.FullAutomatic;
    public void SetFireModeToSemiAuto() => fireMode = FireMode.SemiAutomatic;

    private bool ShouldFireThisFrame(bool isTriggerHeld, bool triggerPressedThisFrame)
    {
        switch (fireMode)
        {
            case FireMode.FullAutomatic:
                return isTriggerHeld;
            case FireMode.SemiAutomatic:
                return triggerPressedThisFrame;
            default:
                return false;
        }
    }

    private void Shoot()
    {
        if (muzzleFlashPrefab != null && muzzlePosition != null)
        {
            ParticleSystem muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePosition.position, muzzlePosition.rotation);
            muzzleFlash.Play();
            Destroy(muzzleFlash.gameObject, muzzleFlash.main.duration > 0 ? muzzleFlash.main.duration : 0.5f);
        }

        if (flashLightPrefab != null && muzzlePosition != null)
        {
            GameObject flashLight = Instantiate(flashLightPrefab, muzzlePosition.position, muzzlePosition.rotation);
            Destroy(flashLight, flashLightDuration > 0f ? flashLightDuration : 0.05f);
        }

        if (cameraTransform != null)
        {
            FireHitscan(cameraTransform.position, cameraTransform.forward);
        }

        if (cameraTransform != null)
        {
            StartCoroutine(ApplyCameraShake());
        }

        EjectShell();
    }

    private void FireHitscan(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            bool killed = false;
            if (hitInfo.rigidbody != null)
            {
                hitInfo.rigidbody.AddForceAtPosition(direction * impactForce, hitInfo.point, ForceMode.Impulse);
            }

            Health targetHealth = hitInfo.collider.GetComponentInParent<Health>();
            if (targetHealth != null)
            {
                killed = targetHealth.ApplyDamage(damage);
                reticleFeedback?.RegisterHit(killed);
            }

            if (bulletHolePrefab != null)
            {
                Instantiate(bulletHolePrefab, hitInfo.point + hitInfo.normal * 0.001f, Quaternion.LookRotation(hitInfo.normal));
            }
        }
    }

    private IEnumerator ApplyCameraShake()
    {
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraShakeDuration;

            float offsetY = Mathf.PerlinNoise(0f, Time.time * 10f) * 2f - 1f;
            Vector3 shake = new Vector3(0f, offsetY, 0f) * cameraShakeAmount * (1f - t);

            cameraTransform.localPosition = originalCameraPosition + shake;

            yield return null;
        }

        cameraTransform.localPosition = originalCameraPosition;
    }

    private void EjectShell()
    {
        if (shellPrefab == null || shellEjectPort == null) return;

        GameObject shell = Instantiate(shellPrefab, shellEjectPort.position, shellEjectPort.rotation);
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shellEjectPort.right * shellEjectForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * shellEjectTorque, ForceMode.Impulse);
        }
        Destroy(shell, shellLifetime > 0f ? shellLifetime : 1f);
    }
}
