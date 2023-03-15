using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Math;
using Fraktalia.VoxelGen;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class TerrainToVoxel : ProceduralVoxelModifier
	{
		public enum EvaluationMode
		{
			Surface,
			DominantTexture,
			IndividualLayer
		}

		[BeginInfo("TERRAINTOVOXEL")]
		[InfoTitle("Terrain to Voxel Converter", "This script allows the conversion of Unity terrain into voxel. " +
			"Create and assign a conventional Unity3D terrain and convert it into a voxel representation. " +
			"It is also possible to convert the texture layer into a voxel representation which requires a multi material setup using UV coordinates (check Obsidian sample material).", "TERRAINTOVOXEL")]
		[InfoSection1("How to use:", "Create a Unity3D terrain and assign it to the converter. Ideally change the size of the terrain to match the Voxel Generator or vise versa. " +
			"The dark region shows the boundary which will be modified.\n\n" +
			"Ideally the world position of the modifier matches the world position of the terrain. You can also simply attach the modifier as child to the terrain game object " +
			"and set the local position to (0,0,0)" +
			"\n\n - The Offset Y changes the height position of the surface." +
			"\n - Top and Bottom Extension increase the upper/lower size of the volume." +
			"\n - Fill positive/negative Side creates a full block above/below the surface." +
			"\n - Falloff defines how strong the ID of a voxel decreases the further the Y coordinate is away from the surface. Ideal value is 10." +
			"\n - Mode defines the evaluation type. Use Surface to get the solid geometry. Dominant Texture and Individual Layer are used to convert the texture information" +
			"\n\n<b>To get a full terrain conversion, you need 2 converters with the same parameters. The first one is used for the surface and should modify Target Dimension 0 " +
			"and the second one should read the texture layers and modify Target Dimension 1. The parameters of both converters should be similar.</b>" +
			"\n\n - Texture Multiplier is used for Texture reading only. It is directly multiplied into the fetched texture value. The dominant layer is the texture layer with the highest alpha value" +
			"\nThe value is calculated with this formula: [Index of dominant Layer] X [Alpha of dominant Layer] X [Texture Multiplier]" +
			"\n\n - Target texture layer defines which texture layer of the terrain should be read when mode is set to individual layer.", "TERRAINTOVOXEL")]
		[InfoVideo("https://www.youtube.com/watch?v=18sfUuKmHwA&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=15", false, "TERRAINTOVOXEL")]
		[InfoText("Terrain To Voxel:", "TERRAINTOVOXEL")]
		
		public Terrain TerrainToConvert;

		public float OffsetY;
		public float TopExtension = 10;
		public float BottomExtension = 10;


		public bool FillPositiveSide;
		public bool FillNegativeSide = true;
		[Range(1, 20)]
		public float FallOff = 10;

		public EvaluationMode Mode;
		[Tooltip("Multiplier for the evaluated texture value. Is only used when Mode is DominantTexture or IndividualLayer.")]
		public int TextureMultiplier;

		[Tooltip("Texture Layer used when mode is set to IndividualLayer.")]
		public int TargetTextureLayer;

		[Range(0, 2)]
		public int Smoothing;

		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

		private float[,,] TerrainAlphamaps;

		public override void OnDrawGizmosSelected()
		{
			if (TargetGenerator)
			{
				targetgenerator_localtoworldmatrix = TargetGenerator.transform.localToWorldMatrix;
				targetgenerator_worldtolocalmatrix = TargetGenerator.transform.worldToLocalMatrix;


				Bounds bound = CalculateBounds();


				Gizmos.color = new Color32(0, 0, 0, 30);
				
				Gizmos.DrawCube(TargetGenerator.transform.localToWorldMatrix.MultiplyPoint3x4(bound.center), bound.size);


				float RootSize = TargetGenerator.RootSize;
				Gizmos.color = Color.blue;
				Gizmos.matrix = TargetGenerator.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));

				Gizmos.matrix = Matrix4x4.identity;
				Gizmos.color = Color.yellow;

				Vector3 centerpos = TargetGenerator.transform.localToWorldMatrix.MultiplyPoint3x4(bound.center) + new Vector3(0, OffsetY, 0);

				Gizmos.DrawWireCube(centerpos, new Vector3(bound.size.x, 0, bound.size.z));

			}
		}

		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{


			TerrainAlphamaps = TerrainToConvert.terrainData.GetAlphamaps(0, 0, TerrainToConvert.terrainData.alphamapWidth, TerrainToConvert.terrainData.alphamapHeight);

			Matrix4x4 TerrainToConvertWorld2Local = TerrainToConvert.transform.worldToLocalMatrix;


			NativeArray<NativeVoxelModificationData> changedata = new NativeArray<NativeVoxelModificationData>(boundaryvoxelcount, Allocator.TempJob);
			for (int x = 0; x < boundaryvoxelsize.x; x++)
			{
				for (int y = 0; y < boundaryvoxelsize.y; y++)
				{
					for (int z = 0; z < boundaryvoxelsize.z; z++)
					{
						Vector3 localPosition = start + new Vector3(voxelsize * x, voxelsize * y, voxelsize * z);
						
						Vector3 worldPosition = transform.localToWorldMatrix.MultiplyPoint3x4(localPosition);
						worldPosition = targetgenerator_localtoworldmatrix.MultiplyPoint3x4(localPosition);

						Vector3 generatorPosition = targetgenerator_worldtolocalmatrix.MultiplyPoint3x4(worldPosition);


						var output = new NativeVoxelModificationData();
						output.Depth = (byte)Depth;
						output.X = generatorPosition.x;
						output.Y = generatorPosition.y;
						output.Z = generatorPosition.z;

						int ID = 0;

						Vector3 terrainPosition = TerrainToConvertWorld2Local.MultiplyPoint3x4(worldPosition);

						if (Mode == EvaluationMode.DominantTexture)
						{
							ID = ReadDominantTextureValue(terrainPosition);
						}
						else if (Mode == EvaluationMode.IndividualLayer)
						{
							ID = ReadTextureLayer(terrainPosition);
						}
						else
						{
							ID = ReadValue(terrainPosition);
						}



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

			Vector3 currentposition = position;

			Vector3Int terrainposition = ConvertToAlphamapCoordinates(currentposition, TerrainToConvert.terrainData);

			float height = 0;
			if (terrainposition.x < 0 || terrainposition.x > TerrainToConvert.terrainData.heightmapResolution
				|| terrainposition.z < 0 || terrainposition.z > TerrainToConvert.terrainData.heightmapResolution)
			{
				return 0;
			}
			else
			{
				height = TerrainToConvert.terrainData.GetHeight(terrainposition.x, terrainposition.z);
			}
			 

			float voxelheight = position.y + OffsetY;
			float difference = voxelheight - height;

			ID = Mathf.Clamp((int)(255 - Mathf.Abs(difference) * FallOff), 0, 255);

			if (FillNegativeSide && difference < 0)
			{
				ID = 255;
			}

			if (FillPositiveSide && difference > 0)
			{
				ID = 255;
			}

			return ID;
		}

		public int ReadDominantTextureValue(Vector3 position)
		{
			int ID = 0;

			Vector3 currentposition = position;

			Vector3Int terrainposition = ConvertToAlphamapCoordinates(currentposition, TerrainToConvert.terrainData);

			if (terrainposition.x < 0 || terrainposition.x >= TerrainToConvert.terrainData.alphamapWidth) return 0;
			if (terrainposition.z < 0 || terrainposition.z >= TerrainToConvert.terrainData.alphamapHeight) return 0;

			float height = TerrainToConvert.terrainData.GetHeight(terrainposition.x, terrainposition.z);

			float voxelheight = position.y;
			float difference = voxelheight - height;

			int textureCount = TerrainAlphamaps.GetLength(2);

			int mostDominantTextureIndex = 0;
			float greatestTextureWeight = float.MinValue;

			for (int textureIndex = 0; textureIndex < textureCount; textureIndex++)
			{
				float textureWeight = TerrainAlphamaps[terrainposition.z, terrainposition.x, textureIndex];

				if (textureWeight > greatestTextureWeight)
				{
					greatestTextureWeight = textureWeight;
					mostDominantTextureIndex = textureIndex;
				}
			}

			ID = Mathf.Clamp((int)(mostDominantTextureIndex * TextureMultiplier * greatestTextureWeight - Mathf.Abs(difference) * FallOff), 0, 255);


			return ID;
		}

		public int ReadTextureLayer(Vector3 position)
		{
			int ID = 0;

			Vector3 currentposition = position;

			Vector3Int terrainposition = ConvertToAlphamapCoordinates(currentposition, TerrainToConvert.terrainData);

			if (terrainposition.x < 0 || terrainposition.x >= TerrainToConvert.terrainData.alphamapWidth) return 0;
			if (terrainposition.z < 0 || terrainposition.z >= TerrainToConvert.terrainData.alphamapHeight) return 0;

			float height = TerrainToConvert.terrainData.GetHeight(terrainposition.x, terrainposition.z);

			float voxelheight = position.y;
			float difference = voxelheight - height;

			int textureCount = TerrainAlphamaps.GetLength(2);

			if (TargetTextureLayer >= 0 && TargetTextureLayer < textureCount)
			{
				float textureWeight = TerrainAlphamaps[terrainposition.z, terrainposition.x, TargetTextureLayer];
				ID = Mathf.Clamp((int)(textureWeight * TextureMultiplier - Mathf.Abs(difference) * FallOff), 0, 255);
			}

			return ID;
		}


		Vector3Int ConvertToAlphamapCoordinates(Vector3 _worldPosition, TerrainData data)
		{
			Vector3 relativePosition = _worldPosition;

			return new Vector3Int
			(
				x: Mathf.RoundToInt(relativePosition.x / data.size.x * data.alphamapWidth),
				y: 0,
				z: Mathf.RoundToInt(relativePosition.z / data.size.z * data.alphamapHeight)
			);
		}

		public override Bounds CalculateBounds()
		{
			if (TerrainToConvert == null) return new Bounds();

			Bounds bound = new Bounds();

			bound.min = Vector3.zero - new Vector3(0, BottomExtension, 0);
			Vector3 size = TerrainToConvert.terrainData.bounds.size;
			size.y += TopExtension;
			bound.max = size;

			bound.center += transform.localPosition;

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
