using DG.Tweening;
using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



[RequireComponent(typeof(Faction))]
public class MeleeSeekAttack : MonoBehaviour
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

    public AudioClip attackSound;
    public AudioClip hitSound;
    private int attackSoundID;


    [Header("Melee Reach")]
    public float zipRange = 100f;
    public float zipTime = 0.25f;

    Transform _zipTarget;
    float _zippingTime = 0;
    bool _isZipping = false;


    Faction _myFaction;



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

    Transform FindNearestValidTarget()
    {
        Vector3 myPosition = transform.position;
        Transform potentialTarget = null;
        float potentialTargetDistance = float.MaxValue;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, zipRange/2);
        foreach (var hitCollider in hitColliders)
        {
            
            var tarFaction = hitCollider.GetComponentInParent<Faction>();
            if (!tarFaction)
                continue;
            if (tarFaction.CurrentFactionID == _myFaction.CurrentFactionID)
                continue;




            float distance = Vector3.Distance(myPosition, tarFaction.transform.position);
            if(distance < potentialTargetDistance)
            {
                // Perform Line of Sight check, to make sure we don't zip through a wall.
                if (!LineOfSight())
                    continue;

                potentialTargetDistance = distance;
                potentialTarget = tarFaction.transform;
            }

        }

        return potentialTarget;
    }

    bool LineOfSight()
    {
        Debug.Log("Not Implimented yet.");
        return true;
    }

    public void Attack()
    {
        Transform target = FindNearestValidTarget();
        if (!target)
            return;

        _fireRateTrack = 0;
        int audioID = EazySoundManager.PlaySound(attackSound, 1, false, transform);
        animScript.PunchAnimation(true);

        
        //if(Input.GetKey(KeyCode.W))
        //    transform.DOLookAt(target.transform.position, .2f, AxisConstraint.W);
        //if (Input.GetKey(KeyCode.A))
        //    transform.DOLookAt(target.transform.position, .2f, AxisConstraint.X);
        //if (Input.GetKey(KeyCode.S))
            transform.DOLookAt(target.transform.position, .2f, AxisConstraint.Y);
        //if (Input.GetKey(KeyCode.D))
        //    transform.DOLookAt(target.transform.position, .2f, AxisConstraint.Z);

        transform.DOMove(target.position, zipTime);
        StartCoroutine(HitTarget(target, zipTime));

        

        //meleeColliderScript.damageInfo = damageInfo;

        
    }

    IEnumerator HitTarget(Transform target, float delay)
    {

        yield return new WaitForSeconds(delay);
        DamageInfo damageInfo;
        damageInfo.attacker = this.gameObject;
        damageInfo.damageType = damageType;
        damageInfo.damage = damage;

        var creature = target.GetComponent<CreatureHit>();
        if(!creature)
            creature = target.GetComponentInChildren<CreatureHit>();
        creature.OnHit(damageInfo);
        int audioID = EazySoundManager.PlaySound(hitSound, 1, false, transform);


    }


    private void Awake()
    {
        _myFaction = GetComponent<Faction>();
        if(!_myFaction)
            _myFaction = GetComponentInParent<Faction>();


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
