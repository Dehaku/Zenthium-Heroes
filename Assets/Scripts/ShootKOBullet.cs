using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootKOBullet : MonoBehaviour
{
    [Range(0.1f,2f)]
    public float fireRate = 1;
    float _fireRateTrack = 0;
    
    public AudioSource gunSound;
    public GameObject bulletPrefab;
    public Transform spawnPos;
    public GameObject aimReticle;

    public LayerMask aimLayer;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        AimReticleAdjust();

        _fireRateTrack += World.Instance.speedForce * Time.deltaTime;
        gunSound.pitch = World.Instance.speedForce;


        if (Input.GetButton("Fire1") && _fireRateTrack >= fireRate)
        {
            _fireRateTrack = 0;
            gunSound.Play();
            GameObject pew = Instantiate(bulletPrefab, spawnPos.position, Quaternion.identity);
            

            Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
            

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) { pew.transform.forward = Camera.main.transform.forward;  }
            else { pew.transform.LookAt(hit.point); }
        }
    }

    private void AimReticleAdjust()
    {
        Ray camRay = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
        if (!Physics.Raycast(camRay, out RaycastHit camHit, Mathf.Infinity)) {}

        Ray ray = new Ray(spawnPos.position, (camHit.point - spawnPos.position).normalized * 100);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, aimLayer)) {}

        aimReticle.transform.position = Camera.main.WorldToScreenPoint(hit.point);
    }
}
