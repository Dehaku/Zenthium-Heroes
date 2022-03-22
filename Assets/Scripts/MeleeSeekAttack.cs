using DG.Tweening;
using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private int attackSoundID = 0;


    [Header("Melee Reach")]
    public float zipRange = 100f;
    public float zipTime = 0.25f;
    public float zipApproachOffsetDistance = 2f;
    
    [Range(1, 10)]
    public float cameraAngleTargetDistanceWeight = 10;

    Transform _zipTarget;
    //float _zippingTime = 0;
    //bool _isZipping = false;


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

        // Grabbing all colliders in range.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, zipRange/2);

        // Getting the important faction component from their parents and reducing it to one entry per.
        List<Faction> factions = new List<Faction>();
        foreach (var hitCollider in hitColliders)
        {
            var tarFaction = hitCollider.GetComponentInParent<Faction>();
            if (tarFaction)
                factions.Add(tarFaction);
        }
        factions = new List<Faction>(factions.Distinct());
        

        foreach (var tarFaction in factions)
        {
            if (tarFaction.CurrentFactionID == _myFaction.CurrentFactionID)
                continue;
            // Should probably have a reference to faction from creature. It'd cut down on get component calls.
            if (!tarFaction.GetComponent<Creature>().isConscious)
                continue;



            var tarPos = tarFaction.transform.position;
            float distance = Vector3.Distance(myPosition, tarPos);

            Vector3 targetDir = tarPos - transform.position;
            distance += (Vector3.Angle(targetDir, Camera.main.transform.forward) / cameraAngleTargetDistanceWeight);


            if (distance < potentialTargetDistance)
            {
                // Perform Line of Sight check, to make sure we don't zip through a wall. Doesn't seem to work with my creatures currently, only blocks.
                //if (!LineOfSight(tarFaction.gameObject))
                //    continue;
                potentialTargetDistance = distance;
                potentialTarget = tarFaction.transform;
            }

        }
        return potentialTarget;
    }

    bool LineOfSight(GameObject target)
    { // Doesn't seem to work for enemies, not sure why yet.
        var transPos = transform.position;
        RaycastHit hit;
        bool gotHit = false;
        gotHit = Physics.Raycast(transPos, target.transform.position - transPos, out hit);
        Debug.Log(hit.transform.name);
        if (hit.transform == target.transform)
        {
            
            Debug.Log("Target Hit");
            return true;
        }
            
        Debug.Log("Nothing Hit!");
        return false;

    }

    Vector3 TargetOffset(Vector3 initialPos)
    {
        return Vector3.MoveTowards(initialPos, transform.position, .95f);
    }

    public void Attack()
    {
        Transform target = FindNearestValidTarget();
        if (!target)
            return;

        _fireRateTrack = 0;
        int audioID = EazySoundManager.PlaySound(attackSound, 1, false, transform);
        animScript.PunchAnimation(true);

        
        // AxisConstraint.Y seems to be the right one to prevent the character from facing weird directions.
        transform.DOLookAt(target.transform.position, .2f, AxisConstraint.Y);
        transform.DOMove(TargetOffset(target.position), zipTime);
        StartCoroutine(HitTarget(target, zipTime));
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
