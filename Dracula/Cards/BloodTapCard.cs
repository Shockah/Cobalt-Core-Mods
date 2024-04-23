using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodTapCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodTap.png")).Sprite,
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
			new ABloodTap { IncludeEnemy = upgrade == Upgrade.B }
		];

	public sealed class ABloodTap : CardAction
	{
		public List<List<CardAction>>? Choices;
		public List<Status>? Statuses;
		public bool IncludeEnemy;

		public override Route? BeginWithRoute(G g, State s, Combat c)
			=> new ActionChoiceRoute
			{
				Title = ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "ui", "title"]),
				Choices = Choices ?? ModEntry.Instance.BloodTapManager.MakeChoices(s, c, includeEnemy: IncludeEnemy).ToList()
			};

		private List<Status> GetStatuses(State s)
		{
			if (Statuses is null && s.route is Combat combat)
				Statuses = ModEntry.Instance.BloodTapManager.GetApplicableStatuses(s, combat, includeEnemy: IncludeEnemy);
			return Statuses ?? [];
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			List<Tooltip> tooltips = [];
			if (s == DB.fakeState)
				return tooltips;

			tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "tooltip", "title"])));
			if (GetStatuses(s).Count == 0)
			{
				tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "tooltip", "none"])));
			}
			else
			{
				foreach (var status in GetStatuses(s))
					tooltips.Add(new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::BloodTap::Status::{status.Key()}")
					{
						Icon = DB.statuses[status].icon,
						TitleColor = Colors.status,
						Title = status.GetLocName()
					});
			}

			return tooltips;
		}
	}
}
