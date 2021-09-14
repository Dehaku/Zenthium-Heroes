using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderController : MonoBehaviour
{
    public bool updateSettings = false;
    public SkinnedMeshRenderer rendererz;
    public Material mat;
    public float health;
    public Color color;




    // Start is called before the first frame update
    void Start()
    {
        //var renderer = GetComponent<MeshRenderer>();
        mat = Instantiate(rendererz.sharedMaterial);
        rendererz.material = mat;

        //previousColor = material.GetColor("_BaseColor");

    }

    private void OnDestroy()
    {
        if(mat != null)
        {
            Destroy(mat);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(updateSettings)
        {
            updateSettings = false;
            UpdateSettings();
        }
    }
    void UpdateSettings()
    {
        mat.SetFloat("Dissolve", health);
    }
}
