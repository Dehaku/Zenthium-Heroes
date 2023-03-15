using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Fraktalia.Utility.NativeNoise
{
	/// <summary>
	/// Random works only in normal C# space. Randomness in Native, Shaders or Compute Shaders require randomness Table.
	/// Pseudo randomness is still randomness. RNGesus truly exists! May the mighty RNG be with you.
	/// </summary>
	public struct PermutationTable_Native
	{

		public int Size;

		public int Seed;

		public int Max;

		public float Inverse;

		private int Wrap;

		private NativeArray<int> Table;

		public PermutationTable_Native(int size, int max, int seed)
		{
			Size = size;
			Wrap = Size - 1;
			Max = Math.Max(1, max);
			Inverse = 1.0f / Max;

			Seed = seed;
			Table = new NativeArray<int>(Size, Allocator.Persistent);

			System.Random rnd = new System.Random(Seed);

			for (int i = 0; i < Size; i++)
			{
				Table[i] = rnd.Next();
			}
		}

		public void CleanUp()
		{
			if (Table.IsCreated) Table.Dispose();
		}

		public bool IsCreated
		{
			get
			{
				return Table.IsCreated;
			}
		}

        internal int this[int i]
        {
            get
            {
                return Table[i & Wrap] & Max;
            }
        }

        internal int this[int i, int j]
        {
            get
            {
                return Table[(j + Table[i & Wrap]) & Wrap] & Max;
            }
        }

        internal int this[int i, int j, int k]
        {
            get
            {
                return Table[(k + Table[(j + Table[i & Wrap]) & Wrap]) & Wrap] & Max;
            }
        }

    }
}
