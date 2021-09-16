using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTransform : MonoBehaviour
{
    [SerializeField]
    public Transform target;

    [SerializeField] bool setPosition = true;
    [SerializeField] bool setRotation = true;
    [SerializeField] bool setScale = true;
    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.transform.SetParent(target, false);
    }

    // Update is called once per frame
    void UpdateTransform()
    {
        if (target == null)
            return;

        if(setPosition)
            transform.position = target.position;
        if(setRotation)
            transform.localRotation = target.localRotation;
        if(setScale)
            transform.localScale = target.localScale;
    }
}
