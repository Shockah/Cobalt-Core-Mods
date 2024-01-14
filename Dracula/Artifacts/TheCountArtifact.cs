using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class TheCountArtifact : Artifact, IDraculaArtifact
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	public bool TriggeredThisCombat { get; set; } = false;

	[JsonProperty]
	public int NextTrigger { get; set; } = 1;

	public static void Register(IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/TheCount.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/TheCountInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("TheCount", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "TheCount", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "TheCount", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> (TriggeredThisCombat ? InactiveSprite : ActiveSprite).Sprite;

	public override int? GetDisplayNumber(State s)
		=> TriggeredThisCombat ? null : NextTrigger;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTGlossary($"action.stunShip"),
			new TTGlossary($"action.endTurn"),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
		NextTrigger = 1;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (TriggeredThisCombat)
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

		TriggeredThisCombat = true;
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