using UnityEngine;
using UnityEngine.AI;

// Use physics raycast hit from mouse click to set agent destination
[RequireComponent(typeof(NavMeshAgent))]
public class ClickToMovePortalPlus : MonoBehaviour
{
    NavMeshAgent m_Agent;
    RaycastHit m_HitInfo = new RaycastHit();
    public NavLinkPortal portal;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
    }

    private int IndexFromMask(int mask)
    {
        for (int i = 0; i < 32; ++i)
        {
            if ((1 << i) == mask)
                return i;
        }
        return -1;
    }

    void SetSpeedBasedOnArea()
    {
        NavMeshHit navMeshHit;
        m_Agent.SamplePathPosition(NavMesh.AllAreas, 0f, out navMeshHit);
        if (IndexFromMask(navMeshHit.mask) == 10)
        {
            m_Agent.speed = 100;
        }
        else
            m_Agent.speed = 9;

    }

    void Update()
    {
        SetSpeedBasedOnArea();
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
                m_Agent.destination = m_HitInfo.point;
            Debug.Log("Normal move.");
        }
        else if(Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("Portal move!");
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
                m_Agent.destination = m_HitInfo.point;
            else
                Debug.Log("???");

            NavMeshPath path = new NavMeshPath();
            m_Agent.CalculatePath(m_HitInfo.point, path);
            if(path.status == NavMeshPathStatus.PathPartial)
            {
                Debug.Log("No path detected, portal time.");
                portal.portalStart = transform.position;
                portal.portalEnd = m_HitInfo.point;
                portal.GeneratePortalLink();
            }
        }
    }
}
