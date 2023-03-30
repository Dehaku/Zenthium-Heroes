using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Survivor : MonoBehaviour
{
    public Transform target;

    // Start is called before the first frame update
    void Awake()
    {
        DaemonBrain.AddSurvivor(this);
    }

    private void OnDestroy()
    {
        DaemonBrain.RemoveSurvivor(this);
    }
}
