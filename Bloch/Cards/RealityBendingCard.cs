using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class RealityBendingCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/RealityBending.png"), StableSpr.cards_Corrode).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RealityBending", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.Api.MakeChooseAura(
				card: this,
				amount: upgrade == Upgrade.B ? 2 : 1
			),
			new AStatus
			{
				targetPlayer = true,
				status = AuraManager.IntensifyStatus.Status,
				statusAmount = 1
			},
			new ScryAction
			{
				Amount = upgrade == Upgrade.B ? 3 : 2
			}
		];
}
