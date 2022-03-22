using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeAttack : MonoBehaviour
{
    [Header("Player Only")]
    [SerializeField] public bool isPlayer = false;
    [SerializeField] private PlayerInput playerInput;
    public PlayerAimController playerCamera;
    public WeaponRecoil recoil;

    [Header("Melee")]
    [Range(0.1f, 2f)]
    public float fireRate = 1;
    float _fireRateTrack = 0;
    public float damage = 10;
    public int damageType = 2;
    public float colliderTime = 0.25f;

    public AudioClip attackSound;
    private int attackSoundID = 0;

    
    [Header("Melee Reach")]
    public GameObject meleeCollider;
    MeleeColliderScript meleeColliderScript;


    AnimationScript animScript;

    bool _isFiring = false;

    public void FireInput(InputAction.CallbackContext context)
    {
        // Do not melee if we're aiming a weapon.
        if (playerInput.actions["Aim"].IsPressed())
            return;


            if (playerInput.actions["Fire"].IsPressed())

            if (context.performed)
                _isFiring = true;

        if (context.canceled)
            _isFiring = false;

    }

    public void Melee(bool toFire = true)
    {
        _isFiring = toFire;
    }

    public void Attack()
    {
        _fireRateTrack = 0;
        int audioID = EazySoundManager.PlaySound(attackSound, 1, false, transform);
        animScript.PunchAnimation(false);


        DamageInfo damageInfo;
        damageInfo.attacker = this.gameObject;
        damageInfo.damageType = damageType;
        damageInfo.damage = damage;

        meleeColliderScript.damageInfo = damageInfo;

        HandleCollider();
        
    }

    void HandleCollider()
    {
        meleeCollider.SetActive(true);
        StartCoroutine(DisableColliderAfterTime(colliderTime));
    }

    IEnumerator DisableColliderAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        meleeCollider.SetActive(false);
    }

    private void Awake()
    {
        meleeCollider.SetActive(false);
        meleeColliderScript = meleeCollider.GetComponent<MeleeColliderScript>();

        if (!isPlayer)
            return;

        playerCamera = GetComponent<PlayerAimController>();
        recoil = GetComponent<WeaponRecoil>();
        recoil.playerCamera = playerCamera;

    }

    // Start is called before the first frame update
    void Start()
    {
        animScript = GetComponent<AnimationScript>();
    }

    


    // Update is called once per frame
    void Update()
    {
        _fireRateTrack += World.Instance.speedForce * Time.deltaTime;
        //gunSound.pitch = World.Instance.speedForce;
        if (EazySoundManager.GetAudio(attackSoundID) != null)
            EazySoundManager.GetAudio(attackSoundID).Pitch = World.Instance.speedForce;


        if (_isFiring && _fireRateTrack >= fireRate)
            Attack();
    }
}
