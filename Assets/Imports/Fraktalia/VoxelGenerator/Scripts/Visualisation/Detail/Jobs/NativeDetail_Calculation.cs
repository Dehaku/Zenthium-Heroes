using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public struct NativeDetail_Calculation : IJob
	{
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;

		public Vector3 positionoffset;
		public float voxelSize;
		public float cellSize;





		public float SmoothingAngle;
		public int MaxBlocks;

		public float Probability;
		public int RandomIndexOffset;

		private int Width;

		[ReadOnly]
		public NativeArray<Vector3> Permutations;

		public int InitialHealth;



		[ReadOnly]
		public NativeArray<DetailRequirement> DetailNeighbourRequirement;

		public FNativeList<Vector3> detailresultArray;

		[BurstDiscard]
		public void Init(int width)
		{
			CleanUp();

			Width = width;

			int blocks = Width * Width * Width;

			MaxBlocks = blocks;

			detailresultArray = new FNativeList<Vector3>(2000, Allocator.Persistent);




		}

		public void Execute()
		{
			detailresultArray.Clear();

			int permutationcount = Permutations.Length;

			int blocks = MaxBlocks;
			float halfcell = cellSize / 2;
			float xhalf = voxelSize / 2;
			float yhalf = voxelSize / 2;
			float zhalf = voxelSize / 2;

			Vector3 blockoffset = positionoffset + new Vector3(xhalf, xhalf, xhalf);


			int neighbourrequirements = DetailNeighbourRequirement.Length;
			for (int index = 0; index < blocks; index++)
			{
				Vector3 random3 = Permutations[(index * 3 + RandomIndexOffset) % permutationcount];
				if (random3.y > Probability) continue;

				int x = index % Width;
				int y = (index - x) / Width % Width;
				int z = ((index - x) / Width - y) / Width;

				int i;
				int ix, iy, iz;

				float fx = positionoffset.x + x * voxelSize + xhalf;
				float fy = positionoffset.y + y * voxelSize + yhalf;
				float fz = positionoffset.z + z * voxelSize + zhalf;





				Vector3 voxeloffset = new Vector3(x, y, z) * voxelSize;
				Vector3 offset = blockoffset + voxeloffset;						
				int requirementscorrect = InitialHealth;
				for (i = 0; i < neighbourrequirements; i++)
				{
					DetailRequirement requirement = DetailNeighbourRequirement[i];

					ix = x + requirement.x;
					iy = y + requirement.y;
					iz = z + requirement.z;

					float fxn = positionoffset.x + ix * voxelSize + xhalf;
					float fyn = positionoffset.y + iy * voxelSize + yhalf;
					float fzn = positionoffset.z + iz * voxelSize + zhalf;

					int ID = data._PeekVoxelId(fxn, fyn, fzn, 10);

					if (requirement.CompMode == DetailRequirement.DetailComparison.Equal)
					{
						if (ID == requirement.TargetID)
						{
							requirementscorrect += requirement.CorrectModifier;
						}
						else
						{
							requirementscorrect += requirement.IncorrectModifier;
						}
					}
					else if (requirement.CompMode == DetailRequirement.DetailComparison.NotEqual)
					{
						if (ID != requirement.TargetID)
						{
							requirementscorrect += requirement.CorrectModifier;
						}
						else
						{
							requirementscorrect += requirement.IncorrectModifier;
						}
					}
					else if (requirement.CompMode == DetailRequirement.DetailComparison.Greater)
					{
						if (ID > requirement.TargetID)
						{
							requirementscorrect += requirement.CorrectModifier;
						}
						else
						{
							requirementscorrect += requirement.IncorrectModifier;
						}
					}
					else if (requirement.CompMode == DetailRequirement.DetailComparison.Smaller)
					{
						if (ID < requirement.TargetID)
						{
							requirementscorrect += requirement.CorrectModifier;
						}
						else
						{
							requirementscorrect += requirement.IncorrectModifier;
						}
					}					
				}

				if (requirementscorrect < 0) continue;

				detailresultArray.Add(new Vector3(fx, fy, fz));

			}

		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (detailresultArray.IsCreated) detailresultArray.Dispose();
		}
	}

	[System.Serializable]
	public struct DetailRequirement
	{
		public enum DetailComparison
		{
			Equal,
			NotEqual,
			Greater,
			Smaller
		}

		public int x;
		public int y;
		public int z;
		public int TargetID;
	
		public DetailComparison CompMode;
		public int CorrectModifier;
		public int IncorrectModifier;

		public float GetChecksum()
		{
			return x + y + z +TargetID+ (int)CompMode + CorrectModifier + IncorrectModifier;				
		}
	}
}
