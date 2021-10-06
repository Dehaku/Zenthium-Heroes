using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Creature : MonoBehaviour
{
    public float healthMax = 100;
    public float health = 100;
    public bool isAlive = true;
    public bool isConscious = true;


    /// <summary>
    /// Affects health and caps it to healthMax automatically for cases of healing.
    /// Positive values will heal, negative values will deal damage.
    /// </summary>
    /// <param name="changeAmount">Parameter value to pass.</param>
    /// <returns>Returns true if they're left unconscious after the change. 
    /// Returns false if still conscious</returns>
    /// <returns>Returns false if still conscious</returns>
    public bool ChangeHealth(float changeAmount)
    {
        health += changeAmount;
        health = Mathf.Min(health, healthMax);

        if (health <= 0)
            return true;


        return false;
    }

    public void Unconscious()
    {
        var nMA = GetComponent<NavMeshAgent>();
        if (nMA)
            nMA.enabled = false;

        var ragdoll = GetComponent<Ragdoll>();
        ragdoll.EnableRagdoll();


        isConscious = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (health < 0)
            Unconscious();
    }
}
