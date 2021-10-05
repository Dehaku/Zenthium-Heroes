using Hellmade.Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootKOBullet : MonoBehaviour
{
    [SerializeField] public bool isPlayer = false;
    [SerializeField] private PlayerInput playerInput;

    [Range(0.1f,2f)]
    public float fireRate = 1;
    float _fireRateTrack = 0;
    
    public AudioClip gunSound;
    private int gunSoundID;
    public GameObject bulletPrefab;
    public Transform spawnPos;
    public GameObject aimReticle;

    public List<GameObject> vfx = new List<GameObject>();
    GameObject effectToSpawn;

    public PlayerAimController playerCamera;
    public WeaponRecoil recoil;

    [Space]
    public float bulletSpeed = 50;
    public float bulletGravity = 1;
    public float bulletForce = 50;
    public float bulletLifeTime = 50;

    [Space]
    public bool predictionLine = true;
    public bool predictionTarget = true;
    public float predictionTime = 1f;
    public float predictionTimeStep = 0.05f;
    public Material predictionMaterial;

    GameObject _predictionGO;
    BulletProjectileRaycast _prediction;


    private void Awake()
    {
        if (!isPlayer)
            return;

        playerCamera = GetComponent<PlayerAimController>();
        recoil = GetComponent<WeaponRecoil>();
        recoil.playerCamera = playerCamera;

        effectToSpawn = vfx[0];
    }

    // Start is called before the first frame update
    void Start()
    {

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
        int audioID = EazySoundManager.PlaySound(gunSound, 1, false, transform);
        
        GameObject pew = Instantiate(bulletPrefab, spawnPos.position, Quaternion.identity);



        Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);


        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) { pew.transform.forward = Camera.main.transform.forward; }
        else { pew.transform.LookAt(hit.point); }

        BulletProjectileRaycast bulletScript = pew.GetComponent<BulletProjectileRaycast>();
        if (bulletScript)
        {
            bulletScript.Initialize(pew.transform, bulletSpeed, bulletGravity);
        }
        Destroy(pew, bulletLifeTime);

        GameObject vfx = spawnVFX(pew.transform);
        vfx.transform.SetParent(pew.transform);


        recoil.GenerateRecoil();
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


        if (_isShooting && _fireRateTrack >= fireRate)
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
            _predictionGO = Instantiate(bulletPrefab, spawnPos.position, Quaternion.identity);
            _prediction = _predictionGO.GetComponent<BulletProjectileRaycast>();
            _prediction.predictionMaterial = predictionMaterial;
        }
        if (!_prediction)
            return;

        _predictionGO.transform.position = spawnPos.position;

        Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) { _predictionGO.transform.forward = Camera.main.transform.forward; }
        else { _predictionGO.transform.LookAt(hit.point); }

        

        if(_prediction.PredictTrajectory(_predictionGO.transform, bulletSpeed, bulletGravity, predictionTime, predictionTimeStep))
        {
            //spawnVFX(_prediction.predictHit.transform);

            if (predictionTarget)
            {
                var aimCon = FindObjectOfType<PlayerAimController>();
                if (aimCon)
                    aimCon.AimReticleAdjust(_prediction.predictHit.point);
        }

        }
            

    }

    private GameObject spawnVFX(Transform spawnInfo)
    {
        GameObject vfx;
        vfx = Instantiate(effectToSpawn, spawnInfo.position, spawnInfo.rotation);
        return vfx;

    }
}
