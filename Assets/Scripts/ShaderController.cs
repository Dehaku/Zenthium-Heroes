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
    [SerializeField]
    public Color glowColor;
}


public class ShaderController : MonoBehaviour
{
    public bool autoUpdateSettings = false;
    public bool updateSettings = false;

    public bool updateSettingsFromSO = false;

    public ColorShaderSettings colorSettings;

    public float dissolve = 0;
    float dissolvePrevious = 0;

    public float glowPower = 1;
    public float glowIntensity = -1.5f;


    [SerializeField]
    public List<MaterialHolder> mats = new List<MaterialHolder>();

    private struct ShaderPropertyIDs
    {
        public int _Color;
        public int _Dissolve;

        public int _GlowColor;
        public int _GlowPower;
        public int _GlowIntensity;

    }
    private ShaderPropertyIDs shaderProps;
    // Start is called before the first frame update
    void Start()
    {
        shaderProps = new ShaderPropertyIDs() {
            _Color = Shader.PropertyToID("_Color"),
            _Dissolve = Shader.PropertyToID("_Dissolve"),

            _GlowColor = Shader.PropertyToID("_GlowColor"),
            _GlowPower = Shader.PropertyToID("_GlowPower"),
            _GlowIntensity = Shader.PropertyToID("_GlowIntensity")
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

        if (updateSettingsFromSO || Input.GetKeyDown(KeyCode.W))
        {
            updateSettingsFromSO = false;
            UpdateSettingsFromSO();
        }

        if(dissolvePrevious != dissolve)
        {
            dissolvePrevious = dissolve;
            UpdateDissolves();
        }

        UpdateGlow();


    }
    void UpdateSettings()
    {
        foreach (var matHold in mats)
        {
            matHold.mat.SetFloat(shaderProps._Dissolve, matHold.health);
            matHold.mat.SetColor(shaderProps._Color, matHold.color);
        }
            
    }

    void UpdateDissolves()
    {
        int inter = 0;
        foreach (var matHold in mats)
        {
            matHold.mat.SetFloat(shaderProps._Dissolve, dissolve);

            inter++;
        }
    }

    void UpdateGlowColor()
    {
        int inter = 0;
        foreach (var matHold in mats)
        {
            matHold.mat.SetColor(shaderProps._GlowColor, matHold.glowColor);

            inter++;
        }
    }

    void UpdateGlow()
    {
        int inter = 0;
        foreach (var matHold in mats)
        {
            matHold.mat.SetFloat(shaderProps._GlowPower, glowPower);
            matHold.mat.SetFloat(shaderProps._GlowIntensity, glowIntensity);

            inter++;
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
            //matHold.mat.SetFloat(shaderProps._Dissolve, matHold.health);
            //matHold.mat.SetColor(shaderProps._Color, settings.colors[inter]);

            inter++;
        }


    }
}
