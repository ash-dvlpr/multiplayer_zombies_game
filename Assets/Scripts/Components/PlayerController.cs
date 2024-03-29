using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Animating;
using FishNet.Connection;


[RequireComponent(typeof(PlayerMovement), typeof(Health), typeof(Ammo))]
public class PlayerController : NetworkBehaviour, IInteractor {
    // ==================== Configuration ====================
    [Header("Shooting")]
    [SerializeField] NetworkAnimator weaponAnimator;
    [SerializeField] LayerMask damageableLayers;

    [Header("Sounds")]
    [SerializeField] AudioClip deathSound;
    [SerializeField] AudioClip shootSound;

    //[Header("Death")]
    //[SerializeField] float timeBeforeCorpseRemoval = 4f;

    // ====================== Variables ======================
    // Here interactable componets are cached
    readonly Dictionary<Type, object> componentCache = new();

    [Header("Other")]
    [SyncVar, ShowOnly] bool _canControl = false;
    [SyncVar, ShowOnly] bool _canShoot = false;
    Vector3 _spawnPosition;

    public bool CanMove { get => base.IsOwner && GameManager.IsPlaying; }
    public bool CanControl {
        get => _canControl && !GameManager.ClientInMenu;
        [Server]
        set => _canControl = value;
    }
    public bool CanShoot {
        get => CanControl && ammo.HasAmmo && weapon != null && _canShoot;
        private set => _canShoot = value;
    }
    public List<Type> AllowedInteractableTypes { get => componentCache.Keys.ToList(); }

    // ====================== References =====================
    [Header("References")]
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] GameObject barsCanvas;
    [SerializeField] Weapon weapon;
    PlayerMovement playerMovement;

    PlayerUI playerUI;
    ResourceBar hudHpBar;
    ResourceBar hudAmmoBar;
    Health health;
    Ammo ammo;

    // ======================== ICache =======================
    public void Register<T>(T resource) {
        var type = typeof(T);
        componentCache[type] = resource;
    }
    public bool Has<T>() {
        var type = typeof(T);
        return componentCache.ContainsKey(type);
    }
    public bool TryGet<T>(out T resource) {
        var type = typeof(T);
        var result = componentCache.TryGetValue(type, out var cachedObj);
        resource = (T) cachedObj;

        return result;
    }


    // ======================= NetCode ========================
    public override void OnStartServer() {
        base.OnStartServer();

        CanControl = CanShoot = true;
        _spawnPosition = this.transform.position;

        // Register player on the enemy spawner
        NetGameManager.Instance?.Players.Add(this);

        health.OnDeath += OnDeath;
        health.OnDeath += NetGameManager.Instance.OnPlayerDied;
        ammo.OnNoAmmoLeft += NetGameManager.Instance.OnPlayerNoAmmoLeft;
    }

    public override void OnStopServer() {
        base.OnStartServer();

        // Deregister player on the enemy spawner
        NetGameManager.Instance?.Players.Remove(this);

        health.OnDeath -= OnDeath;
        health.OnDeath -= NetGameManager.Instance.OnPlayerDied;
        ammo.OnNoAmmoLeft -= NetGameManager.Instance.OnPlayerNoAmmoLeft;
    }


    public override void OnStartClient() {
        base.OnStartClient();

        if (base.IsOwner) {
            hudHpBar.SwapTrackedResource(health);
            hudAmmoBar.SwapTrackedResource(ammo);
            barsCanvas.gameObject.SetActive(false);
        }
        else {
            virtualCamera.enabled = false;
        }

        GameManager.Instance.OnRoundStart += RoundStart;
        GameManager.Instance.OnRoundEnd += RoundEnd;
    }

    public override void OnStopClient() {
        base.OnStopClient();

        GameManager.Instance.OnRoundStart -= RoundStart;
        GameManager.Instance.OnRoundEnd += RoundEnd;
    }
    // ====================== Unity Code ======================
    void Awake() {
        if (!virtualCamera) virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        if (!weapon) weapon = GetComponentInChildren<Weapon>();

        playerMovement = GetComponent<PlayerMovement>();

        // Cache reference, then pass variable as parameter
        Register(health = GetComponent<Health>());
        Register(ammo = GetComponent<Ammo>());


        playerUI = (PlayerUI) MenuManager.Get(MenuID.PlayerUI);
        hudHpBar = playerUI.HPBar;
        hudAmmoBar = playerUI.AmmoBar;
    }

    void OnEnable() {
        //health.OnDeath += OnDeath;
        InputManager.InGame_OnShoot += ShootHandler;
    }

    void OnDisable() {
        //health.OnDeath -= OnDeath;
        InputManager.InGame_OnShoot -= ShootHandler;
    }


    void OnTriggerEnter(Collider other) {
        if (!base.IsClient) return;

        // If the object is interactable
        if (other.TryGetComponent<IInteractable>(out var interactable)) { 
            // Treat Collectibles
            if (interactable is ABaseCollectible) {
                TryInteract((ABaseCollectible) interactable);
            }
        }
    }

    // ===================== Custom Code =====================
    [Server]
    public void Restore(bool hardReset = true) {
        CanShoot = true;
        CanControl = true;
        ammo.ResetValues();

        if (hardReset) { 
            health.ResetValues();
        }
        playerMovement.RequestTeleport(_spawnPosition);
    }
    [Server]
    void RoundStart() {
        CanControl = CanShoot = true;
    }
    [Server]
    void RoundEnd() {
        CanControl = CanShoot = false;
    }
    void OnDeath() {
        CanControl = false;
        PlayDeathSound();

        // TODO: trigger death animation
        //Destroy(this, timeBeforeCorpseRemoval);
    }

    // ====================== Shooting =======================

    [Client]
    void ShootHandler(InputAction.CallbackContext ctx) {
        if (CanMove && CanShoot) {
            StartCoroutine(ApplyShootingDelay());
        }
    }

    IEnumerator ApplyShootingDelay() {
        if (CanShoot) {
            CanShoot = false;

            var camPos = Camera.main.transform.position;
            var camDir = Camera.main.transform.forward;

            weaponAnimator.SetTrigger(AnimatorID.triggerAttack);
            PlayShootEffects();
            Shoot(camPos, camDir);

            yield return new WaitForSeconds(weapon.ShootingDelay);
        }

        CanShoot = true;
    }

    [ServerRpc]
    void Shoot(Vector3 cameraPosition, Vector3 direction) {
        if (ammo.HasAmmo) {
            CL_PlayShootEffects();
            ammo.Consume(1);

            if (Physics.Raycast(cameraPosition, direction, out var hit, weapon.MaxShotDistance, damageableLayers)) {
                var hitHp = hit.transform.GetComponent<Health>();
                hitHp?.Damage(weapon.ShotDamage);
            }
        }
    }

    // ===================== IInteractor =====================
    /// <summary>
    /// A cast is necessary from IInteractable to NetworkBehaviour because of some contrains on FishNet's RPCs.
    /// </summary>
    /// <param name="interactor">Must implement the <see cref="IInteractable">IInteractable</see> interface</param>
    //[ServerRpc(RequireOwnership = false)]
    [ServerRpc]
    public void TryInteract(NetworkBehaviour collectible) {
        try {
            // Will fail if the interactor doesn't implement the interface
            var interactable = collectible as IInteractable;

            if (( (IInteractor) this ).CanInteract(interactable)) {
                Interact(interactable);
            }
        }
        catch (InvalidCastException e) {
            Debug.LogError($"PlayerController.TryInteract(): Collectible doesn't implement the IInteractable interface.\n{e}");
        }
    }

    [Server]
    public void Interact(IInteractable interactable) {
        interactable.Interact(this);
    }

    // ======================= Sounds ========================
    [Client]
    public void PlayShootEffects() {
        if (base.IsClient && weapon != null) 
            weapon.PlayShootEffects();
    }

    [ObserversRpc(ExcludeOwner = true)]
    public void CL_PlayShootEffects() {
        if (base.IsClient && weapon != null) 
            weapon.PlayShootEffects();
    }

    [ObserversRpc]
    public void PlayDeathSound() {
        if (base.IsClient && deathSound != null)
            AudioManager.PlayClipAt(deathSound, this.transform.position);
    }
}
