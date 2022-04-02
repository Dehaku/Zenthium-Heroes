using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Eldemarkki.VoxelTerrain.Player
{

    public class TerrainCrator : MonoBehaviour
    {
        [SerializeField] TerrainDeformer deformer;

        [SerializeField] bool detonateImmediately = false;
        [SerializeField] bool DeformAddTerrain = false;
        [SerializeField] float DeformSpeed = 10;
        [SerializeField] float DeformRange = 10;
        [SerializeField] bool PaintTerrain = false;
        [SerializeField] float PaintOutlineRange = 1;
        [SerializeField] Color32 TerrainColor;
        


        private void Init()
        {
            deformer = FindObjectOfType<TerrainDeformer>();
            if (deformer == null)
            {
                Debug.LogWarning("No Terrain Deformer script exists.");
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        // Update is called once per frame
        void Update()
        {
            if (deformer == null)
                return;
            if (detonateImmediately)
                Detonate(transform.position);

        }

        void Detonate(Vector3 point)
        {
            if (!deformer)
                Init();

            deformer.EditTerrain(point, DeformAddTerrain, DeformSpeed, DeformRange);
            if (PaintTerrain)
                deformer.PaintColorSphere(point, DeformRange + PaintOutlineRange, TerrainColor);
            Destroy(this.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Detonate(collision.contacts[0].point);
        }
    }

}
