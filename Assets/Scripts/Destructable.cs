using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Creature))]
public class Destructable : MonoBehaviour
{
    Creature destructable; // Ment for buildings, if the base type is still creature, I probably decided against fixing my laziness.
    public float fallSpeed = 0.5f;
    public float shakeIntensity = 0.05f;
    public float timeBeforeDisable = 5;
    public GameObject particlesPrefab;

    public GameObject destructableTarget;
    public List<Transform> particlesTrans;

    GameObject _particles;
    bool effectActive = false;



    // Start is called before the first frame update
    void Awake()
    {
        destructable = GetComponent<Creature>();
        if (!destructableTarget)
            destructableTarget = this.gameObject;
        if (particlesTrans.Count == 0)
            particlesTrans.Add(this.transform);
    }


    IEnumerator FallNShake()
    {
        destructableTarget.transform.position += new Vector3(Random.Range(-shakeIntensity, shakeIntensity),
           (-fallSpeed * Time.deltaTime),
           Random.Range(-shakeIntensity, shakeIntensity));
        
        yield return new WaitForSeconds(timeBeforeDisable);
        effectActive = false;
        if (_particles)
        {
            _particles.GetComponent<VisualEffect>().Stop();
            _particles.transform.parent = null;
        }
        destructableTarget.gameObject.SetActive(false);
        
    }

    void Destruction()
    {
        if (!effectActive &&  particlesPrefab)
        {
            effectActive = true;
            foreach (var item in particlesTrans)
            {

                _particles = Instantiate(particlesPrefab, item.position, Quaternion.identity, transform);
                Destroy(_particles, timeBeforeDisable + 10);
            }
            
        }

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
