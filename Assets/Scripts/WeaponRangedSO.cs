using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponRangedSO", menuName = "Weapons/Ranged", order = 0)]
public class WeaponRangedSO : ScriptableObject
{
    public float fireRate = 1;
    public float damage = 10;


    public AudioClip gunSound;


    public GameObject modelGO;
    public List<GameObject> vfx = new List<GameObject>();

}
