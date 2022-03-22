using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillainTarget : MonoBehaviour
{
    public bool _isValidTarget = true;

    public void SetValidTarget(bool val)
    {
        _isValidTarget = val;
    }

    public bool IsValidTarget()
    {
        return _isValidTarget;
    }
}
