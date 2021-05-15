using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public bool Conscious = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Unconscious()
    {
        GetComponent<NavMeshAgent>().enabled = false;
        Conscious = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
