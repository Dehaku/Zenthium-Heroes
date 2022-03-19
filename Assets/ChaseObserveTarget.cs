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
        AcquireTargets acquire;
        public float scanMinRange = 0f;
        public float scanMaxRange = 50f;
        public float scanFrequency = 1.5f;
        [Header("ScanRandom: No Less Than Freq")]
        public float scanRandomness = 0.25f;
        float _scanTimer = 1.5f;
        public Transform sightPos;

        private void Awake()
        {
            _chaseT = GetComponent<ChaseTarget>();
            
        }
        void Start()
        {
            squad = GetComponent<SquadRef>().squad;
            acquire = GetComponent<AcquireTargets>();
        }

        void OnDrawGizmosSelected()
        {
            // Draw a yellow sphere at the transform's position
            
            if(scanMinRange > 0)
            {
                Gizmos.color = new Color(0,0,1,1);
                if (sightPos)
                    Gizmos.DrawWireSphere(sightPos.position, scanMinRange);
                else
                    Gizmos.DrawWireSphere(transform.position, scanMinRange);
            }
            Gizmos.color = new Color(1, 0.92f, 0.016f,1); 
            if (sightPos)
                Gizmos.DrawWireSphere(sightPos.position, scanMaxRange);
            else
                Gizmos.DrawWireSphere(transform.position, scanMaxRange);
        }

        void Scan()
        {
            //var enemy = acquire.AcquireNearestVisibleEnemyWithinRange(SightPos.position, 0, 100);
            GameObject enemy;
            if(sightPos)
                enemy = acquire.AcquireNearestVisibleEnemyWithinRange(sightPos.position, scanMinRange, scanMaxRange);
            else
                enemy = acquire.AcquireNearestVisibleEnemyWithinRange(scanMinRange, scanMaxRange);

            if (enemy)
                Debug.Log("I can see: " + enemy.name);
            else
                Debug.Log("I don't see anyone.");
        }

        void Update()
        {
            _scanTimer -= Time.deltaTime;
            if(_scanTimer < 0)
            {
                _scanTimer = scanFrequency + Random.Range(0f, scanRandomness);
                Scan();
            }
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
            var target = info.target.gameObject.GetComponent<Creature>();
            if (!target.isConscious)
            {
                return;
            }

            // If Squad, and stay in formation, then squad hunt. Else chase by yourself.
            bool seekSolo = true;
            if(GetSquad())
            {
                seekSolo = false;
                if (GetSquad().breakFormation)
                    seekSolo = true;
                else
                    GetSquad().TargetFound(info.target.gameObject);
            }
            
            if(seekSolo)
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