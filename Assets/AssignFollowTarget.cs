using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignFollowTarget : MonoBehaviour
{

    // Start is called before the first frame update
    void Awake()
    {
        if(TryGetComponent<FollowBrainDead>(out FollowBrainDead Follow))
        {
            Follow.followTarget = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (TryGetComponent<LookAtOverTime>(out LookAtOverTime Look))
        {
            Look.target = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }
}
