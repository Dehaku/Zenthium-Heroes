using Fraktalia.Core.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

namespace Fraktalia.VoxelGen.World
{
	public class WorldAlgorithm_Texture : WorldAlgorithm
	{
		public Texture2D Texture;


		public Vector3 Offset;
		public float Frequency;
		public float HeightMultiplier;
		public float FallOff;
		
		public Modify.Procedural.TextureToVoxel.TextureChannel ChannelToRead;


		WorldAlgorithm_Texture_Calculate calculate;

		private Texture2D correctedTexture;




		public override void Initialize(VoxelGenerator template)
		{
			int width = template.GetBlockWidth(Depth);
			int blocks = width * width * width;
			
			calculate.Width = width;
			calculate.Blocks = blocks;
			calculate.Depth = (byte)Depth;
			

			correctTexture();

			if(correctedTexture)
			{
				calculate.BuildTexture(correctedTexture);
			}
			else if(Texture)
			{
				calculate.BuildTexture(Texture);
			}
		}

		public override JobHandle Apply(Vector3 hash, VoxelGenerator targetGenerator, ref JobHandle handle)
		{
			if (Texture == null) return handle;
			calculate.VoxelSize = targetGenerator.GetVoxelSize(Depth);
			calculate.voxeldata = worldGenerator.modificationReservoir.GetDataArray(Depth);
			calculate.ApplyMode = ApplyFunctionPointer;
			calculate.PostProcessFunctionPointer = PostProcessFunctionPointer;
			calculate.PositionOffset = hash * targetGenerator.RootSize + Offset * scale;
			calculate.RootSize = targetGenerator.RootSize;
			calculate.ChannelToRead = (int)ChannelToRead;
			calculate.Frequency = (Frequency * 40) / scale;
			calculate.FallOff = (FallOff * 40) / scale; 
			calculate.HeightMultiplier = (HeightMultiplier / 40) * scale; 
			return calculate .Schedule(calculate.Blocks, 64, handle);	
		}

		private void correctTexture()
		{
			Texture2D texture = Texture;
			if (texture.format != TextureFormat.RGBA32)
			{

				RenderTexture rtex = new RenderTexture(texture.width, texture.height, 0);
				Graphics.Blit(texture, rtex);

				Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
				tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0, false);
				tex.Apply();

				if (correctedTexture) DestroyImmediate(correctedTexture);

				correctedTexture = tex;
			
			}		
		}

		public override void CleanUp()
		{
			if (correctedTexture) DestroyImmediate(correctedTexture);
		}
	}

	
	
	[BurstCompile]
	public unsafe struct WorldAlgorithm_Texture_Calculate : IJobParallelFor
	{
		

		public byte Depth;
		public float VoxelSize;
		public float RootSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;
		
		
		public NativeArray<NativeVoxelModificationData_Inner> voxeldata;
		public FunctionPointer<WorldAlgorithm_Mode> ApplyMode;
		public FunctionPointer<WorldAlgorithm_PostProcess> PostProcessFunctionPointer;

		public int TextureWidth;
		public int TextureHeight;
		public float Frequency;
		public float FallOff;
		public float HeightMultiplier;
		public int ChannelToRead;

		[ReadOnly]
		private NativeArray<byte> TextureData;
		
		public void Execute(int index)
		{

			int x = index % Width;
			int y = (index - x) / Width % Width;
			int z = ((index - x) / Width - y) / Width;

			float Voxelpos_X = x * VoxelSize;
			float Voxelpos_Y = y * VoxelSize;
			float Voxelpos_Z = z * VoxelSize;
			Voxelpos_X += VoxelSize / 2;
			Voxelpos_Y += VoxelSize / 2;
			Voxelpos_Z += VoxelSize / 2;


			float Worldpos_X = PositionOffset.x + Voxelpos_X;
			float Worldpos_Y = PositionOffset.y + Voxelpos_Y;
			float Worldpos_Z = PositionOffset.z + Voxelpos_Z;

			

			NativeVoxelModificationData_Inner info = voxeldata[index];
			info.Depth = Depth;
			info.X = VoxelGenerator.ConvertLocalToInner(Voxelpos_X, RootSize);
			info.Y = VoxelGenerator.ConvertLocalToInner(Voxelpos_Y, RootSize);
			info.Z = VoxelGenerator.ConvertLocalToInner(Voxelpos_Z, RootSize);
			float height = -128;

			

			height += ReadValue(new Vector3(Worldpos_X * Frequency, 0, Worldpos_Z * Frequency)) ;


			height *= HeightMultiplier;


			int value = 0;
			if (Worldpos_Y < height)
			{
				value = 255;
			}
			else
			{
				float rest = Worldpos_Y - height;


				value = (int)(255 - rest * FallOff);
			}
			value = PostProcessFunctionPointer.Invoke(value);
			ApplyMode.Invoke(ref info, value);
			info.ID = Mathf.Clamp(info.ID, 0, 255);

			voxeldata[index] = info;

		}

		public void BuildTexture(Texture2D texture)
		{



			TextureData = texture.GetRawTextureData<byte>();
			TextureWidth = texture.width;
			TextureHeight = texture.height;

		
		}

		public byte ReadValue(Vector3 position)
		{
			byte ID = 0;

			Vector3 currentposition = position;
			
			int posX = (int)currentposition.x % TextureWidth;
			int posY = (int)currentposition.z % TextureHeight;

			ID = TextureData[ ChannelToRead + MathUtilities.Convert2DTo1D(posX, posY, TextureWidth) * 4];

			return ID;
		}


		public void Dispose()
		{
			if (voxeldata.IsCreated) voxeldata.Dispose();
		}
	}
}
