using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Faction : MonoBehaviour
{
    // 0 = Neutral/Wildlife, 1 = Player, 2 = City Residents, 3-8 = various villians
    [SerializeField] public int CurrentFactionID = 0; // For mindcontrolled or spies.
    [SerializeField] public int TrueFactionID = 0; // Their original faction.

    public int GetFactionID()
    {
        return CurrentFactionID;
    }

    public int GetTrueFactionID()
    {
        return TrueFactionID;
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
