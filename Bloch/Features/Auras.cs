﻿using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;

namespace Shockah.Bloch;

internal sealed class AuraManager : IStatusLogicHook
{
	internal static IStatusEntry VeilingStatus { get; private set; } = null!;
	internal static IStatusEntry FeedbackStatus { get; private set; } = null!;
	internal static IStatusEntry IntensifyStatus { get; private set; } = null!;

	private static bool IsDuringNormalDamage = false;
	private static int ReducedDamage = 0;

	public AuraManager()
	{
		VeilingStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Veiling", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Veiling.png")).Sprite,
				color = new("A17FFF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Veiling", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Veiling", "description"]).Localize
		});

		FeedbackStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Feedback", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Feedback.png")).Sprite,
				color = new("A17FFF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Feedback", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Feedback", "description"]).Localize
		});

		IntensifyStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Intensify", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Intensify.png")).Sprite,
				color = new("DBC6FF"),
				affectedByTimestop = true,
				isGood = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Intensify", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Intensify", "description"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ModifyDamageDueToParts)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_ModifyDamageDueToParts_Prefix))
		);

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
	}

	private static void Ship_NormalDamage_Prefix()
		=> IsDuringNormalDamage = true;

	private static void Ship_NormalDamage_Finalizer(Ship __instance, Combat c, ref DamageDone __result)
	{
		IsDuringNormalDamage = false;

		var feedback = __instance.Get(FeedbackStatus.Status);
		var maxFeedback = Math.Min(feedback, __instance.Get(IntensifyStatus.Status) + 1);

		if (maxFeedback > 0)
			c.QueueImmediate([
				new AStatus
				{
					targetPlayer = true,
					status = FeedbackStatus.Status,
					statusAmount = -maxFeedback,
					statusPulse = ReducedDamage > 1 ? IntensifyStatus.Status : null
				},
				new AHurt
				{
					targetPlayer = !__instance.isPlayerShip,
					hurtShieldsFirst = true,
					hurtAmount = maxFeedback,
					statusPulse = FeedbackStatus.Status
				}
			]);

		if (ReducedDamage == 0)
			return;

		c.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = VeilingStatus.Status,
			statusAmount = -ReducedDamage,
			statusPulse = ReducedDamage > 1 ? IntensifyStatus.Status : null
		});
		ReducedDamage = 0;
	}

	private static void Ship_ModifyDamageDueToParts_Prefix(Ship __instance, ref int incomingDamage, Part part, bool piercing)
	{
		if (piercing)
			return;
		if (!IsDuringNormalDamage)
			return;

		var modifier = part.GetDamageModifier();
		if (incomingDamage <= (modifier == PDamMod.weak ? -1 : 0))
			return;

		var veiling = __instance.Get(VeilingStatus.Status);
		var maxVeiling = Math.Min(veiling, __instance.Get(IntensifyStatus.Status) + 1);
		if (maxVeiling <= 0)
			return;

		var toReduce = Math.Min(maxVeiling, incomingDamage - (modifier == PDamMod.armor ? 1 : 0) + (modifier == PDamMod.weak ? 1 : 0));
		if (toReduce <= 0)
			return;

		incomingDamage -= toReduce;
		ReducedDamage = toReduce;
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != IntensifyStatus.Status)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return false;

		amount = 0;
		return false;
	}
}