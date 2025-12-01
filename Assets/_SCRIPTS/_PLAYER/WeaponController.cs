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

    [Header("Shooting Settings")]
    [SerializeField] private FireMode fireMode = FireMode.FullAutomatic;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float maxRange = 200f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float impactForce = 50f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Recoil System")]
    [Tooltip("Reference to the RecoilSystem component. If not set, will attempt to find one.")]
    [SerializeField] private RecoilSystem recoilSystem;

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

    [Header("Weapon Sway Settings")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySmoothness = 4f;

    [Header("Bobbing Settings")]
    [SerializeField] private float bobbingSpeed = 14f;
    [SerializeField] private float bobbingAmount = 0.05f;

    [Header("Audio")]
    [SerializeField] private WeaponSounds weaponSounds;

    [Header("Aiming Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float aimFOV = 30f;
    [SerializeField] private float aimSpeed = 8f;
    [SerializeField] private float aimSensitivityMultiplier = 0.5f;
    [SerializeField] private Vector3 aimPositionOffset = new Vector3(0f, 0f, 0.2f);
    [SerializeField] private float aimPositionSpeed = 10f;

    private float timer;
    private Vector3 targetWeaponPosition;
    private Vector3 recoilOffset;

    private float nextFireTime;
    private Quaternion originalWeaponRotation;
    private Vector3 originalWeaponPosition;

    private PlayerInputHandler inputHandler;
    private CharacterController characterController;
    private Transform cameraTransform;
    private MouseLook mouseLook;
    private Camera playerCamera;
    private bool isAiming;

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        cameraTransform = playerCamera?.transform;
        mouseLook = GetComponent<MouseLook>();

        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
        }

        // Find RecoilSystem if not assigned
        if (recoilSystem == null)
        {
            recoilSystem = GetComponent<RecoilSystem>();
            if (recoilSystem == null)
            {
                recoilSystem = GetComponentInChildren<RecoilSystem>();
            }
        }

        if (weaponTransform == null && cameraTransform != null)
        {
            weaponTransform = cameraTransform.Find("Weapon");
        }

        if (weaponTransform != null)
        {
            originalWeaponRotation = weaponTransform.localRotation;
            originalWeaponPosition = weaponTransform.localPosition;
        }

        if (mouseLook != null)
        {
            mouseLook.recoilPitchOffset = 0f;
        }

        // Find WeaponSounds if not assigned
        if (weaponSounds == null)
        {
            weaponSounds = GetComponent<WeaponSounds>();
        }
    }

    private void Update()
    {
        HandleAiming();
        HandleShooting();
    }

    private void HandleAiming()
    {
        if (inputHandler == null || playerCamera == null) return;

        if (inputHandler.AimInputPressedThisFrame)
        {
            isAiming = !isAiming;
        }

        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);

        if (weaponTransform != null)
        {
            Vector3 targetPosition = isAiming ? originalWeaponPosition + aimPositionOffset : originalWeaponPosition;
            weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, targetPosition, Time.deltaTime * aimPositionSpeed);
        }
    }

    public bool IsAiming()
    {
        return isAiming;
    }

    public float GetSensitivityMultiplier()
    {
        return isAiming ? aimSensitivityMultiplier : 1f;
    }

    public Vector3 HandleWeaponSway()
    {
        if (weaponTransform == null || inputHandler == null || isAiming) return Vector3.zero;

        Vector2 mouseDelta = inputHandler.MouseLookInput;

        float moveX = -mouseDelta.x * swayAmount;
        float moveY = -mouseDelta.y * swayAmount;

        moveX = Mathf.Clamp(moveX, -swayAmount, swayAmount);
        moveY = Mathf.Clamp(moveY, -swayAmount, swayAmount);

        return new Vector3(moveX, moveY, 0f);
    }

    public Vector3 HandleBobbing()
    {
        if (characterController == null || isAiming) return Vector3.zero;

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

        // Apply recoil using the new RecoilSystem
        if (recoilSystem != null)
        {
            recoilSystem.ApplyRecoil();
        }

        // Play fire sound
        if (weaponSounds != null)
        {
            weaponSounds.PlayFireSound();
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

    /// <summary>
    /// Gets or sets the RecoilSystem reference.
    /// </summary>
    public RecoilSystem RecoilSystem
    {
        get => recoilSystem;
        set => recoilSystem = value;
    }
}
