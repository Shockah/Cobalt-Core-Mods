using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class StabilizeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Stabilize.png"), StableSpr.cards_peri).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Stabilize", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var data = new CardData
		{
			description = ModEntry.Instance.Localizations.Localize(["card", "Stabilize", "description"]),
		};
		return upgrade switch
		{
			Upgrade.B => data with { cost = 0, exhaust = true },
			Upgrade.A => data with { cost = 1 },
			_ => data with { cost = 2 },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new Action()];

	private sealed class Action : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
		{
			if (s.route is Combat combat && GetBestOption(s, combat) is { } bestOption)
				foreach (var entry in bestOption)
					if (combat.stuff.TryGetValue(entry.WobblyStackPosition, out var @object))
						@object.hilight = 2;
			
			return [
				Stack.MakeWobblyMidrowAttributeTooltip(),
				Stack.MakeStackedMidrowAttributeTooltip(),
			];
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			if (GetBestOption(s, c) is not { } bestOption)
			{
				timer = 0;
				return;
			}

			foreach (var entry in bestOption)
			{
				if (!c.stuff.TryGetValue(entry.WobblyStackPosition, out var @object))
					continue;
				Stack.SetWobbly(@object, false);
			}

			Audio.Play(Event.Status_PowerUp);
		}

		private static List<(int BayPosition, int WobblyStackPosition)>? GetBestOption(State state, Combat combat)
		{
			var bayPositions = Enumerable.Range(0, state.ship.parts.Count)
				.Where(i => state.ship.parts[i].type == PType.missiles && state.ship.parts[i].active)
				.Select(i => i + state.ship.x)
				.ToList();

			if (bayPositions.Count == 0)
				return null;

			var wobblyStackPositions = combat.stuff
				.Where(kvp => Stack.GetStackedObjects(kvp.Value) is { } stackedObjects && stackedObjects.Count != 0)
				.Where(kvp => Stack.IsWobbly(kvp.Value))
				.Select(kvp => kvp.Key)
				.ToList();

			if (wobblyStackPositions.Count == 0)
				return null;

			return GetAllOptions(bayPositions, wobblyStackPositions, [])
				.MinBy(o => o.Sum(e => Math.Abs(e.WobblyStackPosition - e.BayPosition)));
			
			IEnumerable<List<(int BayPosition, int WobblyStackPosition)>> GetAllOptions(List<int> remainingBayPositions, List<int> remainingWobblyStackPositions, List<(int, int)> current)
			{
				if (remainingBayPositions.Count == 0 || remainingWobblyStackPositions.Count == 0)
				{
					yield return current;
					yield break;
				}

				for (var bayPositionIndex = 0; bayPositionIndex < remainingBayPositions.Count; bayPositionIndex++)
				{
					var bayPosition = remainingBayPositions[bayPositionIndex];
					var newRemainingBayPositions = remainingBayPositions.ToList();
					newRemainingBayPositions.RemoveAt(bayPositionIndex);
					
					for (var wobblyStackPositionIndex = 0; wobblyStackPositionIndex < remainingWobblyStackPositions.Count; wobblyStackPositionIndex++)
					{
						var wobblyStackPosition = remainingWobblyStackPositions[wobblyStackPositionIndex];
						var newRemainingWobblyStackPositions = remainingWobblyStackPositions.ToList();
						newRemainingWobblyStackPositions.RemoveAt(wobblyStackPositionIndex);

						var newCurrent = current.ToList();
						newCurrent.Add((bayPosition, wobblyStackPosition));
						
						foreach (var result in GetAllOptions(newRemainingBayPositions, newRemainingWobblyStackPositions, newCurrent))
							yield return result;
					}
				}
			}
		}
	}
}