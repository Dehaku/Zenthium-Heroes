using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSyncer : MonoBehaviour
{

    public List<Animator> animators;
    public AnimationClip clip;
    public bool playOnStart = true;

    // Start is called before the first frame update
    void Start()
    {
        if(playOnStart)
            Invoke("PlayAnimation", 1);
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("PlayAll")]
    void PlayAnimation()
    {
        foreach (var item in animators)
        {
            item.Play(clip.name);
        }
    }
}
