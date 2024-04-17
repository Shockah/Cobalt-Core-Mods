using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;

namespace Shockah.Bloch;

internal sealed class AuraManager : IStatusRenderHook, IStatusLogicHook
{
	internal static IStatusEntry VeilingStatus { get; private set; } = null!;
	internal static IStatusEntry IntensifyStatus { get; private set; } = null!;

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
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ModifyDamageDueToParts)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_ModifyDamageDueToParts_Prefix))
		);

		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
	}

	private static void Ship_ModifyDamageDueToParts_Prefix(Ship __instance, Combat c, ref int incomingDamage, Part part, bool piercing)
	{
		if (piercing)
			return;
		if (incomingDamage <= 0)
			return;

		var veiling = __instance.Get(VeilingStatus.Status);
		var maxVeiling = Math.Min(veiling, __instance.Get(IntensifyStatus.Status) + 1);
		if (maxVeiling <= 0)
			return;

		var toReduce = Math.Min(maxVeiling, incomingDamage - (part.GetDamageModifier() == PDamMod.armor ? 1 : 0));
		if (toReduce <= 0)
			return;

		incomingDamage -= toReduce;
		c.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = VeilingStatus.Status,
			statusAmount = -toReduce,
			statusPulse = toReduce > 1 ? IntensifyStatus.Status : null
		});
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status == VeilingStatus.Status)
			return [
				..tooltips,
				new TTGlossary("parttrait.armor")
			];
		else
			return tooltips;
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
