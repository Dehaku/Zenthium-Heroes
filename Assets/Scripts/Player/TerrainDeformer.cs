using Eldemarkki.VoxelTerrain.Meshing.MarchingCubes;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.Utilities.Intersection;
using Eldemarkki.VoxelTerrain.World;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Eldemarkki.VoxelTerrain.Player
{
    /// <summary>
    /// The terrain deformer which modifies the terrain
    /// </summary>
    public class TerrainDeformer : MonoBehaviour
    {


        /// <summary>
        /// The voxel data store that will be deformed
        /// </summary>
        [Header("Terrain Deforming Settings")]
        [SerializeField] private VoxelWorld voxelWorld;

        /// <summary>
        /// Does the left mouse button add or remove terrain
        /// </summary>
        [SerializeField] private bool leftClickAddsTerrain = true;

        /// <summary>
        /// How fast the terrain is deformed
        /// </summary>
        [SerializeField] private float deformSpeed = 0.1f;

        /// <summary>
        /// How far the deformation can reach
        /// </summary>
        [SerializeField] private float deformRange = 3f;

        [SerializeField] private int debugNumber = 255;

        /// <summary>
        /// Whether to limit the terrain modification or not.
        /// </summary>
        [SerializeField] public bool applyLevelLimit;

        /// <summary>
        /// Minimum terrain modification level
        /// </summary>
        [SerializeField] public float lowestLevel;

        /// <summary>
        /// Maximum terrain modification level
        /// </summary>
        [SerializeField] public float HighestLevel;

        /// <summary>
        /// How far away points the player can deform
        /// </summary>
        [SerializeField] private float maxReachDistance = Mathf.Infinity;

        /// <summary>
        /// Which key must be held down to flatten the terrain
        /// </summary>
        [Header("Flattening")]
        [SerializeField] private KeyCode flatteningKey = KeyCode.LeftControl;

        /// <summary>
        /// The color that the terrain will be painted with
        /// </summary>
        [Header("Material Painting")]
        [SerializeField] private Color32 paintColor;

        /// <summary>
        /// The game object that the deformation raycast will be cast from
        /// </summary>
        [Header("Player Settings")]
        [SerializeField] private Transform playerCamera;

        /// <summary>
        /// Is the terrain currently being flattened
        /// </summary>
        private bool _isFlattening;

        /// <summary>
        /// The point where the flattening started
        /// </summary>
        private float3 _flatteningOrigin;

        /// <summary>
        /// The normal of the flattening plane
        /// </summary>
        private float3 _flatteningNormal;
        

        private void Awake()
        {

        }

        private void Update()
        {
            if (Input.GetKey(flatteningKey))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3 startP = playerCamera.position;
                    Vector3 destP = startP + playerCamera.forward;
                    Vector3 direction = destP - startP;

                    Ray ray = new Ray(startP, direction);

                    if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
                    _isFlattening = true;

                    _flatteningOrigin = hit.point;
                    _flatteningNormal = hit.normal;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _isFlattening = false;
                }
            }

            if (Input.GetKeyUp(flatteningKey))
            {
                _isFlattening = false;
            }

            if (Input.GetMouseButton(0))
            {
                if (_isFlattening)
                {
                    FlattenTerrain();
                }
                else
                {
                    RaycastToTerrain(leftClickAddsTerrain);
                }
            }
            else if (Input.GetMouseButton(1))
            {
                RaycastToTerrain(!leftClickAddsTerrain);
            }
            else if (Input.GetMouseButton(2))
            {
                PaintColor();
            }

            if(Input.GetKeyDown(KeyCode.X))
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);

                if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
                Vector3 hitPoint = hit.point;

                // EditTerrainCube(hitPoint - new Vector3(0, 0, -5), !leftClickAddsTerrain, deformSpeed, deformRange);
                // EditTerrainCube(hitPoint - new Vector3(0, 0, 5), !leftClickAddsTerrain, deformSpeed, deformRange);
                // 
                // EditTerrain(hitPoint - new Vector3(-5, 0, 0), leftClickAddsTerrain, deformSpeed, deformRange);
                // EditTerrain(hitPoint - new Vector3(5, 0, 0), leftClickAddsTerrain, deformSpeed, deformRange);

                for(int i = 0; i != 10; i++)
                {
                    EditTerrainCube(hitPoint - new Vector3(0, 0, (i * -5)), !leftClickAddsTerrain, deformSpeed, deformRange);
                    EditTerrainCube(hitPoint - new Vector3(0, 0, (i * 3)), !leftClickAddsTerrain, deformSpeed, deformRange);
                }

                for (int i = 0; i != 10; i++)
                {
                    EditTerrain(hitPoint - new Vector3((i * -5), 0, 0), leftClickAddsTerrain, deformSpeed, deformRange);
                    EditTerrain(hitPoint - new Vector3((i * 3), 0, 0), leftClickAddsTerrain, deformSpeed, deformRange);
                }


            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);

                if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
                Vector3 hitPoint = hit.point;
                
                EditTerrainCustom(hitPoint - new Vector3(0, 0, 0), !leftClickAddsTerrain, deformSpeed, deformRange);
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Vector3 startP = playerCamera.position;
                Vector3 destP = startP + playerCamera.forward;
                Vector3 direction = destP - startP;

                Ray ray = new Ray(startP, direction);

                if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
                _isFlattening = true;

                _flatteningOrigin = hit.point;
                _flatteningNormal = hit.normal;
                _flatteningNormal = new Vector3(0, 1, 0);


                float oldDeform = deformRange;
                deformRange = 10;

                for (int i = 0; i != 15; i++)
                    FlattenTerrain();

                deformRange = oldDeform;
            }
        }



        /// <summary>
        /// Shoots a raycast to the terrain and deforms the terrain around the hit point
        /// </summary>
        /// <param name="addTerrain">Should terrain be added or removed</param>
        private void RaycastToTerrain(bool addTerrain)
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
            Vector3 hitPoint = hit.point;

            if(Input.GetKey(KeyCode.G))
                EditTerrain(hitPoint, addTerrain, deformSpeed, deformRange);
            else if (Input.GetKey(KeyCode.X))
            {
                //Vector3 offset = new Vector3(0, 0, 0);
                
                EditTerrainCube(hitPoint - new Vector3(0, 0, -5), !addTerrain, deformSpeed, deformRange);
                EditTerrainCube(hitPoint - new Vector3(0, 0, 5), !addTerrain, deformSpeed, deformRange);

                EditTerrain(hitPoint - new Vector3(-5, 0, 0), addTerrain, deformSpeed, deformRange);
                EditTerrain(hitPoint - new Vector3(5, 0, 0), addTerrain, deformSpeed, deformRange);
            }
            else
                EditTerrainCube(hitPoint, addTerrain, deformSpeed, deformRange);
        }

        /// <summary>
        /// Deforms the terrain in a spherical region around the point
        /// </summary>
        /// <param name="point">The point to modify the terrain around</param>
        /// <param name="addTerrain">Should terrain be added or removed</param>
        /// <param name="deformSpeed">How fast the terrain should be deformed</param>
        /// <param name="range">How far the deformation can reach</param>
        public void EditTerrain(Vector3 point, bool addTerrain, float deformSpeed, float range)
        {
            int buildModifier = addTerrain ? 1 : -1;

            int hitX = Mathf.RoundToInt(point.x);
            int hitY = Mathf.RoundToInt(point.y);
            int hitZ = Mathf.RoundToInt(point.z);
            int3 hitPoint = new int3(hitX, hitY, hitZ);

            int intRange = Mathf.CeilToInt(range);
            int3 rangeInt3 = new int3(intRange, intRange, intRange);

            BoundsInt queryBounds = new BoundsInt((hitPoint - rangeInt3).ToVectorInt(), new int3(intRange * 2).ToVectorInt());

            voxelWorld.VoxelDataStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
            {
                float distance = math.distance(voxelDataWorldPosition, point);
                if(applyLevelLimit)
                    if(voxelDataWorldPosition.y <= lowestLevel || voxelDataWorldPosition.y > HighestLevel)
                        return voxelData;

                if (distance <= range)
                {
                    float modificationAmount = deformSpeed / distance * buildModifier;
                    float oldVoxelData = voxelData / 255f;
                    return (byte)math.clamp((oldVoxelData - modificationAmount) * 255, 0, 255);
                }

                return voxelData;
            });
        }

        private void EditTerrainCube(Vector3 point, bool addTerrain, float deformSpeed, float range)
        {
            int buildModifier = addTerrain ? 1 : -1;

            int hitX = Mathf.RoundToInt(point.x);
            int hitY = Mathf.RoundToInt(point.y);
            int hitZ = Mathf.RoundToInt(point.z);
            int3 hitPoint = new int3(hitX, hitY, hitZ);

            int intRange = Mathf.CeilToInt(range);
            int3 rangeInt3 = new int3(intRange, intRange, intRange);

            BoundsInt queryBounds = new BoundsInt((hitPoint - rangeInt3).ToVectorInt(), new int3(intRange * 2).ToVectorInt());

            voxelWorld.VoxelDataStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
            {
                if (applyLevelLimit)
                    if (voxelDataWorldPosition.y <= lowestLevel || voxelDataWorldPosition.y > HighestLevel)
                        return voxelData;

                float3 q = math.abs(voxelDataWorldPosition) - queryBounds.size.ToInt3();
                float target = math.length(math.max(q, 0)) + math.min(math.max(q.x, math.max(q.y, q.z)), 0);

                float newVoxelData = math.lerp(voxelData / 255f, target * buildModifier, deformSpeed * 0.005f);

                return (byte)(math.saturate(newVoxelData) * 255);
            });
        }

        private void EditTerrainCustom(Vector3 point, bool addTerrain, float deformSpeed, float range)
        {
            int buildModifier = addTerrain ? 1 : -1;

            int hitX = Mathf.RoundToInt(point.x);
            int hitY = Mathf.RoundToInt(point.y);
            int hitZ = Mathf.RoundToInt(point.z);
            int3 hitPoint = new int3(hitX, hitY, hitZ);

            int intRange = Mathf.CeilToInt(range);
            int3 rangeInt3 = new int3(intRange, intRange, intRange);

            BoundsInt queryBounds = new BoundsInt((hitPoint - rangeInt3).ToVectorInt(), new int3(intRange * 2).ToVectorInt());

            voxelWorld.VoxelDataStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
            {
                float3 q = math.abs(voxelDataWorldPosition) - queryBounds.size.ToInt3();
                // float target = math.length(math.max(q, 0)) + math.min(math.max(q.x, math.max(q.y, q.z)), 0);
                // 
                // float newVoxelData = math.lerp(voxelData / 255f, target * buildModifier, deformSpeed * 0.005f);

                Debug.Log(voxelDataWorldPosition + ":" + queryBounds.size.ToInt3() + ":" + queryBounds + ":" + voxelData);

                return (byte)(debugNumber); // 126 makes the closest perfect cubes, 127 and above is 'empty'. Being floats though, 127 is "BARELY" empty... but is for meshes.
            });
        }

        /// <summary>
        /// Get a point on the flattening plane and flatten the terrain around it
        /// </summary>
        private void FlattenTerrain()
        {
            PlaneLineIntersectionResult result = IntersectionUtilities.PlaneLineIntersection(_flatteningOrigin, _flatteningNormal, playerCamera.position, playerCamera.forward, out float3 intersectionPoint);
            if (result != PlaneLineIntersectionResult.OneHit) { return; }

            float flattenOffset = 0;

            // This is a bit hacky. One fix could be that the VoxelMesher class has a flattenOffset property, but I'm not sure if that's a good idea either.
            if (voxelWorld.VoxelMesher is MarchingCubesMesher marchingCubesMesher)
            {
                flattenOffset = marchingCubesMesher.Isolevel;
            }

            int intRange = (int)math.ceil(deformRange);
            int size = 2 * intRange + 1;
            int3 queryPosition = (int3)(intersectionPoint - new int3(intRange));
            BoundsInt worldSpaceQuery = new BoundsInt(queryPosition.ToVectorInt(), new Vector3Int(size, size, size));

            voxelWorld.VoxelDataStore.SetVoxelDataCustom(worldSpaceQuery, (voxelDataWorldPosition, voxelData) =>
            {
                float distance = math.distance(voxelDataWorldPosition, intersectionPoint);
                if (distance > deformRange)
                {
                    return voxelData;
                }

                float voxelDataChange = (math.dot(_flatteningNormal, voxelDataWorldPosition) - math.dot(_flatteningNormal, _flatteningOrigin)) / deformRange;

                return (byte)math.clamp(((voxelDataChange * 0.5f + voxelData / 255f - flattenOffset) * 0.8f + flattenOffset) * 255, 0, 255);
            });
        }

        /// <summary>
        /// Shoots a ray towards the terrain and changes the material around the hitpoint to <see cref="paintColor"/>
        /// </summary>
        private void PaintColor()
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxReachDistance)) { return; }
            Vector3 point = hit.point;

            int hitX = Mathf.RoundToInt(point.x);
            int hitY = Mathf.RoundToInt(point.y);
            int hitZ = Mathf.RoundToInt(point.z);
            int3 hitPoint = new int3(hitX, hitY, hitZ);
            int3 intRange = new int3(Mathf.CeilToInt(deformRange));

            BoundsInt queryBounds = new BoundsInt((hitPoint - intRange).ToVectorInt(), (intRange * 2).ToVectorInt());

            voxelWorld.VoxelColorStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
            {
                float distance = math.distance(voxelDataWorldPosition, point);

                if (distance <= deformRange)
                {
                    return paintColor;
                }

                return voxelData;
            });
        }

        public void PaintColorSphere(Vector3 point, float range, Color32 paintCol)
        {
            int hitX = Mathf.RoundToInt(point.x);
            int hitY = Mathf.RoundToInt(point.y);
            int hitZ = Mathf.RoundToInt(point.z);
            int3 hitPoint = new int3(hitX, hitY, hitZ);
            int3 intRange = new int3(Mathf.CeilToInt(range));

            BoundsInt queryBounds = new BoundsInt((hitPoint - intRange).ToVectorInt(), (intRange * 2).ToVectorInt());

            voxelWorld.VoxelColorStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
            {
                float distance = math.distance(voxelDataWorldPosition, point);

                if (distance <= range)
                {
                    return paintCol;
                }

                return voxelData;
            });
        }
    }
}