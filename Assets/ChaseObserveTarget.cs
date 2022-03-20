using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    [RequireComponent(typeof(ChaseTarget))]
    public class ChaseObserveTarget : MonoBehaviour
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

        void MakeTarget(GameObject target)
        {
            if (!target)
            return;

            var targetCreature = target.GetComponent<Creature>();
            if (!targetCreature.isConscious)
            {
                return;
            }

            // If Squad, and stay in formation, then squad hunt. Else chase by yourself.
            bool seekSolo = true;
            if (GetSquad())
            {
                
                //Setting target so melee units in formation will attack if enemies approach.
                _chaseT.target = target;

            seekSolo = false;
            if (GetSquad().breakFormation)
                    seekSolo = true;
                else
                    GetSquad().TargetFound(target);
            }

            if (seekSolo)
            {
                _chaseT.enabled = true;
                _chaseT.target = target;
            }
        }

        void Scan()
        {
            GameObject enemy;
            if(sightPos)
                enemy = acquire.AcquireNearestVisibleEnemyWithinRange(sightPos.position, scanMinRange, scanMaxRange);
            else
                enemy = acquire.AcquireNearestVisibleEnemyWithinRange(scanMinRange, scanMaxRange);

            if (enemy)
            {
                MakeTarget(enemy);
            }
        }

        void Update()
        {
            _scanTimer -= Time.deltaTime;
            if(_scanTimer < 0)
            {
                _scanTimer = scanFrequency + UnityEngine.Random.Range(0f, scanRandomness);
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
    }
