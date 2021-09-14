using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ColorShaderSettings")]
public class ColorShaderSettings : ScriptableObject
{
    [SerializeField] public Color[] colors;


}
