using Nanoray.PluginManager;
using Nickel;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BlownFuseArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BlownFuse", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/BlownFuse.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BlownFuse", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BlownFuse", "description"]).Localize
		});
	}

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.ship.baseEnergy++;
	}

	public override void OnRemoveArtifact(State state)
	{
		base.OnRemoveArtifact(state);
		state.ship.baseEnergy--;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.isPlayerTurn)
			return;
		if (combat.turn == 0)
			return;

		static void RemoveAllCharges(Ship ship)
		{
			foreach (var part in ship.parts)
				part.SetStickedCharge(null);
		}

		RemoveAllCharges(state.ship);
		RemoveAllCharges(combat.otherShip);
		Pulse();
	}
}