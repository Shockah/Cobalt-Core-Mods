using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class TensionCardTrait : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	private static ISpriteEntry BaseIcon = null!;
	private static readonly Dictionary<int, Spr> Icons = [];
	private static readonly Dictionary<string, Dictionary<Upgrade, int>> TensionPerUpgrade = [];
	private static bool IsDuringEndTurnDiscard;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BaseIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardTraits/Tension.png"));
		
		Trait = helper.Content.Cards.RegisterTrait("Tension", new()
		{
			Icon = (state, card) => ObtainIcon(card is null ? 10 : GetTension(state, card)),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "Tension", "Name"]).Localize,
			Tooltips = (state, card) =>
			{
				string description;
				if (card is null)
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "Tension", "Description", "WithoutCard"]);
				else
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "Tension", "Description", "WithCard"], new { Amount = GetTension(state, card) });

				return [
					new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Tension")
					{
						Icon = ObtainIcon(card is null ? 10 : GetTension(DB.fakeState, card)),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "Tension", "Name"]),
						Description = description,
					}
				];
			}
		});

		helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, args) =>
		{
			if (GetTension(args.State, args.Card) > 0)
				args.SetOverride(Trait, true);
		};
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADiscard), nameof(ADiscard.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADiscard_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADiscard_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DiscardHand)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DiscardHand_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActualDamage)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler))
		);
	}
	
	public static void SetTension(string key, int value)
	{
		SetTension(key, Upgrade.None, value);
		SetTension(key, Upgrade.A, value);
		SetTension(key, Upgrade.B, value);
	}
	
	public static void SetTension(string key, Upgrade upgrade, int value)
	{
		ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(TensionPerUpgrade, key, out var perUpgradeExists);
		if (!perUpgradeExists)
			perUpgrade = [];
		perUpgrade![upgrade] = value;
	}

	public static int GetTension(State state, Card card)
		=> TensionPerUpgrade.TryGetValue(card.Key(), out var perUpgrade) ? perUpgrade.GetValueOrDefault(card.upgrade) : 0;

	private static Spr ObtainIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (Icons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Tension{amount}", () =>
		{
			var baseIcon = SpriteLoader.Get(BaseIcon.Sprite)!;
			return TextureUtils.CreateTexture(new(baseIcon.Width, baseIcon.Height)
			{
				Actions = _ =>
				{
					Draw.Sprite(baseIcon, 0, 0);

					var text = amount > 9 ? "+" : amount.ToString();
					var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
					Draw.Text(text, baseIcon.Width - textRect.w, baseIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				},
			});
		}).Sprite;

		Icons[amount] = icon;
		return icon;
	}

	private static void AEndTurn_Begin_Prefix(Combat c, out List<CardAction> __state)
		=> __state = c.cardActions.ToList();

	private static void AEndTurn_Begin_Postfix(Combat c, in List<CardAction> __state)
	{
		foreach (var action in c.cardActions)
		{
			if (action is not ADiscard { ignoreRetain: true } discardAction)
				continue;
			if (__state.Contains(action))
				continue;

			ModEntry.Instance.Helper.ModData.SetModData(discardAction, "IsTurnEndDiscard", true);
			break;
		}
	}

	private static void ADiscard_Begin_Prefix(ADiscard __instance)
		=> IsDuringEndTurnDiscard = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IsTurnEndDiscard");

	private static void ADiscard_Begin_Finalizer()
		=> IsDuringEndTurnDiscard = false;

	private static void Combat_DiscardHand_Postfix(Combat __instance, State s)
	{
		if (!IsDuringEndTurnDiscard)
			return;

		var tensionedThisTurn = __instance.TensionedThisTurn;
		tensionedThisTurn.Clear();

		foreach (var card in __instance.hand)
			if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
				tensionedThisTurn.Add(card.uuid);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_GetActualDamage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<int>(originalMethod),
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4(Status.powerdrive),
					ILMatches.Call(nameof(Ship.Get)),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod).GetLocalIndex(out var actualDamageLocalIndex),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloca, actualDamageLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler_ModifyActualDamage))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Card_GetActualDamage_Transpiler_ModifyActualDamage(State s, Card? card, ref int actualDamage)
	{
		if (card is null)
			return;
		if (s.route is not Combat combat)
			return;

		var tension = GetTension(s, card);
		if (tension <= 0)
			return;
		if (!combat.TensionedThisTurn.Contains(card.uuid))
			return;
		
		actualDamage += tension;
	}
}

file static class FullAutoCardTraitExt
{
	extension(Combat combat)
	{
		public HashSet<int> TensionedThisTurn
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "TensionedThisTurn");
	}
}