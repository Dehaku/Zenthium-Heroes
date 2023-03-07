using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public Transform myObject;
    public Transform interactPosAndRot;
    public float interactWarmupTime;
    public float interactDuration;
    public CivilianLifeBrain reserved;

    private void OnEnable()
    {
        InteractableGlobalLibrary.Instance.Add(this);
    }

    private void OnDisable()
    {
        Debug.Log(transform.name + ": I'm being disabled!");
        InteractableGlobalLibrary.Instance.Remove(this);
    }

    private void OnDestroy()
    {
        Debug.Log(transform.name + ": I'm being destroyed!");
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!myObject)
            myObject = this.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
