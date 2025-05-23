﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Newtonsoft.Json;
using Nickel;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Shockah.Shared;

namespace Shockah.Dyna;

internal sealed class CustomChargeCard : Card, IRegisterable
{
	private static ISpriteEntry TopArt = null!;
	private static ISpriteEntry BottomArt = null!;
	private static List<ISpriteEntry> QuadArt = null!;
	private static List<ISpriteEntry> QuadIcon = null!;

	[JsonProperty]
	public int FlipIndex { get; private set; }

	[JsonProperty]
	private bool LastFlipped;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TopArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/CustomChargeTop.png"));
		BottomArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/CustomChargeBottom.png"));

		QuadArt = Enumerable.Range(0, 4)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/CustomChargeQuad{i}.png")))
			.ToList();
		QuadIcon = Enumerable.Range(0, 4)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Icons/Quad{i}.png")))
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

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, floppable = true, retain = true, exhaust = true, temporary = true, art = FlipIndex % 2 == 0 ? TopArt.Sprite : BottomArt.Sprite },
			Upgrade.B => new() { cost = 1, floppable = true, retain = true, exhaust = true, temporary = true, art = QuadArt[FlipIndex % 4].Sprite },
			_ => new() { cost = 1, floppable = true, retain = true, exhaust = true, temporary = true, art = FlipIndex % 2 == 0 ? TopArt.Sprite : BottomArt.Sprite },
		};

	public override void ExtraRender(G g, Vec v)
	{
		base.ExtraRender(g, v);
		if (LastFlipped != flipped)
		{
			LastFlipped = flipped;
			FlipIndex = (FlipIndex + 1) % (upgrade == Upgrade.B ? 4 : 2);
		}
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new FireChargeAction { Charge = new DemoCharge(), disabled = FlipIndex % 2 != 0 },
				new ADummyAction(),
				new FireChargeAction { Charge = new FluxCharge(), disabled = FlipIndex % 2 != 1 },
			],
			Upgrade.B => [
				new FireChargeAction { Charge = new DemoCharge(), disabled = FlipIndex % 4 != 0 },
				new FireChargeAction { Charge = new FluxCharge(), disabled = FlipIndex % 4 != 1 },
				new FireChargeAction { Charge = new BurstCharge(), disabled = FlipIndex % 4 != 2 },
				new FireChargeAction { Charge = new SwiftCharge(), disabled = FlipIndex % 4 != 3 },
			],
			_ => [
				new FireChargeAction { Charge = new DemoCharge(), disabled = FlipIndex % 2 != 0 },
				new ADummyAction(),
				new FireChargeAction { Charge = new FluxCharge(), disabled = FlipIndex % 2 != 1 },
			]
		};

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
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
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static Spr Card_Render_Transpiler_ReplaceFloppableIcon(Spr sprite, Card card)
	{
		if (card is not CustomChargeCard thisCard)
			return sprite;
		return thisCard.upgrade == Upgrade.B
			? QuadIcon[thisCard.FlipIndex % QuadIcon.Count].Sprite
			: sprite;
	}

	private static bool Card_Render_Transpiler_ReplaceFlipped(bool flipped, Card card)
		=> card is not CustomChargeCard && flipped;

	private static void Card_GetAllTooltips_Postfix(Card __instance, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (__instance is not CustomChargeCard thisCard || thisCard.upgrade != Upgrade.B)
			return;

		__result = __result
			.Select(tooltip =>
			{
				if (tooltip is not TTGlossary glossary || glossary.key != "cardtrait.floppable")
					return tooltip;

				var buttonText = PlatformIcons.GetPlatform() switch
				{
					Platform.NX => Loc.T("controller.nx.b"),
					Platform.PS => Loc.T("controller.ps.circle"),
					_ => Loc.T("controller.xbox.b"),
				};

				return new GlossaryTooltip("cardtrait.quad")
				{
					Icon = QuadIcon[0].Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "quad", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "quad", "description", PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"], new { Button = buttonText })
				};
			});
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Matrix ModifyCardActionRenderMatrix(IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyCardActionRenderMatrixArgs args)
		{
			if (args.Card is not CustomChargeCard { upgrade: Upgrade.B })
				return Matrix.Identity;
			
			var spacing = 12 * args.G.mg.PIX_SCALE;
			var newXOffset = 12 * args.G.mg.PIX_SCALE;
			var newYOffset = 10 * args.G.mg.PIX_SCALE;
			var index = args.Actions.ToList().IndexOf(args.Action);
			return index switch
			{
				0 => Matrix.CreateTranslation(-newXOffset, -newYOffset - (int)((index - args.Actions.Count / 2.0 + 0.5) * spacing), 0),
				1 => Matrix.CreateTranslation(newXOffset, -newYOffset - (int)((index - args.Actions.Count / 2.0 + 0.5) * spacing), 0),
				2 => Matrix.CreateTranslation(newXOffset, newYOffset - (int)((index - args.Actions.Count / 2.0 + 0.5) * spacing), 0),
				3 => Matrix.CreateTranslation(-newXOffset, newYOffset - (int)((index - args.Actions.Count / 2.0 + 0.5) * spacing), 0),
				_ => Matrix.Identity
			};
		}
	}
}
