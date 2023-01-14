using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRaySwitchLayer : MonoBehaviour
{
    public LayerMask defaultLayer;
    public LayerMask xRayLayer;

    bool xRayActive = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleXRay();
        }
    }

    public void ToggleXRay()
    {
        int layerNum;

        if(xRayActive)
            layerNum = (int)Mathf.Log(defaultLayer.value, 2);
        else
            layerNum = (int)Mathf.Log(xRayLayer.value, 2);
        
        xRayActive = !xRayActive;
        gameObject.layer = layerNum;
        if (transform.childCount > 0)
            SetLayerAllChildren(transform, layerNum);
    }

    void SetLayerAllChildren(Transform root, int layer)
    {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);

        foreach (var child in children)
        {
            child.gameObject.layer = layer;
        }
    }
}
