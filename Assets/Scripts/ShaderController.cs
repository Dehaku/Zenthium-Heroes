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

    public bool updateSettingsFromSO = false;

    public ColorShaderSettings colorSettings;

    float dissolve = 0;
    float dissolvePrevious = 0;
    

    








    [SerializeField]
    public List<MaterialHolder> mats = new List<MaterialHolder>();




    private struct ShaderPropertyIDs
    {
        public int _Color;
        public int _Dissolve;

    }
    private ShaderPropertyIDs shaderProps;
    // Start is called before the first frame update
    void Start()
    {
        shaderProps = new ShaderPropertyIDs() {
            _Color = Shader.PropertyToID("_Color"),
            _Dissolve = Shader.PropertyToID("_Dissolve")
        };


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

        if (updateSettingsFromSO)
        {
            updateSettingsFromSO = false;
            UpdateSettingsFromSO();
        }

    }
    void UpdateSettings()
    {
        foreach (var matHold in mats)
        {
            matHold.mat.SetFloat(shaderProps._Dissolve, matHold.health);
            matHold.mat.SetColor(shaderProps._Color, matHold.color);
        }
            
    }

    void UpdateSettingsFromSO(ColorShaderSettings settings = null)
    {
        if (settings == null)
            settings = colorSettings;
        if (settings == null)
        {
            Debug.LogWarning("No color shader settings for UpdateSettingsFromSO()!");
            return;
        }

        int inter = 0;
        foreach (var matHold in mats)
        {
            matHold.mat.SetFloat("Dissolve", matHold.health);
            matHold.mat.SetColor("_Color", settings.colors[inter]);

            inter++;
        }


    }
}
