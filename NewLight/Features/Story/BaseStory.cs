using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal abstract class BaseStory : IRegisterable
{
	protected static void InjectLocalizations(IModHelper helper, Action<Dictionary<string, string>> @delegate)
	{
		helper.Events.OnLoadStringsForLocale += (_, args) => @delegate(args.Localizations);
		if (helper.Events.ModLoadPhaseState.Phase >= ModLoadPhase.AfterDbInit)
			@delegate(DB.currentLocale.strings);
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryNode), nameof(StoryNode.Filter)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StoryNode_Filter_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StoryNode_Filter_Postfix))
		);
	}

	private static void StoryNode_Filter_Prefix(StoryNode n, StorySearch ctx, out (HashSet<string>? ctx, HashSet<string>? n) __state)
	{
		__state = (ctx.lookup, n.lookup);
		if (n.LookupAny is not null)
		{
			ctx.lookup = null;
			n.lookup = null;
		}
	}

	private static void StoryNode_Filter_Postfix(StoryNode n, StorySearch ctx, in (HashSet<string>? ctx, HashSet<string>? n) __state, ref bool __result)
	{
		ctx.lookup = __state.ctx;
		n.lookup = __state.n;
		
		if (!__result)
			return;
		if (ctx.lookup is null || n.LookupAny is not { } lookupAny)
			return;

		foreach (var lookupKey in ctx.lookup)
		{
			if (lookupAny.Contains(lookupKey))
				continue;
				
			__result = false;
			return;
		}
	}
}

internal static class BaseStoryExt
{
	extension(StoryNode node)
	{
		public HashSet<string>? LookupAny
		{
			get => ModEntry.Instance.Helper.ModData.GetOptionalModData<HashSet<string>>(node, "LookupAny");
			set => ModEntry.Instance.Helper.ModData.SetOptionalModData(node, "LookupAny", value);
		}
	}
}