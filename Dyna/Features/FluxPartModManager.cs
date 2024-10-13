using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class FluxPartModManager : IDynaHook
{
	internal const PDamMod FluxDamageModifier = (PDamMod)2137401;

	internal static ISpriteEntry FluxTraitIcon { get; private set; } = null!;

	private static AAttack? AttackContext;

	public FluxPartModManager()
	{
		FluxTraitIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/FluxTrait.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderPartUI)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_RenderPartUI_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ModifyDamageDueToParts)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_ModifyDamageDueToParts_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		{
			if (AttackContext is null)
				return;
			TriggerFluxIfNeeded(state, combat, part, targetPlayer: true);
		}, 0);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (State state, Combat combat, Part? part) =>
		{
			if (AttackContext is null)
				return;
			TriggerFluxIfNeeded(state, combat, part, targetPlayer: false);
		}, 0);

		ModEntry.Instance.Api.RegisterHook(this, 0);
	}

	internal static IEnumerable<Tooltip> MakeFluxPartModTooltips()
		=> [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::PartDamageModifier::Flux")
			{
				Icon = FluxTraitIcon.Sprite,
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["partModifier", "Flux", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["partModifier", "Flux", "description"])
			},
			..StatusMeta.GetTooltips(Status.tempShield, 1)
		];

	private static void TriggerFluxIfNeeded(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is null || part.invincible || part.GetDamageModifier() != FluxDamageModifier)
			return;

		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is DynaDizzyArtifact) is { } dizzyDuo)
			combat.QueueImmediate(new GainShieldOrTempShieldAction
			{
				TargetPlayer = !targetPlayer,
				Amount = 1,
				ShieldArtifactPulse = dizzyDuo.Key(),
			});
		else
			combat.QueueImmediate(new AStatus
			{
				targetPlayer = !targetPlayer,
				status = Status.tempShield,
				statusAmount = 1
			});
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_RenderPartUI_Postfix(Ship __instance, G g, Part part, int localX, string keyPrefix, bool isPreview)
	{
		if (part.invincible || part.GetDamageModifier() != FluxDamageModifier)
			return;
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey(StableUK.part, localX, keyPrefix)) is not { } box)
			return;

		var offset = isPreview ? 25 : 34;
		var v = box.rect.xy + new Vec(0, __instance.isPlayerShip ? (offset - 16) : 8);

		var color = new Color(1, 1, 1, 0.8 + Math.Sin(g.state.time * 4.0) * 0.3);
		Draw.Sprite(FluxTraitIcon.Sprite, v.x, v.y, color: color);

		if (!box.IsHover())
			return;
		g.tooltips.Add(g.tooltips.pos, MakeFluxPartModTooltips());
	}

	private static bool Ship_ModifyDamageDueToParts_Prefix(int incomingDamage, Part part, ref int __result)
	{
		if (part.GetDamageModifier() != FluxDamageModifier)
			return true;

		part.brittleIsHidden = false;
		__result = part.invincible ? 0 : incomingDamage;
		return false;
	}

	public void OnBlastwaveHit(State state, Combat combat, Ship ship, int originWorldX, int waveWorldX, bool hitMidrow)
	{
		if (ship.GetPartAtWorldX(waveWorldX) is not { } part)
			return;
		TriggerFluxIfNeeded(state, combat, part, ship.isPlayerShip);
	}
}
