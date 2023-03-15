using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.VoxelGen;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify.Procedural
{
	public class BorderVoxelRemoval : ProceduralVoxelModifier
	{
		[BeginInfo("BORDER")]
		[InfoTitle("Border Removal", "This script creates a simple voxel block in specific region defined by the black outline. " +
			"Main usage is to initialize a clean voxel block with solid measurement (Voxel Generator only has full solid or empty). " +
			"People often asked about how to create a block with defined size. Use this script to initialize the block with your desired dimensions.", "BORDER")]
		[InfoSection1("How to use:", "Define the size of the block which is shown by the dark region and hit the apply button. " +
			"\n\n - The Offset Y changes the height position of the surface." +
			"\n\n - Target texture layer defines which texture layer of the terrain should be read when mode is set to individual layer.", "BORDER")]
		[InfoText("Border Removal:", "BORDER")]

		public bool UsePadding = false;
		public Vector3 Padding_Min = new Vector3(1, 1, 1);
		public Vector3 Padding_Max = new Vector3(1, 1, 1);

		public Vector3 Size;
		public Vector3 Pivot = new Vector3(0.5f,0.5f,0.5f);

		[Tooltip("Final Multiplier of the modifier. Negative values would inverse the result")]
		public float finalMultiplier = 1;

		public override void OnDrawGizmosSelected()
		{
			base.OnDrawGizmosSelected();



			if (!TargetGenerator) return;
			float RootSize = TargetGenerator.RootSize;

			if (!UsePadding)
			{
				Vector3 Padding = (new Vector3(RootSize, RootSize, RootSize) - Size);
				Padding_Min = Vector3.Scale(Padding, Pivot);
				Padding_Max = Vector3.Scale(Padding, Vector3.one - Pivot);
			}
			else
			{



				Size = new Vector3(RootSize, RootSize, RootSize) - Padding_Min - Padding_Max;
				Vector3 sum = Padding_Min + Padding_Max;


				Pivot.x = Padding_Min.x / sum.x;
				Pivot.y = Padding_Min.y / sum.y;
				Pivot.z = Padding_Min.z / sum.z;

				if (sum.x.Equals(0)) Pivot.x = 0.5f;
				if (sum.y.Equals(0)) Pivot.y = 0.5f;
				if (sum.z.Equals(0)) Pivot.z = 0.5f;




			}

			Gizmos.color = new Color32(64, 64, 64, 64);
			Gizmos.DrawCube(new Vector3(RootSize, RootSize, RootSize) / 2 - Padding_Min / 2 + Padding_Max / 2, new Vector3(RootSize, RootSize, RootSize) - Padding_Max - Padding_Min);
		}

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

						int ID = 0;




						if (
						voxelPosition.x <= blocksize - Padding_Min.x && voxelPosition.x >= Padding_Max.x &&
						voxelPosition.y <= blocksize - Padding_Min.y && voxelPosition.y >= Padding_Max.y &&
						voxelPosition.z <= blocksize - Padding_Min.z && voxelPosition.z >= Padding_Max.z)
						{
							ID = 255;
						}

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
			bound.min = Vector3.zero;
			bound.max = TargetGenerator.RootSize * Vector3.one;
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
