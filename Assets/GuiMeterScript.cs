using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuiMeterScript : MonoBehaviour
{
    public float value = 100;
    public float maxValue = 100;

    [Header("Starting Shader Values")]
    public float radius = 0;
    public float lineWidth = 0;
    public Color color;
    public float rotation = 0;
    public float segments = 4;
    public float maxSegmentsPerLayer = 10;
    public float segmentSpacing = 0.01f;

    [Header("Layers")]
    public int layers;
    GameObject backLayer;
    public List<Material> meters;
    public List<Color> layerColors;

    public Material baseMaterial;
    public GameObject pfCircleMeter;

    private struct ShaderPropertyIDs
    {
        public int _Radius;
        public int _LineWidth;
        public int _Color;
        public int _Rotation;
        public int _RemovedSegments;
        public int _SegmentCount;
        public int _SegmentSpacing;
    }
    private ShaderPropertyIDs shaderProps;

    // Start is called before the first frame update
    void Start()
    {
        if (!pfCircleMeter)
        {
            Debug.LogWarning(this.name + " does not have a prefab set.");
            return;
        }

        if (!baseMaterial)
        {
            Debug.LogWarning(this.name + " does not have a material set.");
            return;
        }

        shaderProps = new ShaderPropertyIDs()
        {

            _Radius = Shader.PropertyToID("_Radius"),
            _LineWidth = Shader.PropertyToID("_LineWidth"),
            _Color = Shader.PropertyToID("_Color"),
            _Rotation = Shader.PropertyToID("_Rotation"),
            _RemovedSegments = Shader.PropertyToID("_RemovedSegments"),
            _SegmentCount = Shader.PropertyToID("_SegmentCount"),
            _SegmentSpacing = Shader.PropertyToID("_SegmentSpacing")

        };

        backLayer = Instantiate(pfCircleMeter, this.transform);
        backLayer.GetComponent<Image>().material.SetColor(shaderProps._Color, Color.black);


        for (int i = 0; i < layers; i++)
        {

            GameObject layer = Instantiate(pfCircleMeter,this.transform);
            Material layerMat;
            
            layerMat = Instantiate(baseMaterial);
            layer.GetComponent<Image>().material = layerMat;

            layerMat.SetFloat(shaderProps._Radius, radius);
            layerMat.SetFloat(shaderProps._LineWidth, lineWidth);

            Color randomColor = new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255));
            layerMat.SetColor(shaderProps._Color, randomColor);
            layerMat.SetFloat(shaderProps._Rotation, rotation);
            layerMat.SetFloat(shaderProps._RemovedSegments, 0+(value / maxValue));
            layerMat.SetFloat(shaderProps._SegmentCount, segments);
            layerMat.SetFloat(shaderProps._SegmentSpacing, segmentSpacing);


            meters.Add(layerMat);
        }


    }

    float ValueToSegment(float value)
    {
        float returnValue = value;

        returnValue = segments*(-(value-maxValue) / maxValue);
        
        return returnValue;
    }

    float SegmentsPerLayerFromMaxValue(float maxValue, int layer)
    {
        if(layer == 0)
            return Mathf.Clamp(maxValue / 10,0,10);
        

        float baseValue = 1;
        for (int i = 0; i < layer; i++)
            baseValue *= 10;


        Debug.Log("Headhurts: " + maxValue + " : " + layer + " : " + baseValue + " : " + maxValue / (baseValue * 10) );

        if(maxValue < baseValue)
            return 0f;
        if (maxValue > baseValue * 100)
            return 10f;
        
        return Mathf.Clamp(maxValue / (baseValue * 10), 0,10);
    }

    void UpdateAllMeterValues()
    {

        // backLayer
        {
            var backMat = backLayer.GetComponent<Image>().material;
            backMat.SetFloat(shaderProps._Radius, radius);
            backMat.SetFloat(shaderProps._LineWidth, lineWidth);
            backMat.SetFloat(shaderProps._Rotation, rotation);
            backMat.SetFloat(shaderProps._RemovedSegments, 0);
            backMat.SetFloat(shaderProps._SegmentCount, 1);
            backMat.SetFloat(shaderProps._SegmentSpacing, segmentSpacing);
        }

        int increm = 0;
        foreach (var meter in meters)
        {
            meter.SetFloat(shaderProps._Radius, radius);
            meter.SetFloat(shaderProps._LineWidth, lineWidth);

            if(increm < layerColors.Count)
                meter.SetColor(shaderProps._Color, layerColors[increm]);
            else
            {
                int overflow = increm;
                while (overflow >= layerColors.Count)
                    overflow -= layerColors.Count;
                meter.SetColor(shaderProps._Color, layerColors[overflow]);
            }
            meter.SetFloat(shaderProps._Rotation, rotation);
            //meter.SetFloat(shaderProps._RemovedSegments, 0 + (value / maxValue));
            meter.SetFloat(shaderProps._SegmentCount, SegmentsPerLayerFromMaxValue(maxValue,increm));
            meter.SetFloat(shaderProps._SegmentSpacing, segmentSpacing);

            increm++;
        }
    }

    void UpdateMeterValue()
    {
        foreach (var meter in meters)
        {
            meter.SetFloat(shaderProps._RemovedSegments, ValueToSegment(value));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!baseMaterial || !pfCircleMeter)
            return;

        UpdateAllMeterValues();
        
        UpdateMeterValue();

        

    }
}
