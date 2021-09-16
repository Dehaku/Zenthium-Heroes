using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyDemBones : MonoBehaviour
{
    public Transform[] Source;
    public Transform[] MyBoneRoot;

    Transform[] SourceBones;
    Transform[] MyBones;

    // Start is called before the first frame update
    void Start()
    {
        if(Source.Length != 0 && MyBoneRoot.Length != 0)
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
        SourceBones = Source;
        MyBones = MyBoneRoot;

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
        
            
    }




}
