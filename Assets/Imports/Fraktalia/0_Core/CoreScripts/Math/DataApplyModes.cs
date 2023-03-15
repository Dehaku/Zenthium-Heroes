using Unity.Burst;
using UnityEngine;

namespace Fraktalia.Core.Math
{
	[BurstCompile]
	public static class DataApplyModes
	{


		[BurstCompile]
		public static float Set(float input, float value)
		{
			return value;
		}
		[BurstCompile]
		public static float Add(float input, float value)
		{
			return input + value;
		}
		[BurstCompile]
		public static float Subtract(float input, float value)
		{
			return input - value;
		}

		[BurstCompile]
		public static float Multiply(float input, float value)
		{
			return input * value;
		}

		[BurstCompile]
		public static float Divide(float input, float value)
		{
			return input * value;
		}

		[BurstCompile]
		public static float Min(float input, float value)
		{
			return Mathf.Min(input, value);
		}
		[BurstCompile]
		public static float Max(float input, float value)
		{
			return Mathf.Max(input, value);
		}

		public static FunctionPointer<DataApplyModeDelegate> GetFunctionPointer(DataApplyMode ApplyFunction)
		{
			switch (ApplyFunction)
			{
				case DataApplyMode.Set:
					return BurstCompiler.CompileFunctionPointer<DataApplyModeDelegate>(DataApplyModes.Set);

				case DataApplyMode.Add:
					return BurstCompiler.CompileFunctionPointer<DataApplyModeDelegate>(DataApplyModes.Add);

				case DataApplyMode.Subtract:
					return BurstCompiler.CompileFunctionPointer<DataApplyModeDelegate>(DataApplyModes.Subtract);

				case DataApplyMode.Min:
					return BurstCompiler.CompileFunctionPointer<DataApplyModeDelegate>(DataApplyModes.Min);

				case DataApplyMode.Max:
					return BurstCompiler.CompileFunctionPointer<DataApplyModeDelegate>(DataApplyModes.Max);


			}

			return BurstCompiler.CompileFunctionPointer<DataApplyModeDelegate>(DataApplyModes.Set);
		}
	}

	public delegate float DataApplyModeDelegate(float input, float value);

	public enum DataApplyMode
	{
		Set,
		Add,
		Subtract,
		Multiply,
		Divide,
		Min,
		Max
	}
}
