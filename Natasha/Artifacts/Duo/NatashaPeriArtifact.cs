using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Natasha;

internal sealed class NatashaPeriArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	[JsonProperty]
	private bool TriggeredThisTurn;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Peri.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/PeriInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("NatashaPeri", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Peri", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Peri", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.peri]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Postfix))
		);
	}

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.overdrive, 1);

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		TriggeredThisTurn = false;
	}

	private static void AStatus_Begin_Prefix(AStatus __instance, Combat c, out int __state)
		=> __state = __instance.targetPlayer ? 0 : c.otherShip.Get(__instance.status);

	private static void AStatus_Begin_Postfix(AStatus __instance, State s, Combat c, in int __state)
	{
		if (__instance.targetPlayer)
			return;
		if (!c.isPlayerTurn)
			return;
		if (__instance.status is Status.shield or Status.tempShield)
			return;
		if (c.otherShip.Get(__instance.status) <= __state)
			return;
		if (s.EnumerateAllArtifacts().OfType<NatashaPeriArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisTurn)
			return;

		artifact.TriggeredThisTurn = true;
		c.QueueImmediate(new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1, artifactPulse = artifact.Key() });
	}
}