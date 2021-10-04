using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootKOBullet : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    [Range(0.1f,2f)]
    public float fireRate = 1;
    float _fireRateTrack = 0;
    
    public AudioSource gunSound;
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

    private void Awake()
    {
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
        gunSound.Play();
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
        gunSound.pitch = World.Instance.speedForce;


        if (_isShooting && _fireRateTrack >= fireRate)
            Shoot();
    }

    private GameObject spawnVFX(Transform spawnInfo)
    {
        GameObject vfx;
        vfx = Instantiate(effectToSpawn, spawnInfo.position, spawnInfo.rotation);
        return vfx;

    }
}
