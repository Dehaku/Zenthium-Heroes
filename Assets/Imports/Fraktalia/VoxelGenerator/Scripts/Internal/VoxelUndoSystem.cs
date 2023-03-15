using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Fraktalia.Core.Collections;

namespace Fraktalia.VoxelGen
{
	public static class VoxelUndoSystem
	{
		public static List<VoxelUndoManifest> UndoManifests = new List<VoxelUndoManifest>();
		public static Stack<VoxelUndoManifest> RedoManifests = new Stack<VoxelUndoManifest>();
		public static Stack<VoxelUndoManifest> GarbageDump = new Stack<VoxelUndoManifest>();
		public static Stack<VoxelUndoManifestElement> ElementGarbageDump = new Stack<VoxelUndoManifestElement>();

		private static VoxelUndoManifest currentManifest;

		public static VoxelUndoManifestElement GetManifestElement()
		{
			if (ElementGarbageDump.Count > 0)
			{
				return ElementGarbageDump.Pop();
			}
			return new VoxelUndoManifestElement();
		}

		public static void CreateManifest()
		{
			if (currentManifest == null)
			{
				if(GarbageDump.Count > 0)
				{
					currentManifest = GarbageDump.Pop();
					for (int i = 0; i < currentManifest.UndoData.Count; i++)
					{
						currentManifest.UndoData[i].ClearData();
						ElementGarbageDump.Push(currentManifest.UndoData[i]);
					}

					currentManifest.UndoData.Clear();
				}
				else currentManifest = new VoxelUndoManifest();

			}
				
		}

		public static void AddManifestElement(VoxelUndoManifestElement element)
		{
			if(currentManifest != null)
			{
				currentManifest.UndoData.Add(element);
			}
			else
			{
				ElementGarbageDump.Push(element);
			}
		}

		public static void FinishManifest()
		{
			if (currentManifest != null)
			{
				if (currentManifest.UndoData.Count > 0)
					UndoManifests.Add(currentManifest);

				if (UndoManifests.Count >= 10)
				{				
					GarbageDump.Push(UndoManifests[0]);
					UndoManifests.RemoveAt(0);
				}

				while (RedoManifests.Count > 0)
				{
					GarbageDump.Push(RedoManifests.Pop());
				}

				RedoManifests.Clear();
			}

			currentManifest = null;
		}

		public static void Undo()
		{
			if (UndoManifests.Count > 0)
			{
				VoxelUndoManifest manifest = UndoManifests[UndoManifests.Count - 1];
				UndoManifests.RemoveAt(UndoManifests.Count - 1);
				manifest.Undo();
				RedoManifests.Push(manifest);
			}
		}

		public static void Redo()
		{
			if (RedoManifests.Count > 0)
			{
				VoxelUndoManifest manifest = RedoManifests.Pop();
				manifest.Redo();
				UndoManifests.Add(manifest);
			}
		}

		public static void Dispose()
		{
			for (int i = 0; i < UndoManifests.Count; i++)
			{
				UndoManifests[i].Dispose();
			}
			UndoManifests.Clear();
			while(RedoManifests.Count > 0)
			{
				RedoManifests.Pop().Dispose();
			}
			while (GarbageDump.Count > 0)
			{
				GarbageDump.Pop().Dispose();
			}
			while (ElementGarbageDump.Count > 0)
			{
				ElementGarbageDump.Pop().Dispose();
			}
		}

		public class VoxelUndoManifest
		{
			public List<VoxelUndoManifestElement> UndoData = new List<VoxelUndoManifestElement>();

			public void Undo()
			{
				for (int i = UndoData.Count - 1; i >= 0; i--)
				{
					UndoData[i].Undo();
				}
			}

			public void Redo()
			{
				for (int i = 0; i < UndoData.Count; i++)
				{
					UndoData[i].Redo();
				}
			}

			public void Dispose()
			{
				for (int i = 0; i < UndoData.Count; i++)
				{
					UndoData[i].Dispose();
				}
			}
		}

		public class VoxelUndoManifestElement
		{
			public FNativeList<NativeVoxelModificationData> PreviousData = new FNativeList<NativeVoxelModificationData>();
			public FNativeList<NativeVoxelModificationData_Inner> PreviousData_Inner = new FNativeList<NativeVoxelModificationData_Inner>();

			public FNativeList<NativeVoxelModificationData> ModificationData = new FNativeList<NativeVoxelModificationData>();
			public FNativeList<NativeVoxelModificationData_Inner> ModificationData_Inner = new FNativeList<NativeVoxelModificationData_Inner>();

			public int Dimension;
			public VoxelGenerator AffectedTarget;


			public bool Additive;

			internal VoxelUndoManifestElement()
			{
				PreviousData = new FNativeList<NativeVoxelModificationData>(Unity.Collections.Allocator.Persistent);
				PreviousData_Inner = new FNativeList<NativeVoxelModificationData_Inner>(Unity.Collections.Allocator.Persistent);
				ModificationData = new FNativeList<NativeVoxelModificationData>(Unity.Collections.Allocator.Persistent);
				ModificationData_Inner = new FNativeList<NativeVoxelModificationData_Inner>(Unity.Collections.Allocator.Persistent);
			}


			public void Undo()
			{
				if (AffectedTarget == null || !AffectedTarget.IsInitialized) return;

				if (PreviousData.Length > 0)
				{
					float minX = float.MaxValue;
					float maxX = float.MinValue;
					float minY = float.MaxValue;
					float maxY = float.MinValue;
					float minZ = float.MaxValue;
					float maxZ = float.MinValue;

					AffectedTarget._SetVoxels(PreviousData, Dimension);

					for (int i = 0; i < PreviousData.Length; i++)
					{
						NativeVoxelModificationData data = PreviousData[i];

						minX = Math.Min(data.X, minX);
						minY = Math.Min(data.Y, minY);
						minZ = Math.Min(data.Z, minZ);

						maxX = Math.Max(data.X, maxX);
						maxY = Math.Max(data.Y, maxY);
						maxZ = Math.Max(data.Z, maxZ);
					}

					Vector3 min = new Vector3(minX, minY, minZ);
					Vector3 max = new Vector3(maxX, maxY, maxZ);
					AffectedTarget.SetRegionsDirty(min, max, Dimension);
				}

				if (PreviousData_Inner.Length > 0)
				{
					int minX = int.MaxValue;
					int maxX = int.MinValue;
					int minY = int.MaxValue;
					int maxY = int.MinValue;
					int minZ = int.MaxValue;
					int maxZ = int.MinValue;

					AffectedTarget._SetVoxels_Inner(PreviousData_Inner, Dimension);

					for (int i = 0; i < PreviousData_Inner.Length; i++)
					{
						NativeVoxelModificationData_Inner data = PreviousData_Inner[i];

						if (data.X < minX) minX = data.X;
						if (data.Y < minY) minY = data.Y;
						if (data.Z < minZ) minZ = data.Z;
						if (data.X > maxX) maxX = data.X;
						if (data.Y > maxY) maxY = data.Y;
						if (data.Z > maxZ) maxZ = data.Z;
					}

					Vector3 min = VoxelGenerator.ConvertInnerToLocal(new Vector3Int(minX, minY, minZ), AffectedTarget.RootSize);
					Vector3 max = VoxelGenerator.ConvertInnerToLocal(new Vector3Int(maxX, maxY, maxZ), AffectedTarget.RootSize);
					AffectedTarget.SetRegionsDirty(min, max, Dimension);
				}
			}

			public void Redo()
			{
				if (AffectedTarget == null || !AffectedTarget.IsInitialized) return;

				if (Additive)
				{
					if (ModificationData.Length > 0)
					{
						float minX = float.MaxValue;
						float maxX = float.MinValue;
						float minY = float.MaxValue;
						float maxY = float.MinValue;
						float minZ = float.MaxValue;
						float maxZ = float.MinValue;

						AffectedTarget._SetVoxelsAdditive(ModificationData, Dimension);

						for (int i = 0; i < ModificationData.Length; i++)
						{
							NativeVoxelModificationData data = ModificationData[i];
							if (data.X < minX) minX = data.X;
							if (data.Y < minY) minY = data.Y;
							if (data.Z < minZ) minZ = data.Z;
							if (data.X > maxX) maxX = data.X;
							if (data.Y > maxY) maxY = data.Y;
							if (data.Z > maxZ) maxZ = data.Z;
						}

						Vector3 min = new Vector3(minX, minY, minZ);
						Vector3 max = new Vector3(maxX, maxY, maxZ);
						AffectedTarget.SetRegionsDirty(min, max, Dimension);
					}

					if (ModificationData_Inner.Length > 0)
					{
						int minX = int.MaxValue;
						int maxX = int.MinValue;
						int minY = int.MaxValue;
						int maxY = int.MinValue;
						int minZ = int.MaxValue;
						int maxZ = int.MinValue;



						for (int i = 0; i < ModificationData_Inner.Length; i++)
						{
							NativeVoxelModificationData_Inner data = ModificationData_Inner[i];
							if (data.X < minX) minX = data.X;
							if (data.Y < minY) minY = data.Y;
							if (data.Z < minZ) minZ = data.Z;
							if (data.X > maxX) maxX = data.X;
							if (data.Y > maxY) maxY = data.Y;
							if (data.Z > maxZ) maxZ = data.Z;
						}

						Vector3 min = VoxelGenerator.ConvertInnerToLocal(new Vector3Int(minX, minY, minZ), AffectedTarget.RootSize);
						Vector3 max = VoxelGenerator.ConvertInnerToLocal(new Vector3Int(maxX, maxY, maxZ), AffectedTarget.RootSize);
						AffectedTarget._SetVoxelsAdditive_Inner(ModificationData_Inner, Dimension);
						AffectedTarget.SetRegionsDirty(min, max, Dimension);

					}
				}

			}

			public void ClearData()
			{
				PreviousData.Clear();
				PreviousData_Inner.Clear();
				ModificationData.Clear();
				ModificationData_Inner.Clear();
			}

			public void Dispose()
			{
				if(PreviousData.IsCreated) PreviousData.Dispose();
				if(PreviousData_Inner.IsCreated) PreviousData_Inner.Dispose();
				if (ModificationData.IsCreated) ModificationData.Dispose();
				if (ModificationData_Inner.IsCreated) ModificationData_Inner.Dispose();
			}
		}
	}

	


  
}
