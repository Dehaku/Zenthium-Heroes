using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DaemonBrain : MonoBehaviour
{
    public List<Daemon> daemons;
    public List<Daemon> scouts;
    public List<Daemon> grunts;
    public List<Daemon> captains;
    public List<Daemon> overlords;
    public List<Daemon> cubis;

    public List<Survivor> survivors;
    public List<Camp> camps;

    public float campDestroyDistance = 1;

    public float campCheckTime = 1;
    float _campCheckTimer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _campCheckTimer -= Time.deltaTime;
        if(_campCheckTimer < 0)
        {
            _campCheckTimer = campCheckTime;

            foreach (var daemon in daemons)
            {
                foreach (var camp in camps)
                {
                    DaemonVersusCampLogic(daemon, camp);
                }

                DaemonLogicByType(daemon);

            }
        }
    }

    void DaemonLogicByType(Daemon daemon)
    {
        if(daemon.type == Daemon.DemonType.Grunt)
        {
            if(!daemon.target)
            {
                Daemon nearestCaptain = FindNearestDemonCaptain(daemon.transform);
                if (nearestCaptain)
                {
                    daemon.target = nearestCaptain.transform;
                    daemon.GoTo(nearestCaptain.transform.position);
                }
                    
            }
            
        }
        else if(daemon.type == Daemon.DemonType.Captain)
        {
            // Sometimes go report to an Overlord
            if(Random.Range(0,100) == 1)
            {
                if (!daemon.target)
                {
                    Daemon randomOverlord = overlords[Random.Range(0, overlords.Count - 1)];
                    if (randomOverlord)
                    {
                        daemon.target = randomOverlord.transform;
                        daemon.GoTo(randomOverlord.transform.position);
                        daemon.allowedToWander = false;
                    }
                }
            }

            if(daemon.target)
            {
                float overlordReportDistance = 2;
                if(Vector3.Distance(daemon.transform.position,daemon.target.position) <= overlordReportDistance)
                {
                    daemon.target = null;
                    daemon.allowedToWander = true;
                }
            }
            
        }


    }

    Daemon FindNearestDemonOfType(Transform me, Daemon.DemonType daemonType)
    {
        Daemon nearest = null;
        float Distance = float.MaxValue;
        foreach (var mon in daemons)
        {
            if(mon.type == Daemon.DemonType.Captain)
            {
                float distanceCheck = Vector3.Distance(me.position, mon.transform.position);
                if (nearest == null)
                {
                    nearest = mon;
                    Distance = distanceCheck;
                }
                    
                else
                {
                    if(distanceCheck < Distance)
                    {
                        nearest = mon;
                        Distance = distanceCheck;
                    }
                }
            }
        }
        
        return nearest;
    }

    Daemon FindNearestDemonCaptain(Transform me)
    {
        Daemon nearest = null;
        float Distance = float.MaxValue;
        foreach (var mon in captains)
        {
                float distanceCheck = Vector3.Distance(me.position, mon.transform.position);
                if (nearest == null)
                {
                    nearest = mon;
                    Distance = distanceCheck;
                }

                else
                {
                    if (distanceCheck < Distance)
                    {
                        nearest = mon;
                        Distance = distanceCheck;
                    }
                }
        }
        return nearest;
    }

    void DaemonVersusCampLogic(Daemon daemon, Camp camp)
    {
        bool closeEnough = IsNearby(daemon.transform, camp.transform, campDestroyDistance);

        if (closeEnough)
        {
            Destroy(camp.gameObject);
        }
            
    }


    bool IsNearby(Transform me, Transform them, float distance)
    {
        if (Vector3.Distance(me.position, them.position) <= distance)
            return true;
        
        return false;
    }



    static public void AddDaemon(Daemon mon)
    {
        _instance.daemons.Add(mon);
        if (mon.type == Daemon.DemonType.Scout)
            _instance.scouts.Add(mon);
        if (mon.type == Daemon.DemonType.Grunt)
            _instance.grunts.Add(mon);
        if (mon.type == Daemon.DemonType.Captain)
            _instance.captains.Add(mon);
        if (mon.type == Daemon.DemonType.Overlord)
            _instance.overlords.Add(mon);
        if (mon.type == Daemon.DemonType.Cubi)
            _instance.cubis.Add(mon);
    }

    static public void RemoveDaemon(Daemon mon)
    {
        _instance.daemons.Remove(mon);
        if (mon.type == Daemon.DemonType.Scout)
            _instance.scouts.Remove(mon);
        if (mon.type == Daemon.DemonType.Grunt)
            _instance.grunts.Remove(mon);
        if (mon.type == Daemon.DemonType.Captain)
            _instance.captains.Remove(mon);
        if (mon.type == Daemon.DemonType.Overlord)
            _instance.overlords.Remove(mon);
        if (mon.type == Daemon.DemonType.Cubi)
            _instance.cubis.Remove(mon);
    }

    static public void AddSurvivor(Survivor surv)
    {
        _instance.survivors.Add(surv);
    }

    static public void RemoveSurvivor(Survivor surv)
    {
        _instance.survivors.Remove(surv);
    }

    static public void AddCamp(Camp camp)
    {
        _instance.camps.Add(camp);
    }

    static public void RemoveCamp(Camp camp)
    {
        _instance.camps.Remove(camp);
    }

    #region Singleton

    private static DaemonBrain _instance;

    public static DaemonBrain Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    #endregion

}
