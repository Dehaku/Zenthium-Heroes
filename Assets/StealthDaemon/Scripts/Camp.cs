using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camp : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        DaemonBrain.AddCamp(this);
    }

    private void OnDestroy()
    {
        DaemonBrain.RemoveCamp(this);
    }
}
