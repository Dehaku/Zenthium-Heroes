using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSyncer : MonoBehaviour
{

    public List<Animator> animators0;
    public AnimationClip clip0;
    public List<Animator> animators1;
    public AnimationClip clip1;
    public bool playOnStart = true;
    public bool playNow = false;

    // Start is called before the first frame update
    void Start()
    {
        if(playOnStart)
            Invoke("PlayAnimation", 1);
    }

    // Update is called once per frame
    void Update()
    {
        if(playNow)
        {
            playNow = false;
            PlayAnimation();
        }
    }

    [ContextMenu("PlayAll")]
    void PlayAnimation()
    {
        foreach (var item in animators0)
        {
            item.Play(clip0.name);
        }
        foreach (var item in animators1)
        {
            item.Play(clip1.name);
        }
    }
}
