#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Fraktalia.Core.FraktaliaAttributes;

namespace Fraktalia.Core.LMS
{
    [CustomEditor(typeof(NativeMeshCombiner))][CanEditMultipleObjects]
    public class NativeMeshCombinerEditor : Editor
    {


        bool foldout = false;

        string savePath = "Assets/";
        string meshName = "ExportedMesh";

        public override void OnInspectorGUI()
        {


            NativeMeshCombiner mytarget = target as NativeMeshCombiner;

            GUIStyle bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 14;
            bold.richText = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Generation Settings:</color>", bold);

            EditorGUILayout.Space();
          
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentMaterial"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("UseSeperateMaterials"));
			if(mytarget.UseSeperateMaterials)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("SeperateMaterials"), true);
			}



			if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < targets.Length; i++)
                {
                    var current = targets[i] as NativeMeshCombiner;
                    current.SetMaterial(mytarget.currentMaterial);
                }              
            }
            

            EditorGUILayout.Space();


            EditorGUILayout.PropertyField(serializedObject.FindProperty("HasCollision"));
            if (serializedObject.FindProperty("HasCollision").boolValue == true)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ConvexCollider"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isTrigger"));
               
            }
            EditorGUILayout.Space();        
			EditorGUILayout.PropertyField(serializedObject.FindProperty("CreateBarycentricColors"));

			


			if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            
            if (mytarget.HideInHierachy)
            {
                EditorGUILayout.LabelField("Generation is hidden");
                if (GUILayout.Button("Show Generation"))
                {
                    mytarget.HideInHierachy = false;
                    mytarget.UpdateHideFlags();

                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
            else
            {
                EditorGUILayout.LabelField("Generation is visible");
                if (GUILayout.Button("Hide Generation"))
                {
                    mytarget.HideInHierachy = true;
                    mytarget.UpdateHideFlags();

                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                }

            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NoSceneSaving"));          
            if (EditorGUI.EndChangeCheck())
            {
                mytarget.UpdateHideFlags();
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                serializedObject.ApplyModifiedProperties();
            }
           

           
           


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<color=green>Mesh Exporter</color>", bold);


            if (foldout = EditorGUILayout.Foldout(foldout, "Export Options"))
            {
                NativeMeshPiece[] meshes = mytarget.GetComponentsInChildren<NativeMeshPiece>();

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

                        MeshFilter mf = meshes[i].meshfilter;
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
                        MeshFilter mf = meshes[i].meshfilter;
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


			if (PlayerSettings.stripUnusedMeshComponents)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("<color=blue>Important Info:</color>", bold);
				EditorGUILayout.TextArea("You have StripUnusedMeshComponents(Optimize Mesh Data) enabled which causes issues with procedural generated content in Builds due to missing Tangents. " +
					"If the result is wrong(usually black) conside disabling StripUnusedMeshComponents in the player settings.");

				if(GUILayout.Button("Disable StripUnusedMeshComponents"))
				{
					PlayerSettings.stripUnusedMeshComponents = false;
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("UV Modifiers: " + mytarget.GetComponents<NativeUVModifier>().Length);
			var algorithms = FraktaliaEditorUtility.GetDerivedTypesForScriptSelection(typeof(NativeUVModifier), "Add UV Modifier...");
			int selectalgorithm = EditorGUILayout.Popup(0, algorithms.Item2);
			if (selectalgorithm > 0)
			{
				mytarget.gameObject.AddComponent(algorithms.Item1[selectalgorithm]);
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

    // ensure class initializer is called whenever scripts recompile
    [InitializeOnLoad]
    public static class NativeMeshCombiner_JobCleaner
    {
        // register an event handler when the class is initialized
        static NativeMeshCombiner_JobCleaner()
        {
            EditorApplication.playModeStateChanged += Cleanup;

           
        }
     
        
        public static void Cleanup(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                NativeMeshCombiner[] objects = GameObject.FindObjectsOfType<NativeMeshCombiner>();

                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].NoSceneSaving)
                    {
                        objects[i].Reset();
                    }
                }
            }
        }


    }



}
#endif
