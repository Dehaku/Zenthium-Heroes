using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public static class WorldAlgorithm_Modes
{


	[BurstCompile]
	public static void Set(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID = value;
	}
	[BurstCompile]
	public static void Add(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID += value;
	}
	[BurstCompile]
	public static void Subtract(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID -= value;
	}
	[BurstCompile]
	public static void Min(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID = Mathf.Min(data.ID, value);
	}
	[BurstCompile]
	public static void Max(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID = Mathf.Max(data.ID, value);
	}

	[BurstCompile]
	public static void InvertSet(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID = (255 - value);
	}
	[BurstCompile]
	public static void InvertAdd(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID += (255 - value);
	}
	[BurstCompile]
	public static void InvertSubtract(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID -= (255 - value);
	}
	[BurstCompile]
	public static void InvertMin(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID = Mathf.Min(data.ID, (255 - value));
	}
	[BurstCompile]
	public static void InvertMax(ref NativeVoxelModificationData_Inner data, int value)
	{
		data.ID = Mathf.Max(data.ID, (255 - value));
	}
}

public delegate void WorldAlgorithm_Mode(ref NativeVoxelModificationData_Inner data, int value);
