using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class RelativityCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Relativity.png"), StableSpr.cards_Dodge).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Relativity", "name"]).Localize,
		});

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 2 },
			a: () => new() { cost = 2 },
			b: () => new() { cost = 2, description = ModEntry.Instance.Localizations.Localize(["card", "Relativity", "description"]) }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AStatus { targetPlayer = true, status = Relativity.RelativityStatus.Status, statusAmount = 2 },
			],
			a: () => [
				new AStatus { targetPlayer = true, status = Relativity.RelativityStatus.Status, statusAmount = 3 },
			],
			b: () => [
				new MoveEverythingRandomAction(),
				new AStatus { targetPlayer = true, status = Relativity.RelativityStatus.Status, statusAmount = 2 },
			]
		);

	private sealed class MoveEverythingRandomAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var amounts = Enumerable.Range(0, 3).Shuffle(s.rngActions).ToList();
			c.QueueImmediate([
				new AMove { targetPlayer = true, isRandom = true, dir = amounts[0] },
				new ADroneMove { isRandom = true, dir = amounts[1] },
				new AMove { targetPlayer = false, isRandom = true, dir = amounts[2] },
			]);
		}
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not RelativityCard)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}