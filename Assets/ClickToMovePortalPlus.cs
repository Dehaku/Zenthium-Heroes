using UnityEngine;
using UnityEngine.AI;

// Use physics raycast hit from mouse click to set agent destination
[RequireComponent(typeof(NavMeshAgent))]
public class ClickToMovePortalPlus : MonoBehaviour
{
    NavMeshAgent m_Agent;
    RaycastHit m_HitInfo = new RaycastHit();
    public NavLinkPortal portal;
    public OffMeshLink linkPrefab;
    OffMeshLink link;

    public bool CanClimb = true;

    Vector3 desiredDestination;

    [SerializeField] float DistanceToPartialEnd = 0.25f;
    [SerializeField] float maxSampleDistance = 5;
    [Tooltip("This is mostly to prevent link spam.")]
    [SerializeField] float delayExtraLinks = 0.1f;
    float delayTimer = 0;

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
            //m_Agent.speed = 100;
        }
        else
        {
            //m_Agent.speed = 9;
        }


    }

    bool AttemptClimbLink()
    {
        //var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //Ray ray;
        //ray.
        //if(Physics.Raycast(Ray, hitInfo, maxDistance, layerMask))
        //if (Physics.Raycast(m_Agent.transform.position + new Vector3(0,m_Agent.height*2,0), new Vector3(-1,0,0), out m_HitInfo))
        //m_Agent.destination = m_HitInfo.point;
        NavMeshHit startHit;
        NavMeshHit endHit;
        bool navStartHit = false;
        bool navEndHit = false;

        //NavMesh.
        //if (navHit = NavMesh.SamplePosition(m_Agent.transform.position + new Vector3(0, m_Agent.height * 2, 0),
        //    out hit, maxSampleDistance, 1 << NavMesh.GetAreaFromName("Climbable")))

        Vector3 desireNorm = (desiredDestination - m_Agent.transform.position).normalized;
        desireNorm = desireNorm * 1.5f;

        Debug.DrawLine(m_Agent.transform.position, m_Agent.transform.position + desireNorm);

        var startPos = new Vector3(m_Agent.transform.position.x, m_Agent.transform.position.y - m_Agent.height / 2, m_Agent.transform.position.z);

        navStartHit = NavMesh.FindClosestEdge(startPos,
            out startHit, NavMesh.AllAreas);

        navEndHit = NavMesh.FindClosestEdge(m_Agent.transform.position + desireNorm,
            out endHit, NavMesh.AllAreas);

        if (navStartHit && navEndHit)
        {
            Debug.Log("Start Mesh: " + startHit.mask + ", End Mesh: " + endHit.mask);

            //if (IndexFromMask(endHit.mask) == NavMesh.GetAreaFromName("Climbable"))
            if(delayTimer < 0)
            {
                delayTimer = delayExtraLinks;
                link = Instantiate(linkPrefab);
                
                link.startTransform.position =  startHit.position;
                link.endTransform.position = endHit.position;
                //Debug.Log("Making Link: " + startHit.position
                //    + " to " + endHit.position
                //    + ", Dist: " + endHit.distance
                //    + ", Norm: " + endHit.normal
                //    );
                
                //link.costOverride = portalCostModifier;
                link.area = 6;
            }
            
        }
        //Debug.Log("NavHit:" + navHit + ", hitPos: " + hit.position 
        //    + ", NavArea: " + NavMesh.GetAreaFromName("Walkable")
        //    + ", Mask: " + Mathf.RoundToInt(Mathf.Log(NavMesh.GetAreaFromName("Walkable"), 2))
        //    );
            

        

        //NavMesh.Raycast(sourcePosition,targetPosition,NavMeshHit,areaMask)
        //NavMesh.


        return false;
    }

    void MakeClimbNodeCheck()
    {
        if (!m_Agent.hasPath)
            return;
        // Check if our path needs linking.
        if (!(m_Agent.pathStatus == NavMeshPathStatus.PathPartial))
            return;

        //Debug.Log("Pos:" + m_Agent.transform.position + ", nextPos: " + m_Agent.nextPosition
        //    + ", End: " + m_Agent.pathEndPosition
        //    + ", Dest: " + desiredDestination
        //    );

        if (Vector3.Distance(m_Agent.transform.position,m_Agent.pathEndPosition) < DistanceToPartialEnd)
        {
            Debug.Log("Need Link Here");
            AttemptClimbLink();
            
        }
        //m_Agent.isOnNavMesh
        
        
        
    }


    void Update()
    {
        delayTimer -= Time.deltaTime;
        if (CanClimb)
            MakeClimbNodeCheck();

        SetSpeedBasedOnArea();
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
                m_Agent.destination = m_HitInfo.point;
            desiredDestination = m_HitInfo.point;
        }
        else if(Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
                m_Agent.destination = m_HitInfo.point;
            desiredDestination = m_HitInfo.point;

            NavMeshPath path = new NavMeshPath();
            m_Agent.CalculatePath(m_HitInfo.point, path);
            if(path.status == NavMeshPathStatus.PathPartial)
            {
                portal.portalStart = transform.position;
                portal.portalEnd = m_HitInfo.point;
                portal.GeneratePortalLink();
            }
        }
        else if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
                m_Agent.destination = m_HitInfo.point;
            desiredDestination = m_HitInfo.point;

            NavMeshPath path = new NavMeshPath();
            m_Agent.CalculatePath(m_HitInfo.point, path);
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                //portal.portalStart = transform.position;
                //portal.portalEnd = m_HitInfo.point;
                //portal.GeneratePortalLink();
            }
        }
    }
}
