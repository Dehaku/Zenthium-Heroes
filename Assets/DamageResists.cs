using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DamageResist
{
    public int damageType;
    public float resistAmount;
    public bool percentage;
}

public class DamageResists : MonoBehaviour
{
    public List<DamageResist> resists;

    public void AddResistence(DamageResist DR, bool OverwriteWithBest)
    {
        if (OverwriteWithBest)
            Debug.LogWarning("I haven't put this in yet.");

        resists.Add(DR);
    }

    public List<DamageResist> GetResistencesOfDamageType(int damageType)
    {
        List<DamageResist> returnList = new List<DamageResist>();

        foreach (var dR in resists)
            if (dR.damageType == damageType)
                returnList.Add(dR);

        return returnList;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
