using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;

using Fraktalia.Core.Math;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	[BurstCompile]
	public unsafe struct TextureToVoxel_Calculation : IJobParallelFor
	{
		public Vector3 Start;

		public float VoxelSize;
		public float HalfVoxelSize;
		public Vector3Int blocklengths;



		public Matrix4x4 GeneratorLocalToWorld;
		public Matrix4x4 GeneratorWorldToLocal;

		[WriteOnly]
		public NativeArray<NativeVoxelModificationData> changedata;

		public byte Depth;
		public float finalMultiplier;
		public int Smoothing;
		public float Tolerance;
		public int EvaluationMode;

		[ReadOnly]
		public NativeArray<IntPtr> Slices;

		public int TextureWidth;
		public int TextureHeight;

		public Matrix4x4 localToWorldMatrix;
		public float PositionMultiplier;

		public int ChannelToRead;

		public void Execute(int index)
		{
			Vector3Int position = MathUtilities.Convert1DTo3D(index, blocklengths.x, blocklengths.y, blocklengths.z);

			int z = position.z;
			int y = position.y;
			int x = position.x;

			Vector3 localPosition = Start + new Vector3(x * VoxelSize, y * VoxelSize, z * VoxelSize);
		

		
			var output = new NativeVoxelModificationData();
			output.Depth = (byte)Depth;




			Vector3 pixelPosition = localPosition;
			localPosition = localToWorldMatrix.MultiplyPoint3x4(localPosition);
			localPosition = GeneratorWorldToLocal.MultiplyPoint3x4(localPosition);
			Vector3 lookupPosition = pixelPosition / VoxelSize;



			int ID = ReadValue(lookupPosition * PositionMultiplier);



			output.ID = (int)Mathf.Clamp(ID * finalMultiplier, 0,255);

			output.X = localPosition.x;
			output.Y = localPosition.y;
			output.Z = localPosition.z;

			changedata[index] = output;
		}

		public void BuildSlices(List<Texture2D> slices)
		{
			Slices = new NativeArray<IntPtr>(slices.Count, Allocator.Persistent);
			for (int i = 0; i < Slices.Length; i++)
			{
				NativeArray<byte> slice = slices[i].GetRawTextureData<byte>();
				Slices[i] = (IntPtr)slice.GetUnsafePtr();
			}
		}

		public byte ReadValue(Vector3 position)
		{
			byte ID = 0;

			Vector3 currentposition = position;

			int slice = (int)currentposition.y;
			if (slice >= 0 && slice < Slices.Length)
			{
				int posX = (int)currentposition.x % TextureWidth;
				int posY = (int)currentposition.z % TextureHeight;

				IntPtr texture = Slices[slice];
				ID = UnsafeUtility.ReadArrayElement<byte>( texture.ToPointer(), ChannelToRead + MathUtilities.Convert2DTo1D(posX, posY, TextureWidth)*4);
			}

			return ID;
		}

		[BurstDiscard]
		public void CleanUp()
		{		
			changedata.Dispose();
			Slices.Dispose();
		}
	
	}
}
