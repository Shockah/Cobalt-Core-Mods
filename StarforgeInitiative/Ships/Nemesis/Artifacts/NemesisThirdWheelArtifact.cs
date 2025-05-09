using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class NemesisThirdWheelArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("NemesisThirdWheel", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Boss],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Nemesis/Artifact/ThirdWheel.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "ThirdWheel", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "ThirdWheel", "description"]).Localize
		});
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);

		foreach (var part in state.ship.parts)
		{
			if (part.type != PType.wing)
				continue;

			part.type = PType.cannon;
			part.skin = "wing_ares";
			part.active = false;
			part.damageModifier = PDamMod.none;
			part.damageModifierOverrideWhileActive = null;
			break;
		}
	}
}