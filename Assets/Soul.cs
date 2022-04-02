using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soul : MonoBehaviour
{
    public string nameFirst;
    public string nameLast;
    public float mana = 100;
    public float manaMax = 100;
    public double age;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        age += Time.deltaTime;
    }
}
