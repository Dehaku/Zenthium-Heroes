using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Fraktalia.VoxelGen
{
	public unsafe class RawVoxelData_V2
	{
		public int Version;

		public bool IsValid;
		public int SubdivisionPower;
		public float RootSize;		
		public int DimensionCount;


		public int[] VoxelCount;
		public List<byte[]> BytedVoxelData = new List<byte[]>();
		
		
	
		

		public void Serialize(Stream stream, BinaryWriter sw = null)
		{
			bool mustclose = false;
			if (sw == null)
			{
				mustclose = true;
				sw = new BinaryWriter(stream);
			}

			sw.Write((Int32)Version);
			sw.Write(IsValid);
			sw.Write((Int32)SubdivisionPower);
			sw.Write(RootSize);			
			sw.Write((Int32)DimensionCount);

			for (int i = 0; i < DimensionCount; i++)
			{
				int count = VoxelCount[i];
				sw.Write((Int32)count);				
			}

			for (int i = 0; i < DimensionCount; i++)
			{
				byte[] data = BytedVoxelData[i];
				sw.Write(data);
			}

			if (mustclose) sw.Close();


		}

		public void Deserialize(Stream stream, BinaryReader br = null)
		{
			bool mustclose = false;
			if (br == null)
			{
				mustclose = true;
				br = new BinaryReader(stream);
			}

			Version = br.ReadInt32();
			IsValid = br.ReadBoolean();
			SubdivisionPower = br.ReadInt32();
			RootSize = br.ReadSingle();
			DimensionCount = br.ReadInt32();

			VoxelCount = new int[DimensionCount];
			for (int i = 0; i < DimensionCount; i++)
			{
				VoxelCount[i] = br.ReadInt32();
			}

			int elementSize = UnsafeUtility.SizeOf<NativeVoxelModificationData>();

			if (DimensionCount != BytedVoxelData.Count)
			{
				BytedVoxelData = new List<byte[]>();
				for (int i = 0; i < DimensionCount; i++)
				{
					byte[] list = br.ReadBytes(VoxelCount[i] * elementSize);
					BytedVoxelData.Add(list);
				}
			}
			else
			{
				for (int i = 0; i < DimensionCount; i++)
				{
					BytedVoxelData[i] = (br.ReadBytes(VoxelCount[i] * elementSize));				
				}
			}


			if (mustclose)
				br.Close();
		}

		internal IEnumerator DeserializeDynamic(Stream stream, BinaryReader br)
		{
			stream.Position = 0;

			Version = br.ReadInt32();
			IsValid = br.ReadBoolean();
			SubdivisionPower = br.ReadInt32();
			RootSize = br.ReadSingle();
			DimensionCount = br.ReadInt32();

			VoxelCount = new int[DimensionCount];
			for (int i = 0; i < DimensionCount; i++)
			{
				VoxelCount[i] = br.ReadInt32();
			}

			int elementSize = UnsafeUtility.SizeOf<NativeVoxelModificationData>();

			if (DimensionCount != BytedVoxelData.Count)
			{
				BytedVoxelData = new List<byte[]>();
				for (int i = 0; i < DimensionCount; i++)
				{
					byte[] list = br.ReadBytes(VoxelCount[i] * elementSize);
					BytedVoxelData.Add(list);
					yield return null;
				}
			}
			else
			{
				for (int i = 0; i < DimensionCount; i++)
				{				
					BytedVoxelData[i] = br.ReadBytes(VoxelCount[i] * elementSize);
					yield return null;
				}
			}	
		}

		public int GetByteSize()
		{
			int size = 0;
			
			size+= sizeof(Int32);
			size+= sizeof(bool);
			size+= sizeof(Int32);
			size+= sizeof(float);
			size+= sizeof(Int32);

			for (int i = 0; i < DimensionCount; i++)
			{			
				size += sizeof(Int32);
			}

			for (int i = 0; i < DimensionCount; i++)
			{
				byte[] data = BytedVoxelData[i];
				size += sizeof(byte) * data.Length;			
			}

			return size;
		}


	}

}
