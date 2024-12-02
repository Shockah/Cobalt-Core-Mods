using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodScentCard : Card, IDraculaCard
{
	private static ISpriteEntry TopArt = null!;
	private static ISpriteEntry BottomArt = null!;

	public Matrix ModifyCardActionRenderMatrix(G g, IReadOnlyList<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade != Upgrade.A)
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
		TopArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodScentTop.png"));
		BottomArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodScentBottom.png"));

		helper.Content.Cards.RegisterCard("BloodScent", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodScent.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodScent", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = upgrade switch
			{
				Upgrade.A => (flipped ? BottomArt : TopArt).Sprite,
				_ => null
			},
			cost = 1,
			floppable = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				}.Disabled(flipped),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 3
					),
					new AStatus
					{
						targetPlayer = true,
						status = Status.overdrive,
						statusAmount = 2
					}
				).AsCardAction.Disabled(flipped),
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 2
				}.Disabled(!flipped),
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				},
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 3
					),
					new AStatus
					{
						targetPlayer = true,
						status = Status.powerdrive,
						statusAmount = 1
					}
				).AsCardAction
			],
			_ => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				},
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 3
					),
					new AStatus
					{
						targetPlayer = true,
						status = Status.overdrive,
						statusAmount = 2
					}
				).AsCardAction
			]
		};
}
