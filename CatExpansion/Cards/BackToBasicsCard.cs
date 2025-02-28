using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class BackToBasicsCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/BackToBasics.png"), StableSpr.cards_peri).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BackToBasics", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, description = ModEntry.Instance.Localizations.Localize(["card", "BackToBasics", "description", upgrade.ToString()], new { Damage = GetDmg(state, 2) }) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 2) },
				new AAddCard { destination = CardDestination.Hand, card = new CannonColorless { temporaryOverride = true, exhaustOverride = true, exhaustOverrideIsPermanent = true } },
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2) },
				new AAddCard { destination = CardDestination.Hand, card = new CannonColorless { upgrade = Upgrade.B, temporaryOverride = true } },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 2) },
				new AAddCard { destination = CardDestination.Hand, card = new CannonColorless { temporaryOverride = true } },
			],
		};
}