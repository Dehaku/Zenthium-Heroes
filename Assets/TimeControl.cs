using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeControl : MonoBehaviour
{
    [Range(0, 2)]
    public float timeControlOnSpeed = 0.5f;
    public bool AffectPhysicsTimeScale;

    public bool On;
    bool wasOn = false;

    float defaultTimeScale;
    float defaultFixedDeltaTime;

    // Start is called before the first frame update
    void Start()
    {
        defaultTimeScale = Time.timeScale;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    void Update()
    {
        if(On)
        {
            wasOn = true;
            Time.timeScale = timeControlOnSpeed;
            if (AffectPhysicsTimeScale)
                Time.fixedDeltaTime = timeControlOnSpeed;
        }
        else if(wasOn)
        { // We put a small step in so we don't overwrite other time control methods.
            wasOn = false;
            Time.timeScale = defaultTimeScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
        
    }
}
