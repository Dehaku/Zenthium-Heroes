#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Fraktalia.Core.FraktaliaAttributes;

namespace Fraktalia.Core.LMS
{
    [CustomEditor(typeof(LargeMeshCombiner))][CanEditMultipleObjects]
    public class LargeMeshCombinerEditor : Editor
    {


        bool foldout = false;

        string savePath = "Assets/";
        string meshName = "ExportedMesh";

        public override void OnInspectorGUI()
        {


            LargeMeshCombiner mytarget = target as LargeMeshCombiner;

            GUIStyle bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 14;
            bold.richText = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Generation Settings:</color>", bold);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SingleMeshOnly"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AttachOnRoot"), new GUIContent("Single Mesh to Root"));
     
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxVertex"), new GUIContent("Vertices per Piece"));

           
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentMaterial"));

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ConvexCollider"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("isTrigger"), new GUIContent("Collider is Trigger"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("SupressMerges"));

			if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < targets.Length; i++)
                {
                    var current = targets[i] as LargeMeshCombiner;
                    if(current) current.SetMaterial(mytarget.currentMaterial);
                }
            }

          
           

            EditorGUILayout.Space();
            if (mytarget.HideInHierachy)
            {
                EditorGUILayout.LabelField("Generation is hidden");
                if (GUILayout.Button("Show Generation"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var current = targets[i] as LargeMeshCombiner;
                        if (current)
                        {
                            current.HideInHierachy = false;
                            current.UnHideFromInspector();
                        }
                    }

                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
            else
            {
                EditorGUILayout.LabelField("Generation is visible");
                if (GUILayout.Button("Hide Generation"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var current = targets[i] as LargeMeshCombiner;
                        if (current)
                        {
                            current.HideInHierachy = true;
                            current.HideFromInspector();
                        }
                    }

                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                }
            }





            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Mesh Exporter</color>", bold);


            if (foldout = EditorGUILayout.Foldout(foldout, "Export Options"))
            {
                MeshFilter[] meshes = mytarget.GetComponentsInChildren<MeshFilter>();

                meshName = EditorGUILayout.TextField("Export Name", meshName);
                savePath = EditorGUILayout.TextField("Path", savePath);

                for (int i = 0; i < meshes.Length; i++)
                {
                    EditorGUILayout.LabelField("Meshpiece " + i + ": ");
                    EditorGUILayout.BeginHorizontal(
                        new GUILayoutOption[]{
                                GUILayout.MaxWidth(240)

                    });

                    if (GUILayout.Button("Export to .asset"))
                    {

                        MeshFilter mf = meshes[i];
                        if (mf)
                        {

                            string Path = savePath + meshName + i + ".asset";
                            Debug.Log("Saved Mesh to:" + Path);
                            AssetDatabase.CreateAsset(Instantiate<Mesh>(mf.sharedMesh), Path);

                        }
                        AssetDatabase.Refresh();
                    }

                    /*
                    if (GUILayout.Button("Export to .obj"))
                    {
                        MeshFilter mf = meshes[i];
                        MeshRenderer re = mf.GetComponent<MeshRenderer>();
                        if (mf && re)
                        {
                            mytarget.MeshToFile(mf.sharedMesh, re.sharedMaterials, meshName + i + ".obj", savePath);
                            string Path = savePath + meshName + i + ".obj";
                            Debug.Log("Saved Mesh to:" + Path);
                        }
                        AssetDatabase.Refresh();
                    }
                    */
                    EditorGUILayout.EndHorizontal();

                }


            }

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Attachments: " + mytarget.GetComponents<MeshPieceAttachment>().Length);
			var attachments = FraktaliaEditorUtility.GetDerivedTypesForScriptSelection(typeof(MeshPieceAttachment), "Add Mesh Attachment...");
			int selectedattachments = EditorGUILayout.Popup(0, attachments.Item2);
			if (selectedattachments > 0)
			{
				mytarget.gameObject.AddComponent(attachments.Item1[selectedattachments]);
			}



			if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }




    }
}
#endif
