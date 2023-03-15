using UnityEngine;
using System.Collections;
using Fraktalia.Utility;

namespace Fraktalia.Core.LMS
{
	[ExecuteInEditMode]
    public class MeshPieceAttachment_Particle : MeshPieceAttachment
    {
		public const float PARTICLEEMISSIONREFERENCE = 1000;

		
		[Header("Adds particle system to mesh pieces.")]	
		[Tooltip("The original particle effect which should be instantiated and attached to each mesh piece")]
		public ParticleSystem ParticleEffect;

		[Tooltip("If set, emission rate is adapted to the vertex caunt. Else particle effect would concentrate on meshes with few vertices and disperse when the vertex count is large.")]
		public bool MatchEmissionWithVertexCount = true;

		[Tooltip("Multiplicator for adapting the emission rate to the vertexcount. " +
			"Emission rate is calculated by (original emissionrate/1000) * vertex count * EmissionMultiplier." +
			" This means that a mesh with 1 vertex would emit one particle each second if the emission rate is 1000 and the multiplier set to 1")]
		public float EmissionMultiplier = 1;

		[HideInInspector]
		public float ReferenceMultiplier = 1;

		[Tooltip("If true, the original particle system is disabled (play mode only)")]
		public bool DisableOriginalGameObject = true;

		[Tooltip("If true, the original particle is instantiated (play mode only)")]
		public bool EnableInstantiatedGameObject = true;

		[Tooltip("If true, the emission of the original particle is always set to 100. Helps designing when copying result values to the original effect")]
		public bool UseStandarizedEmission = false;

		private bool Initilized = false;

		public override void Effect(GameObject piece)
        {
			if (ParticleEffect == null) return;
			var shapemodule_original = ParticleEffect.shape;
			shapemodule_original.meshRenderer = null;

			if (Application.isPlaying)
			{
				if (DisableOriginalGameObject)
				{
					ParticleEffect.gameObject.SetActive(false);
				}
			}
			

			MeshPieceAttachment_Particle attachment = piece.AddComponent<MeshPieceAttachment_Particle>();
			attachment.ParticleEffect = ParticleEffect;
			attachment.Initilized = true;
			attachment.EmissionMultiplier = EmissionMultiplier;

		

			if (UseStandarizedEmission)
			{
				var emission = ParticleEffect.emission;
				emission.rateOverTime = 100;

					
			}
			
			attachment.ReferenceMultiplier = ParticleEffect.emission.rateOverTime.constant / PARTICLEEMISSIONREFERENCE;
			

			attachment.ParticleEffect = Instantiate(ParticleEffect, attachment.transform);
			attachment.ParticleEffect.gameObject.AddComponent<CopyRestriction>().Initialize(
				"Cloning is forbidden as the cloned particle system will crash Unity as soon as the target mesh of the original particle system is modified.\n\n" +			
				" For some unknown reason, Unity messes up the cloning of game objects with a particle system attached, that uses Mesh shape emission.\n\n" +
				" Instead use right click ParticleSystem > Copy Component > right click on other particle system and select paste component values."

				);

			if (EnableInstantiatedGameObject)
			{
				attachment.ParticleEffect.gameObject.SetActive(true);
			
			}	

			MeshRenderer filter = piece.GetComponent<MeshRenderer>();
			if (filter)
			{
				var shapemodule = attachment.ParticleEffect.shape;
				shapemodule.meshRenderer = filter;
			
			}
		}

		public override void UpdatePiece(GameObject piece, Mesh proceduralMesh)
		{
			if (!Initilized) return;
			if (proceduralMesh == null) return;

			if(proceduralMesh.vertexCount <= 1)
			{			
				var emission = ParticleEffect.emission;
				emission.enabled = false;
			}
			else
			{
				
				var emission = ParticleEffect.emission;
				
				if (MatchEmissionWithVertexCount)
				{
					emission.rateOverTime = (int)(proceduralMesh.vertexCount * EmissionMultiplier * ReferenceMultiplier);
				}
				emission.enabled = true;
				
				
			}
		}
	}
}
