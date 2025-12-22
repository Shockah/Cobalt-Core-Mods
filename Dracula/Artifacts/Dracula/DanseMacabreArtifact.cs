using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DanseMacabreArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("DanseMacabre", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Boss]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Dracula/DanseMacabre.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Dracula", "DanseMacabre", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Dracula", "DanseMacabre", "description", "stateless"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_GetTooltips_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(ModEntry.Instance.BloodMirrorStatus.Status, 1);

	public override void OnTurnStart(State state, Combat combat)
	{
		if (!combat.isPlayerTurn)
			return;
		combat.QueueImmediate(new AStatus
		{
			targetPlayer = combat.turn % 2 != 0,
			status = ModEntry.Instance.BloodMirrorStatus.Status,
			statusAmount = 1,
			artifactPulse = Key()
		});
	}

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
	{
		if (__instance is not DanseMacabreArtifact)
			return;

		var textTooltip = __result.OfType<TTText>().FirstOrDefault(t => t.text.StartsWith("<c=artifact>"));
		if (textTooltip is null)
			return;

		if (MG.inst.g?.state is not { } state || state.IsOutsideRun() || state.route is not Combat combat)
			return;
		textTooltip.text = DB.Join(
			"<c=artifact>{0}</c>\n".FF(__instance.GetLocName()),
			ModEntry.Instance.Localizations.Localize(["artifact", "DanseMacabre", "description", combat.turn % 2 == 0 ? "even" : "odd"], new
			{
				Hull75 = (int)(state.ship.hullMax * 0.75),
				Hull50 = (int)(state.ship.hullMax * 0.5),
				Hull25 = (int)(state.ship.hullMax * 0.25),
			})
		);
	}
}