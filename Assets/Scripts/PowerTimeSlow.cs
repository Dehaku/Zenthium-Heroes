using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerTimeSlow : MonoBehaviour
{
    [Range(0,2)]
    public float speed = 0.5f;
    public bool AffectTimeScale;
    float _previousSpeed;

    
    

    public bool isTimeSlowed;


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
        if(Input.GetKeyDown(KeyCode.R))
        {
            _previousSpeed = World.Instance.speedForce;
            World.Instance.speedForce = speed;
            if(AffectTimeScale)
            {
                Time.timeScale = World.Instance.speedForce;
                Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
            }
            
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            World.Instance.speedForce = _previousSpeed;

            if(AffectTimeScale)
            {
                Time.timeScale = _previousSpeed;
                Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
            }
        }
    }
}
