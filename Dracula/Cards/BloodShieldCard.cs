using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodShieldCard : Card, IDraculaCard
{
	private static ISpriteEntry TopArt = null!;
	private static ISpriteEntry BottomArt = null!;

	public Matrix ModifyCardActionRenderMatrix(G g, List<CardAction> actions, CardAction action, int actionWidth)
	{
		var spacing = 12 * g.mg.PIX_SCALE;
		var halfYCenterOffset = 16 * g.mg.PIX_SCALE;
		var index = actions.IndexOf(action);
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
		TopArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodShieldTop.png"));
		BottomArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodShieldBottom.png"));

		helper.Content.Cards.RegisterCard("BloodShield", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodShield", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = (flipped ? BottomArt : TopArt).Sprite,
			cost = 1,
			floppable = true,
			infinite = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AHurt
			{
				targetPlayer = true,
				hurtAmount = 1,
				disabled = flipped
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.shield,
				statusAmount = upgrade == Upgrade.B ? 4 : 3,
				disabled = flipped
			},
			ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
				ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shield),
					amount: upgrade == Upgrade.B ? 3 : 2
				),
				new AHeal
				{
					targetPlayer = true,
					healAmount = upgrade == Upgrade.B ? 2 : 1
				}
			).AsCardAction.Disabled(!flipped)
		];
}
