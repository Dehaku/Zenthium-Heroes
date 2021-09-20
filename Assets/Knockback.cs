using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ApplyKnockback(Vector3 direction, float strength, bool considerMass = true)
    {
        Debug.Log("Not implimented yet.");
        Debug.Log("Dir:" + direction + ", Str: " + strength + "UseMass: " + considerMass);
    }
}
