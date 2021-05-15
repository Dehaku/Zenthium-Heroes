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
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _fireRateTrack += World.Instance.speedForce * Time.deltaTime;
        gunSound.pitch = World.Instance.speedForce;

        if (Input.GetButton("Fire1") && _fireRateTrack >= fireRate)
        {
            _fireRateTrack = 0;
            gunSound.Play();
            GameObject pew = Instantiate(bulletPrefab, spawnPos.position, Quaternion.identity);
            pew.transform.forward = Camera.main.transform.forward;
        }
    }
}
