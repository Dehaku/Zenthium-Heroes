using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;

#if !COLLECTION_EXISTS
namespace Fraktalia.Core.Collections
{
    internal struct FPair<Key, Value>
    {
        public Key key;
        public Value value;
        public FPair(Key k, Value v)
        {
            key = k;
            value = v;
        }
#if !NET_DOTS
        public override string ToString()
        {
            return $"{key} = {value}";
        }
#endif
    }

    // Tiny does not contains an IList definition (or even ICollection)
#if !NET_DOTS
    internal struct FListPair<Key, Value> where Value : IList
    {
        public Key key;
        public Value value;
        public FListPair(Key k, Value v)
        {
            key = k;
            value = v;
        }

        public override string ToString()
        {
            String result = $"{key} = [";
            for (var v = 0; v < value.Count; ++v)
            {
                result += value[v];
                if (v < value.Count - 1)
                    result += ", ";
            }
            result += "]";
            return result;
        }
    }
#endif

    sealed internal class FNativeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        private FNativeHashMap<TKey, TValue> m_target;
        public FNativeHashMapDebuggerTypeProxy(FNativeHashMap<TKey, TValue> target)
        {
            m_target = target;
        }
        public List<FPair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<FPair<TKey, TValue>>();
                using (var keys = m_target.GetKeyArray(Allocator.Temp))
                {
                    for (var k = 0; k < keys.Length; ++k)
                        if (m_target.TryGetValue(keys[k], out var value))
                            result.Add(new FPair<TKey, TValue>(keys[k], value));
                }
                return result;
            }
        }
#endif
    }
    sealed internal class FNativeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>, IComparable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        private FNativeMultiHashMap<TKey, TValue> m_target;
        public FNativeMultiHashMapDebuggerTypeProxy(FNativeMultiHashMap<TKey, TValue> target)
        {
            m_target = target;
        }
        public List<FListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<FListPair<TKey, List<TValue>>>();
                var keys = m_target.GetUniqueKeyArray(Allocator.Temp);
                using (keys.Item1)
                {
                    for (var k = 0; k < keys.Item2; ++k)
                    {
                        var values = new List<TValue>();
                        if (m_target.TryGetFirstValue(keys.Item1[k], out var value, out var iterator))
                        {
                            do
                            {
                                values.Add(value);
                            } while (m_target.TryGetNextValue(out value, ref iterator));
                        }
                        result.Add(new FListPair<TKey, List<TValue>>(keys.Item1[k], values));
                    }
                }
                return result;
            }
        }
#endif
    }

}
#endif
