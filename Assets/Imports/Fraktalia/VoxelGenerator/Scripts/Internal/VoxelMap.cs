using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fraktalia.VoxelGen
{
	[CreateAssetMenu(fileName = "VoxelMap", menuName = "Fraktalia/VoxelMap", order = 0)]
	public class VoxelMap : ScriptableObject
	{
		public RawVoxelData VoxelData;
	}
}
