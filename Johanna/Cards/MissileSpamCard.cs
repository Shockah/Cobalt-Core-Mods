using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Johanna;

internal sealed class MissileSpamCard : JohannaCard, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohannaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/MissileSpam.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MissileSpam", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 3 },
			Upgrade.A => base.GetData(state) with { cost = 2, exhaust = true },
			_ => base.GetData(state) with { cost = 3, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ASpawn { offset = -2, thing = new MissileCluster { Count = 2 } },
			new ASpawn { offset = -1, thing = new MissileCluster { Count = 2 } },
			new ASpawn { offset = 0, thing = new MissileCluster { Count = 2 } },
			new ASpawn { offset = 1, thing = new MissileCluster { Count = 2 } },
			new ASpawn { offset = 2, thing = new MissileCluster { Count = 2 } },
		];
}