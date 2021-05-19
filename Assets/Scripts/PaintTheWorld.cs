using Eldemarkki.VoxelTerrain.Meshing.MarchingCubes;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.Utilities.Intersection;
using Eldemarkki.VoxelTerrain.World;
using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public class PaintTheWorld : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private VoxelWorld voxelWorld;

    [Header("Settings")]
    public bool paintTerrainOverTime;
    [SerializeField] private float paintRange = 3f;
    public int2 snowRange;
    public int2 grassRange;
    public int2 dirtRange;
    public int2 stoneRange;
    public bool generateGold;
    [Range(0, 1000)] public float generateGoldChance;
    public bool generateZenny;
    [Range(0,1000)]public float generateZennyChance;

    [Header("Material Painting")]
    [SerializeField] private Color32 snowColor;
    [SerializeField] private Color32 grassColor;
    [SerializeField] private Color32 dirtColor;
    [SerializeField] private Color32 stoneColor;
    [SerializeField] private Color32 goldColor;
    [SerializeField] private Color32 zennyColor;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    float paintStartTime;
    bool _paintOverTime = false;
    ChunkTag[] chunks;
    int chunkIterator = 0;

    public void PaintWorld()
    {
        if (paintTerrainOverTime)
        {
            if (_paintOverTime == false)
            {
                paintStartTime = Time.realtimeSinceStartup;
                _paintOverTime = true;
                chunks = FindObjectsOfType<ChunkTag>();
            }
        }
        else
        {
            ChunkTag[] chunks = FindObjectsOfType<ChunkTag>();
            foreach (var chunk in chunks)
            {

                PaintLevelTerrain(chunk.gameObject.transform.position);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(2))
            CountGoldHit();

        if (_paintOverTime && chunkIterator >= chunks.Length)
        {
            Debug.Log("Long Paint:" + ((Time.realtimeSinceStartup - paintStartTime) * 1000f) + "ms");
            _paintOverTime = false;
            chunks = null;
            chunkIterator = 0;
        }
        else if(_paintOverTime)
        {
            PaintLevelTerrain(chunks[chunkIterator].gameObject.transform.position);
            chunkIterator++;
        }

        

        if (Input.GetKeyDown(KeyCode.Home))
        {
            float startTime = Time.realtimeSinceStartup;

            PaintWorld();

            Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
        }
    }

    public int CountGoldHit()
    {
        int goldFound = 0;
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit)) { return 0; }
        Vector3 point = hit.point;

        int hitX = Mathf.RoundToInt(point.x);
        int hitY = Mathf.RoundToInt(point.y);
        int hitZ = Mathf.RoundToInt(point.z);
        int3 hitPoint = new int3(hitX, hitY, hitZ);
        int3 intRange = new int3(Mathf.CeilToInt(paintRange));

        BoundsInt queryBounds = new BoundsInt((hitPoint - intRange).ToVectorInt(), (intRange * 2).ToVectorInt());



        goldFound = 0;
        voxelWorld.VoxelColorStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
        {
            float distance = math.distance(voxelDataWorldPosition, point);

            if (distance <= paintRange)
            {
                if (voxelData.r == goldColor.r &&
                voxelData.g == goldColor.g &&
                voxelData.b == goldColor.b &&
                voxelData.a == goldColor.a)
                {
                    goldFound++;
                }

                return voxelData;
            }

            return voxelData;
        });
        Debug.Log("Gold Found: " + goldFound);
        return goldFound;
    }

    
    private void PaintLevelTerrain(Vector3 coords)
    {
        Vector3 point = coords;
        point.x += 8;
        point.y += 8;
        point.z += 8;
        float chunkPaintRange = 8;


        int hitX = Mathf.RoundToInt(point.x);
        int hitY = Mathf.RoundToInt(point.y);
        int hitZ = Mathf.RoundToInt(point.z);
        int3 hitPoint = new int3(hitX, hitY, hitZ);
        int3 intRange = new int3(Mathf.CeilToInt(chunkPaintRange));

        BoundsInt queryBounds = new BoundsInt((hitPoint - intRange).ToVectorInt(), (intRange * 2).ToVectorInt());

        voxelWorld.VoxelColorStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
        {
            float distance = math.distance(voxelDataWorldPosition, point);

                Color32 paintColor = new Color32(0,0,0,0);

                if (Mathf.Clamp(voxelDataWorldPosition.y, snowRange.x, snowRange.y) == voxelDataWorldPosition.y)
                    paintColor = snowColor;
                else if(Mathf.Clamp(voxelDataWorldPosition.y,grassRange.x,grassRange.y) == voxelDataWorldPosition.y)
                    paintColor = grassColor;
                else if (Mathf.Clamp(voxelDataWorldPosition.y, dirtRange.x, dirtRange.y) == voxelDataWorldPosition.y)
                    paintColor = dirtColor;
                else if (Mathf.Clamp(voxelDataWorldPosition.y, stoneRange.x, stoneRange.y) == voxelDataWorldPosition.y)
                    paintColor = stoneColor;
                if (generateGold)
                    if (UnityEngine.Random.Range(0, 1000) < generateGoldChance)
                        paintColor = goldColor;
                if (generateZenny)
                    if (UnityEngine.Random.Range(0, 1000) < generateZennyChance)
                        paintColor = zennyColor;

                return paintColor;

        });
    }

    private void PaintColor()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit)) { return; }
        Vector3 point = hit.point;

        int hitX = Mathf.RoundToInt(point.x);
        int hitY = Mathf.RoundToInt(point.y);
        int hitZ = Mathf.RoundToInt(point.z);
        int3 hitPoint = new int3(hitX, hitY, hitZ);
        int3 intRange = new int3(Mathf.CeilToInt(paintRange));

        BoundsInt queryBounds = new BoundsInt((hitPoint - intRange).ToVectorInt(), (intRange * 2).ToVectorInt());

        voxelWorld.VoxelColorStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
        {
            float distance = math.distance(voxelDataWorldPosition, point);

            if (distance <= paintRange)
            {
                return dirtColor;
            }

            return voxelData;
        });
    }
}
