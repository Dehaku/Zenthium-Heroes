using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devdog.LosPro.Demo
{
    public class ObserverDetectTestScript : MonoBehaviour, IObserverCallbacks
    {
        void IObserverCallbacks.OnDetectedTarget(SightTargetInfo info)
        {
            Debug.Log("Found " + info.target.name + ":" + info.target.config.category + "!!!");
        }

        void IObserverCallbacks.OnDetectingTarget(SightTargetInfo info)
        {
            //Debug.Log("!");
        }

        void IObserverCallbacks.OnStopDetectingTarget(SightTargetInfo info)
        {
        }

        void IObserverCallbacks.OnTargetCameIntoRange(SightTargetInfo info)
        {
        }

        void IObserverCallbacks.OnTargetDestroyed(SightTargetInfo info)
        {
        }

        void IObserverCallbacks.OnTargetWentOutOfRange(SightTargetInfo info)
        {
        }

        void IObserverCallbacks.OnTryingToDetectTarget(SightTargetInfo info)
        {
            Debug.Log("I think I see " + info.target.name + ":"+ info.target.config.category+"!");
            
        }

        void IObserverCallbacks.OnUnDetectedTarget(SightTargetInfo info)
        {
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
