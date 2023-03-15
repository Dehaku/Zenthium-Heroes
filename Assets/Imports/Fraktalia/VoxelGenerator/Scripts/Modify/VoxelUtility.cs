using System;
using Fraktalia.VoxelGen.Modify.Filter;
using Fraktalia.VoxelGen.Visualisation;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen.Modify
{
	public enum VoxelModifyMode
	{
		Set,
		Additive,
		Subtractive,
		Smooth,
		Rough,
		SolidPaint
	}
	public enum VoxelModifyShape
	{
		Sphere,
		Box,
		RoundedBox,
		Single
	}



	public static class VoxelUtility
	{
		public const int SAFETYLIMIT = 100000;

		private static SetID filterSet;
		private static Smooth filterSmooth;
		private static Rough filterRough;
		private static SolidPaint filterSolidPaint;

		/// <summary>
		/// Applies filter in spherical form
		/// </summary>
		/// <param name="engine">Target voxel block</param>
		/// <param name="center">Center (world position)</param>
		/// <param name="radius">Radius of the sphere</param>
		/// <param name="depth">Target depth</param>
		/// <param name="filter">Applied filter</param>
		public static void ModifyVoxelsSphere(VoxelGenerator generator, Vector3 center,Quaternion rotation, float radius, int depth, int ID, VoxelModifyMode mode, int dimension = 0)
		{
			if (dimension >= generator.DimensionCount) return;

			switch (mode)
			{
				case VoxelModifyMode.Set:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, radius, depth, filterSet, dimension);
					break;
				case VoxelModifyMode.Additive:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = ID;
					VoxelUtility.ApplyFilterAdditive(generator, center, rotation, radius, depth, filterSet, dimension);
					break;
				case VoxelModifyMode.Subtractive:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = -ID;
					VoxelUtility.ApplyFilterAdditive(generator, center, rotation, radius, depth, filterSet, dimension);		
					break;
				case VoxelModifyMode.Smooth:
					if (filterSmooth == null) filterSmooth = new Smooth();
					filterSmooth.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, radius, depth, filterSmooth, dimension);
					break;
				case VoxelModifyMode.Rough:
					if (filterRough == null) filterRough = new Rough();
					filterRough.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, radius, depth, filterRough, dimension);
					break;
				case VoxelModifyMode.SolidPaint:
					if (filterSolidPaint == null) filterSolidPaint = new SolidPaint();
					filterSolidPaint.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, radius, depth, filterSolidPaint, dimension);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Applies a filter to the voxel block box shape
		/// </summary>
		/// <param name="engine">Target voxel block</param>
		/// <param name="center">Center</param>
		/// <param name="size">Size of the modification</param>
		/// <param name="depth">Target depth</param>
		/// <param name="filter">Applied filter</param>
		public static void ModifyVoxelsBox(VoxelGenerator generator, Vector3 center, Quaternion rotation, Vector3 size, int depth, int ID, VoxelModifyMode mode, int dimension = 0)
		{			
			switch (mode)
			{
				case VoxelModifyMode.Set:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size,  Vector4.one * 100000, depth, filterSet, dimension);
					break;
				case VoxelModifyMode.Additive:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = ID;
					VoxelUtility.ApplyFilterAdditive(generator, center, rotation, size, Vector4.one * 100000, depth, filterSet, dimension);
					break;
				case VoxelModifyMode.Subtractive:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = -ID;
					VoxelUtility.ApplyFilterAdditive(generator, center, rotation, size, Vector4.one * 100000, depth, filterSet, dimension);					
					break;
				case VoxelModifyMode.Smooth:
					if (filterSmooth == null) filterSmooth = new Smooth();
					filterSmooth.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size, Vector4.one * 100000, depth, filterSmooth, dimension);
					break;
				case VoxelModifyMode.Rough:
					if (filterRough == null) filterRough = new Rough();
					filterRough.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size, Vector4.one * 100000, depth, filterRough, dimension);
					break;
				case VoxelModifyMode.SolidPaint:
					if (filterSolidPaint == null) filterSolidPaint = new SolidPaint();
					filterSolidPaint.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size, Vector4.one * 100000, depth, filterSolidPaint, dimension);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Applies a filter to the voxel block
		/// </summary>
		/// <param name="engine">Target voxel block</param>
		/// <param name="center">Center(world position)</param>
		/// <param name="size">Size of the modification</param>
		/// <param name="radials">Radial limits. X, Y, Z = Axis aligned radius, W = Spherical</param>
		/// <param name="depth">Target depth</param>
		/// <param name="filter">Applied filter</param>
		public static void ModifyVoxels(VoxelGenerator generator, Vector3 center, Quaternion rotation, Vector3 size, Vector4 radials, int depth, int ID, VoxelModifyMode mode, int dimension = 0)
		{
			
			switch (mode)
			{
				case VoxelModifyMode.Set:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center,rotation, size, radials, depth, filterSet, dimension);
					break;
				case VoxelModifyMode.Additive:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = ID;
					VoxelUtility.ApplyFilterAdditive(generator, center, rotation, size, radials, depth, filterSet, dimension);
					break;
				case VoxelModifyMode.Subtractive:
					if (filterSet == null) filterSet = new SetID();
					filterSet.TargetID = -ID;
					VoxelUtility.ApplyFilterAdditive(generator, center, rotation, size, radials, depth, filterSet, dimension);				
					break;
				case VoxelModifyMode.Smooth:
					if (filterSmooth == null) filterSmooth = new Smooth();
					filterSmooth.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size, radials, depth, filterSmooth, dimension);
					break;
				case VoxelModifyMode.Rough:
					if (filterRough == null) filterRough = new Rough();
					filterRough.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size, radials, depth, filterRough, dimension);
					break;
				case VoxelModifyMode.SolidPaint:
					if (filterSolidPaint == null) filterSolidPaint = new SolidPaint();
					filterSolidPaint.TargetID = ID;
					VoxelUtility.ApplyFilter(generator, center, rotation, size, radials, depth, filterSolidPaint, dimension);
					break;
				default:
					break;
			}
		}

		public static void ModifySingleVoxel(VoxelGenerator generator, Vector3 center, int depth, int ID, VoxelModifyMode mode, int dimension = 0)
		{

			center = generator.transform.InverseTransformPoint(center);
			float size = generator.GetVoxelSize(depth);

			

			switch (mode)
			{
				case VoxelModifyMode.Set:				
					generator._SetVoxel(center,  (byte)depth, (byte)ID, dimension);
					break;
				case VoxelModifyMode.Additive:					
					generator._SetVoxelAdditive(center, (byte)depth, ID, dimension);
					break;
				case VoxelModifyMode.Subtractive:
					generator._SetVoxelAdditive(center, (byte)depth, -ID, dimension);
					break;
				case VoxelModifyMode.Smooth:
					
					break;
				case VoxelModifyMode.Rough:
				
					break;
				case VoxelModifyMode.SolidPaint:
					if (generator.Data[dimension]._PeekVoxelId(center.x, center.y, center.z, (byte)depth) != 0)
					{
						generator._SetVoxel(center, (byte)depth, (byte)ID, dimension);
					}
					break;
				default:
					break;
			}
			generator.SetRegionsDirty(center, Vector3.one * size, Vector3.one * size, dimension);
			if (generator.savesystem) generator.savesystem.IsDirty = true;
		}


		/// <summary>
		/// Applies filter in spherical form
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="depth"></param>
		/// <param name="filter"></param>
		public static void ApplyFilter(VoxelGenerator engine, Vector3 center, Quaternion rotation, float radius, int depth, BaseVoxelFilter filter, int dimension = 0)
		{
			if (dimension >= engine.DimensionCount) return;

			Vector3 enginepoint = engine.transform.InverseTransformPoint(center);
			filter.CalculateFilter(engine, enginepoint,rotation, Vector3.one * radius * 2, Vector4.one * radius, depth);
			engine._SetVoxels_Inner(filter.output, dimension);
			engine.SetRegionsDirty(enginepoint, Vector3.one * radius, Vector3.one * radius, dimension);
			if (engine.savesystem) engine.savesystem.IsDirty = true;
		}

		/// <summary>
		/// Applies filter for additive modification in spherical form
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="depth"></param>
		/// <param name="filter"></param>
		public static void ApplyFilterAdditive(VoxelGenerator engine, Vector3 center, Quaternion rotation, float radius, int depth, BaseVoxelFilter filter, int dimension = 0)
		{
			if (dimension >= engine.DimensionCount) return;

			Vector3 enginepoint = engine.transform.InverseTransformPoint(center);
			filter.CalculateFilter(engine, enginepoint,rotation, Vector3.one * radius * 2, Vector4.one * radius, depth);
			engine._SetVoxelsAdditive_Inner(filter.output, dimension);
			engine.SetRegionsDirty(enginepoint, Vector3.one * radius, Vector3.one * radius, dimension);
			if (engine.savesystem) engine.savesystem.IsDirty = true;
		}

		/// <summary>
		/// Applies a filter to the voxel block
		/// </summary>
		/// <param name="engine">Target voxel block</param>
		/// <param name="center">Center</param>
		/// <param name="size">Size of the modification</param>
		/// <param name="radials">Radial limits. X, Y, Z = Axis aligned radius, W = Spherical</param>
		/// <param name="depth">Target depth</param>
		/// <param name="filter">Applied filter</param>
		public static void ApplyFilter(VoxelGenerator engine, Vector3 center, Quaternion rotation, Vector3 size, Vector4 radials, int depth, BaseVoxelFilter filter, int dimension)
		{
			Vector3 enginepoint = engine.transform.InverseTransformPoint(center);
			filter.CalculateFilter(engine, enginepoint, rotation, size, radials, depth);
			engine._SetVoxels_Inner(filter.output, dimension);

			Vector3 extents = VoxelMath.CalculateRotatedSize(engine, enginepoint, size, rotation);
			engine.SetRegionsDirty(enginepoint, extents, extents, dimension);
			if (engine.savesystem) engine.savesystem.IsDirty = true;
		}

		/// <summary>
		/// Applies a filter to the voxel block
		/// </summary>
		/// <param name="engine">Target voxel block</param>
		/// <param name="center">Center</param>
		/// <param name="size">Size of the modification</param>
		/// <param name="radials">Radial limits. X, Y, Z = Axis aligned radius, W = Spherical</param>
		/// <param name="depth">Target depth</param>
		/// <param name="filter">Applied filter</param>
		public static void ApplyFilterAdditive(VoxelGenerator engine, Vector3 center,Quaternion rotation, Vector3 size, Vector4 radials, int depth, BaseVoxelFilter filter, int dimension)
		{
			Vector3 enginepoint = engine.transform.InverseTransformPoint(center);
			filter.CalculateFilter(engine, enginepoint,rotation, size, radials, depth);
			engine._SetVoxelsAdditive_Inner(filter.output, dimension);

			Vector3 extents = VoxelMath.CalculateRotatedSize(engine, enginepoint, size, rotation);
			engine.SetRegionsDirty(enginepoint, extents, extents, dimension);
			if (engine.savesystem) engine.savesystem.IsDirty = true;
		}

		




		public static int EvaluateModificationCount(VoxelGenerator engine, float radius, int depth)
		{
			if (!engine.IsInitialized) return -1;
			if (depth >= NativeVoxelTree.MaxDepth) return -2;

			float nodesize = engine.Data[0].SizeTable[depth];

			float diameter = radius * 2;
			int modification = (int)(diameter / nodesize);

			int modifications = modification * modification * modification;
			if(modification > 0 && modifications <= 0)
            {
				return 999999999;
            }

			return Mathf.Abs(modifications);
		}
		public static int EvaluateModificationCount(VoxelGenerator engine, Vector3 size, int depth)
		{
			if (!engine.IsInitialized) return -1;
			if (depth >= NativeVoxelTree.MaxDepth) return -2;

			float nodesize = engine.Data[0].SizeTable[depth];



			
			Vector3 modification = (size / nodesize);

			int modifications = (int)(modification.x * modification.y * modification.z);
			if (modifications <= 0 && (modification.x > 0 || modification.y > 0 || modification.z > 0))
			{
				return 999999999;
			}

			return Mathf.Abs(modifications);
		}


		public static bool IsSafe(VoxelGenerator engine, float radius, int depth)
		{
			int count = EvaluateModificationCount(engine, radius, depth);
			if (count <= 0 || count >= SAFETYLIMIT)
			{
				CreateWarningInfo(engine, count);
				return false;
			}
			return true;
		}

		public static bool IsSafe(VoxelGenerator engine, Vector3 size, int depth)
		{

			int count = EvaluateModificationCount(engine, size, depth);
			if (count <= 0 || count >= SAFETYLIMIT)
			{
				CreateWarningInfo(engine, count);
				return false;
			}
			return true;
		}
		
		[Obsolete]
		public static bool IsSave(VoxelGenerator engine, float radius, int depth)
		{
			return IsSafe(engine, radius, depth);
		}

		[Obsolete]
		public static bool IsSave(VoxelGenerator engine, Vector3 size, int depth)
		{
			return IsSafe( engine,  size,  depth);
		}

		public static void CreateWarningInfo(VoxelGenerator generator, int count)
		{

			Quaternion rotation = generator.transform.rotation;
			Vector3 size = Vector3.one * generator.RootSize;
			Vector3 center = size / 2;

			Vector3 extends_max = size * 0.6f;
			Vector3 extends_min = -extends_max;

			Vector3 extends_corner = rotation * (extends_min + new Vector3(extends_max.x - extends_min.x, 0, 0));
			Vector3 extends_corner3 = rotation * (extends_min + new Vector3(0, extends_max.y - extends_min.y, 0));
			Vector3 extends_corner5 = rotation * (extends_min + new Vector3(0, 0, extends_max.z - extends_min.z));

			extends_max = rotation * extends_max;
			extends_min = -extends_max;


			Matrix4x4 matrix = generator.transform.localToWorldMatrix;


			Vector3 extends_corner6 = -extends_corner5;
			Vector3 extends_corner4 = -extends_corner3;
			Vector3 extends_corner2 = -extends_corner;

			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_max), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_min), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner2), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner3), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner4), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner5), Color.red, 0.1f);
			Debug.DrawLine(matrix.MultiplyPoint(center), matrix.MultiplyPoint(center + extends_corner6), Color.red, 0.1f);

			if (count == 0)
			{
				Debug.LogError("Attemting to apply a tool which would modify no Voxel! " +
				"Check your Voxel Modifier Tool! You can reduce Depth or reduce Radius/Size of the voxel modifier or reduce the Subdivision Power of the affected generator." +
				"The tool you are using also tells you why it is considered unsafe!\n\n", generator);
			}
			else
			{
				Debug.LogError("Attemting to apply a tool which would modify:" + count + " Voxel which is outside the safe amount! " +
			"Check your Voxel Modifier Tool! You can reduce Depth or reduce Radius/Size of the voxel modifier or reduce the Subdivision Power of the affected generator." +
			"The tool you are using also tells you why it is considered unsafe!\n\n", generator);
			}
		
		}


		public static void CleanUp()
		{
			filterSet?.CleanUp();
			filterSmooth?.CleanUp();
			filterRough?.CleanUp();
			filterSolidPaint?.CleanUp();	
		}

		public static float CalculateVoxelSize(VoxelGenerator engine, int Depth)
		{
			return engine.RootSize / Mathf.Pow(engine.SubdivisionPower, Depth);
		}

		public static void ApplyChunkOffset(VoxelGenerator referenceGenerator, Vector3 offset, FNativeList<NativeVoxelModificationData> input, FNativeList<NativeVoxelModificationData> output)
		{
			if (input.Length != output.Length) return;

			Vector3 realoffset = offset * referenceGenerator.RootSize;

			for (int i = 0; i < input.Length; i++)
			{
				NativeVoxelModificationData voxel = input[i];
				voxel.X -= realoffset.x;
				voxel.Y -= realoffset.y;
				voxel.Z -= realoffset.z;
				output[i] = voxel;
			}

		}

		public static void InvertModification(FNativeList<NativeVoxelModificationData> input, FNativeList<NativeVoxelModificationData> output)
		{
			if (input.Length != output.Length) return;

			for (int i = 0; i < input.Length; i++)
			{
				NativeVoxelModificationData voxel = input[i];
				voxel.ID *= -1;
				output[i] = voxel;
			}

		}
	}
}
