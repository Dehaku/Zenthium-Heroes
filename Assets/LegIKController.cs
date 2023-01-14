using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegIKController : MonoBehaviour
{
    public float adjustDistance = 1f;
    public float checkTimer = 1f;
    float _checkTimer = 0;
    
    [Space(5)]
    public Transform FRTarget;
    public Transform FRTargetOrigin;
    public Transform FLTarget;
    public Transform FLTargetOrigin;
    public Transform RRTarget;
    public Transform RRTargetOrigin;
    public Transform RLTarget;
    public Transform RLTargetOrigin;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        _checkTimer -= Time.deltaTime;
        if(_checkTimer <= 0)
        {
            _checkTimer = checkTimer;
            CheckLegs();
        }    
    }

    void CheckLegs()
    {
        if(Vector3.Distance(FRTarget.position, FRTargetOrigin.position) >= adjustDistance)
        {
            FRTarget.position = FRTargetOrigin.position;
        }

        if (Vector3.Distance(RRTarget.position, RRTargetOrigin.position) >= adjustDistance)
        {
            RRTarget.position = RRTargetOrigin.position;
        }

        if (Vector3.Distance(FLTarget.position, FLTargetOrigin.position) >= adjustDistance)
        {
            FLTarget.position = FLTargetOrigin.position;
        }

        if (Vector3.Distance(RLTarget.position, RLTargetOrigin.position) >= adjustDistance)
        {
            RLTarget.position = RLTargetOrigin.position;
        }
    }
}
