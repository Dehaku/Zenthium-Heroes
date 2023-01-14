using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomHop : MonoBehaviour
{
    public bool allowedToHop = true;
    public Rigidbody myBody;

    [Header("Timer")]
    public float hopTimer = 10;
    float _hopTimer = 0;
    public float timerVariance = 2.5f;
    [Header("Hop Settings")]
    public float hopPower = 5;
    public ForceMode forceMode;
    



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!allowedToHop || !myBody)
            return;

        
        HopLogic();
    }

    void HopLogic()
    {
        _hopTimer -= Time.deltaTime;
        if (_hopTimer <= 0)
        {
            _hopTimer = hopTimer + Random.Range(-timerVariance, timerVariance);
            Hop();
        }
    }

    void Hop()
    {
        //myBody.AddForce(new Vector3(0, hopPower, 0), forceMode);
        myBody.AddRelativeForce(new Vector3(0, hopPower, hopPower), forceMode);
        
    }
}
