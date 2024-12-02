using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Dracula;

internal sealed class BatFormCard : Card, IDraculaCard
{
	private static List<ISpriteEntry> TriadArt = null!;
	private static List<ISpriteEntry> QuadArt = null!;
	private static List<ISpriteEntry> TriadIcon = null!;
	private static List<ISpriteEntry> QuadIcon = null!;

	[JsonProperty]
	public int FlipIndex { get; private set; }

	[JsonProperty]
	private bool LastFlipped { get; set; }

	public float ActionSpacingScaling
		=> 1.5f;

	public Matrix ModifyNonTextCardRenderMatrix(G g, IReadOnlyList<CardAction> actions)
	{
		if (upgrade == Upgrade.B)
			return Matrix.CreateScale(1.5f);
		return Matrix.Identity;
	}

	public Matrix ModifyCardActionRenderMatrix(G g, IReadOnlyList<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade == Upgrade.B)
			return Matrix.CreateScale(1f / 1.5f);

		var spacing = 12 * g.mg.PIX_SCALE;
		var newXOffset = 12 * g.mg.PIX_SCALE;
		var newYOffset = 10 * g.mg.PIX_SCALE;
		var index = actions.ToList().IndexOf(action);
		return index switch
		{
			0 => Matrix.CreateTranslation(-newXOffset, -newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			1 => Matrix.CreateTranslation(newXOffset, -newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			2 => Matrix.CreateTranslation(newXOffset, newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			3 => Matrix.CreateTranslation(-newXOffset, newYOffset - (int)((index - actions.Count / 2.0 + 0.5) * spacing), 0),
			_ => Matrix.Identity
		};
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TriadArt = Enumerable.Range(0, 3)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/BatFormTriad{i}.png")))
			.ToList();
		QuadArt = Enumerable.Range(0, 4)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Cards/BatFormQuad{i}.png")))
			.ToList();
		TriadIcon = Enumerable.Range(0, 3)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Icons/Triad{i}.png")))
			.ToList();
		QuadIcon = Enumerable.Range(0, 4)
			.Select(i => helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"assets/Icons/Quad{i}.png")))
			.ToList();

		helper.Content.Cards.RegisterCard("BatForm", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BatForm.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BatForm", "name"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetAllTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Postfix))
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = upgrade == Upgrade.B
				? TriadArt[FlipIndex % 3].Sprite
				: QuadArt[FlipIndex % 4].Sprite,
			cost = upgrade == Upgrade.None ? 1 : 0,
			floppable = true
		};

	public override void ExtraRender(G g, Vec v)
	{
		base.ExtraRender(g, v);
		if (LastFlipped != flipped)
		{
			LastFlipped = flipped;
			FlipIndex = (FlipIndex + 1) % (upgrade == Upgrade.B ? 3 : 4);
		}
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove
				{
					targetPlayer = true,
					dir = 1,
					isRandom = true,
					disabled = FlipIndex % 3 != 0,
				},
				new AMove
				{
					targetPlayer = true,
					dir = 2,
					isRandom = true,
					disabled = FlipIndex % 3 != 1,
				},
				new AMove				{
					targetPlayer = true,
					dir = 3,
					isRandom = true,
					disabled = FlipIndex % 3 != 2,
				}
			],
			_ => [
				new AMove
				{
					targetPlayer = true,
					dir = -1,
					ignoreFlipped = true,
					disabled = FlipIndex % 4 != 0,
				},
				new AMove
				{
					targetPlayer = true,
					dir = 1,
					ignoreFlipped = true,
					disabled = FlipIndex % 4 != 1,
				},
				new AMove
				{
					targetPlayer = true,
					dir = 2,
					ignoreFlipped = true,
					disabled = FlipIndex % 4 != 2,
				},
				new AMove
				{
					targetPlayer = true,
					dir = -2,
					ignoreFlipped = true,
					disabled = FlipIndex % 4 != 3,
				}
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
		if (card is not BatFormCard batFormCard)
			return sprite;
		return batFormCard.upgrade == Upgrade.B
			? TriadIcon[batFormCard.FlipIndex % TriadIcon.Count].Sprite
			: QuadIcon[batFormCard.FlipIndex % QuadIcon.Count].Sprite;
	}

	private static bool Card_Render_Transpiler_ReplaceFlipped(bool flipped, Card card)
		=> card is not BatFormCard && flipped;

	private static void Card_GetAllTooltips_Postfix(Card __instance, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (__instance is not BatFormCard batFormCard)
			return;

		__result = __result
			.Select(tooltip =>
			{
				if (tooltip is not TTGlossary { key: "cardtrait.floppable" })
					return tooltip;

				var buttonText = PlatformIcons.GetPlatform() switch
				{
					Platform.NX => Loc.T("controller.nx.b"),
					Platform.PS => Loc.T("controller.ps.circle"),
					_ => Loc.T("controller.xbox.b"),
				};

				return new GlossaryTooltip("cardtrait.triad")
				{
					Icon = batFormCard.upgrade == Upgrade.B
						? TriadIcon[0].Sprite
						: QuadIcon[0].Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize([
						"cardTrait",
						batFormCard.upgrade == Upgrade.B ? "triad" : "quad",
						"name"
					]),
					Description = ModEntry.Instance.Localizations.Localize([
						"cardTrait",
						batFormCard.upgrade == Upgrade.B ? "triad" : "quad",
						"description",
						PlatformIcons.GetPlatform() == Platform.MouseKeyboard ? "m&k" : "controller"
					], new { Button = buttonText })
				};
			});
	}
}
