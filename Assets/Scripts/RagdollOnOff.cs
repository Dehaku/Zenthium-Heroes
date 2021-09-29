using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollOnOff : MonoBehaviour
{
    public Collider mainCollider;
    public GameObject ThisGuysRig;
    public Animator ThisGuysAnimator;
    public Rigidbody ThisGuysRigid;
    public bool FlickRagdollModeOn = false;
    public bool FlickRagdollModeOff = false;
    public GameObject ragdollCenterFemale;
    public GameObject ragdollCenterMale; 


    void Start()
    {
        ThisGuysAnimator = GetComponentInChildren<Animator>();
        GetRagdollBits();
        RagdollModeOff(false);
    }

    void Update()
    {
        if(FlickRagdollModeOff)
        {
            RagdollModeOff();
            FlickRagdollModeOff = false;
        }
        if (FlickRagdollModeOn)
        {
            RagdollModeOn();
            FlickRagdollModeOn = false;
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Knockdown")
        {
            RagdollModeOn();
        }
    }

    Collider[] ragdollColliders;
    Rigidbody[] limbsRigidbodies;

    void GetRagdollBits()
    {
        ragdollColliders = ThisGuysRig.GetComponentsInChildren<Collider>();
        limbsRigidbodies = ThisGuysRig.GetComponentsInChildren<Rigidbody>();
    }

    public void RagdollModeOn()
    {
        ThisGuysAnimator.enabled = false;

        foreach (var col in ragdollColliders)
        {
            col.enabled = true;
        }

        foreach (var rigid in limbsRigidbodies)
        {
            rigid.isKinematic = false;
            rigid.useGravity = true;
        }
        mainCollider.enabled = false;
        ThisGuysRigid.isKinematic = true;
        var enemy = GetComponent<Enemy>();
        if(enemy != null)
            enemy.Unconscious();
    }

    public void RagdollModeOff(bool FixPosition = true)
    {
        if(FixPosition)
        {

            if (ragdollCenterFemale.activeInHierarchy == true)
                mainCollider.transform.position = ragdollCenterFemale.transform.position;
            else if (ragdollCenterMale.activeInHierarchy == true)
                mainCollider.transform.position = ragdollCenterMale.transform.position;
        }
            

        foreach (var col in ragdollColliders)
        {
            col.enabled = false;
        }

        foreach (var rigid in limbsRigidbodies)
        {
            rigid.isKinematic = true;
            rigid.useGravity = false;
        }

        ThisGuysAnimator.enabled = true;
        mainCollider.enabled = true;
        ThisGuysRigid.isKinematic = true; // This needs to be false. Or should it?
        
    }
}
