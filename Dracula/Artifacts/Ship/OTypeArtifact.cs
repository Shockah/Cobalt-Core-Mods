using Newtonsoft.Json;
using Nickel;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class OTypeArtifact : Artifact, IDraculaArtifact
{
	[JsonProperty]
	public int Charges { get; set; } = 3;

	public static void Register(IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("OType", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/OType.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "OType", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "OType", "description"]).Localize
		});
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		if (Charges >= 3)
			return;

		Charges = 3;
		Pulse();
	}
}