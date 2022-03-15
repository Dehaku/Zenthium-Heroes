using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Events;

public class Creature : MonoBehaviour
{
    [SerializeField] private UnityEvent OnUnconscious = new UnityEvent();

    public float healthMax = 100;
    public float health = 100;
    public bool isAlive = true;
    public bool isConscious = true;
    DamageResists _damageResists;


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

    public float ApplyDamageResists(float healthChange, DamageInfo dI)
    {
        if (_damageResists)
        {
            var resists = _damageResists.GetResistencesOfDamageType(dI.damageType);
            resists = resists.OrderByDescending(w => w.percentage).ToList();
            if (resists.Count > 0)
            {
                foreach (var dR in resists)
                {
                    

                    if (dR.percentage)
                        healthChange *= 1 - ((dR.resistAmount) * 0.01f);
                    else
                        healthChange -= dR.resistAmount;
                }
            }
        }
        // We don't want our damage healing the target, or healing damaging the target.
        return Mathf.Max(healthChange,0);
    }

    DamagePopup damagePopup;

    public bool ChangeHealth(DamageInfo dI, float damageMultiplier = 1)
    {
        var change = dI.damage * damageMultiplier;

        // Reduce incoming amount by applicable damage resists
        change = ApplyDamageResists(change, dI);

        // If it's a type that deals damage, negate it so it does harm.
        if (dI.damageType >= 0)
            change = -change;

        health += change;
        health = Mathf.Min(health, healthMax);
        bool isCritical;
        if (Random.Range(0, 100) < 30)
            isCritical = true;
        else
            isCritical = false;

        HandleDamagePopup(transform.position, change, dI.damageType, isCritical);
        

        if (health <= 0)
            return true;

        return false;
    }

    private void HandleDamagePopup(Vector3 position, float change, int damageType, bool isCritical)
    {
        // If we're not currently tracking a popup, make a new one.
        if(!damagePopup)
        {
            damagePopup = DamagePopup.Create(transform.position, change, damageType, isCritical);
            return;
        }
        // If tracked damagetype isn't the same as the newest source of damage's type, make and track a new one
        if(damageType != damagePopup.damageType)
        {
            damagePopup = DamagePopup.Create(transform.position, change, damageType, isCritical);
            return;
        }
        else // If same damagetype, add the new damage to the old one on display
        {
            damagePopup.AddDamage(change);
        }
            


    }

    public void Unconscious()
    {
        var nMA = GetComponent<NavMeshAgent>();
        if (nMA)
            nMA.enabled = false;

        var ragdoll = GetComponent<Ragdoll>();
        if(ragdoll)
            ragdoll.EnableRagdoll();


        isConscious = false;

        OnUnconscious?.Invoke();

    }

    // Start is called before the first frame update
    void Start()
    {
        if (!_damageResists)
            _damageResists = GetComponent<DamageResists>();

    }

    // Update is called once per frame
    void Update()
    {
        if (health < 0)
            Unconscious();
    }
}
