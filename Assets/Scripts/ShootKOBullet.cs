using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootKOBullet : MonoBehaviour
{
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
        if(Input.GetButtonDown("Fire1"))
        {
            //Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
            


            //if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) { return; }

            gunSound.Play();

            GameObject spawnRotation = new GameObject();
            //spawnRotation.transform.LookAt(hit.point);

            
            GameObject pew = Instantiate(bulletPrefab, spawnPos.position, Quaternion.identity);
            //pew.transform.eulerAngles = Camera.main.transform.forward;
            //pew.transform.forward = spawnPos.forward;
            pew.transform.forward = Camera.main.transform.forward;
            //pew


            //spawnRotation = pew;
            //spawnRotation.transform.rotation.eulerAngles
            pew.GetComponent<Rigidbody>().velocity = Camera.main.transform.forward * 5;

        }

    }
}
