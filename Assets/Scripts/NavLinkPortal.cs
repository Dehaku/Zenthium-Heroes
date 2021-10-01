using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavLinkPortal : MonoBehaviour
{
    public OffMeshLink linkPrefab;
    OffMeshLink link;
    public GameObject portalStartPrefab;
    GameObject portalStartObj;
    public GameObject portalEndPrefab;
    GameObject portalEndObj;
    
    public Vector3 portalStart;
    public Vector3 portalEnd;
    public float portalDuration = 5;
    float durationPassed = 0;
    bool portalActive = false;
    public float portalCostModifier = -1;

    // Start is called before the first frame update
    void Start()
    {

    }


    public void GeneratePortalLink()
    {
        // We've already got one.
        if (portalActive)
            return;

        NavMeshHit closestHitStart;
        NavMeshHit closestHitEnd;
        if (!NavMesh.SamplePosition(portalStart, out closestHitStart, 2, 1))
        {
            Debug.Log("Start Portal wasn't close enough to NavMesh.");
            return;
        }
            
        if (!NavMesh.SamplePosition(portalStart, out closestHitEnd, 2, 1))
        {
            Debug.Log("End Portal wasn't close enough to NavMesh.");
            return;
        }

        link = Instantiate(linkPrefab);
        link.startTransform.position = portalStart;
        link.endTransform.position = portalEnd;
        link.costOverride = portalCostModifier;
        link.area = 10;


        portalStartObj = Instantiate(portalStartPrefab, portalStart, Quaternion.identity);
        portalEndObj = Instantiate(portalEndPrefab, portalEnd, Quaternion.identity);
        //portalStartObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //portalEndObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        portalActive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(durationPassed > portalDuration)
        {
            Destroy(portalStartObj);
            Destroy(portalEndObj);
            Destroy(link.gameObject);
            portalActive = false;
            durationPassed = 0;
        }
        
        
        if(portalActive)
            durationPassed += Time.deltaTime;
    }
}
