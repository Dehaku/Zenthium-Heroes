using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Creature))]
public class Destructable : MonoBehaviour
{
    Creature destructable; // Ment for buildings, if the base type is still creature, I probably decided against fixing my laziness.
    public float fallSpeed = 0.5f;
    public float shakeIntensity = 0.05f;
    public float timeBeforeDisable = 5;



    // Start is called before the first frame update
    void Awake()
    {
        destructable = GetComponent<Creature>();
    }


    IEnumerator FallNShake()
    {
       transform.position += new Vector3(Random.Range(-shakeIntensity, shakeIntensity),
           (-fallSpeed * Time.deltaTime),
           Random.Range(-shakeIntensity, shakeIntensity));
        //transform.position = shakePos;
        
        yield return new WaitForSeconds(timeBeforeDisable);
        this.gameObject.SetActive(false);
    }

    void Destruction()
    {
        StartCoroutine(FallNShake());
    }

    // Update is called once per frame
    void Update()
    {
        if (destructable.health <= 0)
        {
            destructable.isAlive = false;
            destructable.isConscious = false;
            Destruction();
            return;
        }
            
    }
}
