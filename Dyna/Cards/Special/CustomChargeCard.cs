using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Shockah.Shared;

namespace Shockah.Dyna;

internal sealed class CustomChargeCard : Card, IRegisterable
{
	private static List<ISpriteEntry> TriadIcon = null!;

	[JsonProperty]
	public int FlipIndex { get; private set; } = 0;

	[JsonProperty]
	private bool LastFlipped;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TriadIcon = Enumerable.Range(0, 3)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Icons/Triad{i}.png")))
			.ToList();

		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(typeof(LockAndLoadCard)),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Art = StableSpr.cards_GoatDrone,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/CustomCharge.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CustomCharge", "name"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(GetAllTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Postfix))
		);

		ModEntry.Instance.KokoroApi.RegisterCardRenderHook(new Hook(), 0);
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			exhaust = upgrade != Upgrade.B,
			floppable = true,
			temporary = true
		};

	public override void ExtraRender(G g, Vec v)
	{
		base.ExtraRender(g, v);
		if (LastFlipped != flipped)
		{
			LastFlipped = flipped;
			FlipIndex = (FlipIndex + 1) % (upgrade == Upgrade.A ? 3 : 2);
		}
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new FireChargeAction
				{
					Charge = new DemoCharge(),
					disabled = FlipIndex % 3 != 0
				},
				new FireChargeAction
				{
					Charge = new FluxCharge(),
					disabled = FlipIndex % 3 != 1
				},
				new FireChargeAction
				{
					Charge = new BurstCharge(),
					disabled = FlipIndex % 3 != 2
				}
			],
			_ => [
				new FireChargeAction
				{
					Charge = new DemoCharge(),
					disabled = FlipIndex % 2 != 0
				},
				new ADummyAction(),
				new FireChargeAction
				{
					Charge = new FluxCharge(),
					disabled = FlipIndex % 2 != 1
				}
			]
		};

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("floppable")
				)
				.Find(ILMatches.LdcI4((int)StableSpr.icons_floppable))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ReplaceFloppableIcon)))
				)
				.Find(ILMatches.Ldfld("flipped"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_ReplaceFlipped)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static Spr Card_Render_Transpiler_ReplaceFloppableIcon(Spr sprite, Card card)
	{
		if (card is not CustomChargeCard thisCard)
			return sprite;
		return thisCard.upgrade == Upgrade.A
			? TriadIcon[thisCard.FlipIndex % TriadIcon.Count].Sprite
			: sprite;
	}

	private static bool Card_Render_Transpiler_ReplaceFlipped(bool flipped, Card card)
		=> card is not CustomChargeCard && flipped;

	private static void Card_GetAllTooltips_Postfix(Card __instance, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (__instance is not CustomChargeCard thisCard || thisCard.upgrade != Upgrade.A)
			return;

		__result = __result
			.Select(tooltip =>
			{
				if (tooltip is not TTGlossary glossary || glossary.key != "cardtrait.floppable")
					return tooltip;

				string buttonText = PlatformIcons.GetPlatform() switch
				{
					Platform.NX => Loc.T("controller.nx.b"),
					Platform.PS => Loc.T("controller.ps.circle"),
					_ => Loc.T("controller.xbox.b"),
				};

				return new GlossaryTooltip("cardtrait.triad")
				{
					Icon = TriadIcon[0].Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "triad", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "triad", "description", PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"], new { Button = buttonText })
				};
			});
	}

	private sealed class Hook : ICardRenderHook
	{
		public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
		{
			if (card is not CustomChargeCard)
				return Matrix.Identity;
			if (card.upgrade != Upgrade.A)
				return Matrix.Identity;
			return Matrix.CreateScale(1.5f);
		}

		public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
		{
			if (card is not CustomChargeCard)
				return Matrix.Identity;
			if (card.upgrade != Upgrade.A)
				return Matrix.Identity;
			return Matrix.CreateScale(1f / 1.5f);
		}
	}
}
