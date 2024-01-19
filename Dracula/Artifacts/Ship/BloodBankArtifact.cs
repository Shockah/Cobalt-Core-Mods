using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodBankArtifact : Artifact, IDraculaArtifact
{
	[JsonProperty]
	public int Charges { get; set; } = 3;

	public static void Register(IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BloodBank", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Ship/BloodBank.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "BloodBank", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ship", "BloodBank", "description"]).Localize
		});


		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Postfix))
		);
	}

	public override int? GetDisplayNumber(State s)
		=> Charges;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			new TTCard
			{
				card = new BatDebitCard()
			}
		];

	private static void Combat_Update_Prefix(G g, ref int __state)
		=> __state = g.state.ship.hull;

	private static void Combat_Update_Postfix(G g, ref int __state)
	{
		if (g.state.ship.hull <= __state)
			return;
		var artifact = g.state.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		if (artifact.Charges >= 5)
			return;

		artifact.Charges++;
		artifact.Pulse();
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
}