#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using Fraktalia.Core.Math;

namespace Fraktalia.Core.Pipe.Native
{
    [CustomEditor(typeof(PipeExtruder_Native))][CanEditMultipleObjects]
    public class PipeExtruder_NativeEditor : Editor
    {


      
        bool editcross = false;
        
       

        public void OnSceneGUI()
        {
            PipeExtruder_Native mytarget = target as PipeExtruder_Native;
            if (!editcross) return;
            Vector2[] querschnitt = mytarget.Querschnitt;



            Vector3[] pos = new Vector3[querschnitt.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                pos[i] = mytarget.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(querschnitt[i].x, 0, querschnitt[i].y));

                pos[i] = Handles.FreeMoveHandle(pos[i], mytarget.transform.localRotation * Quaternion.Euler(90, 0, 0), 0.2f, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newpos = mytarget.transform.worldToLocalMatrix.MultiplyPoint3x4(pos[i]);
                querschnitt[i] = new Vector2(newpos.x, newpos.z);
            }

            Handles.DrawPolyLine(pos);

            Handles.DrawLine(pos[0], pos[pos.Length - 1]);


        }


        public override void OnInspectorGUI()
        {


            PipeExtruder_Native mytarget = target as PipeExtruder_Native;

            GUIStyle bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 14;
            bold.richText = true;

			EditorGUILayout.LabelField("<color=green>UV Setting</color>", bold);         
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UV_Multiplier"), new GUIContent("UV Multiplier"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Cross Section</color>", bold);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("QuerschnittPunkte"), new GUIContent("Cross Section Points"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("QuerschnittStartAngle"), new GUIContent("Cross Section Startangle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CrossScale"), new GUIContent("Cross Scale"));
           
            if (GUILayout.Button("Reset Cross Section"))
            {
                float step = 360 / mytarget.QuerschnittPunkte;

                List<Vector2> newquerschnitt = new List<Vector2>();
                for (int i = 0; i < mytarget.QuerschnittPunkte; i++)
                {
                    newquerschnitt.Add(MathUtilities.kreisPosition(mytarget.QuerschnittStartAngle + step * i));
                }
                newquerschnitt.Reverse();
                mytarget.Querschnitt = newquerschnitt.ToArray();
            }



            if (GUILayout.Button("Edit Cross Section"))
            {
                editcross = true;
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Misc Settings:</color>", bold);
         
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CullEnds"), new GUIContent("Cull Start/End"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ConnectTreshold"), new GUIContent("Maximum Connection Distance"));




            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        void OnDestroy()
        {
            PipeExtruder_Native mytarget = target as PipeExtruder_Native;
            if(mytarget) mytarget.CleanMemory();
        }


    }
}
#endif
