﻿using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DrainEssenceCard : Card, IDraculaCard
{
	private static ISpriteEntry TopArt = null!;
	private static ISpriteEntry BottomArt = null!;

	public Matrix ModifyCardActionRenderMatrix(G g, IReadOnlyList<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade == Upgrade.B)
			return Matrix.Identity;

		var spacing = 12 * g.mg.PIX_SCALE;
		var halfYCenterOffset = 16 * g.mg.PIX_SCALE;
		var index = actions.ToList().IndexOf(action);
		var recenterY = -(int)((index - actions.Count / 2.0 + 0.5) * spacing);
		return index switch
		{
			0 or 1 => Matrix.CreateTranslation(0, recenterY - halfYCenterOffset - spacing / 2 + spacing * index, 0),
			2 => Matrix.CreateTranslation(0, recenterY + halfYCenterOffset, 0),
			_ => Matrix.Identity
		};
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TopArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DrainEssenceTop.png"));
		BottomArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DrainEssenceBottom.png"));

		helper.Content.Cards.RegisterCard("DrainEssence", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DrainEssence.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DrainEssence", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = upgrade switch
			{
				Upgrade.B => null,
				_ => (flipped ? BottomArt : TopArt).Sprite
			},
			cost = 1,
			floppable = upgrade != Upgrade.B,
			recycle = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				}.Disabled(flipped),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 2
					),
					new AHeal
					{
						targetPlayer = true,
						healAmount = 1
					}
				).AsCardAction.Disabled(!flipped),
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1
				},
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				).AsCardAction,
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AHeal
					{
						targetPlayer = true,
						healAmount = 1
					}
				).AsCardAction,
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1
				}.Disabled(flipped),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AHeal
					{
						targetPlayer = true,
						healAmount = 1
					}
				).AsCardAction.Disabled(!flipped),
			]
		};
}
