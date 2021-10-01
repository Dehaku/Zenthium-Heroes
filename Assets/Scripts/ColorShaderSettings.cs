using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ColorShaderSettings")]
public class ColorShaderSettings : ScriptableObject
{
    [System.Serializable]
    public struct ColorPart
    {
        [SerializeField]
        public string part;
        [SerializeField]
        public Color color;
        [SerializeField]
        public Texture tex;
    }

    public Color GetPartColor(string partName)
    {
        foreach (var part in parts)
        {
            if (part.part == partName)
                return part.color;
        }
        Debug.Log("Part wasn't found.");
        return new Color(0,0,0,0);
    }

    public Texture GetPartTex(string partName)
    {
        foreach (var part in parts)
        {
            if (part.part == partName)
                return part.tex;
        }
        Debug.Log(partName + " Part wasn't found.");
        return null;
    }

    [SerializeField] public ColorPart[] parts;


}
