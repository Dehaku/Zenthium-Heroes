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

public struct Damage
{
    public enum damageType
    {
        TechHeal = -3,
        MagicHeal = -2,
        BioHeal = -1,
        Pure = 0,
        Pierce = 1,
        Blunt = 2,
        Tech = 5,
        Fire = 10,
        Cold = 11,
        Shock = 12,
        Toxic = 21,
        Radiation = 22,
        Magic = 31,
        Mental = 32,
        Credit = 51


    }

    public static string GetName(int value)
    {
        return System.Enum.GetName(typeof(Damage.damageType), value);

        
    }

    public static int GetType(string damageType)
    {
        Damage.damageType type;
        bool success = System.Enum.TryParse(damageType, out type);
        if(success)
            return (int)type;

        return int.MinValue;
    }

    
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
