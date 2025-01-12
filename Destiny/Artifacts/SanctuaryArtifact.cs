using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Destiny;

internal sealed class SanctuaryArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	[JsonProperty]
	private bool Moved;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Sanctuary.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/SanctuaryInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("Sanctuary", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DestinyDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Sanctuary", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Sanctuary", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Postfix))
		);
	}

	public override Spr GetSprite()
		=> Moved ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.energyNextTurn, 1);

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		Moved = false;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (Moved)
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.energyNextTurn, statusAmount = 1, artifactPulse = Key() });
	}

	private static void AMove_Begin_Prefix(AMove __instance, State s, Combat c, out int __state)
	{
		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		__state = ship.x;
	}

	private static void AMove_Begin_Postfix(AMove __instance, State s, Combat c, in int __state)
	{
		if (!__instance.targetPlayer)
			return;
		
		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		if (ship.x == __state)
			return;
		if (s.EnumerateAllArtifacts().OfType<SanctuaryArtifact>().FirstOrDefault() is not { } artifact)
			return;

		artifact.Moved = true;
	}
}