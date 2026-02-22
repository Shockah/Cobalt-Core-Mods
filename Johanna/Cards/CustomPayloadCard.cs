using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Johanna;

internal sealed class CustomPayloadCard : JohannaCard, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/CustomPayload.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CustomPayload", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => base.GetData(state) with { cost = 0, floppable = true },
			_ => base.GetData(state) with { cost = 1, floppable = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new MissileCluster { Count = 1, IsHeavy = true }, disabled = flipped },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new ASpawn { thing = new MissileCluster { Count = 1, IsSeeker = true }, disabled = !flipped },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1, disabled = !flipped },
			],
			_ => [
				new ASpawn { thing = new MissileCluster { Count = 1, IsHeavy = true }, disabled = flipped },
				new ADummyAction(),
				new ASpawn { thing = new MissileCluster { Count = 1, IsSeeker = true }, disabled = !flipped },
			],
		};
}