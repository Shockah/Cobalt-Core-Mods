using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class LockAndLoadCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/LockAndLoad.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LockAndLoad", "name"]).Localize
		});

		ModEntry.Instance.KokoroApi.RegisterCardRenderHook(new Hook(), 0);
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			description = ModEntry.Instance.Localizations.Localize(["card", "LockAndLoad", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAddCard
				{
					destination = CardDestination.Hand,
					card = new DemoChargeCard
					{
						discount = -1,
						temporaryOverride = true,
						exhaustOverride = true,
					},
					amount = 1
				},
				new AAddCard
				{
					destination = CardDestination.Hand,
					card = new FluxChargeCard
					{
						discount = -1,
						temporaryOverride = true,
						exhaustOverride = true,
					},
					amount = 1
				},
				new AAddCard
				{
					destination = CardDestination.Hand,
					card = new BurstChargeCard
					{
						discount = -1,
						temporaryOverride = true,
						exhaustOverride = true,
					},
					amount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.evade,
					statusAmount = 1
				}
			],
			_ => [
				new AAddCard
				{
					destination = CardDestination.Hand,
					card = new CustomChargeCard(),
					amount = 2
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.evade,
					statusAmount = upgrade == Upgrade.A ? 2 : 1
				}
			]
		};

	private sealed class Hook : ICardRenderHook
	{
		public Font? ReplaceTextCardFont(G g, Card card)
		{
			if (card is not LockAndLoadCard)
				return null;
			if (card.upgrade != Upgrade.B)
				return null;
			return ModEntry.Instance.KokoroApi.PinchCompactFont;
		}
	}
}
