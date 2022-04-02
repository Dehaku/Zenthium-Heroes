using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletSO", menuName = "Weapons/Bullet", order = 0)]
public class BulletSO : ScriptableObject
{
    public GameObject bulletPrefab;
    public float bulletCount = 1;
    public Vector2 bulletSpread;
    public float bulletSpeed = 50;
    public float bulletGravity = 9.8f;
    public float bulletForce = 50;
    public float bulletLifeTime = 50;
    [Space]
    [Header("|AoE Stats|")]
    [Header("If above 0, it changes damage logic.")]
    // If above 0, it changes damage logic.
    public float aoeRange = 0;
    [Header("Every increment in meters, 10% damage is lost.")]
    //Every increment in meters, 10% damage is lost.
    public float aoeDamageFalloffDistanceIncrement = 1f;
    public bool friendlyFire = false;
}
