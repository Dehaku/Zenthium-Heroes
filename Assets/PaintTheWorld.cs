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
    bool method;
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private VoxelWorld voxelWorld;

    [Header("Settings")]
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

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(2))
            CountGoldHit();
        else if (Input.GetMouseButton(2))
            PaintLevelTerrain();
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

    private void PaintLevelTerrain()
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
            }

            return voxelData;
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
