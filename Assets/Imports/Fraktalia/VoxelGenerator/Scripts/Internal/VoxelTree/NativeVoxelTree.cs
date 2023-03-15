using Fraktalia.Core.Collections;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Fraktalia.VoxelGen
{

	[BurstCompile]
	public unsafe struct NativeVoxelTree
	{
#if VOXEL_DEBUG
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<VoxelTreeDebugInformation> DebugInformation;
#endif
		//16 is megasmall already
		public const int MaxDepth = 16;
		//2 ^ 16 so bottem depth = 16;
		public const int INNERWIDTH = 65536;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public IntPtr OctreePositionTable;

		[ReadOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<float> SizeTable;

		

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelNode> NodestackBuffer;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> indexstack;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelNode> destroyedNodes;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<double> Errors;



		public float RootSize;
		public float halfSize;
		public int startposition_X;
		public int startposition_Y;
		public int startposition_Z;
		public int SubdivisionPower;
		public int SubdivisionPowerBitShift;

		public int MaxIndexDirection;

		public int SubdivisionPower_squared;
		public int NodeChildrenCount;

		public Vector3Int AssignedChunk;


		[NativeDisableUnsafePtrRestriction]
		public IntPtr _ROOT;

		[NativeDisableContainerSafetyRestriction]
		NativeArray<IntPtr> Neighbors;
		


		public bool IsCreated { get; private set; }

		public NativeVoxelReservoir voxelReservoir;


		public void _Initialize(NativeVoxelReservoir reservoir, float rootSize, int subdivisionPower, byte initialID, int MaxCores)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if(subdivisionPower < 2 || subdivisionPower > 5)
			{
				throw new InvalidOperationException("Initializing voxel tree with invalid subdivision power: " + subdivisionPower + "\n" +
					"Only values between 2 and 5 are allowed");
			}
#endif

#if VOXEL_DEBUG
			DebugInformation = new NativeArray<VoxelTreeDebugInformation>(1, Allocator.Persistent);
#endif

			voxelReservoir = reservoir;

			RootSize = rootSize;
			halfSize = rootSize / 2;
			SubdivisionPower = subdivisionPower;
			MaxIndexDirection = SubdivisionPower - 1;
			SubdivisionPowerBitShift = Mathf.ClosestPowerOfTwo(SubdivisionPower)/2;
			SubdivisionPower_squared = subdivisionPower * subdivisionPower;
			NodeChildrenCount = SubdivisionPower * SubdivisionPower * SubdivisionPower;
			startposition_X = startposition_Y = startposition_Z = INNERWIDTH /2;
		
			OctreePositionTable = (IntPtr)UnsafeUtility.Malloc(sizeof(Vector3Int) * SubdivisionPower * SubdivisionPower * SubdivisionPower, UnsafeUtility.AlignOf<Vector3Int>(), Allocator.Persistent);
			//SizeTable = new NativeArray<float>();

			NodestackBuffer = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
			indexstack = new NativeArray<int>(MaxDepth * MaxCores, Allocator.Persistent);
			
			destroyedNodes = new FNativeList<NativeVoxelNode>(Allocator.Persistent);
		
			int index = 0;
			for (int x = 0; x < SubdivisionPower; x++)
			{
				for (int y = 0; y < SubdivisionPower; y++)
				{
					for (int z = 0; z < SubdivisionPower; z++)
					{						
						UnsafeUtility.WriteArrayElement(OctreePositionTable.ToPointer(), index, new Vector3Int(-SubdivisionPower + 1, -SubdivisionPower + 1, -SubdivisionPower + 1) + new Vector3Int(z * 2, y * 2, x * 2));
						index++;					
					}
				}
			}


			SizeTable = new NativeArray<float>(MaxDepth, Allocator.Persistent);
		
			for (int i = 0; i < MaxDepth; i++)
			{			
				int size = INNERWIDTH / (int)Mathf.Pow(SubdivisionPower, i);
				SizeTable[i] = ConvertInnerToLocal(size, rootSize);				
			}

			_ROOT = reservoir.ObtainNodeAddress();

			NativeVoxelNode voxel = new NativeVoxelNode(0, 255, initialID, IntPtr.Zero, _ROOT, SubdivisionPower);
			voxel.X = INNERWIDTH / 2;
			voxel.Y = INNERWIDTH / 2;
			voxel.Z = INNERWIDTH / 2;

			UnsafeUtility.WriteArrayElement<NativeVoxelNode>(_ROOT.ToPointer(), 0, voxel);

			IsCreated = true;

			Errors = new NativeArray<double>(10, Allocator.Persistent);
			for (int i = 0; i < Errors.Length; i++)
			{
				Errors[i] = -1000;
			}

			Neighbors = new NativeArray<IntPtr>(27, Allocator.Persistent);
			for (int i = 0; i < Neighbors.Length; i++)
			{
				Neighbors[i] = IntPtr.Zero;
			}

			//Performance Test to compare stuff while optimizing.
			/*
			Stopwatch watch = new Stopwatch();
			int result = 0;
			watch.Start();
			
			for (int i = 0; i < 1000000; i++)
			{
				//result = UnsafeUtility.ReadArrayElement<int>(sizeTable, 4) >> 1;
				result = NativeVoxelTree.INNERWIDTH >> SubdivisionPowerBitShift * 4 >> 1;
			}
			watch.Stop();
			UnityEngine.Debug.Log(result + " " + watch.ElapsedTicks);
			*/
		}

		public void UpdateScale(float rootSize)
		{
			RootSize = rootSize;
			halfSize = rootSize / 2;

			for (int i = 0; i < MaxDepth; i++)
			{
				int size = INNERWIDTH / (int)Mathf.Pow(SubdivisionPower, i);
				SizeTable[i] = ConvertInnerToLocal(size, rootSize);
			}
		}


		public void _GetVoxel(float fX, float fY, float fZ, byte depth, out NativeVoxelNode output)
		{
			fX = Mathf.Clamp(fX, 0, RootSize - 0.001f);
			fY = Mathf.Clamp(fY, 0, RootSize - 0.001f);
			fZ = Mathf.Clamp(fZ, 0, RootSize - 0.001f);
			int X = ConvertLocalToInner(fX, RootSize);
			int Y = ConvertLocalToInner(fY, RootSize);
			int Z = ConvertLocalToInner(fZ, RootSize);

			int nodeSize = INNERWIDTH;

			int bitshift = SubdivisionPowerBitShift;
			int bitshiftsqr = bitshift << 1;
			int nodechildrencount = NodeChildrenCount;

			NativeVoxelNode current = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(_ROOT.ToPointer(), 0);
		
			int position_X = startposition_X;
			int position_Y = startposition_Y;
			int position_Z = startposition_Z;

			for (int i = 0; i < depth; i++)
			{
				int index = 0;
				nodeSize = nodeSize >> bitshift;

				int fractionedX = (X >> (16 - (i + 1) * bitshift));
				int fractionedY = (Y >> (16 - (i + 1) * bitshift));
				int fractionedZ = (Z >> (16 - (i + 1) * bitshift));

				index += (fractionedX);
				index += (fractionedY << bitshift);
				index += (fractionedZ << bitshiftsqr);

				index = index & (nodechildrencount - 1);


				X = (X & (nodeSize - 1));
				Y = (Y & (nodeSize - 1));
				Z = (Z & (nodeSize - 1));

				current.GetNode(index, ref this, out current);
			
				int halfsize = nodeSize >> 1;

				Vector3Int octreeindex = UnsafeUtility.ReadArrayElement<Vector3Int>(OctreePositionTable.ToPointer(), (int)index);

				position_X += halfsize * octreeindex.x;
				position_Y += halfsize * octreeindex.y;
				position_Z += halfsize * octreeindex.z;

				current.X = position_X;
				current.Y = position_Y;
				current.Z = position_Z;


			}



			output = current;

		}

		public void _GetVoxel_InnerCoordinate(int X, int Y, int Z, byte depth, out NativeVoxelNode output)
		{
			X = Mathf.Clamp(X, 0, INNERWIDTH - 1);
			Y = Mathf.Clamp(Y, 0, INNERWIDTH - 1);
			Z = Mathf.Clamp(Z, 0, INNERWIDTH - 1);
			
			int nodeSize = INNERWIDTH;

			int bitshift = SubdivisionPowerBitShift;
			int bitshiftsqr = bitshift << 1;
			int nodechildrencount = NodeChildrenCount;

			NativeVoxelNode current = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(_ROOT.ToPointer(), 0);

			int position_X = startposition_X;
			int position_Y = startposition_Y;
			int position_Z = startposition_Z;

			for (int i = 0; i < depth; i++)
			{
				int index = 0;
				nodeSize = nodeSize >> bitshift;

				int fractionedX = (X >> (16 - (i + 1) * bitshift));
				int fractionedY = (Y >> (16 - (i + 1) * bitshift));
				int fractionedZ = (Z >> (16 - (i + 1) * bitshift));

				index += (fractionedX);
				index += (fractionedY << bitshift);
				index += (fractionedZ << bitshiftsqr);

				index = index & (nodechildrencount - 1);


				X = (X & (nodeSize - 1));
				Y = (Y & (nodeSize - 1));
				Z = (Z & (nodeSize - 1));

				current.GetNode(index, ref this, out current);

				int halfsize = nodeSize >> 1;

				Vector3Int octreeindex = UnsafeUtility.ReadArrayElement<Vector3Int>(OctreePositionTable.ToPointer(), (int)index);

				position_X += halfsize * octreeindex.x;
				position_Y += halfsize * octreeindex.y;
				position_Z += halfsize * octreeindex.z;

				current.X = position_X;
				current.Y = position_Y;
				current.Z = position_Z;


			}



			output = current;

		}


		/// <summary>
		/// Attempts to get voxel of a given depth. Returns lowest possible voxel if no voxel at given depth exists
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <param name="Z"></param>
		/// <param name="depth"></param>
		/// <param name="output"></param>
		public bool _TryGetVoxel(float fX, float fY, float fZ, byte depth, out NativeVoxelNode output)
		{
			

			int subdivpower = SubdivisionPower;
			int subdivsqr = SubdivisionPower_squared;
			int nodeSize = INNERWIDTH;

			NativeVoxelNode current = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(_ROOT.ToPointer(), 0);

			if (fX >= RootSize - 0.001f || fX <= 0 ||
			fY >= RootSize - 0.001f || fY <= 0 ||
		   fZ >= RootSize - 0.001f || fZ <= 0)
			{
				output = new NativeVoxelNode();
				return false;
			}

			
			int X = ConvertLocalToInner(fX, RootSize);
			int Y = ConvertLocalToInner(fY, RootSize);
			int Z = ConvertLocalToInner(fZ, RootSize);

			int position_X = startposition_X;
			int position_Y = startposition_Y;
			int position_Z = startposition_Z;
			

			for (byte i = 0; i < depth; i++)
			{
				int index = 0;

				nodeSize /= subdivpower;
				int fractionedX = (X / nodeSize);
				int fractionedY = (Y / nodeSize);
				int fractionedZ = (Z / nodeSize);

				index += (fractionedX);
				index += (fractionedY * subdivpower);
				index += (fractionedZ * subdivsqr);

				index = index % NodeChildrenCount;

				X = X % nodeSize;
				Y = Y % nodeSize;
				Z = Z % nodeSize;

				if (!current.HasChildren())
				{
					output = current;
					return false;
				}
				else
				{
					current.GetChild(index, out current);
				}

				int halfsize = nodeSize / 2;

				Vector3Int octreeindex = UnsafeUtility.ReadArrayElement<Vector3Int>(OctreePositionTable.ToPointer(), index);

				position_X += halfsize * octreeindex.x;
				position_Y += halfsize * octreeindex.y;
				position_Z += halfsize * octreeindex.z;

				current.X = position_X;
				current.Y = position_Y;
				current.Z = position_Z;
			}



			output = current;
			return true;
		}

		/// <summary>
		/// Returns the voxel ID at a given local target coordinate.
		/// </summary>
		/// <param name="X">Local Position X</param>
		/// <param name="Y">Local Position Y</param>
		/// <param name="Z">Local Position Z</param>
		/// <param name="depth">Maximum Lookup Depth (like 10)</param>
		/// <param name="shrink">Keep as it is.</param>
		/// <param name="borderValue">Value used if lookup coordinate is out of bounds</param>
		/// <returns></returns>
		public int _PeekVoxelId(float fX, float fY, float fZ, byte depth, float shrink = 0, int borderValue = 0)
		{
			int blockindex = 13;
			float nfX = fX;
			float nfY = fY;
			float nfZ = fZ;

			if (fX >= RootSize)
			{
				blockindex += 1;
				nfX -= RootSize;

			}

			if (fX < shrink)
			{
				blockindex -= 1;
				nfX += RootSize - shrink;

			}

			if (fY >= RootSize)
			{
				blockindex += 3;
				nfY -= RootSize;

			}

			if (fY < shrink)
			{
				blockindex -= 3;
				nfY += RootSize - shrink;

			}

			if (fZ >= RootSize)
			{
				blockindex += 9;
				nfZ -= RootSize;

			}

			if (fZ < shrink)
			{
				blockindex -= 9;
				nfZ += RootSize - shrink;
			}

			if (blockindex != 13)
			{
				
				IntPtr ptr = Neighbors[blockindex];

				if (ptr == IntPtr.Zero || ptr == null)
				{
					return borderValue;
				}
				else
				{

					NativeVoxelTree next = UnsafeUtility.ReadArrayElement<NativeVoxelTree>(ptr.ToPointer(), 0);
					return next._PeekVoxelId(nfX, nfY, nfZ, depth, shrink, borderValue);
				}
			}


			int X = ConvertLocalToInner(fX, RootSize);
			int Y = ConvertLocalToInner(fY, RootSize);
			int Z = ConvertLocalToInner(fZ, RootSize);

			int bitshift = SubdivisionPowerBitShift;
			int bitshiftsqr = bitshift << 1;
			int nodeSize = INNERWIDTH;
			int nodechildrencount = NodeChildrenCount;
			NativeVoxelNode current;
			NativeVoxelNode.PointerToNode(_ROOT, out current);

			int id = current._voxelID;

			for (int i = 0; i < depth; i++)
			{
				int index = 0;
				nodeSize = nodeSize >> bitshift;

				int fractionedX = (X >> (16 - (i + 1) * bitshift));
				int fractionedY = (Y >> (16 - (i + 1) * bitshift));
				int fractionedZ = (Z >> (16 - (i + 1) * bitshift));

				index += (fractionedX);
				index += (fractionedY << bitshift);
				index += (fractionedZ << bitshiftsqr);

				index = index & (nodechildrencount - 1);


				X = (X & (nodeSize - 1));
				Y = (Y & (nodeSize - 1));
				Z = (Z & (nodeSize - 1));

				if (!current.HasChildren())
				{
					return id;
				}
				else
				{
					current.GetChildFast(index, ref current);
					id = current._voxelID;
				}

			}

			return id;
		}


		/// <summary>
		/// Returns the voxel ID at a given local target coordinate.
		/// </summary>
		/// <param name="X">Local Position X</param>
		/// <param name="Y">Local Position Y</param>
		/// <param name="Z">Local Position Z</param>
		/// <param name="depth">Maximum Lookup Depth (like 10)</param>
		/// <param name="shrink">Keep as it is.</param>
		/// <param name="borderValue">Value used if lookup coordinate is out of bounds</param>
		/// <returns></returns>
		public int _PeekVoxelId_InnerCoordinate(int X, int Y, int Z, byte depth, int shrink = 0, int borderValue = 0)
		{
			int blockindex = 13;
			int nfX = X;
			int nfY = Y;
			int nfZ = Z;

			if (X >= NativeVoxelTree.INNERWIDTH)
			{
				blockindex += 1;
				nfX -= NativeVoxelTree.INNERWIDTH;

			}

			if (X < shrink)
			{
				blockindex -= 1;
				nfX += NativeVoxelTree.INNERWIDTH - shrink;

			}

			if (Y >= NativeVoxelTree.INNERWIDTH)
			{
				blockindex += 3;
				nfY -= NativeVoxelTree.INNERWIDTH;

			}

			if (Y < shrink)
			{
				blockindex -= 3;
				nfY += NativeVoxelTree.INNERWIDTH - shrink;

			}

			if (Z >= NativeVoxelTree.INNERWIDTH)
			{
				blockindex += 9;
				nfZ -= NativeVoxelTree.INNERWIDTH;

			}

			if (Z < shrink)
			{
				blockindex -= 9;
				nfZ += NativeVoxelTree.INNERWIDTH - shrink;
			}

			if (blockindex != 13)
			{

				IntPtr ptr = Neighbors[blockindex];

				if (ptr == IntPtr.Zero || ptr == null)
				{
					return borderValue;
				}
				else
				{

					NativeVoxelTree next = UnsafeUtility.ReadArrayElement<NativeVoxelTree>(ptr.ToPointer(), 0);
					return next._PeekVoxelId_InnerCoordinate(nfX, nfY, nfZ, depth, shrink, borderValue);
				}
			}

			int bitshift = SubdivisionPowerBitShift;
			int bitmultiply = bitshift - 1;
			int bitshiftsqr = bitshift << 1;
			int nodeSize = INNERWIDTH;
			int nodechildrencount = NodeChildrenCount;
			NativeVoxelNode current;
			NativeVoxelNode.PointerToNode(_ROOT, out current);

			int id = current._voxelID;

			for (int i = 0; i < depth; i++)
			{
				int index = 0;
				nodeSize = nodeSize >> bitshift;

				int fractionedX = (X >> (16 - ((i + 1) << bitmultiply)));
				int fractionedY = (Y >> (16 - ((i + 1) << bitmultiply)));
				int fractionedZ = (Z >> (16 - ((i + 1) << bitmultiply)));

				index += (fractionedX);
				index += (fractionedY << bitshift);
				index += (fractionedZ << bitshiftsqr);

				index = index & (nodechildrencount - 1);


				X = (X & (nodeSize - 1));
				Y = (Y & (nodeSize - 1));
				Z = (Z & (nodeSize - 1));

				if (!current.HasChildren())
				{
					return id;
				}
				else
				{
					current.GetChildFast(index, ref current);
					id = current._voxelID;
				}

			}

			return id;
		}


		public void _GetAllLeafVoxel(NativeVoxelNode tree, ref FNativeList<NativeVoxelNode> output)
		{
			NodestackBuffer.Clear();

			int elementPos = 0;
			if(elementPos >= NodestackBuffer.Length)	
				NodestackBuffer.Add(tree);		
			else
				NodestackBuffer[elementPos] = tree;
			elementPos++;

			while (elementPos > 0)
			{
				elementPos--;

				NativeVoxelNode current = NodestackBuffer[elementPos];
				if (current.HasChildren())
				{
					for (int i = 0; i < NodeChildrenCount; i++)
					{
						NativeVoxelNode next;
						current.GetChild(i, out next);
						if (elementPos >= NodestackBuffer.Length)
							NodestackBuffer.Add(next);
						else
							NodestackBuffer[elementPos] = next;
						elementPos++;
					}
				}
				else
				{
					output.Add(current);
				}
			}

			NodestackBuffer.Clear();
		}

		public void _GetAllVoxelsBelow(NativeVoxelNode Start, int Depth, ref FNativeList<NativeVoxelNode> output)
		{
			if (output.IsCreated) output.Dispose();
			output = new FNativeList<NativeVoxelNode>(1000, Allocator.Persistent);

			
			int elementPos = 0;
			if(elementPos >= NodestackBuffer.Length)	
				NodestackBuffer.Add(Start);		
			else
				NodestackBuffer[elementPos] = Start;
			elementPos++;
			

			while (elementPos > 0)
			{

				elementPos--;
				NativeVoxelNode current = NodestackBuffer[elementPos];
				if (current.HasChildren())
				{
					for (int i = 0; i < SubdivisionPower * SubdivisionPower * SubdivisionPower; i++)
					{
						if (current.Depth + 1 - Start.Depth <= Depth)
						{
							NativeVoxelNode next;
							current.GetChild(i, out next);
							if (elementPos >= NodestackBuffer.Length)
								NodestackBuffer.Add(next);
							else
								NodestackBuffer[elementPos] = next;
							elementPos++;
						}
					}
				}
				else
				{
					output.Add(current);
				}
			}
		}

		/// <summary>
		/// Transforms a part of an octree to an geometrical representation. 
		/// The starting node does not include itself unless it is the root as it is covered by upper starting nodes
		/// </summary>
		/// <param name="Start"></param>
		/// <param name="Depth"></param>
		/// <param name="offsets"></param>
		/// <param name="sizes"></param>
		/// <param name="neighbours"></param>
		/// <returns></returns>
		public int _GetNativeVoxelsBelow(ref NativeVoxelNode Start, int Depth,
			ref FNativeList<Vector3> offsets,
			ref FNativeList<float> sizes,
			ref FNativeList<int> neighbours,
			bool IncludeNeighbour,
			int CoreID,
			ref FNativeQueue<NativeVoxelNode> nodestack
			)
		{

			int index = 0;

			int stackcount = 0;

			Start.Refresh(out Start);
			if (Start.IsDestroyed())
			{
				return 0;
			}

			NativeVoxelNode first = Start;


			if (first.HasChildren())
			{
				for (int i = 0; i < SubdivisionPower * SubdivisionPower * SubdivisionPower; i++)
				{
					if (first.Depth + 1 - Start.Depth <= Depth)
					{

						NativeVoxelNode next;
						first.GetChild(i, out next);
						nodestack.Enqueue(next);
						stackcount++;

					}
				}
			}
			else if (first.Index == 255 && !first.HasChildren()) //Root edge case if octree has one node
			{
				offsets.Add(ConvertInnerToLocal(new Vector3Int(first.X, first.Y, first.Z), RootSize));
				sizes.Add(SizeTable[first.Depth]);

				neighbours.Add(0);
				neighbours.Add(0);
				neighbours.Add(0);
				neighbours.Add(0);
				neighbours.Add(0);
				neighbours.Add(0);
				index++;
			}

			while (stackcount > 0)
			{
				stackcount--;
				NativeVoxelNode current = nodestack.Dequeue();

				if (current.HasChildren())
				{
					if (current.Depth + 1 - Start.Depth <= Depth)
					{
						for (int i = 0; i < SubdivisionPower * SubdivisionPower * SubdivisionPower; i++)
						{

							NativeVoxelNode next;
							current.GetChild(i, out next);
							nodestack.Enqueue(next);
							stackcount++;
						}
					}
				}
				else if (current._voxelID > 0)
				{
					offsets.Add(ConvertInnerToLocal(new Vector3Int(current.X, current.Y, current.Z), RootSize));
					sizes.Add(SizeTable[current.Depth]);

					neighbours.Add(0);
					neighbours.Add(0);
					neighbours.Add(0);
					neighbours.Add(0);
					neighbours.Add(0);
					neighbours.Add(0);
					

					if (IncludeNeighbour)
					{
						//Passt
						NativeVoxelNode leftneighbour = current._LeftNeighbor(ref this, CoreID);
						if (leftneighbour.IsValid() && !leftneighbour.HasChildren())
						{
							neighbours[index * 6 + 2] = leftneighbour._voxelID;
						}

						NativeVoxelNode rightneighbour = current._RightNeighbor(ref this, CoreID);
						if (rightneighbour.IsValid() && !rightneighbour.HasChildren())
						{
							neighbours[index * 6 + 3] = rightneighbour._voxelID;
						}

						NativeVoxelNode downneighbour = current._DownNeighbor(ref this, CoreID);
						if (downneighbour.IsValid() && !downneighbour.HasChildren())
						{
							neighbours[index * 6 + 4] = downneighbour._voxelID;
						}
						//
						NativeVoxelNode upneighbour = current._UpNeighbor(ref this, CoreID);
						if (upneighbour.IsValid() && !upneighbour.HasChildren())
						{
							neighbours[index * 6 + 5] = upneighbour._voxelID;
						}
						//
						NativeVoxelNode frontneighbour = current._FrontNeighbor(ref this, CoreID);
						if (frontneighbour.IsValid() && !frontneighbour.HasChildren())
						{
							neighbours[index * 6 + 0] = frontneighbour._voxelID;
						}

						NativeVoxelNode backneighbour = current._BackNeighbor(ref this, CoreID);
						if (backneighbour.IsValid() && !backneighbour.HasChildren())
						{
							neighbours[index * 6 + 1] = backneighbour._voxelID;
						}
					}

					index++;

				}
			}

			return index;
		}

		public void UpwardMerge(NativeVoxelNode start, out NativeVoxelNode end)
		{
			int id = start._voxelID;
			end = start;
			if (start.HasChildren())
			{
				return;
			}

			NativeVoxelNode parent;
			start.GetParent(out parent);

			while (parent.IsValid())
			{
				if (!parent.AttemptMerge(ref this, id))
				{
					return;
				}

				end = parent;
				parent.GetParent(out parent);
			}

		}


		[BurstDiscard]
		public void Dispose(VoxelGenerator generator)
		{
			IsCreated = false;

			if (OctreePositionTable != IntPtr.Zero)
			{
				UnsafeUtility.Free(OctreePositionTable.ToPointer(), Allocator.Persistent);
				OctreePositionTable = IntPtr.Zero;
			}
			
			if (SizeTable.IsCreated) SizeTable.Dispose();
			if (NodestackBuffer.IsCreated) NodestackBuffer.Dispose();
			if (indexstack.IsCreated) indexstack.Dispose();
			if (Errors.IsCreated) Errors.Dispose();

			if (_ROOT != IntPtr.Zero)
			{
				NativeVoxelNode rootnode = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(_ROOT.ToPointer(), 0);
				rootnode.DestroyChildrenIgnoreID(ref this);

				voxelReservoir.AddGarbage(_ROOT);
				_ROOT = IntPtr.Zero;
			}

			if (destroyedNodes.IsCreated) destroyedNodes.Dispose();
			if (Neighbors.IsCreated) Neighbors.Dispose();
		}

		public void SetNeighbor(int ID, ref NativeVoxelTree neighbor)
		{
			
			Neighbors[ID] = (IntPtr)UnsafeUtility.AddressOf(ref neighbor);

		}

		public void RemoveNeighbor(int ID)
		{
			Neighbors[ID] = IntPtr.Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ConvertLocalToInner(float localCoordinate, float rootSize)
		{
			return (int)((localCoordinate / rootSize) * INNERWIDTH);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ConvertInnerToLocal(int innerCoordinate, float rootSize)
		{
			return (float)innerCoordinate / (float)INNERWIDTH * rootSize;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ConvertLocalToInner(Vector3 localCoordinate, float rootSize)
		{
			return new Vector3Int(
				(int)((localCoordinate.x / rootSize) * INNERWIDTH),
				(int)((localCoordinate.y / rootSize) * INNERWIDTH),
				(int)((localCoordinate.z / rootSize) * INNERWIDTH)
				);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ConvertInnerToLocal(Vector3Int innerCoordinate, float rootSize)
		{
			return new Vector3(
				(float)innerCoordinate.x / (float)INNERWIDTH * rootSize,
				(float)innerCoordinate.y / (float)INNERWIDTH * rootSize,
				(float)innerCoordinate.z / (float)INNERWIDTH * rootSize);
		}
	}

	public struct NativeVoxelNodeInfo
	{
		public NativeVoxelNode Voxel;
		public Vector3Int Chunk;

		public NativeVoxelNodeInfo(NativeVoxelNode voxel, Vector3Int chunk)
		{
			Voxel = voxel;
			Chunk = chunk;
		}
	}

	public struct ChunkIndex
	{
		public int Index;
		public Vector3Int Chunk;

		public ChunkIndex(int index, Vector3Int chunk)
		{
			Index = index;
			Chunk = chunk;
		}
	}
}
