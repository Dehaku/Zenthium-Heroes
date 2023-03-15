using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using UnityEngine;
using Unity.Burst;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	struct DataVisualisation_Calculation : IJob
	{

		public FNativeList<Vector3> m_Vertices;
		public FNativeList<Vector2> m_uvs;
		public FNativeList<int> m_triangles;
		public FNativeList<float> sizeIndex;
		public FNativeList<Vector3> offsetIndex;
		public FNativeList<int> Neighbours;


		public FNativeQueue<NativeVoxelNode> NodeStackBuffer;
		public NativeVoxelTree Data;
		public NativeVoxelNode Voxel;
		public int MeshDepth;
		public int CoreID;
		public bool FullCubes;
		public float ScaleMultiplicator;
	
		public void Init(int Depth, int subdivision)
		{
			CleanUp();
			if (Neighbours.IsCreated) Neighbours.Dispose();


			int MaxBlocks = 1 + (int)Mathf.Pow(subdivision * subdivision * subdivision, Depth);			

			Neighbours = new FNativeList<int>(2000, Allocator.Persistent);

			if (m_Vertices.IsCreated) m_Vertices.Dispose();
			if (m_uvs.IsCreated) m_uvs.Dispose();
			if (m_triangles.IsCreated) m_triangles.Dispose();

			m_Vertices = new FNativeList<Vector3>(2000, Allocator.Persistent);
			m_uvs = new FNativeList<Vector2>(2000, Allocator.Persistent);
			m_triangles = new FNativeList<int>(2000, Allocator.Persistent);
			offsetIndex = new FNativeList<Vector3>(2000, Allocator.Persistent);
			sizeIndex = new FNativeList<float>(2000, Allocator.Persistent);
			
			NodeStackBuffer = new FNativeQueue<NativeVoxelNode>(Allocator.Persistent);
		}

		public void CleanUp()
		{
			if (Neighbours.IsCreated) Neighbours.Dispose();
			if (m_Vertices.IsCreated) m_Vertices.Dispose();
			if (m_uvs.IsCreated) m_uvs.Dispose();
			if (m_triangles.IsCreated) m_triangles.Dispose();
			if (sizeIndex.IsCreated) sizeIndex.Dispose();
			if (offsetIndex.IsCreated) offsetIndex.Dispose();
			
			if (NodeStackBuffer.IsCreated) NodeStackBuffer.Dispose();


		}



		public void Execute()
		{
			m_Vertices.Clear();
			m_uvs.Clear();
			m_triangles.Clear();
			offsetIndex.Clear();
			sizeIndex.Clear();
			Neighbours.Clear();
			NodeStackBuffer.Clear();


			int solidvoxelcount = Data._GetNativeVoxelsBelow(ref Voxel, MeshDepth,
					   ref offsetIndex, ref sizeIndex, ref Neighbours, !FullCubes, CoreID, ref NodeStackBuffer);

			int Length = solidvoxelcount;
		
			int hullPosition = 0;
			for (int i = 0; i < Length; i++)
			{
				Vector3 offset = offsetIndex[i];

				float xhalf = sizeIndex[i] * ScaleMultiplicator / 2;
				float yhalf = sizeIndex[i] * ScaleMultiplicator / 2;
				float zhalf = sizeIndex[i] * ScaleMultiplicator / 2;

				//links
				if (Neighbours[i * 6 + 0] == 0)
				{
					m_Vertices.Add(new Vector3(-xhalf, -yhalf, -zhalf) + offset);
					m_Vertices.Add(new Vector3(-xhalf, yhalf, -zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, yhalf, -zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, -yhalf, -zhalf) + offset);

					m_uvs.Add(new Vector2(0, 0));
					m_uvs.Add(new Vector2(0, 1));
					m_uvs.Add(new Vector2(1, 1));
					m_uvs.Add(new Vector2(1, 0));

					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 1);
					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 3);
					m_triangles.Add(hullPosition * 4 + 0);

					hullPosition++;
				}

				//rechts
				if (Neighbours[i * 6 + 1] == 0)
				{
					m_Vertices.Add(new Vector3(-xhalf, -yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(-xhalf, yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, -yhalf, zhalf) + offset);

					m_uvs.Add(new Vector2(1, 1));
					m_uvs.Add(new Vector2(1, 0));
					m_uvs.Add(new Vector2(0, 0));
					m_uvs.Add(new Vector2(0, 1));

					m_triangles.Add( hullPosition * 4 + 2);
					m_triangles.Add( hullPosition * 4 + 1);
					m_triangles.Add( hullPosition * 4 + 0);
					m_triangles.Add( hullPosition * 4 + 0);
					m_triangles.Add( hullPosition * 4 + 3);
					m_triangles.Add( hullPosition * 4 + 2);

					hullPosition++;
				}

				//oben
				if (Neighbours[i * 6 + 2] == 0)
				{
					m_Vertices.Add(new Vector3(-xhalf, -yhalf, -zhalf) + offset);
					m_Vertices.Add(new Vector3(-xhalf, -yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(-xhalf, yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(-xhalf, yhalf, -zhalf) + offset);

					m_uvs.Add(new Vector2(1, 1));
					m_uvs.Add(new Vector2(0, 1));
					m_uvs.Add(new Vector2(0, 0));
					m_uvs.Add(new Vector2(1, 0));

					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 1);
					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 3);
					m_triangles.Add(hullPosition * 4 + 0);
					hullPosition++;
				}

				//unten
				if (Neighbours[i * 6 + 3] == 0)
				{
					m_Vertices.Add(new Vector3(xhalf, -yhalf, -zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, -yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, yhalf, -zhalf) + offset);

					m_uvs.Add(new Vector2(0, 0));
					m_uvs.Add(new Vector2(1, 0));
					m_uvs.Add(new Vector2(1, 1));
					m_uvs.Add(new Vector2(0, 1));

					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 1);
					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 3);
					m_triangles.Add(hullPosition * 4 + 2);
					hullPosition++;
				}

				//hinten
				if (Neighbours[i * 6 + 4] == 0)
				{
					m_Vertices.Add(new Vector3(-xhalf, -yhalf, -zhalf) + offset);
					m_Vertices.Add(new Vector3(-xhalf, -yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, -yhalf, zhalf) + offset);
					m_Vertices.Add(new Vector3(xhalf, -yhalf, -zhalf) + offset);

					m_uvs.Add(new Vector2(0, 0));
					m_uvs.Add(new Vector2(0, 1));
					m_uvs.Add(new Vector2(1, 1));
					m_uvs.Add(new Vector2(1, 0));

					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 1);
					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 3);
					m_triangles.Add(hullPosition * 4 + 2);
					hullPosition++;
				}

				//vorne
				if (Neighbours[i * 6 + 5] == 0)
				{
					m_Vertices.Add( new Vector3(-xhalf, yhalf, -zhalf) + offset);
					m_Vertices.Add( new Vector3(-xhalf, yhalf, zhalf) + offset);
					m_Vertices.Add( new Vector3(xhalf, yhalf, zhalf) + offset);
					m_Vertices.Add( new Vector3(xhalf, yhalf, -zhalf) + offset);

					m_uvs.Add(new Vector2(1, 1));
					m_uvs.Add(new Vector2(1, 0));
					m_uvs.Add(new Vector2(0, 0));
					m_uvs.Add(new Vector2(0, 1));

					m_triangles.Add(hullPosition * 4 + 0);
					m_triangles.Add(hullPosition * 4 + 1);
					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 2);
					m_triangles.Add(hullPosition * 4 + 3);
					m_triangles.Add(hullPosition * 4 + 0);
					hullPosition++;
				}
				
			}

		}

	}
}
