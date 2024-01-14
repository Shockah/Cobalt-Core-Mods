using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class TheCountArtifact : Artifact, IDraculaArtifact
{
	private static List<ISpriteEntry> Sprites = null!;

	[JsonProperty]
	public int NextTrigger { get; set; } = 3;

	public static void Register(IModHelper helper)
	{
		Sprites = Enumerable.Range(0, 4)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Artifacts/TheCount{i}.png")))
			.ToList();

		helper.Content.Artifacts.RegisterArtifact("TheCount", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = Sprites.Last().Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "TheCount", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "TheCount", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> Sprites[NextTrigger].Sprite;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTGlossary($"action.stunShip"),
			new TTGlossary($"action.endTurn"),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		NextTrigger = 1;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (NextTrigger <= 0)
			return;

		if (energyCost != NextTrigger)
		{
			if (NextTrigger != 1)
				Pulse();
			NextTrigger = 1;
			if (energyCost != 1)
				return;
		}

		NextTrigger++;
		Pulse();
		if (NextTrigger < 4)
			return;

		NextTrigger = 0;
		combat.Queue([
			new AStunShip
			{
				targetPlayer = false,
				artifactPulse = Key()
			},
			new AEndTurn()
			{
				artifactPulse = Key()
			}
		]);
	}
}