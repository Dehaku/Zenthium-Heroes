using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Fraktalia.Utility.DataStructures
{
	public unsafe struct BlitableNativeArray<T> where T : struct
	{


		private int iscreated;
		private int length;
		private IntPtr buffer;

		public BlitableNativeArray(int size)
		{
			length = size;
			int elementSize = UnsafeUtility.SizeOf<T>();

			buffer = (IntPtr)UnsafeUtility.Malloc(length * elementSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
			iscreated = 1;
		}

		public void Initialize(int size)
		{
			length = size;
			int elementSize = UnsafeUtility.SizeOf<T>();

			buffer = (IntPtr)UnsafeUtility.Malloc(length * elementSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
			iscreated = 1;
		}

		public void Initialize(NativeArray<T> nativearray)
		{
			length = nativearray.Length;
			int elementSize = UnsafeUtility.SizeOf<T>();

			buffer = (IntPtr)UnsafeUtility.Malloc(length * elementSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
			iscreated = 1;

			for (int i = 0; i < length; i++)
			{
				this[i] = nativearray[i];
			}


		}

		public bool IsCreated
		{
			get
			{
				if (iscreated == 1) return true;
				return false;
			}
		}

		public int Length
		{
			get
			{
				return length;
			}
		}


		public T this[int index]
		{
			get
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if ((uint)index >= (uint)length)
					throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{length}' Length.");
#endif
				return UnsafeUtility.ReadArrayElement<T>(buffer.ToPointer(), index);
			}
			set
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if ((uint)index >= (uint)length)
					throw new IndexOutOfRangeException($"Index {index} is out of range in NativeList of '{length}' Length.");
#endif
				UnsafeUtility.WriteArrayElement(buffer.ToPointer(), index, value);
			}
		}

		public void Dispose()
		{
			if (iscreated == 1)
			{
				UnsafeUtility.Free(buffer.ToPointer(), Allocator.Persistent);
				iscreated = 0;
			}
		}
	}
}
