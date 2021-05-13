using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class NavMesherScript : MonoBehaviour
{

    // The center of the build
    public Transform m_Tracked;

    // The size of the build bounds
    public Vector3 m_Size = new Vector3(30.0f, 10.0f, 30.0f);

    NavMeshData m_NavMesh;
    AsyncOperation m_Operation;
    NavMeshDataInstance m_Instance;
    List<NavMeshBuildSource> m_Sources = new List<NavMeshBuildSource>();

    public NavMeshSurface surface1;
    public NavMeshSurface surface2;
    public NavMeshSurface surface3;
    public NavMeshSurface surface4;
    public NavMeshSurface surface5;
    public NavMeshSurface surface6;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            

            NavMeshSourceTag.Collect(ref m_Sources);
            var defaultBuildSettings = NavMesh.GetSettingsByID(0);
            var bounds = QuantizedBounds();

            // NavMesh.AddOffMeshLinks();

            //UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            //NavMeshBuilder.BuildNavMeshData(navData,);
            //NavMeshBuilder.
            //NavMeshBuilder.UpdateNavMeshDataAsync(navData,);
            //Async


            //UnityEditor.AI.NavMeshBuilder.BuildNavMeshAsync();
            // UnityEngine.AI.NavMeshBuilder.

            //NavMeshBuilder.



            //StartCoroutine(ExecuteAfterTime(0.1f));
            //surface1.StartCoroutine("BuildNavMesh");
            //surface1.BuildNavMesh();



            
            surface2.BuildNavMesh();
            surface3.BuildNavMesh();
            surface4.BuildNavMesh();
            surface5.BuildNavMesh();
            surface6.BuildNavMesh();
            //surface6.UpdateNavMesh(navData);





        }
        if (Input.GetKeyDown(KeyCode.Y))
        {

            Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) { return; }

            NavMeshData navData = new NavMeshData();
            //navData.position = new Vector3(0, 0, 0);
            navData.position = hit.point;
            navData.rotation = Quaternion.identity;
            // navData.sourceBounds


            surface1.UpdateNavMesh(navData);
            Debug.Log("Fixin?");

            //NavMeshBuilder.UpdateNavMeshData();
            //UpdateNavMeshData();
            // UnityEngine.AI.NavMeshBuilder.UpdateNavMeshData(m_NavMesh, defaultBuildSettings, m_Sources, bounds);
        }
    }

    static Vector3 Quantize(Vector3 v, Vector3 quant)
    {
        float x = quant.x * Mathf.Floor(v.x / quant.x);
        float y = quant.y * Mathf.Floor(v.y / quant.y);
        float z = quant.z * Mathf.Floor(v.z / quant.z);
        return new Vector3(x, y, z);
    }

    Bounds QuantizedBounds()
    {
        // Quantize the bounds to update only when theres a 10% change in size
        var center = m_Tracked ? m_Tracked.position : transform.position;
        return new Bounds(Quantize(center, 0.1f * m_Size), m_Size);
    }
}
