using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null)
			=> new EnergyVariableHint { TooltipOverride = tooltipOverride };

		public AStatus MakeEnergy(AStatus action, bool energy = true)
		{
			var copy = Mutil.DeepCopy(action);
			copy.targetPlayer = true;
			Instance.Api.SetExtensionData(copy, "energy", energy);
			return copy;
		}
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IEnergyAsStatusApi EnergyAsStatus { get; } = new EnergyAsStatusApi();
		
		public sealed class EnergyAsStatusApi : IKokoroApi.IV2.IEnergyAsStatusApi
		{
			public IKokoroApi.IV2.IEnergyAsStatusApi.IVariableHint? AsVariableHint(AVariableHint action)
				=> action as IKokoroApi.IV2.IEnergyAsStatusApi.IVariableHint;

			public IKokoroApi.IV2.IEnergyAsStatusApi.IVariableHint MakeVariableHint(int? tooltipOverride = null)
				=> new EnergyVariableHint { TooltipOverride = tooltipOverride };

			public IKokoroApi.IV2.IEnergyAsStatusApi.IStatusAction? AsStatusAction(AStatus action)
			{
				if (action is IKokoroApi.IV2.IEnergyAsStatusApi.IStatusAction statusAction)
					return statusAction;
				if (Instance.Api.TryGetExtensionData(action, "energy", out bool isEnergy) && isEnergy)
					return new StatusWrapper { Wrapped = action };
				return null;
			}

			public IKokoroApi.IV2.IEnergyAsStatusApi.IStatusAction MakeStatusAction(int amount)
			{
				var wrapped = new AStatus
				{
					targetPlayer = true,
					statusAmount = amount,
				};
				Instance.Api.SetExtensionData(wrapped, "energy", true);
				return new StatusWrapper { Wrapped = wrapped };
			}

			private sealed class StatusWrapper : IKokoroApi.IV2.IEnergyAsStatusApi.IStatusAction
			{
				public required AStatus Wrapped { get; init; }

				public AStatus AsCardAction
					=> Wrapped;
			}
		}
	}
}

internal sealed class EnergyAsStatusManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetIcon_Postfix))
		);
	}
	
	private static void AStatus_GetTooltips_Postfix(AStatus __instance, ref List<Tooltip> __result)
	{
		if (!ModEntry.Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;

		__result.Clear();
		__result.Add(new GlossaryTooltip("AStatus.Energy")
		{
			Icon = (Spr)ModEntry.Instance.Content.EnergySprite.Id!.Value,
			TitleColor = Colors.energy,
			Title = ModEntry.Instance.Localizations.Localize(["energy", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["energy", "description"]),
		});
	}

	private static void AStatus_GetIcon_Postfix(AStatus __instance, ref Icon? __result)
	{
		if (!ModEntry.Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;
		__result = new(
			path: (Spr)ModEntry.Instance.Content.EnergySprite.Id!.Value,
			number: __instance.mode == AStatusMode.Set ? null : __instance.statusAmount,
			color: Colors.white
		);
	}
}

public sealed class EnergyVariableHint : AVariableHint, IKokoroApi.IV2.IEnergyAsStatusApi.IVariableHint
{
	public int? TooltipOverride { get; set; }

	public AVariableHint AsCardAction
		=> this;
	
	public EnergyVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new(
			path: (Spr)ModEntry.Instance.Content.EnergySprite.Id!.Value,
			number: null,
			color: Colors.white
		);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("AStatus.Energy")
			{
				Description = ModEntry.Instance.Localizations.Localize(["energyVariableHint"]),
				vals =
				[
					(s.route is Combat combat) ? $" </c>(<c=keyword>{ModEntry.Instance.Api.ObtainExtensionData(this, "energyTooltipOverride", () => (int?)null) ?? combat.energy}</c>)" : ""
				]
			}
		];

	public IKokoroApi.IV2.IEnergyAsStatusApi.IVariableHint SetTooltipOverride(int? value)
	{
		TooltipOverride = value;
		return this;
	}
}