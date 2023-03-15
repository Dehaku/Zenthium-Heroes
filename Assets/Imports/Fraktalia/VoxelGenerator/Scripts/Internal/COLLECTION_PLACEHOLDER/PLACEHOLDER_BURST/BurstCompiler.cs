#if !BURST_EXISTS

using System;
using System.Runtime.InteropServices;

namespace Fraktalia.VoxelGen
{
    /// <summary>
    /// The burst compiler runtime frontend.
    /// </summary>
    public static class BurstCompiler
    {   
        /// <summary>
        /// Compile the following delegate with burst and return a new delegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegateMethod"></param>
        /// <returns></returns>
        /// <remarks>NOT AVAILABLE, unsafe to use</remarks>
        internal static unsafe T CompileDelegate<T>(T delegateMethod) where T : class
        {
            // We have added support for runtime CompileDelegate in 2018.2+
            void* function = Compile(delegateMethod);
            object res = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer((IntPtr)function, delegateMethod.GetType());
            return (T)res;
        }

        /// <summary>
        /// Compile the following delegate into a function pointer with burst, only invokable from a burst jobs.
        /// </summary>
        /// <typeparam name="T">Type of the delegate of the function pointer</typeparam>
        /// <param name="delegateMethod">The delegate to compile</param>
        /// <returns>A function pointer invokable from a burst jobs</returns>
        public static unsafe FunctionPointer<T> CompileFunctionPointer<T>(T delegateMethod) where T : class
        {
            // We have added support for runtime CompileDelegate in 2018.2+
            void* function = Compile(delegateMethod);
            return new FunctionPointer<T>(new IntPtr(function));
        }

        private static unsafe void* Compile<T>(T delegateObj) where T : class
        {
            if (delegateObj == null) throw new ArgumentNullException(nameof(delegateObj));
            if (!(delegateObj is Delegate)) throw new ArgumentException("object instance must be a System.Delegate", nameof(delegateObj));

            var delegateMethod = (Delegate)(object)delegateObj;
            if (!delegateMethod.Method.IsStatic)
            {
                throw new InvalidOperationException($"The method `{delegateMethod.Method}` must be static. Instance methods are not supported");
            }

           
        
            void* function = null;

            // The attribute is directly on the method, so we recover the underlying method here
                    

            // When burst compilation is disabled, we are still returning a valid function pointer (the a pointer to the managed function)
            // so that CompileFunctionPointer actually returns a delegate in all cases
            return function == null ? (void*)Marshal.GetFunctionPointerForDelegate(delegateMethod) : function;
        }
           
    }
}
#endif

