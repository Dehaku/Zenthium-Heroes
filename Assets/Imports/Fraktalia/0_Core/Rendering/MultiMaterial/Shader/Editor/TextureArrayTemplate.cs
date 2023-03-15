#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using Fraktalia.Core.FraktaliaAttributes;
using System.Runtime.Serialization;
using System.IO;
using UnityEditor.Callbacks;

namespace Fraktalia.Core.FraktaliaAttributes
{
	public enum SmoothnessSource
    {
		MetallicAlpha,
		AlbedoAlpha
    }

	public class TextureArrayTemplate : ScriptableObject
	{
		[NonReorderable]
		[Header("Materials To Extract")]
		public Material[] Materials;

		[Header("NULL Placeholders")]
		public Texture2D WhiteDefaultTexture;
		public Texture2D BlackDefaultTexture;
		public Texture2D NormalDefaultTexture;
		public Texture2D HeightDefaultTexture;

		[NonReorderable]
		[Header("Texture Arrays")]
		public Texture2D[] OcclusionTextures;
		[NonReorderable]
		public Texture2D[] DiffuseTextures;
		[NonReorderable]
		public Texture2D[] EmmissiveTextures;
		[NonReorderable]
		public Texture2D[] HeightTextures;
		[NonReorderable]
		public Texture2D[] MetallicTextures;
		[NonReorderable]
		public Texture2D[] NormalTextures;

		[NonReorderable]
		public Color[] BaseTextureMultiplier;
		[NonReorderable]
		public Color[] MetallicMultiplier;
		[NonReorderable]
		public Color[] EmissionMultiplier;
		[NonReorderable]
		public SmoothnessSource[] SmoothnessTextureSource;


		public string OutputPath;
		public string FinalName;

		[Header("Target Material for Material Assignment")]
		[Tooltip("Target Material which should use texture arrays or atlas texture. Shader must be compatible")]
		public Material TargetMaterial;
		
		[Space]
		public bool UseMaterialPath;

		public Texture2DArray CreateTextureArray(Texture2D[] ordinaryTextures, Texture2D nullTexture, string FileName, Color[] _Colors = null, SmoothnessSource[] _SmoothMode = null)
		{
			int width = 16;
			int height = 16;
			for (int i = 0; i < ordinaryTextures.Length; i++)
			{
				if (ordinaryTextures[i] != null)
				{
					width = Mathf.Max(width, ordinaryTextures[i].width);
					height = Mathf.Max(height, ordinaryTextures[i].height);
				}
			}

			Texture2DArray texture2DArray = new Texture2DArray(width, height, ordinaryTextures.Length, TextureFormat.RGBA32, true, false);
			texture2DArray.filterMode = FilterMode.Bilinear;
			texture2DArray.wrapMode = TextureWrapMode.Repeat;    // Loop through ordinary textures and copy pixels to the

			Texture2D[] bakedTextures = new Texture2D[ordinaryTextures.Length];
			for (int i = 0; i < ordinaryTextures.Length; i++)
			{
				Texture2D texture2D = ordinaryTextures[i];
				if (texture2D == null)
				{
					texture2D = nullTexture;
				}

				if (!texture2D.isReadable)
				{
					SetTextureReadable(texture2D, true);
				}

				Texture2D smoothnessSource = null;
				if (_SmoothMode != null)
				{
					if (_SmoothMode[i] == SmoothnessSource.AlbedoAlpha)
					{
						smoothnessSource = this.DiffuseTextures[i];
					}
				}

				Color[] pixels = new Color[width * height];
				
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						Color _Color = Color.white;
						if (_Colors != null)
						{
							_Color = _Colors[i];
						}

						Color pixel = GetColor(x, y, _Color, texture2D, smoothnessSource);
						pixels[x + y * width] = pixel;

					}
				}
				texture2DArray.SetPixels(pixels, i, 0);
			}
		
			texture2DArray.Apply();
			

			string path = OutputPath;
			if (!AssetDatabase.IsValidFolder(path))
			{
				path = "Assets";
			}

			path += "/" + FileName + ".asset";

			AssetDatabase.CreateAsset(texture2DArray, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("Saved asset to " + path);

			return texture2DArray;
		}

		public Texture3D Create3DTexture(Texture2D[] ordinaryTextures, Texture2D nullTexture, string FileName, Color[] _Colors = null, SmoothnessSource[] _SmoothMode = null)
		{
			int width = 16;
			int height = 16;
			for (int i = 0; i < ordinaryTextures.Length; i++)
			{
				if (ordinaryTextures[i] != null)
				{
					width = Mathf.Max(width, ordinaryTextures[i].width);
					height = Mathf.Max(height, ordinaryTextures[i].height);
				}
			}

			Texture3D texture2DArray = new Texture3D(width, height, ordinaryTextures.Length, TextureFormat.RGBA32, 10);
			texture2DArray.filterMode = FilterMode.Bilinear;
			texture2DArray.wrapMode = TextureWrapMode.Repeat;    // Loop through ordinary textures and copy pixels to the

			Texture2D[] bakedTextures = new Texture2D[ordinaryTextures.Length];
			for (int i = 0; i < ordinaryTextures.Length; i++)
			{
				Texture2D texture2D = ordinaryTextures[i];
				if (texture2D == null)
				{
					texture2D = nullTexture;
				}

				if (!texture2D.isReadable)
				{
					SetTextureReadable(texture2D, true);
				}

				Texture2D smoothnessSource = null;
				if (_SmoothMode != null)
				{
					if (_SmoothMode[i] == SmoothnessSource.AlbedoAlpha)
					{
						smoothnessSource = this.DiffuseTextures[i];
					}
				}

				Color[] pixels = new Color[width * height];

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						Color _Color = Color.white;
						if (_Colors != null)
						{
							_Color = _Colors[i];
						}

						Color pixel = GetColor(x, y, _Color, texture2D, smoothnessSource);
						pixels[x + y * width] = pixel;
						texture2DArray.SetPixel(x,y, i, pixel,0);
					}
				}
				
			}

			texture2DArray.Apply();


			string path = OutputPath;
			if (!AssetDatabase.IsValidFolder(path))
			{
				path = "Assets";
			}

			path += "/" + FileName + "3D.asset";

			AssetDatabase.CreateAsset(texture2DArray, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("Saved asset to " + path);

			return texture2DArray;
		}


		public Texture2D CreateTextureAtlas(Texture2D[] ordinaryTextures, Texture2D nullTexture, string FileName, Color[] _Colors = null, SmoothnessSource[] _SmoothMode = null)
		{
			int requiredrows = Mathf.NextPowerOfTwo(ordinaryTextures.Length) / 2;


			int width = 16;
			int height = 16;
			for (int i = 0; i < ordinaryTextures.Length; i++)
			{
				if (ordinaryTextures[i] != null)
				{
					width = Mathf.Max(width, ordinaryTextures[i].width);
					height = Mathf.Max(height, ordinaryTextures[i].height);
				}
			}

			int requiredSize = width * requiredrows;

			Texture2D texture = new Texture2D(requiredSize, requiredSize);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Repeat;

			for (int i = 0; i < ordinaryTextures.Length; i++)
			{
				int offsetX = i % requiredrows;
				int offsetY = i / requiredrows;

				Texture2D texture2D = ordinaryTextures[i];
				if (texture2D == null)
				{
					texture2D = nullTexture;
				}

				Texture2D smoothnessSource = null;
				if(_SmoothMode != null)
                {
					if(_SmoothMode[i] == SmoothnessSource.AlbedoAlpha)
                    {
						smoothnessSource = this.DiffuseTextures[i];
                    }
                }

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{											
						Color _Color = Color.white;
						if (_Colors != null)
						{
							_Color = _Colors[i];
						}
						
						Color pixel = GetColor(x, y, _Color, texture2D, smoothnessSource);

						texture.SetPixel(x + offsetX * width, y + offsetY * height, pixel);
					}
				}
			}

			string path = OutputPath;
			if (!AssetDatabase.IsValidFolder(path))
			{
				path = "Assets";
			}

			path += "/" + FileName + "Atlas.png";
			byte[] bytes = texture.EncodeToPNG();
			File.WriteAllBytes(path, bytes);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
		}

		private Color GetColor(int x, int y, Color _InitialColor, Texture2D texture2D, Texture2D smoothnessSource)
        {
			int x2 = x % texture2D.width;
			int y2 = y % texture2D.height;

			Color _Color = _InitialColor;
			
			Color pixel = texture2D.GetPixel(x2, y2) * _Color;
			if (smoothnessSource != null)
			{
				int sx2 = x % texture2D.width;
				int sy2 = y % texture2D.height;

				pixel.a = smoothnessSource.GetPixel(sx2, sy2).a * _Color.a;
			}

			return pixel;
		}

		public void CreateAllTextureArrays()
		{
			Texture2DArray[] array = new Texture2DArray[6];
			array[0] = CreateTextureArray(OcclusionTextures, WhiteDefaultTexture, FinalName + "_OcclusionMaps");
			array[1] = CreateTextureArray(DiffuseTextures, WhiteDefaultTexture, FinalName + "_DiffuseMaps", BaseTextureMultiplier);
			array[2] = CreateTextureArray(EmmissiveTextures, BlackDefaultTexture, FinalName + "_EmissionMaps", EmissionMultiplier);
			array[3] = CreateTextureArray(HeightTextures, HeightDefaultTexture, FinalName + "_HeightMaps");
			array[4] = CreateTextureArray(MetallicTextures, WhiteDefaultTexture, FinalName + "_MetallicMaps", MetallicMultiplier, SmoothnessTextureSource);
			array[5] = CreateTextureArray(NormalTextures, NormalDefaultTexture, FinalName + "_NormalMaps");

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (TargetMaterial)
			{

				AssignTextureArray("_OcclusionMap", array[0]);
				AssignTextureArray("_DiffuseMap", array[1]);
				AssignTextureArray("_EmissionMap", array[2]);
				AssignTextureArray("_ParallaxMap", array[3]);
				AssignTextureArray("_MetallicGlossMap", array[4]);
				AssignTextureArray("_BumpMap", array[5]);
			}

		
		}

		public void CreateAll3DTexture()
		{
			Texture3D[] array = new Texture3D[6];
			array[0] = Create3DTexture(OcclusionTextures, WhiteDefaultTexture, FinalName + "_OcclusionMaps");
			array[1] = Create3DTexture(DiffuseTextures, WhiteDefaultTexture, FinalName + "_DiffuseMaps", BaseTextureMultiplier);
			array[2] = Create3DTexture(EmmissiveTextures, BlackDefaultTexture, FinalName + "_EmissionMaps", EmissionMultiplier);
			array[3] = Create3DTexture(HeightTextures, HeightDefaultTexture, FinalName + "_HeightMaps");
			array[4] = Create3DTexture(MetallicTextures, WhiteDefaultTexture, FinalName + "_MetallicMaps", MetallicMultiplier, SmoothnessTextureSource);
			array[5] = Create3DTexture(NormalTextures, NormalDefaultTexture, FinalName + "_NormalMaps");

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (TargetMaterial)
			{

				AssignTexture3D("_OcclusionMap", array[0]);
				AssignTexture3D("_DiffuseMap", array[1]);
				AssignTexture3D("_EmissionMap", array[2]);
				AssignTexture3D("_ParallaxMap", array[3]);
				AssignTexture3D("_MetallicGlossMap", array[4]);
				AssignTexture3D("_BumpMap", array[5]);
			}


		}

		public void CreateAllTextureAtlases()
        {
			Texture2D texture;
			if (TargetMaterial)
			{
				texture = CreateTextureAtlas(OcclusionTextures, WhiteDefaultTexture, FinalName + "_OcclusionMaps");
				AssignTexture2D("_OcclusionMap", texture, TargetMaterial);

				texture = CreateTextureAtlas(DiffuseTextures, WhiteDefaultTexture, FinalName + "_DiffuseMaps", BaseTextureMultiplier);
				AssignTexture2D("_DiffuseMap", texture, TargetMaterial);

				texture = CreateTextureAtlas(EmmissiveTextures, BlackDefaultTexture, FinalName + "_EmissionMaps", EmissionMultiplier);
				AssignTexture2D("_EmissionMap", texture, TargetMaterial);

				texture = CreateTextureAtlas(HeightTextures, HeightDefaultTexture, FinalName + "_HeightMaps");
				AssignTexture2D("_ParallaxMap", texture, TargetMaterial);

				texture = CreateTextureAtlas(MetallicTextures, WhiteDefaultTexture, FinalName + "_MetallicMaps", MetallicMultiplier, SmoothnessTextureSource);
				AssignTexture2D("_MetallicGlossMap", texture, TargetMaterial);

				texture = CreateTextureAtlas(NormalTextures, NormalDefaultTexture, FinalName + "_NormalMaps");	
				AssignTexture2D("_BumpMap", texture, TargetMaterial);	
			}
		}

		public void SaveProfile()
		{
			string path = OutputPath;
			if (!AssetDatabase.IsValidFolder(path))
			{
				path = "Assets";
			}

			path += "/" + FinalName + ".asset";

			TextureArrayTemplate duplicate = Instantiate(this);
			AssetDatabase.CreateAsset(duplicate, path);
			AssetDatabase.SaveAssets();

		}

		public void ExtractMaterials()
		{
			OcclusionTextures = new Texture2D[Materials.Length];
			DiffuseTextures = new Texture2D[Materials.Length];
			EmmissiveTextures = new Texture2D[Materials.Length];
			HeightTextures = new Texture2D[Materials.Length];
			MetallicTextures = new Texture2D[Materials.Length];
			NormalTextures = new Texture2D[Materials.Length];

			BaseTextureMultiplier = new Color[Materials.Length];
			MetallicMultiplier = new Color[Materials.Length];
			EmissionMultiplier = new Color[Materials.Length];
			SmoothnessTextureSource = new SmoothnessSource[Materials.Length];

			for (int i = 0; i < BaseTextureMultiplier.Length; i++)
			{
				BaseTextureMultiplier[i] = Color.white;
				MetallicMultiplier[i] = Color.white;
				EmissionMultiplier[i] = Color.white;
			}


			for (int i = 0; i < Materials.Length; i++)
			{



				if (Materials[i] != null)
				{
					OcclusionTextures[i] = extracttexture(Materials[i], "_OcclusionMap", WhiteDefaultTexture);
					DiffuseTextures[i] = extracttexture(Materials[i], "_MainTex", WhiteDefaultTexture);
					EmmissiveTextures[i] = extracttexture(Materials[i], "_EmissionMap", BlackDefaultTexture);
					HeightTextures[i] = extracttexture(Materials[i], "_ParallaxMap", HeightDefaultTexture);
					MetallicTextures[i] = extracttexture(Materials[i], "_MetallicGlossMap", WhiteDefaultTexture);
					NormalTextures[i] = extracttexture(Materials[i], "_BumpMap", NormalDefaultTexture);



					BaseTextureMultiplier[i] = extractcolor(Materials[i], "_Color");
					EmissionMultiplier[i] = extractcolor(Materials[i], "_EmissionColor");

					if (MetallicTextures[i] == WhiteDefaultTexture)
					{
						float metallic = extractfloat(Materials[i], "_Metallic");
						float glossiness = extractfloat(Materials[i], "_Glossiness");

						MetallicMultiplier[i] = new Color(metallic, metallic, metallic, glossiness);

					}
					else
					{
						float glossiness = extractfloat(Materials[i], "_GlossMapScale");
						MetallicMultiplier[i] = new Color(1, 1, 1, glossiness);
					}

					SmoothnessTextureSource[i] = (SmoothnessSource)extractfloat(Materials[i], "_SmoothnessTextureChannel");

				}

			}
		}

		private Texture2D extracttexture(Material target, string PropertyName, Texture2D NullTexture)
		{
			if (target == null) return NullTexture;


			if (target.HasProperty(PropertyName) && target.GetTexture(PropertyName) != null)
			{
				return (Texture2D)target.GetTexture(PropertyName);
			}
			else
			{
				return NullTexture;
			}
		}
		private Color extractcolor(Material target, string PropertyName)
		{
			if (target == null) return Color.white;


			if (target.HasProperty(PropertyName) && target.GetColor(PropertyName) != null)
			{
				return target.GetColor(PropertyName);
			}
			else
			{
				return Color.white;
			}
		}

		private float extractfloat(Material target, string PropertyName)
		{
			if (target == null) return 1;


			if (target.HasProperty(PropertyName))
			{
				return target.GetFloat(PropertyName);
			}
			else
			{
				return 1;
			}
		}

		public bool AreTexturesReadable()
		{
			if (!IsReadable(OcclusionTextures)) return false;
			if (!IsReadable(DiffuseTextures)) return false;
			if (!IsReadable(EmmissiveTextures)) return false;
			if (!IsReadable(HeightTextures)) return false;
			if (!IsReadable(MetallicTextures)) return false;
			if (!IsReadable(NormalTextures)) return false;

			return true;
		}

		private bool IsReadable(Texture2D[] textures)
		{
			if (textures != null)
			{
				for (int i = 0; i < textures.Length; i++)
				{
					if(textures[i])
					{
						if (!textures[i].isReadable) return false;
					}
				}
			}

			return true;
		}

		public void SetTextureReadable(Texture2D texture, bool isReadable)
		{
			if (null == texture) return;

			string assetPath = AssetDatabase.GetAssetPath(texture);
			var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if (tImporter != null)
			{

				tImporter.isReadable = isReadable;

				AssetDatabase.ImportAsset(assetPath);
				AssetDatabase.Refresh();
			}
		}

		public void AssignTextureArray(string TextureName, Texture2DArray textureArray)
		{
			MaterialProperty property = MaterialEditor.GetMaterialProperty(new UnityEngine.Object[] { TargetMaterial }, TextureName);
			if (property.textureDimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
			{
				TargetMaterial.SetTexture(TextureName, textureArray);
			}
			else
			{
				Debug.LogError("Cannot assign texture array: " + TextureName + " to TargetMaterial. Shader has no texture array property with name: " + TextureName);
			}
		}

		public void AssignTexture2D(string TextureName, Texture2D texture2D, Material target)
		{
			MaterialProperty property = MaterialEditor.GetMaterialProperty(new UnityEngine.Object[] { target }, TextureName);
			if (property.textureDimension == UnityEngine.Rendering.TextureDimension.Tex2D)
			{
				target.SetTexture(TextureName, texture2D);
			}
			else
			{
				Debug.LogError("Cannot assign texture array: " + TextureName + " to TargetMaterial. Shader has no texture2D property with name: " + TextureName);
			}
		}

		public void AssignTexture3D(string TextureName, Texture3D textureArray)
		{
			MaterialProperty property = MaterialEditor.GetMaterialProperty(new UnityEngine.Object[] { TargetMaterial }, TextureName);
			if (property.textureDimension == UnityEngine.Rendering.TextureDimension.Tex3D)
			{
				TargetMaterial.SetTexture(TextureName, textureArray);
			}
			else
			{
				Debug.LogError("Cannot assign texture array: " + TextureName + " to TargetMaterial. Shader has no texture3D property with name: " + TextureName);
			}
		}

		public string CheckTexture(string TextureName)
		{
			string output = "Tex2DArray. Suitable for Texturearray";
			MaterialProperty property = MaterialEditor.GetMaterialProperty(new UnityEngine.Object[] { TargetMaterial }, TextureName);
			if (property.textureDimension == UnityEngine.Rendering.TextureDimension.Tex2D)
			{
				output = "Suitable for Atlas";
			}
			else if (property.textureDimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
			{
				output = "Suitable for Texture2DArray";
			}
			else
            {
				output = "Not a Tex2D or Tex2DArray";
			}

			return output;
		}

		public string checkMaterial()
		{
			string output = "Check if Materials can be assigned: \n\n";
			output += "_DiffuseMap" + " \t= " + CheckTexture("_DiffuseMap") + "\n";
			output += "_BumpMap" + " \t= " + CheckTexture("_BumpMap") + "\n";
			output += "_OcclusionMap" + " \t= " + CheckTexture("_OcclusionMap") + "\n";

			output += "_EmissionMap" + " \t= " + CheckTexture("_EmissionMap") + "\n";
			output += "_ParallaxMap" + " \t= " + CheckTexture("_ParallaxMap") + "\n";
			output += "_MetallicGlossMap" + " \t= " + CheckTexture("_MetallicGlossMap") + "\n";


			return output;
		}

		[OnOpenAsset]
		public static bool OnOpenAsset(int instanceID, int line)
        {
			TextureArrayTemplate project = EditorUtility.InstanceIDToObject(instanceID) as TextureArrayTemplate;
			if (project != null)
			{
				TextureArrayGenerator.Init();
				TextureArrayGenerator.texturegenerator = project;
				
				return true;
			}
			return false;
		}
	}



}

#endif
