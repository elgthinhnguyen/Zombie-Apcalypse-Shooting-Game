using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ShootingController : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.01f)] private float fireRate = 0.1f;
    [SerializeField] private bool automaticFire = true;

    [SerializeField, Min(0f)] private float fireRange = 10f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField, Min(1)] private int magazineSize = 30;
    [SerializeField, Min(0)] private int reserveAmmo = 90;
    [SerializeField, Min(0f)] private float reloadTime = 1.5f;
    [SerializeField] private bool automaticReload = true;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private GameObject hitIndicator;
    [SerializeField, Min(0f)] private float hitIndicatorDuration = 0.4f;

    [Header("Scope (M82 only)")]
    [SerializeField] private bool scopeEnabledForThisWeapon;
    [SerializeField] private GameObject scopeOverlay;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform viewModelRoot;
    [SerializeField, Min(1f)] private float scopeZoomMultiplier = 3f;
    [SerializeField, Range(0.01f, 1f)] private float scopeSensitivityMultiplier = 0.3f;

    public Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;

    private float nextFireTime = 0f;
    private float reloadFinishTime = 0f;
    private int currentAmmo;
    private bool isReloading;
    private InputAction attackAction;
    private InputAction reloadAction;
    private Renderer[] viewModelRenderers;
    private bool[] originalRendererStates;
    private bool isScoped;
    private Coroutine hitIndicatorCoroutine;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
    public ParticleSystem muzzleFlash;
    public ParticleSystem bloodEffect;
    public int damagePerShot = 10;

    private void Awake()
    {
        currentAmmo = magazineSize;
        UpdateAmmoUI();
        CacheViewModelRenderers();

        if (scopeOverlay != null)
        {
            scopeOverlay.SetActive(false);
        }

        if (hitIndicator != null)
        {
            hitIndicator.SetActive(false);
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }

        if (scopeEnabledForThisWeapon && viewModelRoot == null)
        {
            Debug.LogError("Assign the M82 gun-and-hands root to View Model Root.", this);
        }

        if (scopeEnabledForThisWeapon && scopeOverlay == null)
        {
            Debug.LogError("Assign the Scope UI to Scope Overlay.", this);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    private void OnEnable()
    {
        UpdateAmmoUI();
        SetScoped(false);
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        attackAction?.Enable();
        reloadAction?.Enable();
    }

    private void OnDisable()
    {
        SetScoped(false);
        if (hitIndicatorCoroutine != null)
        {
            StopCoroutine(hitIndicatorCoroutine);
            hitIndicatorCoroutine = null;
        }

        if (hitIndicator != null)
        {
            hitIndicator.SetActive(false);
        }

        attackAction?.Disable();
        reloadAction?.Disable();
        animator?.SetBool("Shoot", false);
        animator?.ResetTrigger("Reload");
    }

    private void Update()
    {
        bool shouldScope = scopeEnabledForThisWeapon
            && (GameManager.instance == null || !GameManager.instance.IsGameOver)
            && Mouse.current != null
            && Mouse.current.rightButton.isPressed;
        SetScoped(shouldScope);

        bool isAttackPressed = attackAction != null && attackAction.IsPressed();
        animator?.SetBool("Shoot", isAttackPressed && !isReloading);

        if (isReloading)
        {
            if (Time.time >= reloadFinishTime)
            {
                FinishReload();
            }

            return;
        }

        if (reloadAction != null && reloadAction.WasPressedThisFrame())
        {
            StartReload();
            return;
        }

        if (automaticReload && currentAmmo == 0)
        {
            StartReload();
            return;
        }

        if (attackAction == null)
        {
            return;
        }

        bool shouldShoot = automaticFire
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        if (shouldShoot)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (firePoint == null || currentAmmo <= 0 || Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;
        currentAmmo--;
        UpdateAmmoUI();
        muzzleFlash.Play();

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, fireRange,targetLayer))
        {
            ZombieAI zombieAI = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombieAI != null)
            {
                ShowHitIndicator();
                zombieAI.TakeDamage(damagePerShot);
                SpawnBloodEffect(hit);
            }

            WaypointZombieAI waypointzombieAI = hit.collider.GetComponentInParent<WaypointZombieAI>();
            if (waypointzombieAI != null)
            {
                ShowHitIndicator();
                waypointzombieAI.TakeDamage(damagePerShot);
                SpawnBloodEffect(hit);
            }
        }
            
    }

    private void SpawnBloodEffect(RaycastHit hit)
    {
        if (bloodEffect == null)
        {
            return;
        }

        ParticleSystem blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
        Destroy(blood.gameObject, blood.main.duration);
    }

    private void ShowHitIndicator()
    {
        if (hitIndicator == null)
        {
            return;
        }

        hitIndicator.SetActive(true);
        if (hitIndicatorCoroutine != null)
        {
            StopCoroutine(hitIndicatorCoroutine);
        }

        hitIndicatorCoroutine = StartCoroutine(HideHitIndicatorAfterDelay());
    }

    private IEnumerator HideHitIndicatorAfterDelay()
    {
        yield return new WaitForSecondsRealtime(hitIndicatorDuration);
        if (hitIndicator != null)
        {
            hitIndicator.SetActive(false);
        }

        hitIndicatorCoroutine = null;
    }

    private void StartReload()
    {
        if (isReloading || currentAmmo >= magazineSize || reserveAmmo <= 0)
        {
            return;
        }

        isReloading = true;
        reloadFinishTime = Time.time + reloadTime;

        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        animator?.SetBool("Shoot", false);
        animator?.SetTrigger("Reload");
    }

    private void FinishReload()
    {
        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{reserveAmmo}";
        }
    }

    private void CacheViewModelRenderers()
    {
        if (viewModelRoot == null)
        {
            return;
        }

        viewModelRenderers = viewModelRoot.GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[viewModelRenderers.Length];
        for (int i = 0; i < viewModelRenderers.Length; i++)
        {
            originalRendererStates[i] = viewModelRenderers[i].enabled;
        }
    }

    private void SetScoped(bool aiming)
    {
        if (isScoped == aiming)
        {
            return;
        }

        isScoped = aiming;
        if (scopeOverlay != null)
        {
            scopeOverlay.SetActive(aiming);
        }

        if (cameraController != null)
        {
            cameraController.SetScopedAim(
                aiming,
                scopeZoomMultiplier,
                scopeSensitivityMultiplier);
        }

        if (viewModelRenderers == null)
        {
            return;
        }

        for (int i = 0; i < viewModelRenderers.Length; i++)
        {
            if (viewModelRenderers[i] != null)
            {
                viewModelRenderers[i].enabled = aiming ? false : originalRendererStates[i];
            }
        }
    }
}
