using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)] // Make this run before interactable scripts so they can insert themselves without issue.
public class InteractableGlobalLibrary : MonoBehaviour
{

    public List<Interactable> interactables;
    
    public void Add(Interactable item)
    {
        interactables.Add(item);
    }

    public void Remove(Interactable item)
    {
        interactables.Remove(item);
    }

    [ContextMenu("PrintAllInteractables")]
    public void PrintAllInteractables()
    {
        foreach (var item in interactables)
        {
            if (item.myObject)
                Debug.Log(item.myObject.name + item.myObject.position);
        }
    }


    #region Singleton

    private static InteractableGlobalLibrary _instance;

    public static InteractableGlobalLibrary Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    #endregion
}
