using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.VoxelGen;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class TextureToVoxel : ProceduralVoxelModifier
	{
		public enum TextureChannel
		{
			Red,
			Green,
			Blue,
			Alpha
		}


		[HideInInspector]
		public float PositionMultiplier = 1;

		[BeginInfo("TEXTURETOVOXEL")]
		[InfoTitle("Texture to Voxel Converter", "This script allows the conversion of texture slices into voxel. " +
			"You can create 3D objects out of images by filling the Slices list with textures. " +
			"Every slice adds exactly one voxel layer and the summary will create a 3D model and is perfectly suited for converting 2D scan images into a 3D object. " +
			"The perfect usage for this script is to convert 2D scans especially medical <b>DICOM</b> scans into a volumetric 3D object. " +
			"Any image or texture can be used as long as it can be read by Unity and coverted into a Texture2D." +
			"\n\n<b><color=red>Image data like medical DICOM which cannot be read must be converted to Texture2D with your own scripts due to legal reasons.</color></b>", "TEXTURETOVOXEL")]
		[InfoSection1("How to use:", "Fill the Slices list with Textures either manually or automatically by third party scripts. " +
			"Then call ApplyProceduralModifier() via code or click on the Apply Procedural Modifier button." +
			"\n\n - The green box shows the region which will be modified and should be inside the boundary of the voxel generator." +
			"\n - Change the boundary multiplier or the depth value to increase/decrease the box size" +
			"\n - You can also decide which texture channel should be read." +
			"\n - Smoothing: Smoothes out the result" +
			"\n\nAny texture type can be used but the preferred type is <b>RGBA32</b>. Other types are converted automatically when applied which costs extra computation time. " +
			"Therefore for real time usage, the used Textures should already be in the correct format. All textures must have the same dimensions." +
			"\n\nThe save limit is set to 50 million voxels as the 3D reconstruction is incredible fast and accurate. " +
			"\nIf you see no results, try to increase the resolution of the hull generator by increasing the Width or Cell Subdivision.", "TEXTURETOVOXEL")]
		[InfoText("Texture To Voxel", "TEXTURETOVOXEL")]
		public float BoundaryMultiplier = 1;

		public List<Texture2D> Slices = new List<Texture2D>();
		public TextureChannel ChannelToRead;


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
				//Gizmos.DrawCube(bound.center, bound.size);

				PositionMultiplier = 1 / BoundaryMultiplier;

				if (!(Slices == null || Slices.Count == 0 || Slices[0] == null))
				{
					Gizmos.color = Color.yellow;




					Vector3 slicebounds = TargetGenerator.GetVoxelSize(Depth) * new Vector3(Slices[0].width, Slices.Count, Slices[0].height) / PositionMultiplier;

					Gizmos.DrawWireCube(slicebounds / 2, slicebounds);
				}



				Gizmos.matrix = Matrix4x4.identity;

				float RootSize = TargetGenerator.RootSize;
				Gizmos.color = Color.blue;
				Gizmos.matrix = TargetGenerator.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));



			}
		}

		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			Queue<Texture2D> clonedtextures = new Queue<Texture2D>();
			List<Texture2D> correcttextures = new List<Texture2D>();
			for (int i = 0; i < Slices.Count; i++)
			{

				if (Slices[i].format == TextureFormat.RGBA32)
				{
					correcttextures.Add(Slices[i]);
				}
				else
				{
					RenderTexture rtex = new RenderTexture(Slices[i].width, Slices[i].height, 0);
					Graphics.Blit(Slices[i], rtex);

					Texture2D tex = new Texture2D(Slices[i].width, Slices[i].height, TextureFormat.RGBA32, false);
					tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0, false);
					tex.Apply();

					correcttextures.Add(tex);
					clonedtextures.Enqueue(tex);

				}

			}


			Vector3 offset = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);

			TextureToVoxel_Calculation calculation = new TextureToVoxel_Calculation();

			calculation.blocklengths = boundaryvoxelsize;
			calculation.changedata = new NativeArray<NativeVoxelModificationData>(boundaryvoxelcount, Allocator.Persistent);
			calculation.HalfVoxelSize = halfvoxelsize;
			calculation.VoxelSize = voxelsize;
			calculation.GeneratorLocalToWorld = targetgenerator_localtoworldmatrix;
			calculation.GeneratorWorldToLocal = targetgenerator_worldtolocalmatrix;
			calculation.Start = start-offset;
			calculation.Depth = (byte)Depth;
			calculation.Smoothing = 0;
			calculation.localToWorldMatrix = transform.localToWorldMatrix;
			calculation.finalMultiplier = finalMultiplier;
			calculation.TextureWidth = Slices[0].width;
			calculation.TextureHeight = Slices[0].height;
			calculation.ChannelToRead = (int)ChannelToRead;
			calculation.BuildSlices(correcttextures);
			calculation.PositionMultiplier = PositionMultiplier;
			JobHandle handle = calculation.Schedule(boundaryvoxelcount, 64);
			handle.Complete();

			if (Smoothing > 0)
			{
				ResultSmoothFilter.Apply(calculation.changedata, boundaryvoxelsize.x, boundaryvoxelsize.y, boundaryvoxelsize.z, Smoothing);
			}

			ProceduralVoxelData.AddRange(calculation.changedata);

			calculation.CleanUp();

			while (clonedtextures.Count > 0)
			{
				DestroyImmediate(clonedtextures.Dequeue());
			}


		}


		public override Bounds CalculateBounds()
		{
			if (Slices == null || Slices.Count == 0) return new Bounds();
			if (Slices[0] == null) return new Bounds();

			Bounds bound = new Bounds();
			Vector3 offset = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);

			bound.min = Vector3.zero + offset;
			bound.max = TargetGenerator.GetVoxelSize(Depth) * new Vector3(Slices[0].width, Slices.Count, Slices[0].height) * BoundaryMultiplier + offset;

			

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
			ErrorMessage = "";
			if (voxels > 50000000 || voxels < 0)
			{
				ErrorMessage += "This setup would modify more than 50000000 Voxels. You do not want to freeze Unity \n";



				issave = false;
			}

			int width = 0;
			int height = 0;
			for (int i = 0; i < Slices.Count; i++)
			{
				if (Slices[i] == null)
				{
					ErrorMessage += "Slice " + i + " is null. This error cannot be ignored as it would crash Unity.\n";
					IgnoreErrors = false;
					issave = false;
					continue;
				}

				if (i == 0)
				{
					width = Slices[i].width;
					height = Slices[i].height;
				}
				else
				{
					if (width != Slices[i].width || height != Slices[i].height)
					{
						ErrorMessage += "Slice " + i + " has different width or height than the first slice. This error cannot be ignored as it would crash Unity.\n";
						IgnoreErrors = false;
						issave = false;
					}
				}

			}


			return issave;
		}
	}
}
