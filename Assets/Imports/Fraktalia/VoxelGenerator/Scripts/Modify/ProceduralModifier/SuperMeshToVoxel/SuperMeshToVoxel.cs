using UnityEngine;
using Unity.Collections;
using UnityEngine.SocialPlatforms;
using Fraktalia.Core.FraktaliaAttributes;
using Unity.Jobs;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class SuperMeshToVoxel : ProceduralVoxelModifier
	{
		public enum DetectionMode
		{
			BruteForce,
			ScanLines
		}

		[BeginInfo("SUPERMESHVOXEL")]
		[InfoTitle("Super Mesh to Voxel", "This script allows the conversion from mesh into voxel. " +
			"Attaching one or more mesh filters to this game object allows you to convert a mesh into a voxel representation.", "SUPERMESHVOXEL")]
		[InfoSection1("How to use:", "Attach as many mesh filter as you want to this component. Then either call ApplyProceduralModifier() via code or click on the Apply Procedural Modifier button" +
			"\n\nIn order to apply this script, the attached mesh filters should at least be located inside the boundary of the target voxel generator." +
			"\n\nWhen this object is selected, the dark region around the collider shows the influene to the voxel map." +
			"\n\nConcave objects, even highly complex ones are possible. Self intersecting meshes, open meshes and bad optimized meshes can reduce accuracy." +
			"\n\n - Smoothing: Smoothes out the result" +
			"\n\n - <b>Mode:</b> Defines algorithm used. Which one is better highly depending on the models used." +
			"\nBruteforce is more accurate and faster on simple meshes but is slow on complex models. Is not accurate on highly concave meshes. Good result on self intersecting and bad geometry" +
			"\nScanlines has better handling on concave meshes and if the mesh has a high vertex count. Self intersecting mesh geometry will create hollow results." +
			"\n\nThis procedural modifier is not supposed to be applied in real time especially with complex 3D models as the computation is quite expensive on high resolutions. " +
			"Increasing the depth increases the accuracy as more sample points are used but it may take so long that you can drink a coffee until it is finished. " +
			"For scientific accuracy like in medical science, converting scanned Textures from Computer Tomography into Voxels is recommended. " +
			"\n\n Low Polygonal models can be converted in real time. The Error message shows the border of realtime feasibility.", "SUPERMESHVOXEL")]		
		[InfoText("Super Mesh To Voxel", "SUPERMESHVOXEL")]

		[InfoVideo("https://youtu.be/qWQf8VmfVd8", false, "SUPERMESHVOXEL")]



		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

		[Range(0, 2)]
		public int Smoothing;

		public DetectionMode Mode;

		public MeshFilter[] colliders;


		public void Initialize()
		{

		}

		public override void OnDrawGizmosSelected()
		{
			if (TargetGenerator)
			{
				targetgenerator_localtoworldmatrix = TargetGenerator.transform.localToWorldMatrix;
				targetgenerator_worldtolocalmatrix = TargetGenerator.transform.worldToLocalMatrix;


				Bounds bound = CalculateBounds();

				Gizmos.color = new Color32(0, 0, 0, 30);
				if (Mode == DetectionMode.ScanLines)
				{
					Gizmos.DrawCube(bound.center, bound.size);
				}
				else
				{
					Gizmos.DrawCube(TargetGenerator.transform.localToWorldMatrix.MultiplyPoint3x4(bound.center), bound.size);
				}

				float RootSize = TargetGenerator.RootSize;
				Gizmos.color = Color.blue;
				Gizmos.matrix = TargetGenerator.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));
			}



		}

		public override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
			if (colliders != null)
			{
				for (int i = 0; i < colliders.Length; i++)
				{
					if (colliders[i])
					{
						if (colliders[i].sharedMesh)
						{
							Gizmos.color = new Color32(128, 128, 128, 128);
							Gizmos.matrix = colliders[i].transform.localToWorldMatrix;
							Gizmos.DrawWireMesh(colliders[i].sharedMesh);

						}
					}
				}
			}
		}


		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			if (Mode == DetectionMode.ScanLines)
			{
				NativeArray<NativeVoxelModificationData> changedata = new NativeArray<NativeVoxelModificationData>(boundaryvoxelcount, Allocator.TempJob);
				SuperMeshToVoxel_FillArray firststep = new SuperMeshToVoxel_FillArray();
				firststep.blocklengths = boundaryvoxelsize;
				firststep.changedata = changedata;
				firststep.HalfVoxelSize = halfvoxelsize;
				firststep.VoxelSize = voxelsize;
				firststep.GeneratorWorldToLocal = targetgenerator_worldtolocalmatrix;
				firststep.Start = start;
				firststep.End = end;
				firststep.Depth = (byte)Depth;
				firststep.finalMultiplier = finalMultiplier;
				JobHandle handle = firststep.Schedule();
				handle.Complete();

				for (int i = 0; i < colliders.Length; i++)
				{



					SuperMeshToVoxel_NewTreebuilder calculation = new SuperMeshToVoxel_NewTreebuilder();
					calculation.blocklengths = boundaryvoxelsize;
					calculation.HalfVoxelSize = halfvoxelsize;
					calculation.VoxelSize = voxelsize;
					calculation.GeneratorLocalToWorld = targetgenerator_localtoworldmatrix;
					calculation.GeneratorWorldToLocal = targetgenerator_worldtolocalmatrix;
					calculation.Start = start;
					calculation.End = end;
					calculation.Depth = (byte)Depth;
					calculation.finalMultiplier = finalMultiplier;
					calculation.mesh = new NativeMesh(colliders[i].sharedMesh);
					calculation.mesh.WorldToLocal = colliders[i].transform.localToWorldMatrix;
					calculation.Initialize();

					handle = calculation.Schedule();
					handle.Complete();


					SuperMeshToVoxel_New calculation2 = new SuperMeshToVoxel_New();
					calculation2.blocklengths = boundaryvoxelsize;
					calculation2.changedata = changedata;
					calculation2.tree = calculation.tree;
					calculation2.HalfVoxelSize = halfvoxelsize;
					calculation2.VoxelSize = voxelsize;
					calculation2.GeneratorLocalToWorld = targetgenerator_localtoworldmatrix;
					calculation2.GeneratorWorldToLocal = targetgenerator_worldtolocalmatrix;
					calculation2.Start = start;
					calculation2.End = end;
					calculation2.Depth = (byte)Depth;
					calculation2.finalMultiplier = finalMultiplier;
					calculation2.mesh = new NativeMesh(colliders[i].sharedMesh);
					calculation2.mesh.WorldToLocal = colliders[i].transform.localToWorldMatrix;
					calculation2.Initialize();

					handle = calculation2.Schedule(boundaryvoxelsize.x * boundaryvoxelsize.y, boundaryvoxelsize.x);
					handle.Complete();


					if (Smoothing > 0)
					{
						ResultSmoothFilter.Apply(calculation2.changedata, boundaryvoxelsize.x, boundaryvoxelsize.y, boundaryvoxelsize.z, Smoothing);
					}


					ProceduralVoxelData.AddRange(calculation2.changedata);


					calculation2.CleanUp();
				}


				ProceduralVoxelData.AddRange(changedata);
				changedata.Dispose();

			}
			else
			{


				SuperMeshToVoxel_Calculation calculation = new SuperMeshToVoxel_Calculation();
				calculation.blocklengths = boundaryvoxelsize;
				calculation.changedata = new NativeArray<NativeVoxelModificationData>(boundaryvoxelcount, Allocator.Persistent);
				calculation.HalfVoxelSize = halfvoxelsize;
				calculation.VoxelSize = voxelsize;
				calculation.GeneratorLocalToWorld = targetgenerator_localtoworldmatrix;
				calculation.Start = start;
				calculation.Depth = (byte)Depth;
				calculation.Smoothing = 0;

				calculation.finalMultiplier = finalMultiplier;
				calculation.EvaluationMode = (int)Mode;

				calculation.meshes = new NativeArray<NativeMesh>(colliders.Length, Allocator.Persistent);
				for (int i = 0; i < calculation.meshes.Length; i++)
				{
					NativeMesh mesh = new NativeMesh(colliders[i].sharedMesh);
					mesh.WorldToLocal = colliders[i].transform.worldToLocalMatrix;
					calculation.meshes[i] = mesh;
				}

				JobHandle handle = calculation.Schedule(boundaryvoxelcount, 64);
				handle.Complete();

				if (Smoothing > 0)
				{
					ResultSmoothFilter.Apply(calculation.changedata, boundaryvoxelsize.x, boundaryvoxelsize.y, boundaryvoxelsize.z, Smoothing);
				}

				ProceduralVoxelData.AddRange(calculation.changedata);

				calculation.CleanUp();
			}
		}

		public override Bounds CalculateBounds()
		{
			colliders = GetComponentsInChildren<MeshFilter>();

			Bounds bound = new Bounds();
			bound.max = new Vector3();
			bound.min = new Vector3();

			for (int i = 0; i < colliders.Length; i++)
			{
				Bounds box = CalculateRotatedBounds(colliders[i].sharedMesh, colliders[i].transform.rotation);


				Vector3 max = box.max;
				Vector3 min = box.min;

				min.Scale(colliders[i].transform.lossyScale);
				max.Scale(colliders[i].transform.lossyScale);


				min += colliders[i].transform.position;
				max += colliders[i].transform.position;

				if (i == 0)
				{
					bound.max = max;
					bound.min = min;
				}
				else
				{
					bound.max = Vector3.Max(bound.max, max);
					bound.min = Vector3.Min(bound.min, min);
				}
			}

			if (Mode != DetectionMode.ScanLines)
			{
				bound.min = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(bound.min);
				bound.max = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(bound.max);


			}

			bound.size = bound.size + Vector3.one * 5 * TargetGenerator.GetVoxelSize(Depth);

			return bound;
		}

		public override bool IsSave()
		{
			ErrorMessage = "";
			bool issave = true;

			Bounds bound = CalculateBounds();
			voxelsize = TargetGenerator.GetVoxelSize(Depth);
			int voxellength_x = (int)(bound.size.x / voxelsize);
			int voxellength_y = (int)(bound.size.y / voxelsize);
			int voxellength_z = (int)(bound.size.z / voxelsize);
			int voxels = voxellength_x * voxellength_y * voxellength_z;

			colliders = GetComponentsInChildren<MeshFilter>();
			if (voxels <= 0 || voxels > 300000)
			{
				issave = false;
				ErrorMessage += "\n - This setup would modify more than 300000 Voxels.";
			}

			for (int i = 0; i < colliders.Length; i++)
			{
				if (colliders[i].sharedMesh)
				{
					int vertex = colliders[i].sharedMesh.vertexCount;

					if (vertex > 10000)
					{
						ErrorMessage += "\n - Mesh attached to " + colliders[i].name + " is too complex. (Vertex Count > 10000)";
						issave = false;
					}
				}
			}

			ErrorMessage += "\n";

			if (!issave)
			{
				ErrorMessage += "\n These settings are highly complex and may freeze Unity for quite a while. So be careful what you do.";
			}

			return issave;

		}

		private Bounds CalculateRotatedBounds(Mesh mesh, Quaternion rotation)
		{
			var vertices = mesh.vertices;

			Bounds result = new Bounds();

			Vector3 maximum = Vector3.zero;
			Vector3 minimum = Vector3.zero;



			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vertex = rotation * vertices[i];
				maximum = Vector3.Max(maximum, vertex);
				minimum = Vector3.Min(minimum, vertex);


			}

			result.min = minimum;
			result.max = maximum;

			return result;

		}


	}
}
