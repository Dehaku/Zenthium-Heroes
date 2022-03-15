using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //SoundManager.Initialize();
        DOTween.SetTweensCapacity(1000,500);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
