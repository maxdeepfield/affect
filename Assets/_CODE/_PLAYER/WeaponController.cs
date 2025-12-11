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
    [Tooltip("Reference to the ReloadAnimation component. If not set, will attempt to find one.")]
    [SerializeField] private ReloadAnimation reloadAnimation;

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

    [Header("Impact Particles")]
    [SerializeField] private int impactParticleCount = 20;
    [SerializeField] private float impactParticleSpeed = 5f;
    [SerializeField] private float impactParticleDuration = 0.5f;
    [SerializeField] private Color impactParticleColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Header("Weapon Sway Settings")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySmoothness = 4f;

    [Header("Bobbing Settings")]
    [SerializeField] private float bobbingSpeed = 14f;
    [SerializeField] private float bobbingAmount = 0.05f;

    [Header("Audio")]
    [SerializeField] private WeaponSounds weaponSounds;

    [Header("Ammo Settings")]
    [SerializeField] private WeaponAmmo weaponAmmo;
    [SerializeField] private float reloadDuration = 1.2f;
    [SerializeField] private bool autoReloadOnEmpty = true;

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
    private bool isReloading;
    private Coroutine reloadRoutine;

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

        // Find ReloadAnimation if not assigned
        if (reloadAnimation == null)
        {
            reloadAnimation = GetComponent<ReloadAnimation>();
            if (reloadAnimation == null)
            {
                reloadAnimation = GetComponentInChildren<ReloadAnimation>();
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

        if (weaponAmmo == null)
        {
            weaponAmmo = GetComponent<WeaponAmmo>();
        }
    }

    private void Update()
    {
        HandleAiming();
        HandleReloading();
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
            // If reloading via RecoilSystem, include reload position while aiming so the animation is visible while scoped
            if (isAiming && recoilSystem != null)
            {
                targetPosition += recoilSystem.CurrentReloadPositionOffset;
            }

            // If using RecoilSystem for positional updates when not aiming, still allow the controller to set aim position
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
        if (isReloading) return;
        if (!ShouldFireThisFrame(isTriggerHeld, triggerPressedThisFrame)) return;
        if (Time.time < nextFireTime) return;

        if (weaponAmmo != null)
        {
            if (!weaponAmmo.TryConsumeRound())
            {
                weaponSounds?.PlayEmptyClick();
                if (autoReloadOnEmpty && reloadRoutine == null && weaponAmmo.CanReload)
                {
                    reloadRoutine = StartCoroutine(ReloadRoutine());
                }
                return;
            }
        }

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

            // Spawn impact particles at hit location
            SpawnImpactParticles(hitInfo.point, hitInfo.normal);

            if (bulletHolePrefab != null)
            {
                Instantiate(bulletHolePrefab, hitInfo.point + hitInfo.normal * 0.001f, Quaternion.LookRotation(hitInfo.normal));
            }
        }
    }

    private void SpawnImpactParticles(Vector3 position, Vector3 normal)
    {
        // Create empty GameObject for particle system
        GameObject particleGO = new GameObject("ImpactParticles");
        particleGO.transform.position = position;
        particleGO.transform.rotation = Quaternion.LookRotation(normal);

        // Add ParticleSystem component
        ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
        // Stop before tweaking duration to avoid play-while-configuring warnings
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer renderer = particleGO.GetComponent<ParticleSystemRenderer>();

        // Configure main module
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.duration = impactParticleDuration;
        main.loop = false;
        main.startLifetime = impactParticleDuration;
        main.startSpeed = impactParticleSpeed;
        main.startColor = impactParticleColor;
        main.maxParticles = impactParticleCount;

        // Configure emission
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, impactParticleCount) });

        // Configure shape (burst outward from normal)
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Configure velocity over lifetime (spread cone)
        ParticleSystem.VelocityOverLifetimeModule velModule = ps.velocityOverLifetime;
        velModule.enabled = true;
        velModule.x = new ParticleSystem.MinMaxCurve(-impactParticleSpeed * 0.5f, impactParticleSpeed * 0.5f);
        velModule.y = new ParticleSystem.MinMaxCurve(impactParticleSpeed * 0.3f, impactParticleSpeed);
        velModule.z = new ParticleSystem.MinMaxCurve(-impactParticleSpeed * 0.5f, impactParticleSpeed * 0.5f);

        // Configure size over lifetime (fade out)
        ParticleSystem.SizeOverLifetimeModule sizeModule = ps.sizeOverLifetime;
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        ));

        // Minimal renderer
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Play and auto-destroy
        ps.Play();
        Destroy(particleGO, impactParticleDuration + 0.1f);
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

    private void HandleReloading()
    {
        if (weaponAmmo == null || inputHandler == null) return;
        if (isReloading) return;

        bool wantsReload = inputHandler.ReloadInputPressedThisFrame;
        if (!wantsReload && autoReloadOnEmpty)
        {
            wantsReload = weaponAmmo.IsMagazineEmpty && weaponAmmo.CanReload;
        }

        if (wantsReload && weaponAmmo.CanReload && reloadRoutine == null)
        {
            reloadRoutine = StartCoroutine(ReloadRoutine());
        }
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        weaponSounds?.PlayReloadSound();

        // Start reload animation if available
        if (recoilSystem != null)
        {
            // Use RecoilSystem's integrated reload animation
            Vector3 posOffset = recoilSystem.Config.reloadPositionOffset;
            Quaternion rotOffset = Quaternion.Euler(recoilSystem.Config.reloadRotationPitch, recoilSystem.Config.reloadRotationYaw, 0f);
            AnimationCurve curve = recoilSystem.Config.reloadAnimationCurve;
            recoilSystem.StartReloadAnimation(posOffset, rotOffset, reloadDuration, curve);
        }
        else if (reloadAnimation != null)
        {
            // Backwards compatibility - fall back to local ReloadAnimation if RecoilSystem is not present
            reloadAnimation.SetReloadParameters(
                recoilSystem != null ? recoilSystem.Config.reloadRotationPitch : -15f,
                recoilSystem != null ? recoilSystem.Config.reloadRotationYaw : 25f,
                recoilSystem != null ? recoilSystem.Config.reloadPositionOffset : new Vector3(0.03f, -0.02f, 0.08f),
                reloadDuration,
                recoilSystem != null ? recoilSystem.Config.reloadAnimationCurve : AnimationCurve.EaseInOut(0, 0, 1, 1)
            );
            reloadAnimation.StartReload(reloadDuration);
        }

        if (reloadDuration > 0f)
            yield return new WaitForSeconds(reloadDuration);

        weaponAmmo?.TryReload();
        isReloading = false;
        reloadRoutine = null;

        // End reload animation via RecoilSystem if present
        if (recoilSystem != null)
        {
            recoilSystem.EndReloadAnimation();
        }
        else if (reloadAnimation != null)
        {
            reloadAnimation.EndReload();
        }
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
