using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class FluxPartModManager : IDynaHook
{
	internal const PDamMod FluxDamageModifier = (PDamMod)2137401;

	private static AAttack? AttackContext;

	public FluxPartModManager()
	{
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
	{
		List<Tooltip> tooltips = [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.parttrait,
				() => StableSpr.icons_libra,
				() => ModEntry.Instance.Localizations.Localize(["partModifier", "Flux", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["partModifier", "Flux", "description"])
			)
		];
		tooltips.AddRange(StatusMeta.GetTooltips(Status.tempShield, 1));
		return tooltips;
	}

	private static void TriggerFluxIfNeeded(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is not { } nonNullPart || nonNullPart.invincible || nonNullPart.GetDamageModifier() != FluxDamageModifier)
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
		Draw.Sprite(StableSpr.icons_libra, v.x, v.y, color: color);

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

	private sealed class GainShieldOrTempShieldAction : CardAction
	{
		public bool TargetPlayer;
		public int Amount;
		public string? ShieldArtifactPulse;
		public Status? ShieldStatusPulse;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			List<CardAction> actions = [];

			var targetShip = TargetPlayer ? s.ship : c.otherShip;
			var amountLeft = Amount;
			var missingShield = Math.Max(targetShip.GetMaxShield() - targetShip.Get(Status.shield), 0);

			if (amountLeft > 0 && missingShield > 0)
			{
				var toGain = Math.Min(amountLeft, missingShield);
				amountLeft -= toGain;
				actions.Add(new AStatus
				{
					targetPlayer = TargetPlayer,
					status = Status.shield,
					statusAmount = toGain,
					artifactPulse = ShieldArtifactPulse ?? artifactPulse,
					statusPulse = ShieldStatusPulse ?? statusPulse
				});
			}

			if (amountLeft > 0)
				actions.Add(new AStatus
				{
					targetPlayer = TargetPlayer,
					status = Status.tempShield,
					statusAmount = amountLeft,
					artifactPulse = artifactPulse,
					statusPulse = statusPulse
				});

			timer = 0;
			c.QueueImmediate(actions);
		}
	}
}
