using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Shockah.Shared;

internal static class ReflectionExt
{
	public static bool IsBuiltInDebugConfiguration(this Assembly assembly)
		=> assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(attr => attr.IsJITTrackingEnabled);

#if !IS_NICKEL_MOD
	public static AssemblyLoadContext CurrentAssemblyLoadContext
		=> AssemblyLoadContext.GetLoadContext(typeof(ReflectionExt).Assembly) ?? AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default;
#endif
}