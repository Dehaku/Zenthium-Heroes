using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/GrassSO")]
public class GrassSO : ScriptableObject
{
    public Vector3 size;
    public Color32 color;
    public float growthSpeed;
    public float growthCap;

}
