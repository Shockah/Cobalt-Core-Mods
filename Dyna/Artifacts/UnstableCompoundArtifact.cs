using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dyna;

internal sealed class UnstableCompoundArtifact : Artifact, IRegisterable, IDynaHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("UnstableCompound", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/UnstableCompound.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnstableCompound", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnstableCompound", "description"]).Localize,
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> new BlastwaveManager.BlastwaveAction { Damage = 1, Range = 1 }.GetTooltips(DB.fakeState);

	public bool ModifyShipBlastwave(State state, Combat combat, AAttack? source, bool targetPlayer, int localX, ref int? damage, ref int range, ref bool isStunwave)
	{
		Pulse();
		range++;
		return false;
	}

	public bool ModifyMidrowBlastwave(State state, Combat combat, AAttack? source, bool playerDidIt, int worldX, ref int? damage, ref int range, ref bool isStunwave)
	{
		Pulse();
		range++;
		return false;
	}
}