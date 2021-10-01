using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerColliderScript : MonoBehaviour
{
    public FlamethrowerScript parentScript;
    public float durationMax;
    public float currentDuration;


    // Start is called before the first frame update
    void Start()
    {
        if (parentScript == null)
            Debug.LogWarning(this.name + " doesn't have a parent set, likely to crash soon.");

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        parentScript.ChildCollisionEnter(this, other);
    }

    private void OnTriggerStay(Collider other)
    {
        parentScript.ChildCollisionStay(this, other);
    }
    private void OnTriggerExit(Collider other)
    {
        parentScript.ChildCollisionExit(this, other);
    }

}
