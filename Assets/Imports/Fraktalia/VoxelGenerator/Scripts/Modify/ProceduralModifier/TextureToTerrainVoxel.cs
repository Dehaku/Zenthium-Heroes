using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Math;
using Fraktalia.VoxelGen;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class TextureToTerrainVoxel : ProceduralVoxelModifier
	{
		[BeginInfo("TEXTURETOTERRAIN")]
		[InfoTitle("Texture to Voxel Terrain", "This converter works similar like the terrain to voxel converter but uses a texture instead. " +
			"Assign a standard texture2D in RGBA format and convert it into a voxel representation. " +
			"\n\n <b>NOTE: The assigned texture must be marked as readible otherwise an exception is thrown.</b>", "TEXTURETOTERRAIN")]
		[InfoSection1("How to use:", "Assign a texture and the target generator to the converter. Then adjust top/bottom extension and size. " +
			"The dark region shows the boundary which will be modified." +
			"\n\n - Invert inverses the result." +
			"\n - Top and Bottom Extension increase the upper/lower size of the volume." +
			"\n - Size defines the dimension in Width and Height." +
			"\n - Fill positive/negative Side creates a full block above/below the surface." +			
			"\n - Height falloff defines how strong the ID of a voxel decreases the further the Y coordinate is away from the surface." +
			"\n - Texture multipliers multiplies the value of the read pixel. + " +
			"\n - Smoothing smoothes the result." +
			"\n\n" +
			"<b>Info:</b> When height falloff is 0, the result is exactly like the slice based texture to voxel converter. " +
			"Increasing the falloff will gradually result into the terrain like relief surface.", "TEXTURETOTERRAIN")]
		[InfoText("Texture to Voxel Terrain:", "TEXTURETOTERRAIN")]
		public Texture2D TextureToConvert;
		public TextureToVoxel.TextureChannel ChannelToRead;
		public bool Invert = false;
		public float TopExtension = 10;
		public float BottomExtension = 10;

		public Vector2Int Size;
		public bool FillPositiveSide;
		public bool FillNegativeSide = true;
		
		public float HeightFallOff = 256;
		
		[Tooltip("Multiplier for the evaluated texture value. Is only used when Mode is DominantTexture or IndividualLayer.")]
		public float TextureMultiplier;

		[Range(0, 2)]
		public int Smoothing;

		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

		public override void OnDrawGizmosSelected()
		{
			if (TargetGenerator)
			{
				targetgenerator_localtoworldmatrix = TargetGenerator.transform.localToWorldMatrix;
				targetgenerator_worldtolocalmatrix = TargetGenerator.transform.worldToLocalMatrix;


				Bounds bound = CalculateBounds();


				Gizmos.color = new Color32(0, 0, 0, 30);
				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.DrawCube(bound.center, bound.size);

				Gizmos.matrix = Matrix4x4.identity;

				float RootSize = TargetGenerator.RootSize;
				Gizmos.color = Color.blue;
				Gizmos.matrix = TargetGenerator.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));



			}
		}

		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			NativeArray<NativeVoxelModificationData> changedata = new NativeArray<NativeVoxelModificationData>(boundaryvoxelcount, Allocator.TempJob);
			for (int x = 0; x < boundaryvoxelsize.x; x++)
			{
				for (int y = 0; y < boundaryvoxelsize.y; y++)
				{
					for (int z = 0; z < boundaryvoxelsize.z; z++)
					{
						Vector3 localPosition = start + new Vector3(voxelsize * x, voxelsize * y, voxelsize * z);

						int ID = ReadValue(localPosition);

						Vector3 worldPosition = transform.localToWorldMatrix.MultiplyPoint3x4(localPosition);
						localPosition = targetgenerator_worldtolocalmatrix.MultiplyPoint3x4(worldPosition);


						var output = new NativeVoxelModificationData();
						output.Depth = (byte)Depth;
						output.X = localPosition.x;
						output.Y = localPosition.y;
						output.Z = localPosition.z;

						
						
						output.ID = (int)(ID * finalMultiplier);
						changedata[MathUtilities.Convert3DTo1D(x, y, z, boundaryvoxelsize.x, boundaryvoxelsize.y, boundaryvoxelsize.z)] = output;



					}

				}

			}



			if (Smoothing > 0)
			{
				ResultSmoothFilter.Apply(changedata, boundaryvoxelsize.x, boundaryvoxelsize.y, boundaryvoxelsize.z, Smoothing);
			}

			ProceduralVoxelData.AddRange(changedata);
			changedata.Dispose();
		}

		public int ReadValue(Vector3 position)
		{


			int ID = 0;

			int texturecoord_X = (int)((position.x / Size.x) * TextureToConvert.width);
			int texturecoord_Y = (int)((position.z / Size.y) * TextureToConvert.height);
			
			Color pixel = TextureToConvert.GetPixel(texturecoord_X, texturecoord_Y);
			float height = pixel[(int)ChannelToRead];
			if(Invert)
			{
				height = 1 - height;
			}
			height = height * 256 * TextureMultiplier;



			float voxelheight = position.y;
			float difference = height - Mathf.Abs(voxelheight * HeightFallOff);

			ID = Mathf.Clamp((int)difference, 0, 255);

			if (FillNegativeSide && voxelheight < 0)
			{
				ID = 255;
			}

			if (FillPositiveSide && voxelheight > 0)
			{
				ID = 255;
			}

			return ID;
		}

		public override Bounds CalculateBounds()
		{
			if (TextureToConvert == null) return new Bounds();

			Bounds bound = new Bounds();

			bound.min = Vector3.zero - new Vector3(0, BottomExtension, 0);
			Vector3 size = new Vector3(Size.x, 0, Size.y);
			size.y += TopExtension;
			bound.max = size;



			return bound;
		}

		public override bool IsSave()
		{


			bool issave = true;


			Bounds bound = CalculateBounds();
			voxelsize = TargetGenerator.GetVoxelSize(Depth);
			int voxellength_x = (int)(bound.size.x / voxelsize);
			int voxellength_y = (int)(bound.size.y / voxelsize);
			int voxellength_z = (int)(bound.size.z / voxelsize);
			int voxels = voxellength_x * voxellength_y * voxellength_z;

			if (voxels > 5000000 || voxels < 0)
			{
				ErrorMessage = "This setup would modify more than 5000000 Voxels. You do not want to freeze Unity";



				issave = false;
			}

			return issave;
		}
	}
}
