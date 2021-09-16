using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostContainer
{
    public CopyDemBones Ghost;
    public float Life = 2;
    public float Fade = 1;

    public GhostContainer(CopyDemBones pGhost, float pLife)
    {
        Ghost = pGhost;
        Life = pLife;
    }
}

public class GhostGenerator : MonoBehaviour
{
    public Transform[] SourceBones;
    public CopyDemBones GhostPrefab;
    public float GhostDistance = 10;
    public float GhostLife = 10;
    public float GhostFadeInTime = 1;
    public float GhostFadeOutTime = 1;
    public float GhostDistortion = .1f;
    public float GhostDistortionOutTime = 1;
    public float GhostScaleOut = .25f;
    

    public bool Generate = false;
    Vector3 LastPosition;
    float DistanceTraveled = 0;
    List<GhostContainer> Ghosts;
    Vector3 InitialScale;

    Queue<CopyDemBones> GhostPool;

    public bool GetGenerate()
    {
        return Generate;
    }

    public void SetGenerate(bool value)
    {
        Generate = value;
    }
    

    // Start is called before the first frame update
    void Start()
    {
        Ghosts = new List<GhostContainer>();
        GhostPool = new Queue<CopyDemBones>();
        LastPosition = transform.position;
    }

    void SetGhostMaterialVar(string pVar, CopyDemBones pGhost, float pValue)
    {
        SkinnedMeshRenderer[] meshes = pGhost.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(SkinnedMeshRenderer mesh in meshes)
        {
            foreach (Material mat in mesh.materials)
            {
                mat.SetFloat(pVar, pValue);
            }
        }
    }

    


    // Update is called once per frame
    void FixedUpdate()
    {
        DistanceTraveled += Vector3.Distance(transform.position, LastPosition);
        LastPosition = transform.position;

        if(Generate && DistanceTraveled >= GhostDistance)
        {
            DistanceTraveled = 0;
            GenerateGhost();
        }

        List<GhostContainer> removeGhosts = new List<GhostContainer>();

        foreach(GhostContainer ghost in Ghosts)
        {
            ghost.Life -= Time.deltaTime;
            if (ghost.Life <= 0)
            {
                removeGhosts.Add(ghost);
            }

            if (ghost.Life < GhostDistortionOutTime)
            {
                float prop = Mathf.Clamp(1 - (ghost.Life / GhostDistortionOutTime), 0f, 1f);
                SetGhostMaterialVar("VertexDistortionStrength", ghost.Ghost, prop * GhostDistortion);

                if(InitialScale == Vector3.zero)
                {
                    InitialScale = ghost.Ghost.transform.localScale;
                }

                ghost.Ghost.transform.localScale = InitialScale * (1 + (GhostScaleOut * prop));
            }

            if(ghost.Life < GhostFadeOutTime)
            {
                float fade = Mathf.Clamp(1 - (ghost.Life / GhostFadeOutTime), 0f, 1f);
                //SetGhostMaterialVar("Fade", ghost.Ghost, fade);
            }
            else
            {
                float fade = Mathf.Clamp(1 - ((GhostLife - ghost.Life) / GhostFadeInTime), 0f, 1f);
                //SetGhostMaterialVar("Fade", ghost.Ghost, fade);
            }
        }

        foreach(GhostContainer ghost in removeGhosts)
        {
            Ghosts.Remove(ghost);
            ghost.Ghost.gameObject.SetActive(false);
            GhostPool.Enqueue(ghost.Ghost);
        }
    }

    void GenerateGhost()
    {
        CopyDemBones ghost;
        if (GhostPool.Count > 0)
        {
            ghost = GhostPool.Dequeue();
            //SetGhostMaterialVar("VertexDistortionStrength", ghost, 0);
            ghost.transform.localScale = InitialScale;
            ghost.gameObject.SetActive(true);
        }
        else
        {
            ghost = Instantiate(GhostPrefab, transform.parent);
        }

        Ghosts.Add(new GhostContainer(ghost, GhostLife));
        ghost.transform.localPosition = transform.localPosition;
        ghost.transform.localRotation = transform.localRotation;

        ghost.Source = SourceBones;
        ghost.Initialize();
        ghost.CopyBones();
    }


}
