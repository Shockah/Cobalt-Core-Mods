using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Natasha;

internal sealed class NatashaIsaacArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("NatashaIsaac", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Isaac.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Isaac", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Isaac", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.goat]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = CreateCard(DB.fakeState) }];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AAddCard { destination = CardDestination.Hand, card = CreateCard(state), artifactPulse = Key() });
	}

	private static Card CreateCard(State state)
	{
		var card = new AttackDroneCard();
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, true, false);
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.Helper.Content.Cards.RetainCardTrait, true, false);
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.KokoroApi.Limited.Trait, true, false);
		ModEntry.Instance.KokoroApi.Limited.SetLimitedUses(state, card, 3);
		return card;
	}
}