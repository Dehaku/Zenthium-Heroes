using System.Collections;
using UnityEngine;
using Unity.Collections;
using System;
using Fraktalia.Core.FraktaliaAttributes;
using Unity.Jobs;
using Fraktalia.Core.Collections;


#if UNITY_EDITOR
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
    [System.Serializable]
	public unsafe class SurfaceModifier_VertexColor : SurfaceModifier
	{
		public bool CalculateBarycentricColors;
		public bool Additive;
		public Color InitialColor = Color.white;

		[PropertyKey("Color channels:", false)]
		[Range(-1, 4)]
		public int TextureDimensionRed = -1;

		[Range(-1, 4)]
		public int TextureDimensionGreen = -1;

		[Range(-1, 4)]
		public int TextureDimensionBlue = -1;

		[Range(-1, 4)]
		public int TextureDimensionAlpha = -1;
		[Space]
		public float TexturePowerRed = 1;
		public float TexturePowerGreen = 1;
		public float TexturePowerBlue = 1;
		public float TexturePowerAlpha = 1;

		public JobHandle[] handles;
		public HullGenerationResult_ColorJob[] jobs;
		protected override void initializeModule()
		{
			handles = new JobHandle[HullGenerator.CurrentNumCores];
			jobs = new HullGenerationResult_ColorJob[HullGenerator.CurrentNumCores];
		}


		public override IEnumerator beginCalculationasync(float cellSize, float voxelSize)
		{
			for (int coreindex = 0; coreindex < HullGenerator.activeCores; coreindex++)
			{
				int cellIndex = HullGenerator.activeCells[coreindex];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.RequiresNonGeometryData) continue;

				if (HullGenerator.DebugMode)
					Debug.Log("Modify Geometry");

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];

				jobs[coreindex].verticeArray = data.verticeArray;
				jobs[coreindex].colorArray = data.colorArray;

				if (TextureDimensionRed != -1 && TextureDimensionRed < HullGenerator.engine.Data.Length)
				{
					jobs[coreindex].texturedata_red = HullGenerator.engine.Data[TextureDimensionRed];
				}

				if (TextureDimensionGreen != -1 && TextureDimensionGreen < HullGenerator.engine.Data.Length)
				{
					jobs[coreindex].texturedata_green = HullGenerator.engine.Data[TextureDimensionGreen];
				}

				if (TextureDimensionBlue != -1 && TextureDimensionBlue < HullGenerator.engine.Data.Length)
				{
					jobs[coreindex].texturedata_blue = HullGenerator.engine.Data[TextureDimensionBlue];
				}

				if (TextureDimensionAlpha != -1 && TextureDimensionAlpha < HullGenerator.engine.Data.Length)
				{
					jobs[coreindex].texturedata_alpha = HullGenerator.engine.Data[TextureDimensionAlpha];
				}

				jobs[coreindex].TexturePowerRed = TexturePowerRed;
				jobs[coreindex].TexturePowerGreen = TexturePowerGreen;
				jobs[coreindex].TexturePowerBlue = TexturePowerBlue;
				jobs[coreindex].TexturePowerAlpha = TexturePowerAlpha;

				jobs[coreindex].CalculateBarycentricColors = CalculateBarycentricColors ? 1 : 0;
				jobs[coreindex].Additive = Additive ? 1 : 0;


				jobs[coreindex].InitialColor = InitialColor;


				handles[coreindex] = jobs[coreindex].Schedule();
			}

			for (int i = 0; i < HullGenerator.activeCores; i++)
			{
				while (!handles[i].IsCompleted)
				{
					if (HullGenerator.synchronitylevel < 0) break;
					yield return new YieldInstruction();
				}
				handles[i].Complete();
			}

			yield return null;
		}

		public struct HullGenerationResult_ColorJob : IJob
		{
			[NativeDisableParallelForRestriction]
			public NativeVoxelTree texturedata_red;

			[NativeDisableParallelForRestriction]
			public NativeVoxelTree texturedata_green;

			[NativeDisableParallelForRestriction]
			public NativeVoxelTree texturedata_blue;

			[NativeDisableParallelForRestriction]
			public NativeVoxelTree texturedata_alpha;

			public float TexturePowerRed;
			public float TexturePowerGreen;
			public float TexturePowerBlue;
			public float TexturePowerAlpha;

			public FNativeList<Vector3> verticeArray;
			public FNativeList<Color> colorArray;

			public int CalculateBarycentricColors;
			public int Additive;
			public Color InitialColor;


			public void Execute()
			{
				int count = verticeArray.Length;
				
				if (CalculateBarycentricColors == 1)
				{
					colorArray.Clear();
					for (int i = 0; i < count; i += 3)
					{
						colorArray.Add(new Color(1, 0, 0));
						colorArray.Add(new Color(0, 1, 0));
						colorArray.Add(new Color(0, 0, 1));
					}
				}
				else
				{
					if ((colorArray.Length != verticeArray.Length))
					{
						colorArray.Clear();
					}

					float powerRed = (float)TexturePowerRed / 256.0f;
					float powerGreen = (float)TexturePowerGreen / 256.0f;
					float powerBlue = (float)TexturePowerBlue / 256.0f;
					float powerAlpha = (float)TexturePowerAlpha / 256.0f;

					if (colorArray.Length == 0)
					{
						for (int i = 0; i < count; i++)
						{
							Vector3 vertex = verticeArray[i];

							Color color = InitialColor;
							if (texturedata_red.IsCreated)
							{
								color.r += texturedata_red._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerRed;
							}

							if (texturedata_green.IsCreated)
							{
								color.g += texturedata_green._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerGreen;
							}

							if (texturedata_blue.IsCreated)
							{
								color.b += texturedata_blue._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerBlue;
							}

							if (texturedata_alpha.IsCreated)
							{
								color.a += texturedata_alpha._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerAlpha;
							}

							if (colorArray.Length != verticeArray.Length)
								colorArray.Add(color);
						}
					}
					else
					{
						for (int i = 0; i < count; i++)
						{
							Vector3 vertex = verticeArray[i];

							Color color = InitialColor;
							if (Additive == 1)
							{
								color = colorArray[i];
							}

							if (texturedata_red.IsCreated)
							{
								color.r += texturedata_red._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerRed;
							}

							if (texturedata_green.IsCreated)
							{
								color.g += texturedata_green._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerGreen;
							}

							if (texturedata_blue.IsCreated)
							{
								color.b += texturedata_blue._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerBlue;
							}

							if (texturedata_alpha.IsCreated)
							{
								color.a += texturedata_alpha._PeekVoxelId(vertex.x, vertex.y, vertex.z, 10, 0) * powerAlpha;
							}


							colorArray[i] = color;
						}

						
					}

				}



			}

		}


		internal override ModularUniformVisualHull.WorkType EvaluateWorkType(int dimension)
		{
			if (dimension == TextureDimensionRed || dimension == TextureDimensionGreen || dimension == TextureDimensionBlue || dimension == TextureDimensionAlpha)
				return ModularUniformVisualHull.WorkType.RequiresNonGeometryData;
		
			return ModularUniformVisualHull.WorkType.Nothing;
		}

		internal override void GetFractionalGeoChecksum(ref ModularUniformVisualHull.FractionalChecksum fractional, SurfaceModifierContainer container)
		{
			float sum = TexturePowerRed + TexturePowerGreen + TexturePowerBlue + TexturePowerAlpha + TextureDimensionRed + TextureDimensionGreen + TextureDimensionBlue + TextureDimensionAlpha;
			sum += (CalculateBarycentricColors ? 1 : 0) + InitialColor.r + InitialColor.g + InitialColor.b + InitialColor.a;
			fractional.nongeometryChecksum += sum + (container.Disabled ? 0 : 1) + (Additive ? 1 : 0);
		}
	}
}
