using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectileRaycast : MonoBehaviour
{
    public GameObject muzzlePrefab;
    public GameObject hitPrefab;
    public float speed = 50;
    public float gravity = 1;
    public float knockbackForce = 50;

    Vector3 startPosition;
    Vector3 startForward;
    bool isInitialized = false;
    float startTime = -1;
    bool isPrediction = false;
    public Material predictionMaterial;
    public RaycastHit predictHit;


    public void Initialize(Transform startPoint, float speed, float gravity)
    {
        startPosition = startPoint.position;
        startForward = startPoint.forward.normalized;
        this.speed = speed;
        this.gravity = gravity;
        isInitialized = true;
    }

    public Vector3 FindPointOnParabola(float time)
    {
        Vector3 point = startPosition + (startForward * speed * time);
        Vector3 gravityVec = Vector3.down * gravity * time * time;
        return point + gravityVec;
    }

    void OnHit(RaycastHit hit)
    {
        if (hitPrefab != null)
        {

            var hitVFX = Instantiate(muzzlePrefab, hit.point, Quaternion.Euler(hit.normal));
            hitVFX.transform.forward = gameObject.transform.forward;
            var psHit = hitVFX.GetComponent<ParticleSystem>();
            if (psHit != null)
                Destroy(hitVFX, psHit.main.duration);
            else
            {
                var psChild = hitVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
                Destroy(hitVFX, psChild.main.duration);
            }
        }

        ShootableObject shootableObject = hit.transform.GetComponent<ShootableObject>();
        if (shootableObject)
        {
            shootableObject.OnHit(hit);
        }

        // var myRigid = GetComponent<Rigidbody>();
        // 
        // var hitTar = hit.collider.o  collision.gameObject;
        // 
        // Rigidbody[] rigidbodies;
        // rigidbodies = hitTar.GetComponentsInChildren<Rigidbody>();
        // foreach (var rigid in rigidbodies)
        // {
        //     rigid.velocity += transform.forward * (knockbackForce / rigid.mass);   //new Vector3(0, speed, 0);
        // }


        //StartCoroutine(WaitToDelete(0.02f));
        Destroy(gameObject);
    }

    public void SetLineActive(bool active)
    {
        if (!active)
            Destroy(_line);
    }

    LineRenderer _line;
    public bool PredictTrajectory(Transform startPoint, float speed, float gravity, float futureTime, float timeStep = 0.05f)
    {
        
        Initialize(startPoint, speed, gravity);
        isPrediction = true;
        

        float currentTime = 0;
        Vector3 currentPoint = FindPointOnParabola(currentTime);
        float timeTracker = currentTime;
        Vector3 previousPoint = currentPoint;


        if(!_line)
        {
            _line = gameObject.AddComponent<LineRenderer>(); //new LineRenderer();
            if (predictionMaterial != null)
                _line.material = predictionMaterial;
        }
            

        _line.startColor = Color.red;
        _line.endColor = Color.blue;
        
        _line.startWidth = 0.2f;
        _line.endWidth = 0.2f;
        _line.positionCount = (int) ((futureTime / timeStep) + 2);
        Debug.Log("Positions: " + _line.positionCount + ":" + ((futureTime / timeStep) + 1));

        int increm = 0;
        _line.SetPosition(increm, currentPoint);
        
        
        while (timeTracker < futureTime)
        {
            //Debug.Log(timeTracker);

            timeTracker += timeStep;

            previousPoint = currentPoint;
            currentPoint = FindPointOnParabola(timeTracker);


            
            increm++;
            _line.SetPosition(increm, currentPoint);

            if (Physics.Linecast(previousPoint, currentPoint, out predictHit))
            {
                //Hit
                _line.positionCount = increm+1;
                _line.SetPosition(increm, predictHit.point);
                return true;
            }


        }
        return false;
    }

    private void FixedUpdate()
    {
        if (isPrediction) return;
        if (!isInitialized) return;
        if (startTime < 0) startTime = Time.time;


        RaycastHit hit;
        float currentTime = Time.time - startTime;
        float previousTime = currentTime - Time.fixedDeltaTime;
        float nextTime = currentTime + Time.fixedDeltaTime;

        Vector3 currentPoint = FindPointOnParabola(currentTime);
        
        Vector3 nextPoint = FindPointOnParabola(nextTime);

        if (previousTime > 0)
        {
            Vector3 prevPoint = FindPointOnParabola(previousTime);
            if (Physics.Linecast(prevPoint, currentPoint, out hit))
            {
                //Hit
                OnHit(hit);
            }
        }

        if(Physics.Linecast(currentPoint, nextPoint, out hit))
        {
            //Hit
            OnHit(hit);
        }
    }

    private void Update()
    {
        if (isPrediction) return;
        if (!isInitialized) return;
        if (startTime < 0) return;

        float currentTime = Time.time - startTime;
        Vector3 currentPoint = FindPointOnParabola(currentTime);

        transform.position = currentPoint;

    }

    

    

    // Start is called before the first frame update
    void Start()
    {
        if (isPrediction) return;

        if (muzzlePrefab != null)
        {
            var muzzleVFX = Instantiate(muzzlePrefab, transform.position, Quaternion.identity);
            muzzleVFX.transform.forward = gameObject.transform.forward;
            var psMuzzle = muzzleVFX.GetComponent<ParticleSystem>();
            if (psMuzzle != null)
                Destroy(muzzleVFX, psMuzzle.main.duration);
            else
            {
                var psChild = muzzleVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
                Destroy(muzzleVFX, psChild.main.duration);
            }
        }
    }

    private IEnumerator WaitToDelete(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            Destroy(gameObject);
        }
    }


    
}
