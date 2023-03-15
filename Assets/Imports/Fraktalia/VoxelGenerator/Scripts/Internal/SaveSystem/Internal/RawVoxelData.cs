using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fraktalia.VoxelGen
{
	[System.Serializable]
	public class RawVoxelData
	{
		public bool IsValid;
		public int SubdivisionPower;
		public float RootSize;

		public int VoxelCount;

		[SerializeField]
		[HideInInspector]
		public NativeVoxelModificationData[] VoxelData;


		public int DimensionCount;

		[SerializeField]		
		public AdditionaVoxelData[] AdditionalData;



		public void Serialize(Stream stream, BinaryWriter sw = null)
		{
			bool mustclose = false;
			if (sw == null)
			{
				mustclose = true;
				sw = new BinaryWriter(stream);
			}

			sw.Write(IsValid);
			sw.Write((byte)SubdivisionPower);
			sw.Write(RootSize);
			sw.Write(VoxelCount);

			int count = VoxelCount;
			for (int i = 0; i < count; i++)
			{

				NativeVoxelModificationData nodedata = VoxelData[i];
				sw.Write(nodedata.X);
				sw.Write(nodedata.Y);
				sw.Write(nodedata.Z);
				sw.Write((byte)nodedata.Depth);
				sw.Write((byte)nodedata.ID);
			}

			if (AdditionalData != null && AdditionalData.Length > 0)
			{
				sw.Write(AdditionalData.Length);

				for (int dataindex = 0; dataindex < AdditionalData.Length; dataindex++)
				{
					AdditionaVoxelData adddata = AdditionalData[dataindex];
					sw.Write(adddata.VoxelCount);
					count = adddata.VoxelCount;

					for (int i = 0; i < count; i++)
					{
						NativeVoxelModificationData nodedata = adddata.VoxelData[i];
						sw.Write(nodedata.X);
						sw.Write(nodedata.Y);
						sw.Write(nodedata.Z);
						sw.Write((byte)nodedata.Depth);
						sw.Write((byte)nodedata.ID);
					}
				}

			}
			else
			{
				sw.Write(0);
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

			IsValid = br.ReadBoolean();
			SubdivisionPower = br.ReadByte();
			RootSize = br.ReadSingle();
			VoxelCount = br.ReadInt32();

			int count = VoxelCount;
			VoxelData = new NativeVoxelModificationData[count];
			for (int i = 0; i < count; i++)
			{
				NativeVoxelModificationData nodedata;
				nodedata.X = br.ReadSingle();
				nodedata.Y = br.ReadSingle();
				nodedata.Z = br.ReadSingle();
				nodedata.Depth = br.ReadByte();
				nodedata.ID = br.ReadByte();


				VoxelData[i] = nodedata;
			}

			if (stream.Position == stream.Length)
			{
				DimensionCount = 1;
				AdditionalData = new AdditionaVoxelData[0];

				if (mustclose)
					br.Close();
				return;
			}
			else
			{
				int additionaldatacount = br.ReadInt32();
				AdditionalData = new AdditionaVoxelData[additionaldatacount];

				for (int dataindex = 0; dataindex < additionaldatacount; dataindex++)
				{
					AdditionaVoxelData adddata = new AdditionaVoxelData();
					adddata.VoxelCount = br.ReadInt32();
					count = adddata.VoxelCount;
					adddata.VoxelData = new NativeVoxelModificationData[count];

					for (int i = 0; i < count; i++)
					{
						NativeVoxelModificationData nodedata;
						nodedata.X = br.ReadSingle();
						nodedata.Y = br.ReadSingle();
						nodedata.Z = br.ReadSingle();
						nodedata.Depth = br.ReadByte();
						nodedata.ID = br.ReadByte();
						adddata.VoxelData[i] = nodedata;
					}
					AdditionalData[dataindex] = adddata;
				}

				DimensionCount = 1 + additionaldatacount;
			}




			if (mustclose)
				br.Close();

		}

		public IEnumerator DeserializeDynamic(Stream stream, BinaryReader br, int LoadSpeed)
		{
			stream.Position = 0;


			IsValid = br.ReadBoolean();
			SubdivisionPower = br.ReadByte();
			RootSize = br.ReadSingle();

			VoxelCount = br.ReadInt32();
			int count = VoxelCount;
			int step = 0;
			VoxelData = new NativeVoxelModificationData[count];
			for (int i = 0; i < count; i++)
			{
				NativeVoxelModificationData nodedata;
				nodedata.X = br.ReadSingle();
				nodedata.Y = br.ReadSingle();
				nodedata.Z = br.ReadSingle();
				nodedata.Depth = br.ReadByte();
				nodedata.ID = br.ReadByte();

				VoxelData[i] = nodedata;
				if (step > LoadSpeed)
				{
					step = 0;
					yield return null;
				}
				step++;
			}

			if (stream.Position == stream.Length)
			{
				DimensionCount = 1;
				yield break;
			}
			else
			{
				int additionaldatacount = br.ReadInt32();
				AdditionalData = new AdditionaVoxelData[additionaldatacount];

				for (int dataindex = 0; dataindex < additionaldatacount; dataindex++)
				{
					AdditionaVoxelData adddata = new AdditionaVoxelData();
					adddata.VoxelCount = br.ReadInt32();
					count = adddata.VoxelCount;
					adddata.VoxelData = new NativeVoxelModificationData[count];

					for (int i = 0; i < count; i++)
					{
						NativeVoxelModificationData nodedata;
						nodedata.X = br.ReadSingle();
						nodedata.Y = br.ReadSingle();
						nodedata.Z = br.ReadSingle();
						nodedata.Depth = br.ReadByte();
						nodedata.ID = br.ReadByte();
						adddata.VoxelData[i] = nodedata;
						if (step > LoadSpeed)
						{
							step = 0;
							yield return null;
						}
						step++;
					}

					AdditionalData[dataindex] = adddata;
				}

				DimensionCount = 1 + additionaldatacount;
			}
		}
	
	}

}
