using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Dracula;

internal sealed class DominateCard : Card, IDraculaCard
{
	private static ISpriteEntry NonFlipArt = null!;
	private static ISpriteEntry OptionalOnIcon = null!;
	private static ISpriteEntry OptionalOffIcon = null!;

	private static bool IsDuringTryPlayCard = false;

	public Matrix ModifyCardActionRenderMatrix(G g, List<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade == Upgrade.None)
			return Matrix.Identity;

		var spacing = 12 * g.mg.PIX_SCALE;
		var halfYCenterOffset = 16 * g.mg.PIX_SCALE;
		var index = actions.IndexOf(action);
		var recenterY = -(int)((index - actions.Count / 2.0 + 0.5) * spacing);
		return index switch
		{
			0 => Matrix.CreateTranslation(0, recenterY - halfYCenterOffset, 0),
			1 or 2 => Matrix.CreateTranslation(0, recenterY + halfYCenterOffset - spacing / 2 + spacing * (index - 1), 0),
			_ => Matrix.Identity
		};
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		NonFlipArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DominateNonFlip.png"));
		OptionalOnIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/OptionalOn.png"));
		OptionalOffIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/OptionalOff.png"));

		helper.Content.Cards.RegisterCard("Dominate", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Dominate.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dominate", "name"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
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
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = upgrade != Upgrade.None && flipped ? NonFlipArt.Sprite : null,
			cost = 1,
			floppable = upgrade != Upgrade.None
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];

		if (IsDuringTryPlayCard)
		{
			for (var i = 0; i < s.ship.parts.Count; i++)
				if (s.ship.parts[i].type == PType.missiles)
					actions.Add(new APositionalDroneFlip
					{
						WorldX = s.ship.x + i,
						disabled = upgrade != Upgrade.None && flipped
					});
		}
		else
		{
			actions.Add(new APositionalDroneFlip
			{
				WorldX = s.ship.x,
				disabled = upgrade != Upgrade.None && flipped
			});
		}

		if (upgrade == Upgrade.A)
		{
			if (IsDuringTryPlayCard)
			{
				for (var i = 0; i < s.ship.parts.Count; i++)
					if (s.ship.parts[i].type == PType.missiles)
						actions.Add(new APositionalDroneBubble
						{
							WorldX = s.ship.x + i
						});
			}
			else
			{
				actions.Add(new APositionalDroneBubble
				{
					WorldX = s.ship.x
				});
			}
		}
		else if (upgrade == Upgrade.B)
		{
			if (IsDuringTryPlayCard)
			{
				for (var i = 0; i < s.ship.parts.Count; i++)
					if (s.ship.parts[i].type == PType.missiles)
						actions.Add(new APositionalDroneTrigger
						{
							WorldX = s.ship.x + i
						});
			}
			else
			{
				actions.Add(new APositionalDroneTrigger
				{
					WorldX = s.ship.x
				});
			}
		}

		actions.Add(new ADrawCard
		{
			count = 1
		});

		return actions;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;

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
		if (card is not DominateCard)
			return sprite;
		return (card.flipped ? OptionalOffIcon : OptionalOnIcon).Sprite;
	}

	private static bool Card_Render_Transpiler_ReplaceFlipped(bool flipped, Card card)
		=> card is not DominateCard && flipped;

	private static void Card_GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (__instance is not DominateCard)
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

				return new GlossaryTooltip("cardtrait.optional")
				{
					Icon = OptionalOffIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "optional", "name"]),
					Description = ModEntry.Instance.Localizations.Localize([
						"cardTrait",
						"optional",
						"description",
						PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"
					], new { Button = buttonText })
				};
			});
	}
}
