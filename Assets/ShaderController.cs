using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MaterialHolder
{
    [SerializeField]
    public Material mat;
    [SerializeField]
    public float health;
    [SerializeField]
    public Color color;

}


public class ShaderController : MonoBehaviour
{
    public bool autoUpdateSettings = false;
    public bool updateSettings = false;
    public SkinnedMeshRenderer rendererz;
    //private Material mat;
    public float health;
    public Color color;

    [SerializeField]
    public List<MaterialHolder> mats = new List<MaterialHolder>();

 

    // Start is called before the first frame update
    void Start()
    {

        SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skin in skins)
        {
            Material[] matArray = skin.materials;
            int inter = 0;
            foreach (var matIndiv in matArray)
            {
                MaterialHolder matHold = new MaterialHolder();
                matHold.mat = Instantiate(matIndiv);
                matArray[inter] = matHold.mat;
                mats.Add(matHold);
                inter++;
            }
            skin.materials = matArray;
        }
    }

    private void OnDestroy()
    {
        foreach (var matHold in mats)
        {
            if (matHold.mat != null)
            {
                Destroy(matHold.mat);
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (autoUpdateSettings)
            updateSettings = true;

        if(updateSettings)
        {
            updateSettings = false;
            UpdateSettings();
        }
    }
    void UpdateSettings()
    {
        foreach (var matHold in mats)
        {
            matHold.mat.SetFloat("Dissolve", matHold.health);
            matHold.mat.SetColor("_Color", matHold.color);
        }
            
    }
}
