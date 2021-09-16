using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyDemBonesArrayPablo : MonoBehaviour
{
    public GameObject Source;
    public GameObject MyBoneRoot;

    Transform[] SourceBones;
    Transform[] MyBones;

    // Start is called before the first frame update
    void Start()
    {
        if(Source && MyBoneRoot)
        {
            Initialize();
        }
    }

    int CompareName(Transform a, Transform b)
    {
        return (a.name.CompareTo(b.name));
    }

    public void Initialize()
    {
        SourceBones = Source.GetComponentsInChildren<Transform>();
        MyBones = MyBoneRoot.GetComponentsInChildren<Transform>();

        Array.Sort(SourceBones, CompareName);
        Array.Sort(MyBones, CompareName);
    }

    public void CopyBones()
    {
        for(int i=0; i<SourceBones.Length; i++)
        {
            MyBones[i].localPosition = SourceBones[i].localPosition;
            MyBones[i].localRotation = SourceBones[i].localRotation;
            MyBones[i].localScale = SourceBones[i].localScale;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CopyBones();
            Rigidbody[] bodies;
            bodies = MyBoneRoot.GetComponentsInChildren<Rigidbody>();
            foreach (var body in bodies)
            {
                body.isKinematic = true;
            }

            Collider[] col;
            col = MyBoneRoot.GetComponentsInChildren<Collider>();
            foreach (var body in col)
            {
                body.enabled = false;
            }
        }
            
    }




}
