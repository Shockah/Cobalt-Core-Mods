using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class BackdoorArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Backdoor", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Backdoor.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Backdoor", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Backdoor", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(MG.inst.g.state, null) ?? [],
			.. StatusMeta.GetTooltips(Status.corrode, 1),
		];

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.KokoroApi.Limited.Trait))
			return;
		combat.Queue(new Action { CardId = card.uuid });
	}

	private sealed class Action : CardAction
	{
		public required int CardId;
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.deck.Concat(c.discard).Concat(c.hand).Any(c => c.uuid == CardId))
				return;
			if (s.FindCard(CardId) is { } card && ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, card) > 1)
				return;
			if (s.EnumerateAllArtifacts().OfType<BackdoorArtifact>().FirstOrDefault() is not { } artifact)
				return;
			
			c.QueueImmediate(new AStatus
			{
				targetPlayer = false,
				status = Status.corrode,
				statusAmount = 1,
				artifactPulse = artifact.Key(),
			});
		}
	}
}