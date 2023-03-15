using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fraktalia.Utility
{
	[System.Serializable]
	public struct PlacementManifest
	{
		public Vector3 Offset_min;
		public Vector3 Offset_max;
		public Vector3 Scale_min;
		public Vector3 Scale_max;
		public float ScaleFactor_min;
		public float ScaleFactor_max;
		public Vector3 Rotation_min;
		public Vector3 Rotation_max;

		


		public Vector3 _GetOffset(float rngValue_X, float rngValue_Y, float rngValue_Z)
		{
			Vector3 result;
			result.x = Mathf.Lerp(Offset_min.x, Offset_max.x, rngValue_X);
			result.y = Mathf.Lerp(Offset_min.y, Offset_max.y, rngValue_Y);
			result.z = Mathf.Lerp(Offset_min.z, Offset_max.z, rngValue_Z);
			return result;
		}

		public Vector3 _GetRotation(float rngValue_X, float rngValue_Y, float rngValue_Z)
		{
			Vector3 result;
			result.x = Mathf.Lerp(Rotation_min.x, Rotation_max.x, rngValue_X);
			result.y = Mathf.Lerp(Rotation_min.y, Rotation_max.y, rngValue_Y);
			result.z = Mathf.Lerp(Rotation_min.z, Rotation_max.z, rngValue_Z);
			return result;
		}

		public Vector3 _GetScale(float rngValue_X, float rngValue_Y, float rngValue_Z)
		{
			Vector3 result;
			result.x = Mathf.Lerp(Scale_min.x, Scale_max.x, rngValue_X);
			result.y = Mathf.Lerp(Scale_min.y, Scale_max.y, rngValue_Y);
			result.z = Mathf.Lerp(Scale_min.z, Scale_max.z, rngValue_Z);
			return result;
		}

		public float _GetScaleFactor(float rngValue)
		{
			return Mathf.Lerp(ScaleFactor_min, ScaleFactor_max, rngValue);		
		}

		public float _GetChecksum()
		{
			float sum = 0;
			sum+= Offset_min.sqrMagnitude;
			sum+= Offset_max.sqrMagnitude;
			sum+= Scale_min.sqrMagnitude;
			sum+= Scale_max.sqrMagnitude;
			sum+= Rotation_min.sqrMagnitude;
			sum+= Rotation_max.sqrMagnitude;
			sum+= ScaleFactor_min;
			sum+= ScaleFactor_max;
			return sum;
		}

		

	}
}

