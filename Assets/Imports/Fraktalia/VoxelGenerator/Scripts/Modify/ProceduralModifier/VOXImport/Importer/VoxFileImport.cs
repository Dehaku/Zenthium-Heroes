using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using Fraktalia.Core.Math;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Fraktalia.VoxelGen.Modify.Import
{
	[System.Serializable]
	public struct VoxFileHeader
	{
		public int[] header;
		public int version;
	}

	[System.Serializable]
	public struct VoxFilePack
	{
		public int[] name;
		public int chunkContent;
		public int chunkNums;
		public int modelNums;
	}

	[System.Serializable]
	public struct VoxFileSize
	{
		public int[] name;
		public int chunkContent;
		public int chunkNums;
		public int x;
		public int y;
		public int z;
	}

	[System.Serializable]
	public struct VoxFileXYZI
	{
		public byte[] name;
		public int chunkContent;
		public int chunkNums;
		public VoxData voxels;
	}

	[System.Serializable]
	public struct VoxFileRGBA
	{
		public byte[] name;
		public int chunkContent;
		public int chunkNums;
		public Color32[] values;
	}

	[System.Serializable]
	public struct VoxFileChunkChild
	{
		public VoxFileSize size;
		public VoxFileXYZI xyzi;
	}

	[System.Serializable]
	public struct VoxFileChunk
	{
		public int[] name;
		public int chunkContent;
		public int chunkNums;
	}

	[System.Serializable]
	public struct VoxFileMaterial
	{
		public int id;
		public int type;
		public float weight;
		public int propertyBits;
		public float[] propertyValue;
	}

	[System.Serializable]
	public class VoxFileData
	{
		public VoxFileHeader hdr;
		public VoxFileChunk main;
		public VoxFilePack pack;
		public List<VoxFileChunkChild> chunkChild;
		public VoxFileRGBA palette;
	}

	[System.Serializable]
	public class VoxData
	{
		public int maxX, maxY, maxZ;

		[HideInInspector]
		public int[] voxels;

		public int count
		{
			get
			{
				int _count = 0;

				for (int i = 0; i < maxX; ++i)
				{
					for (int j = 0; j < maxY; ++j)
						for (int k = 0; k < maxZ; ++k)
							if (voxels[MathUtilities.Convert3DTo1D(i, j, k, maxX,maxY,maxZ)] != int.MaxValue)
								_count++;
				}

				return _count;
			}
		}

		public VoxData()
		{
			maxX = 0; maxY = 0; maxZ = 0;
		}

		public VoxData(byte[] _voxels, int xx, int yy, int zz)
		{
			maxX = xx;
			maxY = zz;
			maxZ = yy;
			voxels = new int[maxX * maxY * maxZ];

			for (int i = 0; i < maxX; ++i)
			{
				for (int j = 0; j < maxY; ++j)
					for (int k = 0; k < maxZ; ++k)
						voxels[MathUtilities.Convert3DTo1D(i, j, k, maxX, maxY, maxZ)] = int.MaxValue;
			}

			for (int j = 0; j < _voxels.Length; j += 4)
			{
				var px = _voxels[j];
				var py = _voxels[j + 1];
				var pz = _voxels[j + 2];
				var c = _voxels[j + 3];
				
				voxels[MathUtilities.Convert3DTo1D(px, pz, py, maxX, maxY, maxZ)] = c;
			}
		}

		public int GetMajorityColorIndex(int xx, int yy, int zz, int lodLevel)
		{
			xx = Mathf.Min(xx, maxX - 2);
			yy = Mathf.Min(yy, maxY - 2);
			zz = Mathf.Min(zz, maxZ - 2);

			int[] samples = new int[lodLevel * lodLevel * lodLevel];

			for (int i = 0; i < lodLevel; i++)
			{
				for (int j = 0; j < lodLevel; j++)
				{
					for (int k = 0; k < lodLevel; k++)
					{
						if (xx + i > maxX - 1 || yy + j > maxY - 1 || zz + k > maxZ - 1)
							samples[i * lodLevel * lodLevel + j * lodLevel + k] = int.MaxValue;
						else
							samples[i * lodLevel * lodLevel + j * lodLevel + k] = voxels[MathUtilities.Convert3DTo1D(xx + i, yy + j, zz + k, maxX, maxY, maxZ)];
					}
				}
			}

			int maxNum = 1;
			int maxNumIndex = 0;

			int[] numIndex = new int[samples.Length];

			for (int i = 0; i < samples.Length; i++)
				numIndex[i] = samples[i] == int.MaxValue ? 0 : 1;

			for (int i = 0; i < samples.Length; i++)
			{
				for (int j = 0; j < samples.Length; j++)
				{
					if (i != j && samples[i] != int.MaxValue && samples[i] == samples[j])
					{
						numIndex[i]++;
						if (numIndex[i] > maxNum)
						{
							maxNum = numIndex[i];
							maxNumIndex = i;
						}
					}
				}
			}

			return samples[maxNumIndex];
		}

		public VoxData GetVoxelDataLOD(int level)
		{
			if (maxX <= 1 || maxY <= 1 || maxZ <= 1)
				return null;

			level = Mathf.Clamp(level, 0, 16);
			if (level <= 1)
				return this;

			if (maxX <= level && maxY <= level && maxZ <= level)
				return this;

			VoxData data = new VoxData();
			data.maxX = Mathf.CeilToInt((float)maxX / level);
			data.maxY = Mathf.CeilToInt((float)maxY / level);
			data.maxZ = Mathf.CeilToInt((float)maxZ / level);

			data.voxels = new int[data.maxX * data.maxY * data.maxZ];

			for (int x = 0; x < data.maxX; x++)
			{
				for (int y = 0; y < data.maxY; y++)
				{
					for (int z = 0; z < data.maxZ; z++)
					{
						data.voxels[MathUtilities.Convert3DTo1D(x, y, z, data.maxX, data.maxY, data.maxZ)] = this.GetMajorityColorIndex(x * level, y * level, z * level, level);
					}
				}
			}

			return data;
		}

		public int GetVoxel(int x, int y, int z)
		{		
			return voxels[MathUtilities.Convert3DTo1D(x, y, z, maxX, maxY, maxZ)];
		}
	}

	public class VoxFileImport
	{
		private static uint[] _paletteDefault = new uint[256]
		{
				0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
				0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
				0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
				0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
				0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
				0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
				0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
				0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
				0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
				0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
				0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
				0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
				0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
				0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
				0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
				0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
		};

		private static UnityEngine.Object _assetPrefab;

		public static int[] byteToIntArray(byte[] input)
		{
			int[] result = new int[input.Length];
			for (int i = 0; i < 4; i++)
			{
				result[i] = input[i];
			}
			return result;
		}

		public static VoxFileData Load(string path)
		{
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				if (stream == null)
					throw new System.Exception("Failed to open file for FileStream.");

				using (var reader = new BinaryReader(stream))
				{
					VoxFileData voxel = new VoxFileData();
					voxel.hdr.header = new int[4];
					byte[] bytes = reader.ReadBytes(4);
					for (int i = 0; i < 4; i++)
					{
						voxel.hdr.header[i] = bytes[i];
					} 

					voxel.hdr.version = reader.ReadInt32();

					if (voxel.hdr.header[0] != 'V' || voxel.hdr.header[1] != 'O' || voxel.hdr.header[2] != 'X' || voxel.hdr.header[3] != ' ')
						throw new System.Exception("Bad Token: token is not VOX.");

					if (voxel.hdr.version != 150)
					{
						Debug.LogWarning("The version of file isn't 150 that version of vox, tihs version of file is " + voxel.hdr.version + ".");
						
					}
					

					voxel.main.name = byteToIntArray(reader.ReadBytes(4));
					voxel.main.chunkContent = reader.ReadInt32();
					voxel.main.chunkNums = reader.ReadInt32();

					if (voxel.main.name[0] != 'M' || voxel.main.name[1] != 'A' || voxel.main.name[2] != 'I' || voxel.main.name[3] != 'N')
						throw new System.Exception("Bad Token: token is not MAIN.");

					if (voxel.main.chunkContent != 0)
						throw new System.Exception("Bad Token: chunk content is " + voxel.main.chunkContent + ", it should be 0.");

					char p = (char)reader.PeekChar();
					if (reader.PeekChar() == 'P')
					{
						voxel.pack.name = byteToIntArray(reader.ReadBytes(4));
						if (voxel.pack.name[0] != 'P' || voxel.pack.name[1] != 'A' || voxel.pack.name[2] != 'C' || voxel.pack.name[3] != 'K')
							throw new System.Exception("Bad Token: token is not PACK");

						voxel.pack.chunkContent = reader.ReadInt32();
						voxel.pack.chunkNums = reader.ReadInt32();
						voxel.pack.modelNums = reader.ReadInt32();

						if (voxel.pack.modelNums == 0)
							throw new System.Exception("Bad Token: model nums must be greater than zero.");
					}
					else
					{
						voxel.pack.chunkContent = 0;
						voxel.pack.chunkNums = 0;
						voxel.pack.modelNums = 1;
					}

					voxel.chunkChild = new List<VoxFileChunkChild>();

					while (true)
					{
						var chunk = new VoxFileChunkChild();

						chunk.size.name = byteToIntArray(reader.ReadBytes(4));
						chunk.size.chunkContent = reader.ReadInt32();
						chunk.size.chunkNums = reader.ReadInt32();
						chunk.size.x = reader.ReadInt32();
						chunk.size.y = reader.ReadInt32();
						chunk.size.z = reader.ReadInt32();

						if (chunk.size.name[0] != 'S' || chunk.size.name[1] != 'I' || chunk.size.name[2] != 'Z' || chunk.size.name[3] != 'E')
							throw new System.Exception("Bad Token: token is not SIZE");

						if (chunk.size.chunkContent != 12)
							throw new System.Exception("Bad Token: chunk content is " + chunk.size.chunkContent + ", it should be 12.");

						chunk.xyzi.name = reader.ReadBytes(4);
						if (chunk.xyzi.name[0] != 'X' || chunk.xyzi.name[1] != 'Y' || chunk.xyzi.name[2] != 'Z' || chunk.xyzi.name[3] != 'I')
							throw new System.Exception("Bad Token: token is not XYZI");

						chunk.xyzi.chunkContent = reader.ReadInt32();
						chunk.xyzi.chunkNums = reader.ReadInt32();
						if (chunk.xyzi.chunkNums != 0)
							throw new System.Exception("Bad Token: chunk nums is " + chunk.xyzi.chunkNums + ",i t should be 0.");

						var voxelNums = reader.ReadInt32();
						var voxels = new byte[voxelNums * 4];
						if (reader.Read(voxels, 0, voxels.Length) != voxels.Length)
							throw new System.Exception("Failed to read voxels");

						chunk.xyzi.voxels = new VoxData(voxels, chunk.size.x, chunk.size.y, chunk.size.z);

						voxel.chunkChild.Add(chunk);

						if(reader.PeekChar() != 'S')
						{
							break;
						}
					}

					while (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						byte[] palette = reader.ReadBytes(4);

						char x = (char)palette[0];
						char x2 = (char)palette[1];
						char x3 = (char)palette[2];
						char x4 = (char)palette[3];
						reader.BaseStream.Position -= 4;
						if (palette[0] == 'R' && palette[1] == 'G' && palette[2] == 'B' && palette[3] == 'A')
						{
							break;
						}
						reader.ReadByte();
						
					}

					if (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						

						byte[] palette = reader.ReadBytes(4);

						char x = (char)palette[0];
						char x2 = (char)palette[1];
						char x3 = (char)palette[2];
						char x4 = (char)palette[3];

						if (palette[0] != 'R' || palette[1] != 'G' || palette[2] != 'B' || palette[3] != 'A')
							throw new System.Exception("Bad Token: token is not RGBA");

						voxel.palette.chunkContent = reader.ReadInt32();
						voxel.palette.chunkNums = reader.ReadInt32();

						var bytePalette = new byte[voxel.palette.chunkContent];
						reader.Read(bytePalette, 0, voxel.palette.chunkContent);

						voxel.palette.values = new Color32[voxel.palette.chunkContent / 4];

						for (int i = 4; i < bytePalette.Length; i += 4)
						{
							int value = (int)BitConverter.ToUInt32(bytePalette, i - 4);
							Color32 color = new Color32(bytePalette[i - 4], bytePalette[i - 3], bytePalette[i - 2], bytePalette[i - 1]);
							voxel.palette.values[i / 4] = color;
						}
							

					}
					else
					{
						voxel.palette.values = new Color32[256];
						_paletteDefault.CopyTo(voxel.palette.values, 0);
					}

					return voxel;
				}
			}
		}

		public static Color32[] CreateColor32FromPelatte(uint[] palette)
		{
			Debug.Assert(palette.Length == 256);

			Color32[] colors = new Color32[256];

			for (uint j = 0; j < 256; j++)
			{
				uint rgba = palette[j];

				Color32 color = new Color32();
				color.r = (byte)((rgba >> 0) & 0xFF);
				color.g = (byte)((rgba >> 8) & 0xFF);
				color.b = (byte)((rgba >> 16) & 0xFF);
				color.a = (byte)((rgba >> 24) & 0xFF);

				colors[j] = color;
			}

			return colors;
		}

		public static Texture2D CreateTextureFromColor16x16(Color32[] colors)
		{
			Debug.Assert(colors.Length == 256);

			Texture2D texture = new Texture2D(16, 16, TextureFormat.ARGB32, false, false);
			texture.name = "texture";
			texture.SetPixels32(colors);
			texture.Apply();

			return texture;
		}

		public static Texture2D CreateTextureFromColor256(Color32[] colors)
		{
			Debug.Assert(colors.Length == 256);

			Texture2D texture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false);
			texture.name = "texture";
			texture.SetPixels32(colors);
			texture.Apply();

			return texture;
		}

		public static Texture2D CreateTextureFromPelatte16x16(uint[] palette)
		{
			Debug.Assert(palette.Length == 256);
			return CreateTextureFromColor16x16(CreateColor32FromPelatte(palette));
		}
	}
}
