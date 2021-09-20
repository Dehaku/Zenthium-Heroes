using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ScaleOverTime : MonoBehaviour
{
    [SerializeField] Vector3 EndScale;
    [SerializeField] float duration;
    [SerializeField] float initialDelay = 0;


    IEnumerator Effect()
    {
        yield return new WaitForSeconds(initialDelay);

        transform.DOScale(EndScale, duration).SetEase(Ease.OutQuad);

    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("Effect");
        // transform.DOScale(EndScale, duration).SetEase(Ease.OutQuad);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
