using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify
{


	public class VoxelModifier_TargetSphereCast : VoxelModifier_Target
	{		
		public LayerMask SphereCastLayer = int.MaxValue;
		public int Maximum = 3;
		public float Radius = 10;

		
		public VoxelGenerator lastReference;
		public override VoxelGenerator Reference {
			get
			{
				if(lastReference == null)
				{
					return base.Reference;
				}

				return lastReference;
			}
			
		}
		public override void fetchGenerators(List<VoxelGenerator> targets, Vector3 worldPosition)
		{
			base.fetchGenerators(targets, worldPosition);

			List<VoxelGenerator> fetchedGenerators = FindGeneratorsBySphereCast(worldPosition, Radius, SphereCastLayer);

			for (int i = 0; i < fetchedGenerators.Count; i++)
			{
				if (i >= 3) break;
				VoxelGenerator generator = fetchedGenerators[i];
				if(!targets.Contains(generator) && !NeverModify.Contains(generator))
				{
					targets.Add(generator);
					if (i == 0) lastReference = fetchedGenerators[i];
				}

				
			}

		}

		public List<VoxelGenerator> FindGeneratorsBySphereCast(Vector3 worldPosition, float radius, LayerMask mask)
		{
			Collider[] impact = Physics.OverlapSphere(worldPosition, radius, mask);

			List<VoxelGenerator> result = new List<VoxelGenerator>();
			for (int i = 0; i < impact.Length; i++)
			{
				VoxelGenerator generator = impact[i].GetComponentInParent<VoxelGenerator>();
				if (generator)
				{
					result.Add(generator);
				}
			}
			return result;
		}
	}
}
