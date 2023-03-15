#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Fraktalia.VoxelGen.Modify;
using Fraktalia.VoxelGen.Visualisation;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fraktalia.VoxelGen
{
	public class VoxelGeneratorEditorMisc : Editor
	{
		[MenuItem("GameObject/Fraktalia/Voxel with GPU Hull", false, 1000)]
		private static void CreateVoxelicaAtSelection(MenuCommand menuCommand)
		{
			
			var go = Instantiate(Resources.Load<GameObject>("Voxelica_GPU_v2"));
			go.GetComponent<VoxelGenerator>().GenerateBlock();
			go.GetComponent<VoxelSaveSystem>().Save();
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}
	}
	public class VoxelGeneratorSettings : EditorWindow
	{
		public static KeyCode PaintingKey;
		public static KeyCode ApplyKey;
		public static KeyCode UndoKey;
		public static KeyCode RedoKey;
		public static int UndoQueue;

		// Add menu named "My Window" to the Window menu
		[MenuItem("Tools/Fraktalia/Voxel Generator Settings")]
		public static void Init()
		{
			// Get existing open window or if none, make a new one:
			VoxelGeneratorSettings window = (VoxelGeneratorSettings) EditorWindow.GetWindow(typeof(VoxelGeneratorSettings));
			window.Show();
		}

		public static void DisplaySettingsInfo(string label, string info, GUIStyle style)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label + "<b>" + info + "</b>", style);
			if (GUILayout.Button("Edit"))
			{
				VoxelGeneratorSettings.Init();
			}
			EditorGUILayout.EndHorizontal();
		}

		void OnGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;
			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;
			Texture2D colortex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			colortex.SetPixel(0, 0, new Color32(240, 240, 240, 255));
			colortex.Apply();
			GUIStyle text = new GUIStyle();
			text.fontSize = 12;
			text.richText = true;
			text.wordWrap = true;
			text.margin = new RectOffset(5, 5, 5, 5);
			text.normal.background = colortex;
			text.padding = new RectOffset(5, 5, 5, 5);
			EditorStyles.textField.wordWrap = true;
			EditorGUILayout.LabelField("Voxel Generator Settings", title);
			EditorGUILayout.LabelField("This settings window contains settings for editor related functionality.", text);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Settings for Voxel Modifier V2:", bold);
			PaintingKey = (KeyCode) EditorGUILayout.EnumPopup("Editor Paint Mode Key:", PaintingKey);
			ApplyKey = (KeyCode) EditorGUILayout.EnumPopup("Apply Key:", ApplyKey);
			UndoKey = (KeyCode) EditorGUILayout.EnumPopup("Undo Key:", UndoKey);
			RedoKey = (KeyCode) EditorGUILayout.EnumPopup("Redo Key:", RedoKey);
			UndoQueue = EditorGUILayout.IntField("Undo Lenght:", UndoQueue);
			#if !COLLECTION_EXISTS
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Settings for Debugging:", bold);
			NativeLeakDetection.Mode = (NativeLeakDetectionMode)EditorGUILayout.EnumPopup("Leak Detection Mode", NativeLeakDetection.Mode);
			#endif
			if (GUI.changed)
			{
				EditorPrefs.SetInt("FraktaliaVoxelGenerator_PaintingMode", (int) PaintingKey);
				EditorPrefs.SetInt("FraktaliaVoxelGenerator_SpotPaintingMode", (int) ApplyKey);
				EditorPrefs.SetInt("FraktaliaVoxelGenerator_UndoQueue", UndoQueue);
				EditorPrefs.SetInt("FraktaliaVoxelGenerator_UndoKey", (int) UndoKey);
				EditorPrefs.SetInt("FraktaliaVoxelGenerator_RedoKey", (int) RedoKey);
			}
			if (GUILayout.Button("Clear Preferences"))
			{
				EditorPrefs.DeleteKey("FraktaliaVoxelGenerator_PaintingMode");
				EditorPrefs.DeleteKey("FraktaliaVoxelGenerator_SpotPaintingMode");
				EditorPrefs.DeleteKey("FraktaliaVoxelGenerator_UndoQueue");
				EditorPrefs.DeleteKey("FraktaliaVoxelGenerator_UndoKey");
				EditorPrefs.DeleteKey("FraktaliaVoxelGenerator_RedoKey");
			}
		}

		internal static void InitializeSettings()
		{
			VoxelGeneratorSettings.PaintingKey = LoadKey("FraktaliaVoxelGenerator_PaintingMode", KeyCode.LeftControl);
			VoxelGeneratorSettings.ApplyKey = LoadKey("FraktaliaVoxelGenerator_SpotPaintingMode", KeyCode.V);
			VoxelGeneratorSettings.ApplyKey = LoadKey("FraktaliaVoxelGenerator_SpotPaintingMode", KeyCode.V);
			VoxelGeneratorSettings.UndoKey = LoadKey("FraktaliaVoxelGenerator_UndoQueue", KeyCode.Z);
			VoxelGeneratorSettings.RedoKey = LoadKey("FraktaliaVoxelGenerator_RedoQueue", KeyCode.Y);
			if (!EditorPrefs.HasKey("FraktaliaVoxelGenerator_UndoQueue"))
			{
				VoxelGeneratorSettings.UndoQueue = 10;
			} else
			{
				VoxelGeneratorSettings.UndoQueue = EditorPrefs.GetInt("FraktaliaVoxelGenerator_UndoQueue");
			}
		}

		private static KeyCode LoadKey(string key, KeyCode defaultvalue)
		{
			if (!EditorPrefs.HasKey(key))
			{
				return defaultvalue;
			} else
			{
				return (KeyCode) EditorPrefs.GetInt(key);
			}
		}
	}
	[InitializeOnLoad]
	public static class VoxelGeneratorSettingsSetup
	{
		// register an event handler when the class is initialized
		static VoxelGeneratorSettingsSetup()
		{
			EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
			EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
			VoxelGeneratorSettings.InitializeSettings();
		}

		private static void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) { VoxelGeneratorSettings.InitializeSettings(); }
		private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj) { VoxelGeneratorSettings.InitializeSettings(); }
	}
}
#endif