using Hellmade.Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;


public class ShootKOBullet : MonoBehaviour
{
    [Header("Player Only")]
    [SerializeField] public bool isPlayer = false;
    [SerializeField] private PlayerInput playerInput;
    public GameObject aimReticle;
    public PlayerAimController playerCamera;
    PlayerAimController playerAimController;
    public WeaponRecoil recoil;


    [Space]
    [Header("Gun")]
    public WeaponRangedSO weapon;
    
    [Range(0.1f,2f)]
    float _fireRateTrack = 0;
    
    
    private int gunSoundID = 0;
    
    public Transform spawnPos;
    

    GameObject effectToSpawn;



    [Space]
    [Header("Bullet")]
    public BulletSO bullet;
    

    [Space]
    [Header("Prediction")]
    public bool predictionLine = true;
    public bool predictionTarget = true;
    public float predictionTime = 1f;
    public float predictionTimeStep = 0.05f;
    public Material predictionMaterial;

    GameObject _predictionGO;
    BulletProjectileRaycast _prediction;

    public LayerMask rayMask;


    private void Awake()
    {
        if(weapon.vfx[0])
            effectToSpawn = weapon.vfx[0];

        if (!isPlayer)
            return;

        playerCamera = GetComponent<PlayerAimController>();
        recoil = GetComponent<WeaponRecoil>();
        recoil.playerCamera = playerCamera;
        
    }

    // Start is called before the first frame update
    void Start()
    {
        if(!bullet || !weapon)
            Debug.LogError(name + " doesn't have it's bullet/weapon set!");
    }

    bool _isShooting = false;

    public void ShootInput(InputAction.CallbackContext context)
    {
        if(playerInput.actions["Aim"].IsPressed())

        if (context.performed)
            _isShooting = true;

        if (context.canceled)
            _isShooting = false;

    }

    public void Shoot()
    {
        _fireRateTrack = 0;
        //gunSound.Play();
        int audioID = EazySoundManager.PlaySound(weapon.gunSound, 1, false, transform);
        
        if(!bullet.bulletPrefab)
        {
            Debug.LogWarning("Prefab not found in ShootKOBullet");
            return;
        }

        DamageInfo damageInfo;
        damageInfo.attacker = this.gameObject;
        damageInfo.damageType = 1;
        if (Input.GetKey(KeyCode.D))
            damageInfo.damageType = -2;
        damageInfo.damage = weapon.damage;

        for (int i = 0; i < bullet.bulletCount; i++)
        {
            GameObject pew = Instantiate(bullet.bulletPrefab, spawnPos.position, Quaternion.identity);
            


            

            if (isPlayer)
            {
                Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
                if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, rayMask, QueryTriggerInteraction.Ignore)) { pew.transform.forward = Camera.main.transform.forward; }
                else { pew.transform.LookAt(hit.point); }
            }
            else
                pew.transform.forward = spawnPos.transform.forward;

            if(bullet.bulletSpread != Vector2.zero)
            {
                var baseAngles = pew.transform.eulerAngles;
                baseAngles.x += Random.Range(-bullet.bulletSpread.y, bullet.bulletSpread.y);
                baseAngles.y += Random.Range(-bullet.bulletSpread.x, bullet.bulletSpread.x);
                pew.transform.eulerAngles = baseAngles;
            }

            BulletProjectileRaycast bulletScript = pew.GetComponent<BulletProjectileRaycast>();
            if (bulletScript)
            {
                bulletScript.Initialize(pew.transform, bullet.bulletSpeed, bullet.bulletGravity, bullet.aoeRange, bullet.aoeDamageFalloffDistanceIncrement);
                bulletScript.damageInfo = damageInfo;
            }
            Destroy(pew, bullet.bulletLifeTime);

            if (effectToSpawn)
            {
                GameObject vfx = spawnVFX(pew.transform);
                if (vfx)
                    vfx.transform.SetParent(pew.transform);
            }
        }

        
        


        if(isPlayer)
            recoil.GenerateRecoil();
    }

    public void Fire(bool toFire = true)
    {
        _isShooting = toFire;
    }

    // Update is called once per frame
    void Update()
    {
        //Setup a StartFiring() function and add:
        // recoil.Reset();


        _fireRateTrack += World.Instance.speedForce * Time.deltaTime;
        //gunSound.pitch = World.Instance.speedForce;
        if(EazySoundManager.GetAudio(gunSoundID) != null)
            EazySoundManager.GetAudio(gunSoundID).Pitch = World.Instance.speedForce;


        if (_isShooting && _fireRateTrack >= weapon.fireRate)
            Shoot();

        

        
    }

    private void LateUpdate()
    {
        if(playerInput)
        {
            bool aimMode = playerInput.actions["Aim"].IsPressed();
            if (predictionLine && aimMode)
            {
                PredictionLine();
            }
            else if (_prediction)
                _prediction.SetLineActive(false);
        }
        else
        {
            if (predictionLine)
            {
                PredictionLine();
            }
            else if (_prediction)
                _prediction.SetLineActive(false);
        }
        
    }



    void PredictionLine()
    {
        if(!_predictionGO)
        {
            if (!bullet.bulletPrefab)
            {
                Debug.LogWarning("Prefab not found in ShootKOBullet");
                return;
            }

            _predictionGO = Instantiate(bullet.bulletPrefab, spawnPos.position, Quaternion.identity);
            _prediction = _predictionGO.GetComponent<BulletProjectileRaycast>();
            _prediction.predictionMaterial = predictionMaterial;
            
            for(int i = 0; i != _prediction.transform.childCount; i++)
            {
                _prediction.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        if (!_prediction)
            return;

        _predictionGO.transform.position = spawnPos.position;

        if(isPlayer)
        {
            Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, rayMask, QueryTriggerInteraction.Ignore)) { _predictionGO.transform.forward = Camera.main.transform.forward; }
            else { _predictionGO.transform.LookAt(hit.point); }
        }
        else
        {
            _predictionGO.transform.forward = spawnPos.transform.forward;
        }
        

        

        if(_prediction.PredictTrajectory(_predictionGO.transform, bullet.bulletSpeed, bullet.bulletGravity, predictionTime, predictionTimeStep))
        {
            //spawnVFX(_prediction.predictHit.transform);

            if (predictionTarget && isPlayer)
            {
                if(!playerAimController)
                    playerAimController = FindObjectOfType<PlayerAimController>();
                if (playerAimController)
                    playerAimController.AimReticleAdjust(_prediction.predictHit.point);
        }

        }
            

    }

    

    private GameObject spawnVFX(Transform spawnInfo)
    {
        if (!effectToSpawn)
        {
            Debug.LogWarning("VFXPrefab not found in ShootKOBullet");
            return null;
        }

        GameObject vfx;
        vfx = Instantiate(effectToSpawn, spawnInfo.position, spawnInfo.rotation);
        return vfx;

    }
}
