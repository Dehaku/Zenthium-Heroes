using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Squad")]
public class SquadDefSO : ScriptableObject
{
    public List<GameObject> units = new List<GameObject>();
    public int squadSize = 5;
    public float scaleSize = 1;
    public int difficulty = 1;

    public void RandomSize()
    {
        scaleSize = Random.Range(0.5f, 1.5f);
    }

}
