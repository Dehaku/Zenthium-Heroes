using Hellmade.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactSoundScript : MonoBehaviour
{
    public AudioClip impactSound;
    // Start is called before the first frame update
    void Start()
    {
        if (!impactSound)
            Debug.LogWarning("No impact sound set on " + transform.name);
    }

    public void PlayImpactSound()
    {
        int audioID = EazySoundManager.PlaySound(impactSound, 1, false, transform);
    }

}
