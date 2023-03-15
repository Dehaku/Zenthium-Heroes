using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Fraktalia.VoxelGen.Visualisation
{
	public static class Cubic_Data
	{
		private static NativeArray<int> VertexOffset;

		public static NativeArray<int> GetVertexOffset
		{
			get
			{
				if (!VertexOffset.IsCreated)
				{
					CreateCubeOffsets();
				}

				return VertexOffset;
			}
		}

		#region CubeInformations
		private static void CreateCubeOffsets()
		{
			if (VertexOffset.IsCreated) return;
			int[] vertexOffset = new int[]
			{
			0, 0, -1,
			0, 0, 1,
			-1, 0, 0,
			1, 0, 0,
			0, -1,0,
			0, 1, 0
			};
			VertexOffset = new NativeArray<int>(vertexOffset, Allocator.Persistent);
		}

		public static void DisposeStaticInformation()
		{
			if (VertexOffset.IsCreated) VertexOffset.Dispose();
		}
		#endregion
	}
}