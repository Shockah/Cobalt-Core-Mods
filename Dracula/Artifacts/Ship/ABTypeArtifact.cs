using Newtonsoft.Json;
using Nickel;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class ABTypeArtifact : Artifact, IDraculaArtifact
{
	[JsonProperty]
	public int Charges { get; set; } = 3;

	public static void Register(IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("ABType", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Boss]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/ABType.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "ABType", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "ABType", "description"]).Localize
		});
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;
		if (state.ship.hull < state.ship.hullMax)
			return;

		combat.QueueImmediate(new AEnergy
		{
			changeAmount = 1,
			artifactPulse = Key()
		});
	}
}