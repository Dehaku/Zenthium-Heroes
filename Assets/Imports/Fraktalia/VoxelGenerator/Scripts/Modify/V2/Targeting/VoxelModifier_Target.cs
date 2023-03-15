using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify
{


	public class VoxelModifier_Target : MonoBehaviour
	{
		public List<VoxelGenerator> AlwaysModify = new List<VoxelGenerator>();
		public List<VoxelGenerator> NeverModify = new List<VoxelGenerator>();

		public virtual VoxelGenerator Reference
		{
			get
			{
				if (AlwaysModify.Count > 0)
				{
					return AlwaysModify[0];
				}
				return null;
			}
		}
		

		public virtual void fetchGenerators(List<VoxelGenerator> targets, Vector3 worldPosition)
		{
			for (int i = 0; i < AlwaysModify.Count; i++)
			{
				if (AlwaysModify[i])
				{
					VoxelGenerator generator = AlwaysModify[i];
					if (!targets.Contains(generator) && !NeverModify.Contains(generator))
					{
						targets.Add(generator);
					}
					
				}
			}	
			
		}
	}
}
