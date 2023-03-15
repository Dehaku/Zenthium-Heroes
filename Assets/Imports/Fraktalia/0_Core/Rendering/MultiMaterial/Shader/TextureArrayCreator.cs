using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Core.FraktaliaAttributes
{
	public class TextureArrayCreator : MonoBehaviour
	{
		public Transform root;
		public MeshRenderer[] renderers = new MeshRenderer[0];

		public Transform resultroot;
		public MeshRenderer[] resultrenderers = new MeshRenderer[0];

		public Material targetmaterial;
		private void OnDrawGizmosSelected()
		{
			if(root)
			renderers = root.GetComponentsInChildren<MeshRenderer>();

			if (resultroot)
				resultrenderers = resultroot.GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < resultrenderers.Length; i++)
            {
				resultrenderers[i].sharedMaterial = targetmaterial;
            }

		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(TextureArrayCreator))]
	public class TextureArrayCreatorEditor : Editor
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


			TextureArrayCreator mytarget = target as TextureArrayCreator;


			DrawDefaultInspector();

			if (GUILayout.Button("Create TextureArray."))
			{			
				Material[] mats = new Material[mytarget.renderers.Length];

				for (int i = 0; i < mytarget.renderers.Length; i++)
				{
					mats[i] = mytarget.renderers[i].sharedMaterial;
				}

				if (TextureArrayGenerator.texturegenerator == null) TextureArrayGenerator.CreateGenerator();

				TextureArrayGenerator.texturegenerator.Materials = mats;
				TextureArrayGenerator.texturegenerator.TargetMaterial = mytarget.targetmaterial;
				TextureArrayGenerator.texturegenerator.UseMaterialPath = true;
				TextureArrayGenerator.texturegenerator.OutputPath = AssetDatabase.GetAssetPath(TextureArrayGenerator.texturegenerator.TargetMaterial).Replace("/" + TextureArrayGenerator.texturegenerator.TargetMaterial.name + ".mat", "");
				TextureArrayGenerator.texturegenerator.FinalName = TextureArrayGenerator.texturegenerator.TargetMaterial.name;
			
				TextureArrayGenerator.texturegenerator.ExtractMaterials();
				TextureArrayGenerator.texturegenerator.CreateAllTextureArrays();
			}

			if (GUILayout.Button("Create 3D Texture."))
			{
				Material[] mats = new Material[mytarget.renderers.Length];

				for (int i = 0; i < mytarget.renderers.Length; i++)
				{
					mats[i] = mytarget.renderers[i].sharedMaterial;
				}

				if (TextureArrayGenerator.texturegenerator == null) TextureArrayGenerator.CreateGenerator();

				TextureArrayGenerator.texturegenerator.Materials = mats;
				TextureArrayGenerator.texturegenerator.TargetMaterial = mytarget.targetmaterial;
				TextureArrayGenerator.texturegenerator.UseMaterialPath = true;
				TextureArrayGenerator.texturegenerator.OutputPath = AssetDatabase.GetAssetPath(TextureArrayGenerator.texturegenerator.TargetMaterial).Replace("/" + TextureArrayGenerator.texturegenerator.TargetMaterial.name + ".mat", "");
				TextureArrayGenerator.texturegenerator.FinalName = TextureArrayGenerator.texturegenerator.TargetMaterial.name;

				TextureArrayGenerator.texturegenerator.ExtractMaterials();
				TextureArrayGenerator.texturegenerator.CreateAll3DTexture();
			}


			if (GUILayout.Button("Create Texture Atlas."))
			{
				Material[] mats = new Material[mytarget.renderers.Length];

				for (int i = 0; i < mytarget.renderers.Length; i++)
				{
					mats[i] = mytarget.renderers[i].sharedMaterial;
				}

				if (TextureArrayGenerator.texturegenerator == null) TextureArrayGenerator.CreateGenerator();

				TextureArrayGenerator.texturegenerator.Materials = mats;
				TextureArrayGenerator.texturegenerator.TargetMaterial = mytarget.targetmaterial;
				TextureArrayGenerator.texturegenerator.UseMaterialPath = true;
				TextureArrayGenerator.texturegenerator.OutputPath = AssetDatabase.GetAssetPath(TextureArrayGenerator.texturegenerator.TargetMaterial).Replace("/" + TextureArrayGenerator.texturegenerator.TargetMaterial.name + ".mat", "");
				TextureArrayGenerator.texturegenerator.FinalName = TextureArrayGenerator.texturegenerator.TargetMaterial.name;

				TextureArrayGenerator.texturegenerator.ExtractMaterials();
				TextureArrayGenerator.texturegenerator.CreateAllTextureAtlases();
			}

			if (GUILayout.Button("Open Editor"))
			{
				Material[] mats = new Material[mytarget.renderers.Length];

				for (int i = 0; i < mytarget.renderers.Length; i++)
				{
					mats[i] = mytarget.renderers[i].sharedMaterial;
				}


				TextureArrayGenerator.Init();
				TextureArrayGenerator.texturegenerator.Materials = mats;
				TextureArrayGenerator.texturegenerator.TargetMaterial = mytarget.targetmaterial;
				TextureArrayGenerator.texturegenerator.UseMaterialPath = true;
				TextureArrayGenerator.texturegenerator.OutputPath = AssetDatabase.GetAssetPath(TextureArrayGenerator.texturegenerator.TargetMaterial).Replace("/" + TextureArrayGenerator.texturegenerator.TargetMaterial.name + ".mat", "");
				TextureArrayGenerator.texturegenerator.FinalName = TextureArrayGenerator.texturegenerator.TargetMaterial.name;

			}

		}
	}
#endif
}