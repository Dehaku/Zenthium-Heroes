using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using System.Reflection;

namespace NativeCopyFast
{
	public static class NativeUtility
	{

		public static unsafe T[] CopyToFast<T>(
				NativeArray<T> nativeArray)
				where T : struct
		{


			int nativeArrayLength = nativeArray.Length;

			T[] array = new T[nativeArrayLength];

			if (nativeArrayLength > 0)
			{
				int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
				void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
				void* nativeBuffer = nativeArray.GetUnsafePtr();
				UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
			}
			return array;
		}

		public static unsafe T[] CopyToFast<T>(
			 NativeArray<T> nativeArray, ref T[] array)
			 where T : struct
		{


			int nativeArrayLength = nativeArray.Length;

			if (nativeArrayLength > 0)
			{
				int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
				void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
				void* nativeBuffer = nativeArray.GetUnsafePtr();
				UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
			}
			return array;
		}

		/// <summary>
		/// Completely writes content of native array into a normal list overwriting the previous content.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="nativeArray"></param>
		/// <param name="list"></param>
		public static unsafe void NativeListToList<T>(
		 NativeArray<T> nativeArray, List<T> list)
		 where T : struct
		{


			int nativeArrayLength = nativeArray.Length;

			if (nativeArrayLength > 0)
			{
				if(list.Capacity < nativeArrayLength)
				{
					list.Capacity = nativeArrayLength * 2;
				}


				T[] array = list.GetFieldValue("_items") as T[];

				int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
				void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
				void* nativeBuffer = nativeArray.GetUnsafePtr();
				UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);

				list.SetFieldValue("_size", nativeArrayLength);
			}		
		}


		public static unsafe void CopyFromFast<T>(
			  ref NativeArray<T> nativeArray, T[] input)
			  where T : struct
		{


			int nativeArrayLength = nativeArray.Length;

			if (nativeArrayLength > 0)
			{
				int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
				void* managedBuffer = UnsafeUtility.AddressOf(ref input[0]);
				void* nativeBuffer = nativeArray.GetUnsafePtr();
				UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
			}

		}

		public static unsafe void CopyFromFast<T>(
		   ref NativeArray<T> nativeArray, List<T> input)
		   where T : struct
		{


			int nativeArrayLength = nativeArray.Length;
			int inputlength = input.Count;
			if (nativeArrayLength > 0)
			{
				int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();

				T[] array = input.GetFieldValue("_items") as T[];
				void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
				void* nativeBuffer = nativeArray.GetUnsafePtr();
				UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
			}

		}
	}

	public static class ReflectionHelper
	{
		private static FieldInfo GetFieldInfo(Type type, string fieldName)
		{
			FieldInfo fieldInfo;
			do
			{
				fieldInfo = type.GetField(fieldName,
					   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				type = type.BaseType;
			}
			while (fieldInfo == null && type != null);
			return fieldInfo;
		}

		public static object GetFieldValue(this object obj, string fieldName)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			Type objType = obj.GetType();
			FieldInfo fieldInfo = GetFieldInfo(objType, fieldName);
			if (fieldInfo == null)
				throw new ArgumentOutOfRangeException("fieldName",
				  string.Format("Couldn't find field {0} in type {1}", fieldName, objType.FullName));
			return fieldInfo.GetValue(obj);
		}

		public static void SetFieldValue(this object obj, string fieldName, object val)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			Type objType = obj.GetType();
			FieldInfo fieldInfo = GetFieldInfo(objType, fieldName);
			if (fieldInfo == null)
				throw new ArgumentOutOfRangeException("fieldName",
				  string.Format("Couldn't find field {0} in type {1}", fieldName, objType.FullName));
			fieldInfo.SetValue(obj, val);
		}
	}
}

