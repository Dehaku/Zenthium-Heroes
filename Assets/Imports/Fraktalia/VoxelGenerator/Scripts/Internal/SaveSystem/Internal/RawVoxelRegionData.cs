using UnityEngine;
using System.IO;

namespace Fraktalia.VoxelGen.SaveSystem
{
	[System.Serializable]
	public class RawVoxelRegionData
	{
		public bool IsDirty;
		public Vector3Int RegionHash;
		public int RegionSize;
		
		public RawVoxelRegionData(Vector3Int regionHash, int regionSize)
		{
			RegionHash = regionHash;
			RegionSize = regionSize;

			RegionData = new RawVoxelData[RegionSize*RegionSize*RegionSize];
			IsDirty = false;
		}
	


		public RawVoxelData[] RegionData;

		public void Serialize(Stream stream)
		{
			BinaryWriter sw = new BinaryWriter(stream);
			sw.Write(RegionHash.x);
			sw.Write(RegionHash.y);
			sw.Write(RegionHash.z);
			sw.Write(RegionSize);
		

			for (int i = 0; i < RegionData.Length; i++)
			{
				if (!RegionData[i].IsValid)
				{
					sw.Write(0);
				}
				else 
				{
					sw.Write(10);
					RegionData[i].Serialize(stream, sw);
				}
			}
			sw.Close();
		}

		public void Deserialize(Stream stream)
		{
			BinaryReader br = new BinaryReader(stream);
			RegionHash.x = br.ReadInt32();
			RegionHash.y = br.ReadInt32();
			RegionHash.z = br.ReadInt32();
			RegionSize = br.ReadInt32();
			

			RegionData = new RawVoxelData[RegionSize*RegionSize*RegionSize];

			for (int i = 0; i < RegionData.Length; i++)
			{
				int exists = br.ReadInt32();
				if (exists == 10)
				{
					RegionData[i] = new RawVoxelData();
					RegionData[i].Deserialize(stream, br);
				}
			}
			br.Close();
		}
	}
}
