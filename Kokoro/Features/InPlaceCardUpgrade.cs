using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IInPlaceCardUpgradeApi InPlaceCardUpgrade { get; } = new InPlaceCardUpgradeApi();
		
		public sealed class InPlaceCardUpgradeApi : IKokoroApi.IV2.IInPlaceCardUpgradeApi
		{
			public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade ModifyCardUpgrade(CardUpgrade route)
				=> new InPlaceCardUpgradeManager.CardUpgradeWrapper(Mutil.DeepCopy(route));
			
			internal sealed class InPlaceCardUpgradeStrategyApplyInPlaceCardUpgradeArgs : IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy.IApplyInPlaceCardUpgradeArgs
			{
				public State State { get; internal set; } = null!;
				public CardUpgrade Route { get; internal set; } = null!;
				public Card TargetCard { get; internal set; } = null!;
				public Card TemplateCard { get; internal set; } = null!;
			}
		}
	}
}

internal static class InPlaceCardUpgradeManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(CardUpgrade.FinallyReallyUpgrade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardUpgrade_FinallyReallyUpgrade_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(CardUpgrade.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardUpgrade_Render_Transpiler))
		);
	}
	
	private static bool CardUpgrade_FinallyReallyUpgrade_Prefix(CardUpgrade __instance, G g, Card newCard)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IsInPlace"))
			return true;

		var card = g.state.FindCard(newCard.uuid);
		if (card is null)
			return true;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy>(__instance, "InPlaceCardUpgradeStrategy") is { } strategy)
			ModEntry.Instance.ArgsPool.Do<ApiImplementation.V2Api.InPlaceCardUpgradeApi.InPlaceCardUpgradeStrategyApplyInPlaceCardUpgradeArgs>(args =>
			{
				args.State = g.state;
				args.Route = __instance;
				args.TargetCard = card;
				args.TemplateCard = newCard;

				strategy.ApplyInPlaceCardUpgrade(args);
			});
		else
			card.upgrade = newCard.upgrade;

		return false;
	}

	private static IEnumerable<CodeInstruction> CardUpgrade_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Ldstr("uiShared.btnCancel"))
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.Before, [
					ILMatches.Ldarg(0),
					ILMatches.Ldflda(nameof(CardUpgrade.animationTimer)),
					ILMatches.Call("get_HasValue"),
					ILMatches.Brtrue.GetBranchTarget(out var afterCancelRenderLabel),
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardUpgrade_Render_Transpiler_ShouldAllowCancel))),
					new CodeInstruction(OpCodes.Brfalse, afterCancelRenderLabel.Value),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool CardUpgrade_Render_Transpiler_ShouldAllowCancel(CardUpgrade route)
		=> ModEntry.Instance.Api.V2.InPlaceCardUpgrade.ModifyCardUpgrade(route).AllowCancel;
	
	internal sealed class CardUpgradeWrapper(CardUpgrade route) : IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade
	{
		[JsonIgnore]
		public CardUpgrade AsRoute
			=> route;

		public bool IsInPlace
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(route, "IsInPlace");
			set => ModEntry.Instance.Helper.ModData.SetModData(route, "IsInPlace", value);
		}
		
		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy? InPlaceCardUpgradeStrategy
		{
			get => ModEntry.Instance.Helper.ModData.GetOptionalModData<IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy>(route, "InPlaceCardUpgradeStrategy");
			set => ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "InPlaceCardUpgradeStrategy", value);
		}

		public bool AllowCancel
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault(route, "AllowCancel", true);
			set => ModEntry.Instance.Helper.ModData.SetModData(route, "AllowCancel", value);
		}

		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade SetIsInPlace(bool value)
		{
			this.IsInPlace = value;
			return this;
		}

		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade SetInPlaceCardUpgradeStrategy(IKokoroApi.IV2.IInPlaceCardUpgradeApi.IInPlaceCardUpgradeStrategy? value)
		{
			this.InPlaceCardUpgradeStrategy = value;
			return this;
		}

		public IKokoroApi.IV2.IInPlaceCardUpgradeApi.ICardUpgrade SetAllowCancel(bool value)
		{
			this.AllowCancel = value;
			return this;
		}
	}
}