using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{

    private Rigidbody[] rigids;
    private Animator anim;
    public bool isRagdolled = false;

    void Start()
    {
        rigids = GetComponentsInChildren<Rigidbody>();
        anim = GetComponent<Animator>();
        if(!anim)
            anim = GetComponentInChildren<Animator>();

        DisableRagdoll();
    }

    public void EnableRagdoll()
    {
        anim.enabled = false;
        isRagdolled = true;
        foreach (Rigidbody rig in rigids)
        {
            rig.useGravity = true;
            rig.isKinematic = false;
        }

        var enemy = GetComponent<Enemy>();
        if (enemy != null)
            enemy.Unconscious();
    }

    public void DisableRagdoll(bool fixPosition = true)
    {
        //if (fixPosition)
        //    gameObject.transform.position = rigids[0].transform.position;

        anim.enabled = true;
        isRagdolled = false;
        foreach (Rigidbody rig in rigids)
        {
            rig.useGravity = false;
            rig.isKinematic = true;
        }
    }
}