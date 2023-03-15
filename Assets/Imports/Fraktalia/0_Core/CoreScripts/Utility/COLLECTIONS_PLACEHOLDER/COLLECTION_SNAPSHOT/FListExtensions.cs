using System;
using System.Collections.Generic;
using Unity.Collections;

#if !COLLECTION_EXISTS
namespace Fraktalia.Core.Collections
{
    public static class FListExtensions
    {
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }
    }

    public static class FArrayExtensions
    {
        public static int IndexOf<T>(this NativeArray<T> array, T value) where T : struct, IComparable<T>
        {
            for (int i = 0; i != array.Length; i++)
            {
                if (array[i].CompareTo(value) == 0)
                    return i;
            }
            return -1;
        }
    }

}
#endif
