using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindSoundScript : MonoBehaviour
{
    AudioSource audioSource;
    CharacterController cc;
    SimpleCameraShake cameraShake;

    [SerializeField]
    bool isPlaying = false;
    [SerializeField]
    float velocityToPlay = 5;
    [SerializeField]
    float maxVolume = 1;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        cc = GetComponentInParent<CharacterController>();
        cameraShake = GetComponent<SimpleCameraShake>();
    }

    // Update is called once per frame
    void Update()
    {
        if(cc.velocity.magnitude > velocityToPlay)
        {
            cameraShake.StartShake();

            isPlaying = true;

            if (!audioSource.isPlaying)
            {
                audioSource.time = Random.Range(0f, audioSource.clip.length);
                audioSource.volume = 0;
                audioSource.Play();
            }
        }
        else
        {
            cameraShake.StopShake();

            isPlaying = false;
        }
        
        
        if(isPlaying)
        {
            audioSource.volume = Mathf.Lerp(audioSource.volume, maxVolume, Time.deltaTime*2);
            
        }
        else
        {
            audioSource.volume = Mathf.Lerp(audioSource.volume, 0, Time.deltaTime*2);
            
            // Removing the pop and smoothly killing the sound.
            if(audioSource.volume < 0.1)
                audioSource.Stop();
            if (audioSource.volume < 0.2)
                audioSource.volume = Mathf.Lerp(audioSource.volume, 0, Time.deltaTime * 2);


        }
    }
}
