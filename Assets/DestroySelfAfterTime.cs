using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelfAfterTime : MonoBehaviour
{
    [SerializeField] float duration = 1f;
    float durationTimer = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        durationTimer += Time.deltaTime;
        if (durationTimer > duration)
            Destroy(this.gameObject);

    }
}
