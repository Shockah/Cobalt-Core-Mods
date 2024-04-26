using FSPRO;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class JohnsonMaxArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	public int TurnCounter;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("JohnsonMax", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/JohnsonMax.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonMax", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonMax", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.JohnsonDeck.Deck, Deck.hacker]);
	}

	public override int? GetDisplayNumber(State s)
		=> TurnCounter;

	public override List<Tooltip>? GetExtraTooltips()
	{
		List<Tooltip> tooltips = [];

		var state = MG.inst.g.state ?? DB.fakeState;
		if (!state.IsOutsideRun() && state != DB.fakeState)
		{
			var combat = state.route as Combat;
			IEnumerable<Card> cards = [..state.deck, ..combat?.discard ?? [], ..combat?.hand ?? []];

			var groups = cards
				.GroupBy(card => card.Key())
				.OrderByDescending(group => group.Count())
				.Take(2)
				.ToList();

			var hasEffect = groups.Count == 1 || (groups.Count == 2 && groups[0].Count() != groups[1].Count());
			tooltips.Add(
				hasEffect
					? new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "Duo", "JohnsonMax", "hasEffect"], new { CardName = Loc.T($"card.{groups[0].First().Key()}.name") }))
					: new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "Duo", "JohnsonMax", "hasNoEffect"]))
			);
		}

		tooltips.Add(new TTGlossary("cardtrait.discount", 1));
		return tooltips;
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn)
			return;
		if (++TurnCounter < 3)
			return;

		TurnCounter -= 3;

		var groups = state.deck.Concat(combat.discard).Concat(combat.hand)
			.GroupBy(card => card.Key())
			.OrderByDescending(group => group.Count())
			.Take(2)
			.ToList();
		if (groups.Count == 2 && groups[0].Count() == groups[1].Count())
			return;

		combat.Queue(new Action
		{
			CardIds = groups[0].Select(card => card.uuid).ToList(),
			artifactPulse = Key()
		});
	}

	private sealed class Action : CardAction
	{
		public required List<int> CardIds;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			foreach (var cardId in CardIds)
			{
				if (s.FindCard(cardId) is not { } card)
					continue;
				card.discount -= 1;
			}
			Audio.Play(Event.CardHandling);
		}
	}
}