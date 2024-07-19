using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ForkbombArtifact : Artifact, IRegisterable, IStatusLogicHook
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	private bool TriggeredThisCombat;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Forkbomb.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/ForkbombInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("Forkbomb", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Forkbomb", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Forkbomb", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> (TriggeredThisCombat ? InactiveSprite : ActiveSprite).Sprite;

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	public int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (TriggeredThisCombat)
			return newAmount;
		if (ship.isPlayerShip)
			return newAmount;
		if (!DB.statuses.TryGetValue(status, out var definition))
			return newAmount;
		if (definition.isGood)
			return newAmount;

		TriggeredThisCombat = true;
		return newAmount + 1;
	}
}