using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public static class WorldAlgorithm_PostProcesses
{
	[BurstCompile]
	public static int Nothing(int value)
	{
		return value;
	}
	[BurstCompile]
	public static int NoNegatives(int value)
	{
		return Mathf.Max(0, value);
	}
}

public delegate int WorldAlgorithm_PostProcess(int value);
