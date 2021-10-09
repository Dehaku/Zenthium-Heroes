using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DamageInfo
{
    public GameObject attacker;
    public int damageType;
    public float damage;
}

public abstract class ShootableObject : MonoBehaviour
{
    public abstract void OnHit(RaycastHit hit, DamageInfo dI = new DamageInfo());
    public abstract void OnHit(DamageInfo dI = new DamageInfo());
}
