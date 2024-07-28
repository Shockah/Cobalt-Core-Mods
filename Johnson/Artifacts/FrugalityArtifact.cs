using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class FrugalityArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Frugality", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Frugality.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Frugality", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Frugality", "description"]).Localize
		});

		DB.story.all[$"ShopkeeperInfinite_{ModEntry.Instance.JohnsonDeck.Deck.Key()}_Frugality"] = new()
		{
			type = NodeType.@event,
			bg = "BGShop",
			lines = [
				new CustomSay()
				{
					who = ModEntry.Instance.JohnsonDeck.Deck.Key(),
					AlternativeTexts = ModEntry.Instance.Localizations.Localize(["artifact", "Frugality", "dialogue"]).Split("\n").ToList(),
					loopTag = "neutral"
				}
			]
		};

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MapShop), nameof(MapShop.MakeRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MapShop_MakeRoute_Postfix))
		);
	}

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.ship.baseEnergy++;
	}

	private static void MapShop_MakeRoute_Postfix(State s, ref Route __result)
	{
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is FrugalityArtifact) is not { } artifact)
			return;
		__result = Dialogue.MakeDialogueRouteOrSkip(s, DB.story.QuickLookup(s, $"ShopkeeperInfinite_{ModEntry.Instance.JohnsonDeck.Deck.Key()}_Frugality"), OnDone.map);
		artifact.Pulse();
	}
}
