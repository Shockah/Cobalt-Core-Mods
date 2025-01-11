using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class ChainReactionArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	public int Boost;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("ChainReaction", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DestinyDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/ChainReaction.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ChainReaction", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ChainReaction", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? []];

	public override int? GetDisplayNumber(State s)
		=> Boost;

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Boost = 0;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Explosive.ExplosiveTrait))
			return;
		combat.Queue(new BoostExplosiveAction { artifactPulse = Key() });
	}

	public void ModifyExplosiveDamage(IDestinyApi.IHook.IModifyExplosiveDamageArgs args)
		=> args.CurrentDamage += Boost;

	private sealed class BoostExplosiveAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.EnumerateAllArtifacts().OfType<ChainReactionArtifact>().FirstOrDefault() is not { } artifact)
				return;
			artifact.Boost++;
		}
	}
}