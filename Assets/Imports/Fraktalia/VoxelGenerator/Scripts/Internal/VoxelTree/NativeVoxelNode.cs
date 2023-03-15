using System.Diagnostics;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Profiling;
using System.Runtime.CompilerServices;

namespace Fraktalia.VoxelGen
{
	[BurstCompile]
	[DebuggerTypeProxy(typeof(NativeVoxelNodeDebugView))]
	public unsafe struct NativeVoxelNode
	{
		[NativeDisableUnsafePtrRestriction]
		private IntPtr Address;
		[NativeDisableUnsafePtrRestriction]
		private IntPtr Parent;
		[NativeDisableUnsafePtrRestriction]
		private IntPtr Children;

		public int X;
		public int Y;
		public int Z;

		public int _voxelID;

		/// <summary>
		/// Settings bit position:
		/// pos 0: if 1, it has children, else not.
		/// pos 1: if 1, it has been marked as destroyed
		/// pos 2: if 1, node is valid else invalid
		/// </summary>

	
		private byte Settings;
		
		public byte Depth;
		public byte Index;	

		public NativeVoxelNode(byte depth, byte index, int voxelID, IntPtr parent, IntPtr address, int Subdivision)
		{
			Children = IntPtr.Zero;
			Settings = 0;
			Settings = (byte)(Settings | 1 << 2);

			Address = address;
			Depth = depth;
			_voxelID = voxelID;
			Parent = parent;
			Index = index;
			X = 0;
			Y = 0;
			Z = 0;		
			//test = false;
		}

		public void GetNode(int index, ref NativeVoxelTree octree, out NativeVoxelNode output)
		{		
			if (!HasChildren())
			{
				FillChildren(ref octree);

			}

			output = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(Children.ToPointer(), index);

		}

		public void GetParent(out NativeVoxelNode parent)
		{
			if (Parent != IntPtr.Zero)
			{


				if (Parent.ToPointer() == null)
				{
					parent = new NativeVoxelNode();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					throw new NullReferenceException(
					   "Node has parent pointer but parent is null. " +
					   "Called inside GetParent");
#endif
				}



				UnsafeUtility.CopyPtrToStructure<NativeVoxelNode>(Parent.ToPointer(), out parent);
			}
			else
			{
				parent = new NativeVoxelNode();
			}


		}
		public void GetChild(int index, out NativeVoxelNode child)
		{
			if (!HasChildren())
			{
				child = this;
				return;
			}


			if (Children.ToPointer() == null)
			{
				child = this;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
				throw new NullReferenceException(
				   "Node has children but children is null " +
				   "Called inside GetChild");
#endif

			}


			child = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(Children.ToPointer(), index);
		}

		
		public void GetChildFast(int index, ref NativeVoxelNode child)
		{
			void* ptr = Children.ToPointer();
			if(ptr != null)
			child = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(ptr, index);
		}


		public void Refresh(out NativeVoxelNode node)
		{

			if (Address.ToPointer() == null)
			{
				node = new NativeVoxelNode();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				throw new NullReferenceException(
				   "Node has children but children is null" +
				   "Called inside Refresh");
#endif

			}


			UnsafeUtility.CopyPtrToStructure(Address.ToPointer(), out node);
		}

		public void SetVoxel(ref NativeVoxelTree octree, byte ID)
		{
			
			if (HasChildren())
			{
				DestroyChildrenIgnoreID(ref octree);
			}

			_voxelID = ID;
			UnsafeUtility.CopyStructureToPtr(ref this, Address.ToPointer());
		}

		public void SetVoxelAdditive(ref NativeVoxelTree octree, int ID)
		{
			if (HasChildren())
			{
				DestroyChildren(ref octree);
			}

			int n = _voxelID + ID;
			n = n > 255 ? 255 : n;
			_voxelID = (byte)(n < 0 ? 0 : n);


			UnsafeUtility.CopyStructureToPtr(ref this, Address.ToPointer());

		}

		public void DestroyChildren(ref NativeVoxelTree octree)
		{
			if (HasChildren())
			{
				MarkNodeAsLeaf();

				int length = octree.NodeChildrenCount;
				
				void* childptr = Children.ToPointer();
				for (int i = 0; i < length; i++)
				{
					NativeVoxelNode voxel = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(childptr, i);					
					voxel.DestroyChildren(ref octree);
					voxel.MarkNodeAsDestroyed();
					
					UnsafeUtility.WriteArrayElement<NativeVoxelNode>(childptr, i, voxel);
					octree.destroyedNodes.Add(voxel);
				}
				
				octree.voxelReservoir.AddGarbage(Children);
				//Children = IntPtr.Zero;
			}
		}

		public void DestroyChildrenIgnoreID(ref NativeVoxelTree octree)
		{
			if (HasChildren())
			{
				MarkNodeAsLeaf();

				int length = octree.NodeChildrenCount;
				
				void* childptr = Children.ToPointer();
				for (int i = 0; i < length; i++)
				{
					NativeVoxelNode voxel = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(childptr, i);					
					voxel.DestroyChildrenIgnoreID(ref octree);
					voxel.MarkNodeAsDestroyed();

					UnsafeUtility.WriteArrayElement<NativeVoxelNode>(childptr, i, voxel);
					octree.destroyedNodes.Add(voxel);
				}
			
				octree.voxelReservoir.AddGarbage(Children);
				//Children = IntPtr.Zero;

			}
		}

		public bool AttemptMerge(ref NativeVoxelTree octree, int id)
		{


			if (HasChildren())
			{
				int length = octree.NodeChildrenCount;

				void* childptr = Children.ToPointer();
				for (int i = 0; i < length; i++)
				{
					NativeVoxelNode voxel = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(childptr, i);
					if (voxel._voxelID != id || voxel.HasChildren())
					{
						return false;
					}				
				}
				_voxelID = id;

				DestroyChildren(ref octree);
				UnsafeUtility.CopyStructureToPtr(ref this, Address.ToPointer());
				return true;
			}

			return false;

		}

		public bool FillChildren(ref NativeVoxelTree octree)
		{
			int length = octree.NodeChildrenCount;
			int elementSize = UnsafeUtility.SizeOf<NativeVoxelNode>();
			Children = octree.voxelReservoir.ObtainNodeAddress();

#if VOXEL_DEBUG
			var debug = octree.DebugInformation[0];
			debug.Allocations++;
			octree.DebugInformation[0] = debug;
#endif

			void* childptr = Children.ToPointer();
			void* octreepositionptr = octree.OctreePositionTable.ToPointer();
			
			
			int nextdepth = Depth + 1;
			
			int halfSize = NativeVoxelTree.INNERWIDTH >> octree.SubdivisionPowerBitShift * nextdepth >> 1;
			
			for (int i = 0; i < length; i++)
			{
				IntPtr location = IntPtr.Add(Children, i * elementSize);
				NativeVoxelNode newVoxel = new NativeVoxelNode((byte)(nextdepth), (byte)i, _voxelID, Address, location, octree.SubdivisionPower);
				Vector3Int octreeposition = UnsafeUtility.ReadArrayElement<Vector3Int>(octreepositionptr, i);

				newVoxel.X = X + octreeposition.x * (halfSize);
				newVoxel.Y = Y + octreeposition.y * (halfSize);
				newVoxel.Z = Z + octreeposition.z * (halfSize);

				UnsafeUtility.CopyStructureToPtr(ref newVoxel, location.ToPointer());
			}

			MarkNodeAsParent();

			UnsafeUtility.CopyStructureToPtr(ref this, Address.ToPointer());
			return true;
		}


		public static void PointerToNode(IntPtr pointer, out NativeVoxelNode output)
		{
			output = UnsafeUtility.ReadArrayElement<NativeVoxelNode>(pointer.ToPointer(), 0);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		public bool HasChildren()
		{
			return ((Settings & (1 << 0)) != 0);
		}


		public bool IsDestroyed()
		{
			return ((Settings & (1 << 1)) != 0);
		}

		public bool IsValid()
		{
			return ((Settings & (1 << 2)) != 0);
		}
		public string GetAdressInfo
		{
			get
			{
				return Address.ToString();
			}
		}
		public string GetChildrenInfo
		{
			get
			{
				return Children.ToString();
			}
		}
		public string GetParentInfo
		{
			get
			{
				return Parent.ToString();
			}
		}

		public byte GetSettings()
		{
			return Settings;
		}

		private void SetInvalid()
		{
			Settings = (byte)(Settings & ~(1 << 2));
		}
		private void MarkNodeAsDestroyed()
		{
			Settings = (byte)(Settings | 1 << 1);
		}

		private void MarkNodeAsLeaf()
		{
			Settings = (byte)(Settings & ~(1 << 0));
		}

		private void MarkNodeAsParent()
		{
			Settings = (byte)(Settings | 1 << 0);
		}

		private void SetBit(int pos)
		{
			Settings = (byte)(Settings | 1 << pos);
		}
		private void ZeroBit(int pos)
		{
			Settings = (byte)(Settings & ~(1 << pos));
		}
		private byte Clamp(int n)
		{
			n = n > 255 ? 255 : n;
			return (byte)(n < 0 ? 0 : n);
		}

		public NativeVoxelNode _LeftNeighbor(ref NativeVoxelTree octree, int CoreID, bool ForceCreation = false)
		{
			int offset = NativeVoxelTree.MaxDepth * CoreID;
			int stackcount = offset;
			NativeArray<int> indexstack = octree.indexstack;
			NativeVoxelNode current = this;
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();


			while (current.Parent != IntPtr.Zero)
			{

				if (current.Index % octree.SubdivisionPower > 0)
				{
					indexstack[stackcount] = (current.Index - 1);
					stackcount++;
					current.GetParent(out current);
					break;
				}
				else
				{
					indexstack[stackcount] = (current.Index + 1);
					stackcount++;
					current.GetParent(out current);
				}
			}

			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();


			while (stackcount > offset)
			{
				stackcount--;
				int nextindex = indexstack[stackcount];
				if (!current.HasChildren())
				{
					if (ForceCreation)
					{
						current.GetNode(nextindex, ref octree, out current);
					}
					else
					{
						return current;
					}
				}
				else
				{
					current.GetChild(nextindex, out current);
				}
			}

			return current;
		}
		public NativeVoxelNode _RightNeighbor(ref NativeVoxelTree octree, int CoreID, bool ForceCreation = false)
		{
			int offset = NativeVoxelTree.MaxDepth * CoreID;
			int stackcount = offset;
			NativeArray<int> indexstack = octree.indexstack;
			NativeVoxelNode current = this;
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (current.Parent != IntPtr.Zero)
			{

				if (current.Index % octree.SubdivisionPower < octree.SubdivisionPower - 1)
				{
					indexstack[stackcount] = (current.Index + 1);
					stackcount++;
					current.GetParent(out current);
					break;
				}
				else
				{
					indexstack[stackcount] = (current.Index - 1);
					stackcount++;
					current.GetParent(out current);
				}
			}
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (stackcount > offset)
			{
				stackcount--;
				int nextindex = indexstack[stackcount];
				if (!current.HasChildren())
				{
					if (ForceCreation)
					{
						current.GetNode(nextindex, ref octree, out current);
					}
					else
					{
						return current;
					}
				}
				else
				{
					current.GetChild(nextindex, out current);
				}
			}

			return current;
		}
		public NativeVoxelNode _DownNeighbor(ref NativeVoxelTree octree, int CoreID, bool ForceCreation = false)
		{
			int offset = NativeVoxelTree.MaxDepth * CoreID;
			int stackcount = offset;
			NativeArray<int> indexstack = octree.indexstack;
			NativeVoxelNode current = this;
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (current.Parent != IntPtr.Zero)
			{
				int nextindex = (current.Index % (octree.SubdivisionPower * octree.SubdivisionPower));
				if (nextindex >= octree.SubdivisionPower)
				{
					indexstack[stackcount] = (current.Index - octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
					break;
				}
				else
				{
					indexstack[stackcount] = (current.Index + octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
				}
			}
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (stackcount > offset)
			{
				stackcount--;
				int nextindex = indexstack[stackcount];
				if (!current.HasChildren())
				{
					if (ForceCreation)
					{
						current.GetNode(nextindex, ref octree, out current);
					}
					else
					{
						return current;
					}
				}
				else
				{
					current.GetChild(nextindex, out current);
				}
			}

			return current;
		}
		public NativeVoxelNode _UpNeighbor(ref NativeVoxelTree octree, int CoreID, bool ForceCreation = false)
		{
			int offset = NativeVoxelTree.MaxDepth * CoreID;
			int stackcount = offset;
			NativeArray<int> indexstack = octree.indexstack;
			NativeVoxelNode current = this;
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (current.Parent != IntPtr.Zero)
			{
				int nextindex = current.Index % (octree.SubdivisionPower * octree.SubdivisionPower);
				if (nextindex < octree.SubdivisionPower * octree.SubdivisionPower - +octree.SubdivisionPower)
				{
					indexstack[stackcount] = (current.Index + octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
					break;
				}
				else
				{
					indexstack[stackcount] = (current.Index - octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
				}
			}
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (stackcount > offset)
			{
				stackcount--;
				int nextindex = indexstack[stackcount];
				if (!current.HasChildren())
				{
					if (ForceCreation)
					{
						current.GetNode(nextindex, ref octree, out current);
					}
					else
					{
						return current;
					}
				}
				else
				{
					current.GetChild(nextindex, out current);
				}
			}

			return current;
		}
		public NativeVoxelNode _FrontNeighbor(ref NativeVoxelTree octree, int CoreID, bool ForceCreation = false)
		{
			int offset = NativeVoxelTree.MaxDepth * CoreID;
			int stackcount = offset;
			NativeArray<int> indexstack = octree.indexstack;
			NativeVoxelNode current = this;
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (current.Parent != IntPtr.Zero)
			{
				int nextindex = current.Index;
				if (nextindex >= octree.SubdivisionPower * octree.SubdivisionPower)
				{
					indexstack[stackcount] = (current.Index - octree.SubdivisionPower * octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
					break;
				}
				else
				{
					indexstack[stackcount] = (current.Index + octree.SubdivisionPower * octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
				}
			}
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (stackcount > offset)
			{
				stackcount--;
				int nextindex = indexstack[stackcount];
				if (!current.HasChildren())
				{
					if (ForceCreation)
					{
						current.GetNode(nextindex, ref octree, out current);
					}
					else
					{
						return current;
					}
				}
				else
				{
					current.GetChild(nextindex, out current);
				}
			}

			return current;
		}
		public NativeVoxelNode _BackNeighbor(ref NativeVoxelTree octree, int CoreID, bool ForceCreation = false)
		{
			int offset = NativeVoxelTree.MaxDepth * CoreID;
			int stackcount = offset;
			NativeArray<int> indexstack = octree.indexstack;
			NativeVoxelNode current = this;
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (current.Parent != IntPtr.Zero)
			{
				int nextindex = current.Index;
				if (nextindex < octree.SubdivisionPower * octree.SubdivisionPower * octree.SubdivisionPower - octree.SubdivisionPower * octree.SubdivisionPower)
				{
					indexstack[stackcount] = (current.Index + octree.SubdivisionPower * octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
					break;
				}
				else
				{
					indexstack[stackcount] = (current.Index - octree.SubdivisionPower * octree.SubdivisionPower);
					stackcount++;
					current.GetParent(out current);
				}
			}
			if (current.Parent == IntPtr.Zero) return new NativeVoxelNode();

			while (stackcount > offset)
			{
				stackcount--;
				int nextindex = indexstack[stackcount];
				if (!current.HasChildren())
				{
					if (ForceCreation)
					{
						current.GetNode(nextindex, ref octree, out current);
					}
					else
					{
						return current;
					}
				}
				else
				{
					current.GetChild(nextindex, out current);
				}
			}

			return current;
		}
	}

	internal sealed class NativeVoxelNodeDebugView
	{
		private NativeVoxelNode m_node;

		public NativeVoxelNodeDebugView(NativeVoxelNode node)
		{
			m_node = node;
		}

		public VoxelNodeDebug Item
		{
			get
			{
				VoxelNodeDebug output = new VoxelNodeDebug();
				output.Depth = m_node.Depth;
				output.Index = m_node.Index;
				output.Position = new Vector3(m_node.X, m_node.Y, m_node.Z);
				output._voxelID = m_node._voxelID;
				output.Address = m_node.GetAdressInfo;
				output.Parent = m_node.GetParentInfo;
				output.Children = m_node.GetChildrenInfo;
				output.Settings = m_node.GetSettings();
				output.Valid = m_node.IsValid();
				output.HasChildren = m_node.HasChildren();
				output.IsDestroyed = m_node.IsDestroyed();

				return output;
			}
		}
	}

	public class VoxelNodeDebug
	{
		public string Address;
		public string Parent;
		public string Children;
		public Vector3 Position;

		public int _voxelID;
		public int Depth;
		public int Index;

		/// <summary>
		/// Settings bit position:
		/// pos 0: if 1, it has children, else not.
		/// pos 1: if 1, it has been marked as destroyed
		/// pos 2: if 1, node is valid else invalid
		/// </summary>
		public byte Settings;
		public bool Valid;
		public bool HasChildren;
		public bool IsDestroyed;


	}
}
