using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Kokoro;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ForkbombArtifact : Artifact, IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static IArtifactEntry Entry { get; private set; } = null!;
	
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	private bool TriggeredThisCombat;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Forkbomb.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/ForkbombInactive.png"));

		Entry = helper.Content.Artifacts.RegisterArtifact("Forkbomb", new()
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

	public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
	{
		if (TriggeredThisCombat)
			return args.NewAmount;
		if (args.Ship.isPlayerShip)
			return args.NewAmount;
		if (!DB.statuses.TryGetValue(args.Status, out var definition))
			return args.NewAmount;
		if (definition.isGood)
			return args.NewAmount;

		Pulse();
		TriggeredThisCombat = true;
		return args.NewAmount + 1;
	}
}