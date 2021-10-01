using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Eldemarkki.VoxelTerrain.Player
{

    public class TerrainCrator : MonoBehaviour
    {
        [SerializeField] TerrainDeformer deformer;

        [SerializeField] bool DeformAddTerrain = false;
        [SerializeField] float DeformSpeed = 10;
        [SerializeField] float DeformRange = 10;
        [SerializeField] bool PaintTerrain = false;
        [SerializeField] float PaintOutlineRange = 1;
        [SerializeField] Color32 TerrainColor;

        // Start is called before the first frame update
        void Start()
        {
                deformer = FindObjectOfType<TerrainDeformer>();
                if(deformer == null)
                {
                    Debug.LogWarning("No Terrain Deformer script exists.");
                }
        }

        // Update is called once per frame
        void Update()
        {
                if (deformer == null)
                    return;


        }
        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collision: " + collision.contacts[0].point);

            deformer.EditTerrain(collision.contacts[0].point, DeformAddTerrain, DeformSpeed, DeformRange);
            if(PaintTerrain)
                deformer.PaintColorSphere(collision.contacts[0].point, DeformRange + PaintOutlineRange, TerrainColor);
            Destroy(this.gameObject);
        }
    }

}
