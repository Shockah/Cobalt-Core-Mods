using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodTapCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BloodTap", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodTap", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			infinite = upgrade == Upgrade.A,
			recycle = upgrade == Upgrade.B,
			retain = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "description", upgrade.ToString()], new { Damage = GetDmg(state, 1) })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ABloodTap
			{
				Statuses = ModEntry.Instance.BloodTapManager.GetStatuses(c, includeEnemy: upgrade == Upgrade.B),
				Choices = ModEntry.Instance.BloodTapManager.MakeChoices(s, c, includeEnemy: upgrade == Upgrade.B)
			}
		];

	public sealed class ABloodTap : CardAction
	{
		public List<List<CardAction>>? Choices;
		public List<Status>? Statuses;

		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new ActionChoiceRoute
			{
				Title = ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "ui", "title"]),
				Choices = Choices ?? ModEntry.Instance.BloodTapManager.MakeChoices(s, c, includeEnemy: false)
			};

		public override List<Tooltip> GetTooltips(State s)
		{
			Statuses ??= [];
			List<Tooltip> tooltips = [];
			if (s == DB.fakeState)
				return tooltips;

			tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "tooltip", "title"])));
			if (Statuses.Count == 0)
			{
				tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "tooltip", "none"])));
			}
			else
			{
				foreach (var status in Statuses)
					tooltips.Add(new CustomTTGlossary(
						CustomTTGlossary.GlossaryType.status,
						() => DB.statuses[status].icon,
						() => status.GetLocName(),
						() => ""
					));
			}

			return tooltips;
		}
	}
}
