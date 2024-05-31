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
				pools = [.. ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!), ArtifactPool.Unreleased],
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

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn)
			return;
		if (combat.turn <= 1)
			return;
		
		static bool RemoveAllCharges(Ship ship)
		{
			var removedAny = false;
			foreach (var part in ship.parts)
			{
				if (part.GetStickedCharge() is null)
					continue;
				part.SetStickedCharge(null);
				removedAny = true;
			}
			return removedAny;
		}

		var removedAny = false;
		removedAny |= RemoveAllCharges(state.ship);
		removedAny |= RemoveAllCharges(combat.otherShip);

		if (removedAny)
			Pulse();
	}
}