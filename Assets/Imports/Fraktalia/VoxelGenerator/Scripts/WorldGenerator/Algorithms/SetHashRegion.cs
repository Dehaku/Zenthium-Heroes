using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.World
{
	public class SetHashRegion : WorldAlgorithm
	{
		public int SkyBorder = 5;


		public override JobHandle Apply(Vector3 hash, VoxelGenerator targetGenerator, ref JobHandle handle)
		{

			return handle;

		}	
	}
}
