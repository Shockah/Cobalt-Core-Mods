using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaRiggsArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	public bool TriggeredThisCombat { get; set; } = false;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaRiggs.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaRiggsInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("DynaRiggs", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaRiggs", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaRiggs", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.riggs]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Prefix)), priority: Priority.Low)
		);
	}

	public override Spr GetSprite()
		=> (TriggeredThisCombat ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.energyLessNextTurn, Math.Max(MG.inst.g.state.ship.Get(Status.energyLessNextTurn), 1))
			.Concat(new SwiftCharge().GetTooltips(MG.inst.g.state ?? DB.fakeState))
			.ToList();

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	private static void AStatus_Begin_Prefix(AStatus __instance, State s, Combat c)
	{
		if (__instance.status != Status.energyLessNextTurn || !__instance.targetPlayer)
			return;
		if (s.EnumerateAllArtifacts().OfType<DynaRiggsArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisCombat)
			return;

		var currentAmount = s.ship.Get(Status.energyLessNextTurn);
		var newAmount = __instance.mode switch
		{
			AStatusMode.Set => __instance.statusAmount,
			AStatusMode.Add => currentAmount + __instance.statusAmount,
			AStatusMode.Mult => currentAmount * __instance.statusAmount,
			_ => currentAmount
		};
		if (newAmount < currentAmount)
			return;

		artifact.TriggeredThisCombat = true;
		c.QueueImmediate(new FireChargeAction
		{
			Charge = new SwiftCharge(),
			artifactPulse = artifact.Key(),
		});
	}
}