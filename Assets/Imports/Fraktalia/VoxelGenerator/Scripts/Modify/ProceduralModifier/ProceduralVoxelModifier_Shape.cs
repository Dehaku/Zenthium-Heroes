using UnityEngine;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	/// <summary>
	/// TODO CREATE BRUSH LIBRARY USING SHAPES, STAR SHAPE, SPHERES, Zylinders
	/// </summary>
	public class ProceduralVoxelModifier_Shape : ProceduralVoxelModifier
	{
		public Vector3 BoundarySize = new Vector3(1, 1, 1);

		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

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



						int ID = 255;


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

			Bounds bound = new Bounds();

			bound.center = targetgenerator_worldtolocalmatrix.MultiplyPoint3x4(transform.position);
			bound.size = BoundarySize;

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
