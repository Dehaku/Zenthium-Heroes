using UnityEngine;
using Fraktalia.Core.FraktaliaAttributes;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class ColliderToVoxel : ProceduralVoxelModifier
	{


		[BeginInfo("COLLIDERTOVOXEL")]
		[InfoTitle("Collider to Voxel", "This script was requested by customers and allows you to convert Unity Colliders into a Voxel representation. " +
			"Attaching one or more mesh colliders to this game object allows you to convert a mesh into a voxel representation.", "COLLIDERTOVOXEL")]
		[InfoSection1("How to use:", "Attach as many colliders as you want to this component. Then either call ApplyProceduralModifier() via code or click on the Apply Procedural Modifier button" +
			"\n\nIn order to apply this script, the attached colliders should at least be located inside the boundary of the target voxel generator." +
			"\n\nWhen this object is selected, the dark region around the collider shows the influene to the voxel map." +
			"\n\nThe maximum amount of voxels which can be modified is set to 5000000." +
			"\n\nWhen using Mesh Collider, it is recommended to have Convex set to true. The internal collision check from Unity3D does not behave as it should when Convex is not flagged." +
			" Therefore if you Mesh is concave, you have to split the mesh up into concave parts." +
			"\n\n<b>Keep in mind that voxel modifier and game objects can collide with the applied colliders.</b>", "COLLIDERTOVOXEL")]
		[InfoVideo("https://youtu.be/7mYdDT3CUO4", false, "COLLIDERTOVOXEL")]
		[InfoText("Collider To Voxel:", "COLLIDERTOVOXEL")]

		[Tooltip("Voxel will be completely solid if distance between voxel position and nearest collision contact point is smaller. Ideal value = 0.001f")]
		public float tolerance = 0.001f;

		[Tooltip("If Voxel Position is not touching or inside the collider, fallof is applied to smooth the surface. Large values make the result blocky, Smaller values increases the smoothness. " +
			"Ideal value = 10000, 2000 = Very Smooth, 20000 = Very sharp-blocky")]
		public float falloffdist = 2000;

		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

		public Collider[] colliders;

		public override void EvaluateVoxelInfo(Vector3 start, Vector3 end)
		{
			Vector3 voxelPosition;
			for (voxelPosition.x = start.x; voxelPosition.x <= end.x; voxelPosition.x += voxelsize)
			{
				for (voxelPosition.y = start.y; voxelPosition.y <= end.y; voxelPosition.y += voxelsize)
				{
					for (voxelPosition.z = start.z; voxelPosition.z <= end.z; voxelPosition.z += voxelsize)
					{
						Vector3 localPosition = voxelPosition + Vector3.one * halfvoxelsize;
						Vector3 worldPos = targetgenerator_localtoworldmatrix.MultiplyPoint3x4(localPosition);
						var output = new NativeVoxelModificationData();
						output.Depth = (byte)Depth;

						int ID = IsPointWithinCollider(worldPos);

						output.ID = (int)(ID * finalMultiplier);

						output.X = localPosition.x;
						output.Y = localPosition.y;
						output.Z = localPosition.z;

						ProceduralVoxelData.Add(output);
					}
				}
			}
		}

		public override Bounds CalculateBounds()
		{
			colliders = GetComponentsInChildren<Collider>();

			Bounds bound = new Bounds();
			bound.max = new Vector3();
			bound.min = new Vector3();

			for (int i = 0; i < colliders.Length; i++)
			{
				Vector3 max = colliders[i].bounds.max;
				Vector3 min = colliders[i].bounds.min;

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


			bound.min = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(bound.min);
			bound.max = TargetGenerator.transform.worldToLocalMatrix.MultiplyPoint3x4(bound.max);
			bound.size = bound.size * 1.1f;

			return bound;
		}

		public byte IsPointWithinCollider(Vector3 point)
		{

			float dist2 = float.MaxValue;

			for (int i = 0; i < colliders.Length; i++)
			{
				dist2 = Mathf.Min(dist2, (colliders[i].ClosestPoint(point) - point).sqrMagnitude);
			}

			float toleranceinvariant = tolerance * (blocksize / 40);

			if (dist2 < toleranceinvariant * toleranceinvariant)
			{
				return 255;
			}
			else
			{
				float rest = dist2 - voxelsize * toleranceinvariant * toleranceinvariant;

				int value = 255 - (int)(rest * (falloffdist / (blocksize / 40)));

				return (byte)Mathf.Clamp(value, 0, 255);
			}



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

			if (voxels > 500000 || voxels < 0)
			{
				ErrorMessage = "This setup would modify more than 500000 Voxels. You do not want to freeze Unity";



				issave = false;
			}


			MeshCollider[] meshcolliders = GetComponentsInChildren<MeshCollider>();
			for (int i = 0; i < meshcolliders.Length; i++)
			{
				if (!meshcolliders[i].convex)
				{
					ErrorMessage = "Mesh Collider " + meshcolliders[i].name + " is not set convex. Concave is not supported";
					issave = false;
				}
			}

			return issave;
		}

	}
}
