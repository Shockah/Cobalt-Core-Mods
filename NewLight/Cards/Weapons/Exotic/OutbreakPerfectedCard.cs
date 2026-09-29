using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class OutbreakPerfectedCard : ExoticWeaponCard, IRegisterable
{
	protected override WeaponElement WeaponElement => WeaponElement.Kinetic;

	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Weapon.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "OutbreakPerfected", "Name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => base.GetData(state) with { cost = 2, recycle = true },
			_ => base.GetData(state) with { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 1), fast = true },
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat).AsCardAction,
				new AAttack { damage = GetDmg(s, (ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1) * 2), fast = true, xHint = 2 },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 2), fast = true },
				ModEntry.Instance.KokoroApi.TimesPlayed.MakeVariableHintAction(uuid, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat).AsCardAction,
				new AAttack { damage = GetDmg(s, ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(this, IKokoroApi.IV2.ITimesPlayedApi.Interval.Combat) + 1), fast = true, xHint = 1 },
			],
		};
}