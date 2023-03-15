using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fraktalia.VoxelGen.Visualisation
{
	[System.Serializable]
	public struct DetailPlacement
	{
		public int MinID;
		public int MaxID;
		[Range(0,1)]
		public float FallOff;

		public float CalculateDetail(float fX, float fY, float fZ, ref NativeVoxelTree dataset)
		{
			float value = 1;

			int ID = dataset._PeekVoxelId(fX, fY, fZ, 10);

			if(ID > MaxID)
			{
				float difference = ID - MaxID;
				value = 1 - difference * FallOff; 
			}
			else if(ID < MinID)
			{
				float difference = MinID - ID;
				value = 1 - difference * FallOff;
			}

			
			return Mathf.Clamp01(value);
		}

		public float CalculateLife(float fX, float fY, float fZ, ref NativeVoxelTree dataset)
		{
			float ID = dataset._PeekVoxelId(fX, fY, fZ, 10);	
			return Mathf.Clamp01(ID/256f);
		}

		internal float GetChecksum()
		{
			return MinID + MaxID + FallOff*100;
		}
	}
}
