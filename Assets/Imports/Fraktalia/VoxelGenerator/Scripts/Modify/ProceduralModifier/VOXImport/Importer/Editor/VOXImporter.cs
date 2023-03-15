#if UNITY_EDITOR


using UnityEngine;

using System.IO;
using Fraktalia.Core.Math;

namespace Fraktalia.VoxelGen.Modify.Import
{

	[UnityEditor.AssetImporters.ScriptedImporter(1, "vox")]
	public class VOXImporter : UnityEditor.AssetImporters.ScriptedImporter
	{
		public bool CreateTextureAtlas;

		public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
		{
			VOXObject asset = ScriptableObject.CreateInstance<VOXObject>();
			asset.CurrentData = VoxFileImport.Load(ctx.assetPath); 
			ctx.AddObjectToAsset("Text", asset);
			ctx.SetMainObject(asset);

			if(CreateTextureAtlas)
			{
				Texture2D tex = new Texture2D(256, 256);
				for (int x = 0; x < 256; x++)
				{
					for (int y = 0; y < 256; y++)
					{
						int index = MathUtilities.Convert2DTo1D(x/16, y/16, 16);
						Color32 color = asset.CurrentData.palette.values[index];
						tex.SetPixel(x, y, color);
					}
				}
				tex.name = "PaletteTexture";
				tex.filterMode = FilterMode.Point;
				tex.Apply();
				ctx.AddObjectToAsset("Palette", tex);

			
			}
			

		}
	}
}


#endif
