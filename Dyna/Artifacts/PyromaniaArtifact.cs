using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class PyromaniaArtifact : Artifact, IRegisterable, IDynaHook
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	private bool TriggeredThisTurn;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Pyromania.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/PyromaniaInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("Pyromania", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Pyromania", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Pyromania", "description"]).Localize,
		});
	}

	public override Spr GetSprite()
		=> (TriggeredThisTurn ? InactiveSprite : ActiveSprite).Sprite;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.isPlayerTurn)
			TriggeredThisTurn = false;
	}

	public void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX)
	{
		if (TriggeredThisTurn)
			return;
		if (ship.isPlayerShip)
			return;

		TriggeredThisTurn = true;
		combat.QueueImmediate(new AEnergy
		{
			changeAmount = 1,
			artifactPulse = Key(),
		});
	}
}