using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObjectLoop : MonoBehaviour
{
    public Vector3 rotationAmount;
    private Vector3 _rotationAmount;
    public float rotationTime;
    public LoopType loopType;
    public Ease easeMethod;
    public UpdateType updateType;


    private Vector3 _startRotation;
    // Start is called before the first frame update
    void Awake()
    {
        _startRotation = transform.eulerAngles;
    }

    private void Start()
    {
        var rotate = transform.DORotate(rotationAmount, rotationTime, RotateMode.FastBeyond360)
            .SetLoops(-1, loopType)
            .SetRelative()
            .SetEase(easeMethod)
            .SetUpdate(updateType);
    }

    // Update is called once per frame
    void Update()
    {
        
        /*
        
        if(transform.eulerAngles == _startRotation)
            transform.DORotate(rotationAmount, rotationTime, RotateMode.FastBeyond360);
        
        if (transform.eulerAngles == rotationAmount)
            transform.DORotate(_startRotation, rotationTime, RotateMode.FastBeyond360);

        */
        
    }

    private void FixedUpdate()
    {
        //transform.eulerAngles = _startRotation + _rotationAmount;
    }
}
