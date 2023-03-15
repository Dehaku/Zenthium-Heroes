//HI I am just a placeholder file for the Burst Compile attribute
//With me, scripts which are supposed to require burst can still be used although with normal efficiency
//When the burst package is installed please delete me
//Best Regards :)

//If you still want me, just comment these lines
using System;

#if !BURST_EXISTS
namespace Fraktalia.VoxelGen
{
	public class BurstCompileAttribute : Attribute { }
}
#endif
