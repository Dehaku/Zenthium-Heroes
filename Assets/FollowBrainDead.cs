using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBrainDead : MonoBehaviour
{
    public Transform followTarget;
    public float mySpeed = 5f;
    [Space]

    public bool onlyFollowIfTargetInSight = true;
    public float sightTimer = 1f;
    float _sightTimer = 0;
    bool _canSeeTarget = false;

    [Header("Debug")]
    public Rigidbody myBody;



    // Start is called before the first frame update
    void Start()
    {
        if (!myBody)
            myBody = GetComponent<Rigidbody>();
        if (!myBody)
            myBody = GetComponentInChildren<Rigidbody>();
        if (!myBody)
            Debug.Log(gameObject.name + " has no rigid body.");

        if (!followTarget)
            Debug.Log(gameObject.name + " has no Follow Target assigned.");
    }

    // Update is called once per frame
    void Update()
    {
        if (!followTarget)
            return;

        _sightTimer -= Time.deltaTime;
        if(_sightTimer <= 0)
        {
            _sightTimer = (sightTimer + Random.Range(-0.05f, 0.05f));
            CanSeeTarget();
        }
        

        if (onlyFollowIfTargetInSight)
        {
            if (_canSeeTarget)
            {
                MoveTowardsTarget();
            }
            else
                return;
        }
        else
            MoveTowardsTarget();

    }

    bool CanSeeTarget()
    {
        //int layerMask = 1 << 20;

        var visible = GetComponentInChildren<VisibilityTag>();
        var visionTarget = followTarget.GetComponentInChildren<VisibilityTag>();

        Vector3 rayDir = visionTarget.transform.position - visible.transform.position;

        Ray ray = new Ray(visible.transform.position, rayDir);

        RaycastHit hit;
        if (Physics.Raycast(visible.transform.position, rayDir, out hit, 100f))
        {
            //Debug.DrawLine(hit.point, visible.transform.position);
            //Debug.Log(hit.transform.name);
            if(hit.transform.GetComponentInChildren<VisibilityTag>())
            {
                var faction = hit.transform.GetComponentInChildren<Faction>();
                if (faction)
                {
                    if(faction.CurrentFactionID == 1)
                    {
                        _canSeeTarget = true;
                        return true;
                    }
                }
                
            }
                
        }
        _canSeeTarget = false;
        return false;
    }

    void MoveTowardsTarget()
    {
        if (myBody)
            MoveTowardsTargetRigidbody();
    }

    void MoveTowardsTargetRigidbody()
    {
        Vector3 direction = (followTarget.position - myBody.position).normalized;


        myBody.AddForce(direction * mySpeed, ForceMode.Force);
    }
}
