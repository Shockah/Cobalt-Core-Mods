using Nickel;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class OTypeArtifact : Artifact, IDraculaArtifact
{
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

		var artifact = state.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		if (artifact.Charges >= 3)
			return;

		artifact.Charges = 3;
		artifact.Pulse();
		Pulse();
	}
}