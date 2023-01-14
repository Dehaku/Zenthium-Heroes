using UnityEngine;

// LateFollow.cs
// A simple concave collider solution for raycasts
// Compatible with offset position and rotation
//
// To use, place MeshCollider object outside physics object,
// attach this script to the MeshCollider object, and
// assign the physics object as FollowTarget.
// Be sure to disable collision between the objects via
// physics layers!
//
// For questions: /u/ActionScripter9109

public class LateFollow : MonoBehaviour
{
    public Transform FollowTarget;
    public bool FollowPos = true;
    public bool FollowRot = true;

    Vector3 _localPosShift;
    Quaternion _localRotShift;

    void Awake()
    {
        if (FollowTarget == null)
        {
            Debug.LogError("No follow target assigned for object " + gameObject.name);
            enabled = false;
            return;
        }
        _localPosShift = FollowTarget.InverseTransformPoint(transform.position);
        _localRotShift = Quaternion.Inverse(FollowTarget.rotation) * transform.rotation;
        
    }

    void LateUpdate()
    {
        if(FollowRot)
            transform.rotation = FollowTarget.rotation * _localRotShift;
        if(FollowPos)
            transform.position = FollowTarget.TransformPoint(_localPosShift);
    } 
    
}