using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Devdog.LosPro.Demo
{
    [RequireComponent(typeof(ChaseTarget))]
    public class ChaseObserveTarget : MonoBehaviour, IObserverCallbacks
    {
        ChaseTarget _chaseT;
        SquadScript squad;
        private void Awake()
        {
            _chaseT = GetComponent<ChaseTarget>();
            
        }
        void Start()
        {
            squad = GetComponent<SquadRef>().squad;
        }

        SquadScript GetSquad()
        {
            if (squad)
                return squad;

            squad = GetComponent<SquadRef>().squad;
            return squad;
        }

        public void OnDetectedTarget(SightTargetInfo info)
        {
            if(GetSquad())
            {
                GetSquad().TargetFound(info.target.gameObject);
            }
            else
            {
                _chaseT.enabled = true;
                _chaseT.target = info.target.gameObject;
            }
        }

        public void OnDetectingTarget(SightTargetInfo info)
        {
        }

        public void OnStopDetectingTarget(SightTargetInfo info)
        {
        }

        public void OnTargetCameIntoRange(SightTargetInfo info)
        {
        }

        public void OnTargetDestroyed(SightTargetInfo info)
        {
        }

        public void OnTargetWentOutOfRange(SightTargetInfo info)
        {
        }

        public void OnTryingToDetectTarget(SightTargetInfo info)
        {
        }

        public void OnUnDetectedTarget(SightTargetInfo info)
        {
        }

    }
}