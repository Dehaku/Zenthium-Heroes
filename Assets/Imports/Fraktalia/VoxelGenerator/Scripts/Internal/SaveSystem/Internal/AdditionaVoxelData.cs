using UnityEngine;
using System;
using System.Runtime.InteropServices;




namespace Fraktalia.VoxelGen
{
	[System.Serializable]
	public struct AdditionaVoxelData
	{
		public int VoxelCount;
		[SerializeField]
		[HideInInspector]
		[MarshalAs(UnmanagedType.ByValArray)]
		public NativeVoxelModificationData[] VoxelData;

	
	}

}
