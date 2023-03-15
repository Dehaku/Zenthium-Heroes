using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Fraktalia.Core.Math;
using Unity.Burst;
using Fraktalia.Utility.NativeNoise;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
#endif

namespace Fraktalia.Core.ProceduralUVCreator
{
    public class ProceduralUV : MonoBehaviour
    {
		public bool Inactive;
		public DataApplyMode ApplyMode;
		public ProceduralUVGenerator Generator;

		protected FunctionPointer<DataApplyModeDelegate> ApplyFunctionPointer;
		
	
		private DataApplyMode previousapplymode;



		public void LaunchAlgorithm(
            ref NativeArray<Vector3> positionData,
               ref NativeArray<Vector2> uvData)
        {
            if (HasErrors()) return;

			if (ApplyMode != previousapplymode || !ApplyFunctionPointer.IsCreated)
			{
				previousapplymode = ApplyMode;
				ApplyFunctionPointer = DataApplyModes.GetFunctionPointer(ApplyMode);
			}
				

            Algorithm(ref positionData, ref uvData);
        }


        protected virtual void Algorithm(
            ref NativeArray<Vector3> positionData,
               ref NativeArray<Vector2> uvData)
        {

        }    

        public virtual bool HasErrors()
        {
            return false;
        }

		public virtual void CleanUp()
		{

		}		
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(ProceduralUV), true)]
	public class WorldAlgorithmEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;


			EditorStyles.textField.wordWrap = true;


			ProceduralUV myTarget = (ProceduralUV)target;




			DrawDefaultInspector();


			if (GUI.changed)
			{
				if (myTarget.Generator)
				{
					myTarget.Generator.Apply();					
				}
				else
				{
					myTarget.Generator = myTarget.GetComponentInParent<ProceduralUVGenerator>();
				}
			}
		}
	}
#endif
}
