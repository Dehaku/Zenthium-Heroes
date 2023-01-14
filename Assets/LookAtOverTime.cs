using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtOverTime : MonoBehaviour
{
    public Transform looker;
    public Transform target;
    [Space]
    public float lookSpeed = 1;
    // Start is called before the first frame update
    void Start()
    {
        if (!looker)
            Debug.Log("No looker assigned");
        if (!target)
            Debug.Log("No lookee assigned");
    }

    Vector3 lookDir;
    float singleStep;
    Vector3 newDirection;

    // Update is called once per frame
    void Update()
    {
        lookDir = target.position - looker.position;
        singleStep = lookSpeed * Time.deltaTime;

        newDirection = Vector3.RotateTowards(looker.forward, lookDir, singleStep, 0.0f);

        looker.rotation = Quaternion.LookRotation(newDirection);
    }
}
