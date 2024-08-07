using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodBankArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry HealBoosterInactiveIcon = null!;

	[JsonProperty]
	public int Charges { get; set; } = 3;

	[JsonProperty]
	private int LastHull = 0;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		HealBoosterInactiveIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/HealBoosterInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("BloodBank", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/BloodBank.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "BloodBank", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "BloodBank", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_Update_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AHeal), nameof(AHeal.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AHeal_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(HealBooster), nameof(HealBooster.ModifyHealAmount)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(HealBooster_ModifyHealAmount_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetSprite)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_GetSprite_Postfix))
		);
	}

	public override int? GetDisplayNumber(State s)
		=> Charges;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "ship", "BloodBank", "healBoosterRestrictionDescription"])),
			new TTDivider(),
			new TTCard { card = new BatDebitCard() }
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn)
			return;
		if (state.EnumerateAllArtifacts().OfType<HealBooster>().FirstOrDefault() is not { } healBooster)
			return;
		ModEntry.Instance.Helper.ModData.RemoveModData(healBooster, "UsedThisTurn");
	}

	public override void OnCombatEnd(State state)
	{
		base.OnCombatEnd(state);
		if (state.EnumerateAllArtifacts().OfType<HealBooster>().FirstOrDefault() is not { } healBooster)
			return;
		ModEntry.Instance.Helper.ModData.RemoveModData(healBooster, "UsedThisTurn");
	}

	private static void State_Update_Postfix(State __instance)
	{
		if (__instance.IsOutsideRun())
			return;

		var artifact = __instance.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		if (artifact.LastHull == __instance.ship.hull)
			return;

		if (artifact.LastHull <= 0)
		{
			artifact.LastHull = __instance.ship.hull;
			return;
		}

		if (__instance.ship.hull > artifact.LastHull && artifact.Charges < 5)
		{
			artifact.Charges++;
			artifact.Pulse();
		}
		artifact.LastHull = __instance.ship.hull;
	}

	private static void Combat_DrainCardActions_Prefix(Combat __instance, ref bool __state)
		=> __state = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;

	private static void Combat_DrainCardActions_Postfix(Combat __instance, G g, ref bool __state)
	{
		var isWorkingOnActions = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;
		if (isWorkingOnActions || !__state)
			return;
		if (__instance.hand.Count >= 10)
			return;
		if (__instance.hand.Any(c => c is BatDebitCard))
			return;

		var artifact = g.state.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		__instance.SendCardToHand(g.state, new BatDebitCard());
		artifact.Pulse();
	}

	private static void AHeal_Begin_Postfix(State s)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BloodBankArtifact))
			return;
		if (s.EnumerateAllArtifacts().OfType<HealBooster>().FirstOrDefault() is not { } healBooster)
			return;
		ModEntry.Instance.Helper.ModData.SetModData(healBooster, "UsedThisTurn", true);
	}

	private static void HealBooster_ModifyHealAmount_Postfix(HealBooster __instance, ref int __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "UsedThisTurn"))
			__result = 0;
	}

	private static void Artifact_GetSprite_Postfix(Artifact __instance, ref Spr __result)
	{
		if (__instance is not HealBooster healBooster)
			return;
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(healBooster, "UsedThisTurn"))
			return;
		__result = HealBoosterInactiveIcon.Sprite;
	}
}