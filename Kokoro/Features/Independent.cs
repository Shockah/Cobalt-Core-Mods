using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IIndependentApi Independent { get; } = new IndependentApi();
		
		public sealed class IndependentApi : IKokoroApi.IV2.IIndependentApi
		{
			public ICardTraitEntry Trait
				=> IndependentManager.Trait;

			public IKokoroApi.IV2.IIndependentApi.ICardSelect ModifyCardSelect(ACardSelect action)
				=> new CardSelectWrapper { Wrapped = action };

			public IKokoroApi.IV2.IIndependentApi.ICardBrowse ModifyCardBrowse(CardBrowse route)
				=> new CardBrowseWrapper { Wrapped = route };
			
			private sealed class CardSelectWrapper : IKokoroApi.IV2.IIndependentApi.ICardSelect
			{
				public required ACardSelect Wrapped { get; init; }

				public bool? FilterIndependent
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterIndependent");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterIndependent", value);
				}

				[JsonIgnore]
				public ACardSelect AsCardAction
					=> Wrapped;
				
				public IKokoroApi.IV2.IIndependentApi.ICardSelect SetFilterIndependent(bool? value)
				{
					FilterIndependent = value;
					return this;
				}
			}
			
			private sealed class CardBrowseWrapper : IKokoroApi.IV2.IIndependentApi.ICardBrowse
			{
				public required CardBrowse Wrapped { get; init; }

				public bool? FilterIndependent
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterIndependent");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterIndependent", value);
				}

				public CardBrowse AsRoute
					=> Wrapped;
				
				public IKokoroApi.IV2.IIndependentApi.ICardBrowse SetFilterIndependent(bool? value)
				{
					FilterIndependent = value;
					return this;
				}
			}
		}
	}
}

internal sealed class IndependentManager
{
	internal static readonly IndependentManager Instance = new();
	
	internal static ICardTraitEntry Trait = null!;
	
	private IndependentManager()
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Independent.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Independent", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Independent", "name"]).Localize,
			Tooltips = (_, card) =>
			{
				if (card is null || ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(card.GetMeta().deck) is not { } characterEntry)
				{
					return [
						new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Independent")
						{
							Icon = icon.Sprite,
							TitleColor = Colors.cardtrait,
							Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Independent", "name"]),
							Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Independent", "description", "generic"]),
						},
					];
				}
				else
				{
					return [
						new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Independent")
						{
							Icon = icon.Sprite,
							TitleColor = Colors.cardtrait,
							Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Independent", "name"]),
							Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Independent", "description", "owned"], new { Status = Loc.T($"status.{characterEntry.MissingStatus.Status}.name").ToUpper() }),
						},
						.. StatusMeta.GetTooltips(characterEntry.MissingStatus.Status, 1),
					];
				}
			}
		});
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Finalizer))
		);
	}

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(1),
					ILMatches.Ldloc<CardMeta>(originalMethod),
					ILMatches.Ldfld(nameof(CardMeta.deck)),
					ILMatches.Instruction(OpCodes.Newobj),
					ILMatches.Call(nameof(State.CharacterIsMissing)),
					ILMatches.Brfalse.GetBranchTarget(out var playCardLabel),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_ShouldPlayRegardlessOfOwnerMissing))),
					new CodeInstruction(OpCodes.Brtrue, playCardLabel.Value),
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

	private static bool Combat_TryPlayCard_Transpiler_ShouldPlayRegardlessOfOwnerMissing(State state, Card card)
		=> ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait);

	private static void Card_Render_Prefix(Card __instance, G g, State? fakeState, out int? __state)
	{
		var state = fakeState ?? g.state;
		
		__state = null;
		if (ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(__instance.GetMeta().deck) is not { } characterEntry)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, __instance, Trait))
			return;
		
		__state = state.ship.Get(characterEntry.MissingStatus.Status);
		state.ship.Set(characterEntry.MissingStatus.Status, 0);
	}

	private static void Card_Render_Finalizer(Card __instance, G g, State? fakeState, in int? __state)
	{
		var state = fakeState ?? g.state;
		
		if (__state is null)
			return;
		if (ModEntry.Instance.Helper.Content.Characters.V2.LookupByDeck(__instance.GetMeta().deck) is not { } characterEntry)
			return;

		state.ship.Set(characterEntry.MissingStatus.Status, __state.Value);
	}
}