using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MuscleMemoryArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("MuscleMemory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/MuscleMemory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.AllAssemblies()
				.First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro")
				.GetType("Shockah.Kokoro.OnDiscardManager")!
				.GetNestedType("TriggerAction", AccessTools.all)!
				.GetMethod("Begin", AccessTools.all)!,
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(OnDiscardManager_TriggerAction_Begin_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> ModEntry.Instance.KokoroApi.Actions.MakeOnDiscardAction(new ADummyAction()).GetTooltips(DB.fakeState);

	private static void OnDiscardManager_TriggerAction_Begin_Postfix(CardAction __instance, G g, State s, Combat c)
	{
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is MuscleMemoryArtifact) is not { } artifact)
			return;

		c.QueueImmediate(ModEntry.Instance.KokoroApi.Actions.GetWrappedCardActions(__instance).Select(action =>
		{
			if (string.IsNullOrEmpty(action.artifactPulse))
				action.artifactPulse = artifact.Key();
			return action;
		}));
	}
}